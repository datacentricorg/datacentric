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
using NodaTime;

namespace DataCentric
{
    /// <summary>
    /// Temporal data source with datasets based on MongoDB.
    ///
    /// The term Temporal applied means the data source stores complete revision
    /// history including copies of all previous versions of each record.
    ///
    /// In addition to being temporal, this data source is also hierarchical; the
    /// records are looked up across a hierarchy of datasets, including the dataset
    /// itself, its direct Imports, Imports of Imports, etc., ordered by dataset's
    /// RecordId.
    ///
    /// When FreezeImports is false, a query retrieves all records with a given key.
    /// The records are sorted by dataset's RecordId in descending order first, then
    /// by record's RecordId also in descending order. The first record in sort order
    /// is returned by the query, and all other records are ignored. This rule has
    /// the effect of retrieving the latest record in the latest dataset.
    ///
    /// When FreezeImports is true, those records whose RecordId is greater than
    /// RecordId of the next dataset in the lookup sequence (when the sequence is
    /// ordered by dataset's RecordId) are excluded, after which the rule described
    /// in the previous paragraph is applied. This has the effect of freezing the
    /// state of each imported datasets in the import lookup sequence as of the creation
    /// time of the next dataset in the sequence.
    ///
    /// The purpose of the FreezeImports flag is to prevent modification of records
    /// in imported datasets from affecting the calculations in a dataset to which they
    /// have been imported.
    /// </summary>
    public class TemporalMongoDataSourceData : MongoDataSourceData
    {
        /// <summary>Dictionary of collections indexed by type T.</summary>
        private Dictionary<Type, object> collectionDict_ = new Dictionary<Type, object>();

        //--- ELEMENTS

        /// <summary>
        /// Records where timestamp of _id rounded down to one second
        /// is greater than SavedByTime will be ignored by the data source.
        ///
        /// The value of this element must fall precisely on the second,
        /// error message otherwise.
        ///
        /// SavedByTime and SavedById elements are alternates;
        /// they cannot be specified at the same time.
        ///
        /// If either SavedByTime or SavedById is specified, the
        /// data source is readonly and its IsReadOnly() method returns true.
        /// </summary>
        public LocalDateTime? SavedByTime { get; set; }

        /// <summary>
        /// Records where _id is greater than SavedById will be
        /// ignored by the data source.
        ///
        /// SavedByTime and SavedById elements are alternates;
        /// they cannot be specified at the same time.
        ///
        /// If either SavedByTime or SavedById is specified, the
        /// data source is readonly and its IsReadOnly() method returns true.
        /// </summary>
        public RecordId? SavedById { get; set; }

        /// <summary>
        /// When FreezeImports is false, a query retrieves all records with a given key.
        /// The records are sorted by dataset's RecordId in descending order first, then
        /// by record's RecordId also in descending order. The first record in sort order
        /// is returned by the query, and all other records are ignored. This rule has
        /// the effect of retrieving the latest record in the latest dataset.
        ///
        /// When FreezeImports is true, those records whose RecordId is greater than
        /// RecordId of the next dataset in the lookup sequence (when the sequence is
        /// ordered by dataset's RecordId) are excluded, after which the rule described
        /// in the previous paragraph is applied. This has the effect of freezing the
        /// state of each imported datasets in the import lookup sequence as of the creation
        /// time of the next dataset in the sequence.
        ///
        /// The purpose of the FreezeImports flag is to prevent modification of records
        /// in imported datasets from affecting the calculations in a dataset to which they
        /// have been imported.
        /// </summary>
        public bool FreezeImports { get; set; } = true; // TODO - review the default

        //--- METHODS

        /// <summary>Flush data to permanent storage.</summary>
        public override void Flush()
        {
            // Do nothing
        }

        /// <summary>
        /// Returns true if the data source is readonly,
        /// which may be for the following reasons:
        ///
        /// * ReadOnly flag is true; or
        /// * One of SavedByTime or SavedById is set
        /// </summary>
        public override bool IsReadOnly()
        {
            return ReadOnly == true || SavedByTime != null || SavedById != null;
        }

        /// <summary>
        /// Load record by its RecordId.
        ///
        /// Return null if there is no record for the specified RecordId;
        /// however an exception will be thrown if the record exists but
        /// is not derived from TRecord.
        /// </summary>
        public override TRecord LoadOrNull<TRecord>(RecordId id)
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

