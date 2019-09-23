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
        /// <summary>True for scalar and false for hierarchical discriminator convention for _t.</summary>
        protected const bool useScalarDiscriminatorConvention_ = false;

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

        /// <summary>Previous ObjectId returned by CreateOrderedObjectId() method.</summary>
        private ObjectId prevObjectId_ = ObjectId.Empty;

        //--- PROPERTIES

        /// <summary>Interface to Mongo database in MongoDB C# driver.</summary>
        public IMongoDatabase Db { get; private set; }

        //--- CONSTRUCTORS

        /// <summary>
        /// Use static constructor to set discriminator convention.
        ///
        /// This call is in static constructor because MongoDB driver
        /// complains if it is called more than once.
        /// </summary>
        static MongoDataSourceData()
        {
            if (useScalarDiscriminatorConvention_)
            {
                // Set discriminator convention to scalar. For this convention,
                // BSON element _t is a single string value equal to GetType().Name,
                // rather than the list of names for the entire inheritance chain.
                BsonSerializer.RegisterDiscriminatorConvention(typeof(Data), new ScalarDiscriminatorConvention("_t"));
            }
            else
            {
                // Set discriminator convention to hierarchical. For this convention,
                // BSON element _t is either an array of GetType().Name values for ell
                // types in the inheritance chain, or a single string value for a chain
                // of length 1.
                //
                // Choosing root type to be RecordBase ensures that _t is always an array.
                BsonSerializer.RegisterDiscriminatorConvention(typeof(Data), new HierarchicalDiscriminatorConvention("_t"));
            }
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
    }
}
