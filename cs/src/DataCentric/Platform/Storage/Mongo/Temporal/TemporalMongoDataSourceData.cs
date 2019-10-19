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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
        private ConcurrentDictionary<Type, object> collectionDict_ = new ConcurrentDictionary<Type, object>();
        private Dictionary<string, RecordId> dataSetDict_ { get; } = new Dictionary<string, RecordId>();
        private Dictionary<RecordId, HashSet<RecordId>> importDict_ { get; } = new Dictionary<RecordId, HashSet<RecordId>>();

        //--- ELEMENTS

        /// <summary>
        /// Records where _id is greater than CutoffTime will be
        /// ignored by the data source.
        /// </summary>
        public RecordId? CutoffTime { get; set; }

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
        /// Load record by its RecordId.
        ///
        /// Return null if there is no record for the specified RecordId;
        /// however an exception will be thrown if the record exists but
        /// is not derived from TRecord.
        /// </summary>
        public override TRecord LoadOrNull<TRecord>(RecordId id)
        {
            if (CutoffTime != null)
            {
                // Return null for any record that has ID greater than CutoffTime.
                if (id > CutoffTime.Value) return null;
            }

            // Find last record in last dataset without constraining record type.
            // The result may not be derived from TRecord.
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
            var recordId = CreateOrderedRecordId();

            // RecordId of the record must be strictly later
            // than RecordId of the dataset where it is stored
            if (recordId <= saveTo)
                throw new Exception(
                    $"Attempting to save a record with RecordId={recordId} that is later " +
                    $"than RecordId={saveTo} of the dataset where it is being saved.");

            // Assign ID and DataSet, and only then initialize, because
            // initialization code may use record.ID and record.DataSet
            record.Id = recordId;
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
            var recordId = CreateOrderedRecordId();
            record.Id = recordId;

            // Assign dataset and then initialize, as the results of
            // initialization may depend on record.DataSet
            record.DataSet = deleteIn;

            // By design, insert will fail if RecordId is not unique within the collection
            collection.BaseCollection.InsertOne(record);
        }

        /// <summary>
        /// Apply the final constraints after all prior Where clauses but before OrderBy clause:
        ///
        /// * The constraint on dataset lookup list, restricted by CutoffTime (if not null)
        /// * The constraint on ID being strictly less than CutoffTime (if not null)
        /// </summary>
        public IQueryable<TRecord> ApplyFinalConstraints<TRecord>(IQueryable<TRecord> queryable, RecordId loadFrom)
            where TRecord : Record
        {
            // Get lookup list by expanding the list of imports to arbitrary
            // depth with duplicates and cyclic references removed.
            //
            // The list will not include datasets that are after the value of
            // CutoffTime if specified, or their imports (including
            // even those imports that are earlier than the constraint).
            IEnumerable<RecordId> dataSetLookupList = GetDataSetLookupList(loadFrom);

            // Apply constraint that the value is _dataset is
            // one of the elements of dataSetLookupList_
            var result = queryable.Where(p => dataSetLookupList.Contains(p.DataSet));

            // Apply revision time constraint. By making this constraint the
            // last among the constraints, we optimize the use of the index.
            //
            // The property savedBy_ is set using either CutoffTime element.
            // Only one of these two elements can be set at a given time.
            if (CutoffTime != null)
            {
                result = result.Where(p => p.Id <= CutoffTime.Value);
            }

            return result;
        }

        /// <summary>
        /// Get RecordId of the dataset with the specified name.
        ///
        /// All of the previously requested dataSetIds are cached by
        /// the data source. To load the latest version of the dataset
        /// written by a separate process, clear the cache first by
        /// calling DataSource.ClearDataSetCache() method.
        ///
        /// Returns null if not found.
        /// </summary>
        public override RecordId? GetDataSetOrNull(string dataSetName, RecordId loadFrom)
        {
            if (dataSetDict_.TryGetValue(dataSetName, out RecordId result))
            {
                // Check if already cached, return if found
                return result;
            }
            else
            {
                // Otherwise load from storage (this also updates the dictionaries)
                DataSetKey dataSetKey = new DataSetKey() { DataSetName = dataSetName };
                DataSetData dataSetData = this.LoadOrNull(dataSetKey, loadFrom);

                // If not found, return RecordId.Empty
                if (dataSetData == null) return null;

                // If found, cache result in RecordId dictionary
                dataSetDict_[dataSetName] = dataSetData.Id;

                // Build and cache dataset lookup list if not found
                if (!importDict_.TryGetValue(dataSetData.Id, out HashSet<RecordId> importSet))
                {
                    importSet = BuildDataSetLookupList(dataSetData);
                    importDict_.Add(dataSetData.Id, importSet);
                }

                return dataSetData.Id;
            }
        }

        /// <summary>
        /// Save new version of the dataset.
        ///
        /// This method sets Id element of the argument to be the
        /// new RecordId assigned to the record when it is saved.
        /// The timestamp of the new RecordId is the current time.
        ///
        /// This method updates in-memory cache to the saved dataset.
        /// </summary>
        public override void SaveDataSet(DataSetData dataSetData, RecordId saveTo)
        {
            // Save dataset to storage. This updates its Id
            // to the new RecordId created during save
            Save<DataSetData>(dataSetData, saveTo);

            // Update dataset dictionary with the new Id
            dataSetDict_[dataSetData.Key] = dataSetData.Id;

            // Update lookup list dictionary
            var lookupList = BuildDataSetLookupList(dataSetData);
            importDict_.Add(dataSetData.Id, lookupList);
        }

        /// <summary>
        /// Returns enumeration of import datasets for specified dataset data,
        /// including imports of imports to unlimited depth with cyclic
        /// references and duplicates removed.
        ///
        /// The list will not include datasets that are after the value of
        /// CutoffTime if specified, or their imports (including
        /// even those imports that are earlier than the constraint).
        /// </summary>
        public IEnumerable<RecordId> GetDataSetLookupList(RecordId loadFrom)
        {
            // Root dataset has no imports (there is not even a record
            // where these imports can be specified).
            //
            // Return list containing only the root dataset (RecordId.Empty) and exit
            if (loadFrom == RecordId.Empty)
            {
                return new RecordId[] { RecordId.Empty };
            }

            if (importDict_.TryGetValue(loadFrom, out HashSet<RecordId> result))
            {
                // Check if the lookup list is already cached, return if yes
                return result;
            }
            else
            {
                // Otherwise load from storage (returns null if not found)
                DataSetData dataSetData = LoadOrNull<DataSetData>(loadFrom);

                if (dataSetData == null) throw new Exception($"Dataset with RecordId={loadFrom} is not found.");
                if (dataSetData.DataSet != RecordId.Empty) throw new Exception($"Dataset with RecordId={loadFrom} is not stored in root dataset.");

                // Build the lookup list
                result = BuildDataSetLookupList(dataSetData);

                // Add to dictionary and return
                importDict_.Add(loadFrom, result);
                return result;
            }
        }

        //--- PRIVATE

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
        private TemporalMongoCollection<TRecord> GetOrCreateCollection<TRecord>()
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
            string collectionName = rootType.Name;

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
            collectionDict_.TryAdd(typeof(TRecord), result);
            return result;
        }

        /// <summary>
        /// Builds hashset of import datasets for specified dataset data,
        /// including imports of imports to unlimited depth with cyclic
        /// references and duplicates removed. This method uses cached lookup
        /// list for the import datasets but not for the argument dataset.
        ///
        /// The list will not include datasets that are after the value of
        /// CutoffTime if specified, or their imports (including
        /// even those imports that are earlier than the constraint).
        ///
        /// This overload of the method will return the result hashset.
        ///
        /// This private helper method should not be used directly.
        /// It provides functionality for the public API of this class.
        /// </summary>
        private HashSet<RecordId> BuildDataSetLookupList(DataSetData dataSetData)
        {
            // Delegate to the second overload
            var result = new HashSet<RecordId>();
            BuildDataSetLookupList(dataSetData, result);
            return result;
        }

        /// <summary>
        /// Builds hashset of import datasets for specified dataset data,
        /// including imports of imports to unlimited depth with cyclic
        /// references and duplicates removed. This method uses cached lookup
        /// list for the import datasets but not for the argument dataset.
        ///
        /// The list will not include datasets that are after the value of
        /// CutoffTime if specified, or their imports (including
        /// even those imports that are earlier than the constraint).
        ///
        /// This overload of the method will return the result hashset.
        ///
        /// This private helper method should not be used directly.
        /// It provides functionality for the public API of this class.
        /// </summary>
        private void BuildDataSetLookupList(DataSetData dataSetData, HashSet<RecordId> result)
        {
            // Return if the dataset is null or has no imports
            if (dataSetData == null) return;

            // Error message if dataset has no Id or Key set
            dataSetData.Id.CheckHasValue();
            dataSetData.Key.CheckHasValue();

            if (CutoffTime != null && dataSetData.Id > CutoffTime.Value)
            {
                // Do not add if revision time constraint is set and is before this dataset.
                // In this case the import datasets should not be added either, even if they
                // do not fail the revision time constraint
                return;
            }

            // Add self to the result
            result.Add(dataSetData.Id);

            // Add imports to the result
            if (dataSetData.Imports != null)
            {
                foreach (var dataSetId in dataSetData.Imports)
                {
                    // Dataset cannot include itself as its import
                    if (dataSetData.Id == dataSetId)
                        throw new Exception(
                            $"Dataset {dataSetData.Key} with RecordId={dataSetData.Id} includes itself in the list of its imports.");

                    // The Add method returns true if the argument is not yet present in the hashset
                    if (result.Add(dataSetId))
                    {
                        // Add recursively if not already present in the hashset
                        var cachedImportList = GetDataSetLookupList(dataSetId);
                        foreach (var importId in cachedImportList)
                        {
                            result.Add(importId);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Error message if either ReadOnly or CutoffTime is set.
        /// Historical view of the data cannot be written to.
        /// </summary>
        private void CheckNotReadOnly()
        {
            if (ReadOnly != null && ReadOnly.Value)
                throw new Exception(
                    $"Attempting write operation for data source {DataSourceName} where ReadOnly flag is set.");
            if (CutoffTime != null)
                throw new Exception(
                    $"Attempting write operation for data source {DataSourceName} where " +
                    $"CutoffTime is set. Historical view of the data cannot be written to.");
        }
    }
}