            // Check not only for null but also for the DeletedRecord
            if (baseResult != null && !baseResult.Is<DeletedRecord>())
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
                        $"Stored type {result.GetType().Name} for RecordId={id} and " +
                        $"Key={result.Key} is not an instance of the requested type {typeof(TRecord).Name}.");
                }

                // Initialize before returning
                result.Init(Context);
                return result;
            }
            else
            {
                // Record not found or is a DeletedRecord, return null
                return null;
            }
        }

        /// <summary>
        /// Load record by string key from the specified dataset or
        /// its list of imports. The lookup occurs first in descending
        /// order of dataset RecordIds, and then in the descending
        /// order of record RecordIds within the first dataset that
        /// has at least one record. Both dataset and record RecordIds
        /// are ordered chronologically to one second resolution,
        /// and are unique within the database server or cluster.
        ///
        /// The root dataset has empty RecordId value that is less
        /// than any other RecordId value. Accordingly, the root
        /// dataset is the last one in the lookup order of datasets.
        ///
        /// The first record in this lookup order is returned, or null
        /// if no records are found or if DeletedRecord is the first
        /// record.
        ///
        /// Return null if there is no record for the specified RecordId;
        /// however an exception will be thrown if the record exists but
        /// is not derived from TRecord.
        /// </summary>
        public override TRecord LoadOrNull<TKey, TRecord>(TypedKey<TKey, TRecord> key, RecordId loadFrom)
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

            // Check not only for null but also for the DeletedRecord
            if (baseResult != null && !baseResult.Is<DeletedRecord>())
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
                        $"DataSet={loadFrom} is not an instance of the requested type {typeof(TRecord).Name}.");
                }

                // Initialize before returning
                result.Init(Context);
                return result;
            }
            else
            {
                // Record not found or is a DeletedRecord, return null
                return null;
            }
        }

        /// <summary>
        /// Get query for the specified type.
        ///
        /// After applying query parameters, the lookup occurs first in
        /// descending order of dataset RecordIds, and then in the descending
        /// order of record RecordIds within the first dataset that
        /// has at least one record. Both dataset and record RecordIds
        /// are ordered chronologically to one second resolution,
        /// and are unique within the database server or cluster.
        ///
        /// The root dataset has empty RecordId value that is less
        /// than any other RecordId value. Accordingly, the root
        /// dataset is the last one in the lookup order of datasets.
        ///
        /// Generic parameter TRecord is not necessarily the root data type;
        /// it may also be a type derived from the root data type.
        /// </summary>
        public override IQuery<TRecord> GetQuery<TRecord>(RecordId loadFrom)
        {
            // Get or create collection, then create query from collection
            var collection = GetOrCreateCollection<TRecord>();
            return new TemporalMongoQuery<TRecord>(collection, loadFrom);
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
        /// This method guarantees that RecordIds will be in strictly increasing
        /// order for this instance of the data source class always, and across
        /// all processes and machine if they are not created within the same
        /// second.
        /// </summary>
        public override void Save<TRecord>(TRecord record, RecordId saveTo)
        {
            CheckNotReadOnly();

            var collection = GetOrCreateCollection<TRecord>();

            // This method guarantees that RecordIds will be in strictly increasing
            // order for this instance of the data source class always, and across
            // all processes and machine if they are not created within the same
            // second.
            var objectId = CreateOrderedRecordId();

            // RecordId of the record must be strictly later
            // than RecordId of the dataset where it is stored
            if (objectId <= saveTo)
                throw new Exception(
                    $"Attempting to save a record with RecordId={objectId} that is later " +
                    $"than RecordId={saveTo} of the dataset where it is being saved.");

            // Assign ID and DataSet, and only then initialize, because
            // initialization code may use record.ID and record.DataSet
            record.Id = objectId;
            record.DataSet = saveTo;
            record.Init(Context);

            // By design, insert will fail if RecordId is not unique within the collection
            collection.TypedCollection.InsertOne(record);
        }

        /// <summary>
        /// Write a DeletedRecord in deleteIn dataset for the specified key
        /// instead of actually deleting the record. This ensures that
        /// a record in another dataset does not become visible during
        /// lookup in a sequence of datasets.
        ///
        /// To avoid an additional roundtrip to the data store, the delete
        /// marker is written even when the record does not exist.
        /// </summary>
        public override void Delete<TKey, TRecord>(TypedKey<TKey, TRecord> key, RecordId deleteIn)
        {
            CheckNotReadOnly();

            // Create DeletedRecord with the specified key
            var record = new DeletedRecord {Key = key.Value};

            // Get collection
            var collection = GetOrCreateCollection<TRecord>();

            // This method guarantees that RecordIds will be in strictly increasing
            // order for this instance of the data source class always, and across
            // all processes and machine if they are not created within the same
            // second.
            var objectId = CreateOrderedRecordId();
            record.Id = objectId;

            // Assign dataset and then initialize, as the results of
            // initialization may depend on record.DataSet
            record.DataSet = deleteIn;

            // By design, insert will fail if RecordId is not unique within the collection
            collection.BaseCollection.InsertOne(record);
        }

        //--- PROTECTED

        /// <summary>
        /// Records where _id is greater than the returned value will be
        /// ignored by the data source.
        ///
        /// This element is set based on either SavedByTime and SavedById
        /// elements that are alternates; only one of them can be specified.
        /// </summary>
        protected override RecordId? GetSavedBy()
        {
            // Set savedBy_ based on either SavedByTime or SavedById element
            if (SavedByTime == null && SavedById == null)
            {
                // Clear the revision time constraint.
                //
                // This is only required when  running Init(...) again
                // on an object that has been initialized before.
                return null;
            }
            else if (SavedByTime != null && SavedById == null)
            {
                // We already know that SavedBy is not null,
                // but we need to check separately that it is not empty
                SavedByTime.CheckHasValue();

                // Convert to the least value of RecordId with the specified timestamp
                return SavedByTime.ToRecordId();
            }
            else if (SavedByTime == null && SavedById != null)
            {
                // We already know that SavedById is not null,
                // but we need to check separately that it is not empty
                SavedById.Value.CheckHasValue();

                // Set the revision time constraint
                return SavedById;
            }
            else
            {
                throw new Exception(
                    "Elements SavedByTime and SavedById are alternates; " +
                    "they cannot be specified at the same time.");
            }
        }

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
        /// Additional indices may be created using class attribute
        /// [IndexElements] for further performance optimization.
        /// </summary>
        protected TemporalMongoCollection<TRecord> GetOrCreateCollection<TRecord>()
            where TRecord : Record
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
            Type rootType = DataTypeInfo.GetOrCreate(typeof(TRecord)).RootType;
            string collectionName = ClassInfo.GetOrCreate(rootType).MappedClassName;

            // Get interfaces to base and typed collections for the same name
            var baseCollection = Db.GetCollection<Record>(collectionName);
            var typedCollection = Db.GetCollection<TRecord>(collectionName);

            if (true)
            {
                // Each data type has an index for optimized loading by key.
                // This index consists of Key in ascending order, followed by
                // DataSet and ID in descending order.
                var indexKeys = Builders<TRecord>.IndexKeys
                    .Ascending(new StringFieldDefinition<TRecord>("_key")) // .Key
                    .Descending(new StringFieldDefinition<TRecord>("_dataset")) // .DataSet
                    .Descending(new StringFieldDefinition<TRecord>("_id")); // .Id

                // Use index definition convention to specify the index name
                var indexName = "Key-DataSet-Id";
                var indexModel = new CreateIndexModel<TRecord>(indexKeys, new CreateIndexOptions { Name = indexName });
                typedCollection.Indexes.CreateOne(indexModel);
            }

            // Additional indices are provided using IndexAttribute for the class.
            if (true)
            {
                // Get a sorted dictionary of (definition, name) pairs
                // for the inheritance chain of the specified type.
                var indexDict = IndexElementsAttribute.GetAttributesDict<TRecord>();

                // Iterate over the dictionary to define the index
                foreach (var indexInfo in indexDict)
                {
                    string indexDefinition = indexInfo.Key;
                    string indexName = indexInfo.Value;

                    // Parse index definition to get a list of (ElementName,SortOrder) tuples
                    List<(string, int)> indexTokens = IndexElementsAttribute.ParseDefinition<TRecord>(indexDefinition);

                    var indexKeysBuilder = Builders<TRecord>.IndexKeys;
                    IndexKeysDefinition<TRecord> indexKeys = null;

                    // Iterate over (ElementName,SortOrder) tuples
                    foreach (var indexToken in indexTokens)
                    {
                        (string elementName, int sortOrder) = indexToken;

                        if (indexKeys == null)
                        {
                            // Create from builder for the first element
                            if (sortOrder == 1) indexKeys = indexKeysBuilder.Ascending(new StringFieldDefinition<TRecord>(elementName));
                            else if (sortOrder == -1) indexKeys = indexKeysBuilder.Descending(new StringFieldDefinition<TRecord>(elementName));
                            else throw new Exception("Sort order must be 1 or -1.");
                        }
                        else
                        {
                            // Chain to the previous list of index keys for the remaining elements
                            if (sortOrder == 1) indexKeys = indexKeys.Ascending(new StringFieldDefinition<TRecord>(elementName));
                            else if (sortOrder == -1) indexKeys = indexKeys.Descending(new StringFieldDefinition<TRecord>(elementName));
                            else throw new Exception("Sort order must be 1 or -1.");
                        }
                    }

                    if (indexName == null) throw new Exception("Index name cannot be null.");
                    var indexModel = new CreateIndexModel<TRecord>(indexKeys, new CreateIndexOptions { Name = indexName });

                    // Add to indexes for the collection
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
