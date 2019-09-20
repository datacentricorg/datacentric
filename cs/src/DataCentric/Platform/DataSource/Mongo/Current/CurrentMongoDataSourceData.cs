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
    /// Current data source with datasets based on MongoDB.
    ///
    /// The term Current applied to a data source means that the data source
    /// stores only the current snapshot of the data, but not its history (i.e.,
    /// a non-temporal data source). This type is a current data source;
    /// a temporal data source is provided by TemporalMongoDataSource type.
    ///
    /// IMPORTANT - this data source imposes the restriction that a key
    /// can be present in not more than one dataset in a dataset chain.
    /// This restriction is enforced by the save method of this data source,
    /// and is used in query optimization. Some error checking is performed
    /// on read to ensure only one key is present in dataset chain, however
    /// one should not rely on read checking for enforcing this condition;
    /// it should be implemented on write.
    /// </summary>
    public class CurrentMongoDataSourceData : MongoDataSourceData
    {
        /// <summary>Dictionary of collections indexed by type T.</summary>
        private Dictionary<Type, object> collectionDict_ = new Dictionary<Type, object>();

        //--- METHODS

        /// <summary>
        /// Load record by its ObjectId.
        ///
        /// Return null if there is no record for the specified ObjectId;
        /// however an exception will be thrown if the record exists but
        /// is not derived from TRecord. 
        /// </summary>
        public override TRecord LoadOrNull<TRecord>(ObjectId id)
        {
            var savedBy = GetSavedBy();
            if (savedBy != null)
            {
                // Return null for any record that has ID greater than
                // the value returned by GetSavedBy() method
                if (id > savedBy.Value) return null;
            }

            // Find last record in last dataset without constraining record type
            // The result may not be derived from TRecord
            var baseResult = GetOrCreateCollection<TRecord>()
                .BaseCollection
                .AsQueryable()
                .FirstOrDefault(p => p.Id == id);

            // Check not only for null but also for the delete marker
            if (baseResult != null && !baseResult.Is<DeleteMarker>())
            {
                // Record is found but we do not yet know if it has the right type.
                // Attempt to cast Record to TRecord and check if the result is null.
                TRecord result = baseResult.As<TRecord>();
                if (result == null)
                {
                    // If cast result is null, the record was found but it is an instance
                    // of class that is not derived from TRecord, in this case the API
                    // requires error message, not returning null
                    throw new Exception(
                        $"Stored type {result.GetType().Name} for ObjectId={id} and " +
                        $"Key={result.Key} is not derived from the queried type {typeof(TRecord).Name}.");
                }

                // Initialize before returning
                result.Init(Context);
                return result;
            }
            else
            {
                // Record not found or is a delete marker, return null
                return null;
            }
        }

        /// <summary>
        /// This method does not use cached value inside the key
        /// and always retrieves a new record from storage. To get
        /// the record cached inside the key instead (if present), use
        /// the caching variant of this method:
        ///
        /// LoadOrNull(key, loadFrom)
        /// 
        /// Load record by string key from the specified dataset or
        /// its list of imports. The lookup occurs first in descending
        /// order of dataset ObjectIds, and then in the descending
        /// order of record ObjectIds within the first dataset that
        /// has at least one record. Both dataset and record ObjectIds
        /// are ordered chronologically to one second resolution,
        /// and are unique within the database server or cluster. 
        ///
        /// The root dataset has empty ObjectId value that is less
        /// than any other ObjectId value. Accordingly, the root
        /// dataset is the last one in the lookup order of datasets.
        ///
        /// The first record in this lookup order is returned, or null
        /// if no records are found or if delete marker is the first
        /// record.
        ///
        /// Return null if there is no record for the specified ObjectId;
        /// however an exception will be thrown if the record exists but
        /// is not derived from TRecord. 
        /// </summary>
        public override TRecord ReloadOrNull<TKey, TRecord>(Key<TKey, TRecord> key, ObjectId loadFrom)
        {
            // String value of the key in semicolon delimited format for use in the query
            string keyValue = key.ToString();

            // Look for exact match of the key
            var baseQueryable = GetOrCreateCollection<TRecord>()
                .BaseCollection
                .AsQueryable()
                .Where(p => p.Key == keyValue);

            // Apply constraints on dataset and revision time
            var queryableWithFinalConstraints = ApplyFinalConstraints(baseQueryable, loadFrom);

            // Order by dataset and then by ID in descending order
            var orderedQueryable = queryableWithFinalConstraints
                .OrderByDescending(p => p.DataSet)
                .OrderByDescending(p => p.Id);

            // Result will be null if the record is not found
            var baseResult = orderedQueryable.FirstOrDefault();

            // Check not only for null but also for the delete marker
            if (baseResult != null && !baseResult.Is<DeleteMarker>())
            {
                // Record is found but we do not yet know if it has the right type.
                // Attempt to cast Record to TRecord and check if the result is null.
                TRecord result = baseResult.As<TRecord>();
                if (result == null)
                {
                    // If cast result is null, the record was found but it is an instance
                    // of class that is not derived from TRecord, in this case the API
                    // requires error message, not returning null
                    throw new Exception(
                        $"Stored type {result.GetType().Name} for Key={key.Value} in " +
                        $"DataSet={loadFrom} is not derived from the queried type {typeof(TRecord).Name}.");
                }

                // Initialize before returning
                result.Init(Context);
                return result;
            }
            else
            {
                // Record not found or is a delete marker, return null
                return null;
            }
        }

        /// <summary>
        /// Get query for the specified type.
        ///
        /// After applying query parameters, the lookup occurs first in
        /// descending order of dataset ObjectIds, and then in the descending
        /// order of record ObjectIds within the first dataset that
        /// has at least one record. Both dataset and record ObjectIds
        /// are ordered chronologically to one second resolution,
        /// and are unique within the database server or cluster.
        ///
        /// The root dataset has empty ObjectId value that is less
        /// than any other ObjectId value. Accordingly, the root
        /// dataset is the last one in the lookup order of datasets.
        ///
        /// Generic parameter TRecord is not necessarily the root data type;
        /// it may also be a type derived from the root data type.
        /// </summary>
        public override IQuery<TRecord> GetQuery<TRecord>(ObjectId loadFrom)
        {
            // Get or create collection, then create query from collection
            var collection = GetOrCreateCollection<TRecord>();
            return new CurrentMongoQuery<TRecord>(collection, loadFrom);
        }

        /// <summary>
        /// Save record to the specified dataset. After the method exits,
        /// record.DataSet will be set to the value of the dataSet parameter.
        ///
        /// All Save methods ignore the value of record.DataSet before the
        /// Save method is called. When dataset is not specified explicitly,
        /// the value of dataset from the context, not from the record, is used.
        /// The reason for this behavior is that the record may be stored from
        /// a different dataset than the one where it is used.
        ///
        /// This method guarantees that ObjectIds will be in strictly increasing
        /// order for this instance of the data source class always, and across
        /// all processes and machine if they are not created within the same
        /// second.
        /// </summary>
        public override void Save<TRecord>(TRecord record, ObjectId saveTo)
        {
            CheckNotReadOnly();

            var collection = GetOrCreateCollection<TRecord>();

            // This method guarantees that ObjectIds will be in strictly increasing
            // order for this instance of the data source class always, and across
            // all processes and machine if they are not created within the same
            // second.
            var objectId = CreateOrderedObjectId();

            // ObjectId of the record must be strictly later
            // than ObjectId of the dataset where it is stored
            if (objectId <= saveTo)
                throw new Exception(
                    $"Attempting to save a record with ObjectId={objectId} that is later " +
                    $"than ObjectId={saveTo} of the dataset where it is being saved.");

            // Assign ID and DataSet, and only then initialize, because 
            // initialization code may use record.ID and record.DataSet
            record.Id = objectId;
            record.DataSet = saveTo;
            record.Init(Context);

            // By design, insert will fail if ObjectId is not unique within the collection
            collection.TypedCollection.InsertOne(record);
        }

        /// <summary>
        /// Write a delete marker in deleteIn dataset for the specified key
        /// instead of actually deleting the record. This ensures that
        /// a record in another dataset does not become visible during
        /// lookup in a sequence of datasets.
        ///
        /// To avoid an additional roundtrip to the data store, the delete
        /// marker is written even when the record does not exist.
        /// </summary>
        public override void Delete<TKey, TRecord>(Key<TKey, TRecord> key, ObjectId deleteIn)
        {
            CheckNotReadOnly();

            // Create delete marker with the specified key
            var record = new DeleteMarker {Key = key.Value};

            // Get collection
            var collection = GetOrCreateCollection<TRecord>();

            // This method guarantees that ObjectIds will be in strictly increasing
            // order for this instance of the data source class always, and across
            // all processes and machine if they are not created within the same
            // second.
            var objectId = CreateOrderedObjectId();
            record.Id = objectId;

            // Assign dataset and then initialize, as the results of 
            // initialization may depend on record.DataSet
            record.DataSet = deleteIn;

            // By design, insert will fail if ObjectId is not unique within the collection
            collection.BaseCollection.InsertOne(record);
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
        protected CurrentMongoCollection<TRecord> GetOrCreateCollection<TRecord>()
            where TRecord : RecordBase
        {
            // Check if collection object has already been cached
            // for this type and return cached result if found
            if (collectionDict_.TryGetValue(typeof(TRecord), out object collectionObj))
            {
                var cachedResult = collectionObj.CastTo<CurrentMongoCollection<TRecord>>();
                return cachedResult;
            }

            // Check that scalar discriminator convention is set for TRecord
            var discriminatorConvention = BsonSerializer.LookupDiscriminatorConvention(typeof(TRecord));
            if (useScalarDiscriminatorConvention_)
            {
                if (!discriminatorConvention.Is<ScalarDiscriminatorConvention>())
                    throw new Exception(
                        $"Scalar discriminator convention is not set for type {typeof(TRecord).Name}. " +
                        $"The convention should have been set set in the static constructor of " +
                        $"MongoDataSourceData");
            }
            else
            {
                if (!discriminatorConvention.Is<HierarchicalDiscriminatorConvention>())
                    throw new Exception(
                        $"Hierarchical discriminator convention is not set for type {typeof(TRecord).Name}. " +
                        $"The convention should have been set set in the static constructor of " +
                        $"MongoDataSourceData");
            }

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
                var indexModel = new CreateIndexModel<TRecord>(indexKeys, new CreateIndexOptions { Name = indexName });
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
                foreach (var indexInfo in indexDict)
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

                    var indexModel = new CreateIndexModel<TRecord>(indexKeys, new CreateIndexOptions { Name = indexName });
                    typedCollection.Indexes.CreateOne(indexModel);
                }
            }

            // Create result that holds both base and typed collections
            CurrentMongoCollection<TRecord> result = new CurrentMongoCollection<TRecord>(this, baseCollection, typedCollection);

            // Add the result to the collection dictionary and return
            collectionDict_.Add(typeof(TRecord), result);
            return result;
        }
    }
}
