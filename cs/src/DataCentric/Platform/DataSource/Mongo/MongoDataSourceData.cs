/*
Copyright (C) 2013-present The DataCentric Authors.

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

   http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace DataCentric
{
    /// <summary>
    /// Abstract base class for data source implementations based on MongoDB.
    ///
    /// This class provides functionality shared by all MongoDB data source types.
    /// </summary>
    public abstract class MongoDataSourceData : DataSourceData
    {
        //--- FIELDS

        /// <summary>Prohibited characters in database name.</summary>
        static readonly char[] prohibitedDbNameSymbols_ = new char[] { '/', '\\', '.', ' ', '"', '$', '*', '<', '>', ':', '|', '?' };

        /// <summary>Maximum length of the database on Mongo server including delimiters.</summary>
        static int maxDbNameLength_ = 64;

        /// <summary>
        /// Type of instance controls the ability to do certain
        /// actions such as deleting (dropping) the database.
        /// </summary>
        private InstanceType instanceType_;

        /// <summary>Full name of the database on Mongo server including delimiters.</summary>
        private string dbName_;

        /// <summary>Interface to Mongo client in MongoDB C# driver.</summary>
        private IMongoClient client_;

        /// <summary>Dictionary of collections indexed by type T.</summary>
        private Dictionary<Type, object> collectionDict_ = new Dictionary<Type, object>();

        /// <summary>Previous ObjectId returned by CreateOrderedObjectId() method.</summary>
        private ObjectId prevObjectId_ = ObjectId.Empty;

        //--- PROPERTIES

        /// <summary>Interface to Mongo database in MongoDB C# driver.</summary>
        public IMongoDatabase Db { get; private set; }

        /// <summary>
        /// Use static constructor to set discriminator convention.
        ///
        /// This call is in static constructor because MongoDB driver
        /// complains if it is called more than once.
        /// </summary>
        static MongoDataSourceData()
        {
            // Set discriminator convention to scalar. For this convention,
            // BSON element _t is a single string value equal to GetType().Name,
            // rather than the list of names for the entire inheritance chain.
            BsonSerializer.RegisterDiscriminatorConvention(typeof(Data), new ScalarDiscriminatorConvention("_t"));
        }

        //--- METHODS

        /// <summary>
        /// Set context and perform initialization or validation of object data.
        ///
        /// All derived classes overriding this method must call base.Init(context)
        /// before executing the the rest of the code in the method override.
        /// </summary>
        public override void Init(IContext context)
        {
            // Initialize the base class
            base.Init(context);

            // Configures serialization conventions for standard types
            var pack = new ConventionPack();
            pack.Add(new IgnoreIfNullConvention(true));
            pack.Add(new EnumRepresentationConvention(BsonType.String));
            ConventionRegistry.Register("Default", pack, t => true);

            // Configure custom BSON serializers
            BsonSerializer.RegisterSerializationProvider(new BsonSerializationProvider());

            // Perform key validation
            if (DbName == null) throw new Exception("DB key is null or empty.");
            if (DbName.InstanceType == InstanceType.Empty) throw new Exception("DB instance type is not specified.");
            if (String.IsNullOrEmpty(DbName.InstanceName)) throw new Exception("DB instance name is not specified.");
            if (String.IsNullOrEmpty(DbName.EnvName)) throw new Exception("DB environment name is not specified.");

            // The name is the database key in the standard semicolon delimited format.
            dbName_ = DbName.ToString();
            instanceType_ = DbName.InstanceType;

            // Perform additional validation for restricted characters and database name length.
            if (dbName_.IndexOfAny(prohibitedDbNameSymbols_) != -1)
                throw new Exception(
                    $"MongoDB database name {dbName_} contains a space or another " +
                    $"prohibited character from the following list: /\\.\"$*<>:|?");
            if (dbName_.Length > maxDbNameLength_)
                throw new Exception(
                    $"MongoDB database name {dbName_} exceeds the maximum length of 64 characters.");

            // Load data store object by key; if key is not specified, create with default settings
            MongoDataStoreData dataStoreData = null;
            if (DataStore != null)
            {
                // Load if data store key is set; the key may point to a local or remote server or cluster
                dataStoreData = DataStore.Load(Context, ObjectId.Empty).CastTo<MongoDataStoreData>();
            }
            else
            {
                // Otherwise default to local Mongo server on default port
                dataStoreData = new LocalMongoDataStoreData();
            }

            // Get client interface using the server instance loaded from root dataset
            string dbUri = dataStoreData.GetMongoServerUri();
            client_ = new MongoClient(dbUri);

            // Get database interface using the client and database name
            Db = client_.GetDatabase(dbName_);
        }

        /// <summary>
        /// The returned ObjectIds have the following order guarantees:
        ///
        /// * For this data source instance, to arbitrary resolution; and
        /// * Across all processes and machines, to one second resolution
        ///
        /// One second resolution means that two ObjectIds created within
        /// the same second by different instances of the data source
        /// class may not be ordered chronologically unless they are at
        /// least one second apart.
        /// </summary>
        public override ObjectId CreateOrderedObjectId()
        {
            CheckNotReadOnly();

            // Generate ObjectId and check that it is later
            // than the previous generated ObjectId
            ObjectId result = ObjectId.GenerateNewId();
            int retryCounter = 0;
            while (result <= prevObjectId_)
            {
                // Getting inside the while loop will be very rare as this would
                // require the increment to roll from max int to min int within
                // the same second, therefore it is a good idea to log the event
                if (retryCounter++ == 0) Context.Log.Warning("MongoDB generated ObjectId not in increasing order, retrying.");

                // If new ObjectId is not strictly greater than the previous one,
                // keep generating new ObjectIds until it changes
                result = ObjectId.GenerateNewId();
            }

            // Report the number of retries
            if (retryCounter != 0)
            {
                Context.Log.Warning($"Generated ObjectId in increasing order after {retryCounter} retries.");
            }

            // Update previous ObjectId and return
            prevObjectId_ = result;
            return result;
        }

        /// <summary>
        /// Apply the final constraints after all prior Where clauses but before OrderBy clause:
        ///
        /// * The constraint on dataset lookup list, restricted by SavedBy (if not null)
        /// * The constraint on ID being strictly less than SavedBy (if not null)
        /// </summary>
        public IQueryable<TRecord> ApplyFinalConstraints<TRecord>(IQueryable<TRecord> queryable, ObjectId loadFrom)
            where TRecord : RecordBase
        {
            // Get lookup list by expanding the list of imports to arbitrary
            // depth with duplicates and cyclic references removed.
            //
            // The list will not include datasets that are after the value of
            // SavedByTime/SavedById if specified, or their imports (including
            // even those imports that are earlier than the constraint).
            IEnumerable<ObjectId> dataSetLookupList = GetDataSetLookupList(loadFrom);

            // Apply constraint that the value is _dataset is
            // one of the elements of dataSetLookupList_
            var result = queryable.Where(p => dataSetLookupList.Contains(p.DataSet));

            // Apply revision time constraint. By making this constraint the
            // last among the constraints, we optimize the use of the index.
            //
            // The property savedBy_ is set using either SavedByTime or SavedById element.
            // Only one of these two elements can be set at a given time.
            var savedBy = GetSavedBy();
            if (savedBy != null)
            {
                result = result.Where(p => p.Id <= savedBy.Value);
            }

            return result;
        }

        /// <summary>
        /// Permanently deletes (drops) the database with all records
        /// in it without the possibility to recover them later.
        ///
        /// This method should only be used to free storage. For
        /// all other purposes, methods that preserve history should
        /// be used.
        ///
        /// ATTENTION - THIS METHOD WILL DELETE ALL DATA WITHOUT
        /// THE POSSIBILITY OF RECOVERY. USE WITH CAUTION.
        /// </summary>
        public override void DeleteDb()
        {
            CheckNotReadOnly();

            // Do not delete (drop) the database this class did not create
            if (client_ != null && Db != null)
            {
                // As an extra safety measure, this method will delete
                // the database only if the first token of its name is
                // TEST or DEV.
                //
                // Use other tokens such as UAT or PROD to protect the
                // database from accidental deletion
                if (instanceType_ == InstanceType.DEV
                    || instanceType_ == InstanceType.USER
                    || instanceType_ == InstanceType.TEST)
                {
                    // The name is the database key in the standard
                    // semicolon delimited format. However this method
                    // performs additional validation for restricted
                    // characters and database name length.
                    client_.DropDatabase(dbName_);
                }
                else
                {
                    throw new Exception(
                        $"As an extra safety measure, database {dbName_} cannot be " +
                        $"dropped because this operation is not permitted for database " +
                        $"instance type {instanceType_}.");
                }
            }
        }

        //--- PROTECTED

        /// <summary>
        /// Returned object holds two collection references - one for the base
        /// type of all records and the other for the record type specified
        /// as generic parameter.
        ///
        /// The need to hold two collection arises from the requirement
        /// that query for a derived type takes into account that another
        /// record with the same key and later dataset or object timestamp
        /// may exist. For this reason, the typed collection is used for
        /// LINQ constraints and base collection is used to iterate over
        /// objects.
        ///
        /// This method also creates indices if they do not exist. The
        /// two default indices are always created:  one for optimizing
        /// loading by key and the other by query.
        ///
        /// Additional indices may be created using property attribute
        ///
        /// [Indexed]
        ///
        /// for further performance optimization.
        /// </summary>
        protected TemporalMongoCollection<TRecord> GetOrCreateCollection<TRecord>()
            where TRecord : RecordBase
        {
            // Check if collection object has already been cached
            // for this type and return cached result if found
            if (collectionDict_.TryGetValue(typeof(TRecord), out object collectionObj))
            {
                var cachedResult = collectionObj.CastTo<TemporalMongoCollection<TRecord>>();
                return cachedResult;
            }

            // Check that scalar discriminator convention is set for TRecord
            var discriminatorConvention = BsonSerializer.LookupDiscriminatorConvention(typeof(TRecord));
            if (!discriminatorConvention.Is<ScalarDiscriminatorConvention>())
                throw new Exception(
                    $"Scalar discriminator convention is not set for type {typeof(TRecord).Name}. " +
                    $"The convention should have been set set in the static constructor of " +
                    $"MongoDataSourceData");

            // Collection name is root class name of the record without prefix
            Type rootType = DataInfo.GetOrCreate(typeof(TRecord)).RootType;
            string collectionName = ClassInfo.GetOrCreate(rootType).MappedClassName;

            // Get interfaces to base and typed collections for the same name
            var baseCollection = Db.GetCollection<RecordBase>(collectionName);
            var typedCollection = Db.GetCollection<TRecord>(collectionName);

            // Each data type has an index for optimized loading by key.
            // This index consists of Key in ascending sort order,
            // followed by DataSet and ID in descending sort order.
            if (true)
            {
                // This code is wrapper into the if (...) block to make it easier
                // turn it on or off to test the impact of indexing on performance

                var indexKeys = Builders<TRecord>.IndexKeys
                    .Ascending(new StringFieldDefinition<TRecord>("_key")) // .Key
                    .Descending(new StringFieldDefinition<TRecord>("_dataset")) // .DataSet
                    .Descending(new StringFieldDefinition<TRecord>("_id")); // .Id

                var indexName = typeof(TRecord).Name + ".Key";
                var indexModel = new CreateIndexModel<TRecord>(indexKeys, new CreateIndexOptions {Name = indexName });
                typedCollection.Indexes.CreateOne(indexModel);
            }

            // Additional indices are provided for optimized loading by query. They
            // are constructed from elements that specify the [Indexed] attribute.
            //
            // When attribute Name is not specified, all elements marked by [Indexed]
            // are included in the same index in the order of declaration within
            // the class, from base to parent.
            //
            // The elements are indexed in ascending (alphabetical or increasing)
            // sort order, followed by DataSet and ID in descending (latest first)
            // sort order.
            if (true)
            {
                // This code is wrapper into the if (...) block to make it easier
                // turn it on or off to test the impact of indexing on performance

                // Dictionary(IndexName, SortedDictionary(ElementOrder, ElementName))
                Dictionary<string, SortedDictionary<int, string>> indexDict =
                    new Dictionary<string, SortedDictionary<int, string>>();

                // Holds index names with default and with user defined order
                // for the purposes of checking that order is specified for
                // all elements of a given index if it is specified for at
                // least one element
                HashSet<string> indicesWithDefaultOrder = new HashSet<string>();
                HashSet<string> indicesWithUserDefinedOrder = new HashSet<string>();

                // Iterate over the data elements to populate the index dictionary
                var dataElements = DataInfo.GetOrCreate(typeof(TRecord)).DataElements;
                int defaultElementOrder = -1;
                foreach (var dataElement in dataElements)
                {
                    string elementName = dataElement.Name;
                    defaultElementOrder++;

                    // Holds index names specified by [Indexed] attributes
                    // for this element for the purposes of checking that
                    // they are unique
                    HashSet<string> indexNames = new HashSet<string>();

                    // There can be more than one [Indexed] attribute for an element
                    var attributes = dataElement.GetCustomAttributes<IndexedAttribute>();
                    foreach (var attribute in attributes)
                    {
                        string indexName = attribute.Index;
                        int elementOrder = attribute.Order;

                        if (indexName == "Key") throw new Exception(
                            $"Index name 'Key' is reserved for the index used for lookup by key. " +
                            $"It cannot be the value of Index parameter of the [Indexed] attribute.");
                        if (indexName == "Default") throw new Exception(
                            $"Index name 'Default' is reserved for the index for which no name is specified. " +
                            $"It cannot be the value of Index parameter of the [Indexed] attribute.");

                        if (elementOrder == IntUtils.Empty)
                        {
                            // Default order, add to one hashset and check that it is not part of the other
                            indicesWithDefaultOrder.Add(indexName);
                            if (indicesWithUserDefinedOrder.Contains(indexName))
                                throw new Exception(
                                    $"Index {indexName} combines elements with default and user defined index order.");

                            // Set order to be the element index
                            elementOrder = defaultElementOrder;
                        }
                        else
                        {
                            // User defined order, add to one hashset and check that it is not part of the other
                            indicesWithUserDefinedOrder.Add(indexName);
                            if (indicesWithDefaultOrder.Contains(indexName))
                                throw new Exception(
                                    $"Index {indexName} combines elements with default and user defined index order.");
                        }

                        // Check that index name is not repeated for a
                        // single element in multiple [Indexed] attributes
                            if (!indexNames.Add(indexName))
                            throw new Exception(
                                $"Index name {indexName} is encountered more than once for " +
                                $"element {dataElement.Name} in type {dataElement.DeclaringType.Name}");

                        // Initialize dictionary for the index if not yet initialized
                        if (!indexDict.TryGetValue(indexName, out SortedDictionary<int, string> sortedDictByOrder))
                        {
                            sortedDictByOrder = new SortedDictionary<int, string>();
                            indexDict.Add(indexName, sortedDictByOrder);
                        }

                        // Check that the dictionary does not yet have an entry for this order
                        if (sortedDictByOrder.ContainsKey(elementOrder))
                            throw new Exception(
                                $"Index {indexName} has two elements with the same " +
                                $"user defined index order {elementOrder}.");

                        sortedDictByOrder.Add(elementOrder, elementName);
                    }
                }

                // Define each index
                foreach(var indexInfo in indexDict)
                {
                    var indexName = indexInfo.Key;
                    var indexElements = indexInfo.Value;

                    var indexKeysBuilder = Builders<TRecord>.IndexKeys;
                    IndexKeysDefinition<TRecord> indexKeys = null;

                    // Index elements in default or user specified order
                    var indexElementNames = indexElements.Values;

                    foreach (var indexElementName in indexElementNames)
                    {
                        if (indexKeys == null)
                        {
                            // Create from builder for the first element
                            indexKeys = indexKeysBuilder
                                .Ascending(new StringFieldDefinition<TRecord>(indexElementName));
                        }
                        else
                        {
                            // Chain to the previous list of index keys for the remaining elements
                            indexKeys = indexKeys
                                .Ascending(new StringFieldDefinition<TRecord>(indexElementName));
                        }
                    }

                    // Add Key in ascending order, then DataSet and ID in descending order
                    indexKeys = indexKeys
                        .Ascending(new StringFieldDefinition<TRecord>("_key")) // Key
                        .Descending(new StringFieldDefinition<TRecord>("_dataset")) // DataSet
                        .Descending(new StringFieldDefinition<TRecord>("_id")); // ID

                    // By convention, use the name 'Default' for the index whose name is not specified
                    if (indexName == null) throw new Exception("Index name cannot be null.");
                    if (indexName == String.Empty) indexName = "Default";

                    // Combine the name with type because sometimes index is generated for both base and derived
                    indexName = String.Join(".", typeof(TRecord).Name, indexName);

                    var indexModel = new CreateIndexModel<TRecord>(indexKeys, new CreateIndexOptions {Name = indexName});
                    typedCollection.Indexes.CreateOne(indexModel);
                }
            }

            // Create result that holds both base and typed collections
            TemporalMongoCollection<TRecord> result = new TemporalMongoCollection<TRecord>(this, baseCollection, typedCollection);

            // Add the result to the collection dictionary and return
            collectionDict_.Add(typeof(TRecord), result);
            return result;
        }
    }
}
