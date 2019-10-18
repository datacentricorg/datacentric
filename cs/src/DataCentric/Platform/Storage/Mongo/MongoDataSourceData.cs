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
using MongoDB.Bson.Serialization.Attributes;
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
        protected const bool useScalarDiscriminatorConvention_ = false;
        static readonly char[] prohibitedDbNameSymbols_ = new char[] { '/', '\\', '.', ' ', '"', '$', '*', '<', '>', ':', '|', '?' };
        static int maxDbNameLength_ = 64;
        private InstanceType instanceType_;
        private string dbName_;
        private IMongoClient client_;
        private RecordId prevRecordId_ = RecordId.Empty;

        //--- ELEMENTS

        /// <summary>
        /// Specifies Mongo server for this data source.
        ///
        /// Defaults to local server on the standard port if not specified.
        ///
        /// Server URI specified here must refer to the entire server, not
        /// an individual database.
        /// </summary>
        public MongoServerKey MongoServer { get; set; }

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
                // Choosing root type to be Record ensures that _t is always an array.
                BsonSerializer.RegisterDiscriminatorConvention(typeof(Data), new HierarchicalDiscriminatorConvention("_t"));
            }
        }

        //--- METHODS

        /// <summary>
        /// Set Context property and perform validation of the record's data,
        /// then initialize any fields or properties that depend on that data.
        ///
        /// This method must work when called multiple times for the same instance,
        /// possibly with a different context parameter for each subsequent call.
        ///
        /// All overrides of this method must call base.Init(context) first, then
        /// execute the rest of the code in the override.
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
            if (string.IsNullOrEmpty(DbName.InstanceName)) throw new Exception("DB instance name is not specified.");
            if (string.IsNullOrEmpty(DbName.EnvName)) throw new Exception("DB environment name is not specified.");

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

            // Get client interface using the server instance loaded from root dataset
            if (MongoServer != null)
            {
                // Create with the specified server URI
                client_ = new MongoClient(MongoServer.MongoServerUri);
            }
            else
            {
                // Create for the server running on default port on localhost
                client_ = new MongoClient();
            }

            // Get database interface using the client and database name
            Db = client_.GetDatabase(dbName_);
        }

        /// <summary>
        /// The returned RecordIds have the following order guarantees:
        ///
        /// * For this data source instance, to arbitrary resolution; and
        /// * Across all processes and machines, to one second resolution
        ///
        /// One second resolution means that two RecordIds created within
        /// the same second by different instances of the data source
        /// class may not be ordered chronologically unless they are at
        /// least one second apart.
        /// </summary>
        public override RecordId CreateOrderedRecordId()
        {
            CheckNotReadOnly();

            // Generate RecordId and check that it is later
            // than the previous generated RecordId
            RecordId result = RecordId.GenerateNewId();
            int retryCounter = 0;
            while (result <= prevRecordId_)
            {
                // Getting inside the while loop will be very rare as this would
                // require the increment to roll from max int to min int within
                // the same second, therefore it is a good idea to log the event
                if (retryCounter++ == 0) Context.Log.Warning("MongoDB generated RecordId not in increasing order, retrying.");

                // If new RecordId is not strictly greater than the previous one,
                // keep generating new RecordIds until it changes
                result = RecordId.GenerateNewId();
            }

            // Report the number of retries
            if (retryCounter != 0)
            {
                Context.Log.Warning($"Generated RecordId in increasing order after {retryCounter} retries.");
            }

            // Update previous RecordId and return
            prevRecordId_ = result;
            return result;
        }

        /// <summary>
        /// Apply the final constraints after all prior Where clauses but before OrderBy clause:
        ///
        /// * The constraint on dataset lookup list, restricted by SavedBy (if not null)
        /// * The constraint on ID being strictly less than SavedBy (if not null)
        /// </summary>
        public IQueryable<TRecord> ApplyFinalConstraints<TRecord>(IQueryable<TRecord> queryable, RecordId loadFrom)
            where TRecord : Record
        {
            // Get lookup list by expanding the list of imports to arbitrary
            // depth with duplicates and cyclic references removed.
            //
            // The list will not include datasets that are after the value of
            // SavedByTime/SavedById if specified, or their imports (including
            // even those imports that are earlier than the constraint).
            IEnumerable<RecordId> dataSetLookupList = GetDataSetLookupList(loadFrom);

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
