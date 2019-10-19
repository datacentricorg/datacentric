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
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

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
    /// </summary>
    public class TemporalMongoDataSourceData : MongoDataSourceData
    {
        /// <summary>Dictionary of collections indexed by type T.</summary>
        private ConcurrentDictionary<Type, object> collectionDict_ = new ConcurrentDictionary<Type, object>();
        private Dictionary<string, RecordId> dataSetDict_ { get; } = new Dictionary<string, RecordId>();
        private Dictionary<RecordId, RecordId> dataSetParentDict_ { get; } = new Dictionary<RecordId, RecordId>();
        private Dictionary<RecordId, DataSetDetailData> dataSetDetailDict_ { get; } = new Dictionary<RecordId, DataSetDetailData>();
        private Dictionary<RecordId, HashSet<RecordId>> importDict_ { get; } = new Dictionary<RecordId, HashSet<RecordId>>();

        //--- ELEMENTS

        /// <summary>
        /// Records with RecordId that is greater than or equal to CutoffTime
        /// will be ignored by load methods and queries, and the latest available
        /// record where RecordId is less than CutoffTime will be returned instead.
        ///
        /// CutoffTime applies to both the records stored in the dataset itself,
        /// and the reports loaded through the Imports list.
        ///
        /// CutoffTime may be set in data source globally, or for a specific dataset
        /// in its details record. If CutoffTime is set for both, the earlier of the
        /// two values will be used.
        /// </summary>
        public RecordId? CutoffTime { get; set; }

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
            // This is a preliminary check for CutoffTime of the data source
            // to avoid unnecessary loading. Once the record is loaded, we 
            // will perform full check using GetCutoffTime(...) method
            // that takes into account both CutoffTime of the data source
            // and CutoffTime of the dataset.
            if (CutoffTime != null)
            {
                // Return null for any record that has RecordId
                // that is greater than or equal to CutoffTime.
                if (id >= CutoffTime.Value) return null;
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
                // Now we use GetCutoffTime() for the full check
                RecordId? cutoffTime = GetCutoffTime(baseResult.DataSet);
                if (cutoffTime != null)
                {
                    // Return null for any record that has RecordId
                    // that is greater than or equal to CutoffTime.
                    if (id >= cutoffTime.Value) return null;
                }

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
            CheckNotReadOnly(saveTo);

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
            CheckNotReadOnly(deleteIn);

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
            RecordId? cutoffTime = GetCutoffTime(loadFrom);
            if (cutoffTime != null)
            {
                result = result.Where(p => p.Id < cutoffTime.Value);
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

                // Get or create dataset detail record
                var dataSetDetailKey = new DataSetDetailKey {DataSetId = dataSetData.Id};
                DataSetDetailData dataSetDetailData = this.LoadOrNull(dataSetDetailKey, loadFrom);
                if (dataSetDetailData == null)
                {
                    dataSetDetailData = new DataSetDetailData {DataSetId = dataSetData.Id};
                    Context.Save(dataSetDetailData, loadFrom);
                }

                // Cache RecordId for the dataset and its parent
                dataSetDict_[dataSetName] = dataSetData.Id;
                dataSetParentDict_[dataSetData.Id] = dataSetData.DataSet;

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

            // Cache RecordId for the dataset and its parent
            dataSetDict_[dataSetData.Key] = dataSetData.Id;
            dataSetParentDict_[dataSetData.Id] = dataSetData.DataSet;

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

        /// <summary>
        /// Get detail of the specified dataset.
        ///
        /// Returns null if the details record does not exist.
        ///
        /// The detail is loaded for the dataset specified in the first argument
        /// (detailFor) from the dataset specified in the second argument (loadFrom).
        /// </summary>
        public DataSetDetailData GetDataSetDetailOrNull(RecordId detailFor)
        { 
            if (detailFor == RecordId.Empty)
            {
                // Root dataset does not have details
                // as it has no parent where the details
                // would be stored, and storing details
                // in the dataset itself would subject
                // them to their own settings.
                //
                // Accordingly, return null.
                return null;
            }
            else if (dataSetDetailDict_.TryGetValue(detailFor, out DataSetDetailData result))
            {
                // Check if already cached, return if found
                return result;
            }
            else
            {
                // Get dataset parent from the dictionary.
                // We should not get here unless the value
                // is already cached.
                var parentId = dataSetParentDict_[detailFor];

                // Otherwise try loading from storage (this also updates the dictionaries)
                var dataSetDetailKey = new DataSetDetailKey { DataSetId = detailFor };
                result = this.LoadOrNull(dataSetDetailKey, parentId);

                // Cache in dictionary even if null
                dataSetDetailDict_[detailFor] = result;
                return result;
            }
        }

        /// <summary>
        /// CutoffTime should only be used via this method which also takes into
        /// account the CutoffTime set in dataset detail record, and never directly.
        /// 
        /// CutoffTime may be set in data source globally, or for a specific dataset
        /// in its details record. If CutoffTime is set for both, this method will
        /// return the earlier of the two values will be used.
        /// 
        /// Records with RecordId that is greater than or equal to CutoffTime
        /// will be ignored by load methods and queries, and the latest available
        /// record where RecordId is less than CutoffTime will be returned instead.
        ///
        /// CutoffTime applies to both the records stored in the dataset itself,
        /// and the reports loaded through the Imports list.
        /// </summary>
        public RecordId? GetCutoffTime(RecordId dataSetId)
        {
            // Get imports cutoff time for the dataset detail record.
            // If the record is not found, consider its CutoffTime null.
            var dataSetDetailData = GetDataSetDetailOrNull(dataSetId);
            RecordId? dataSetCutoffTime = dataSetDetailData != null ? dataSetDetailData.CutoffTime : null;

            // If CutoffTime is set for both data source and dataset,
            // this method returns the earlier of the two values.
            var result = RecordId.Min(CutoffTime, dataSetCutoffTime);
            return result;
        }

        /// <summary>
        /// Gets ImportsCutoffTime from the dataset detail record.
        /// Returns null if dataset detail record is not found.
        /// 
        /// Imported records (records loaded through the Imports list)
        /// where RecordId is greater than or equal to CutoffTime
        /// will be ignored by load methods and queries, and the latest
        /// available record where RecordId is less than CutoffTime will
        /// be returned instead.
        ///
        /// This setting only affects records loaded through the Imports
        /// list. It does not affect records stored in the dataset itself.
        ///
        /// Use this feature to freeze Imports as of a given CreatedTime
        /// (part of RecordId), isolating the dataset from changes to the
        /// data in imported datasets that occur after that time.
        /// </summary>
        public RecordId? GetImportsCutoffTime(RecordId dataSetId)
        {
            // Get dataset detail record
            var dataSetDetailData = GetDataSetDetailOrNull(dataSetId);

            // Return null if the record is not found
            if (dataSetDetailData != null) return dataSetDetailData.ImportsCutoffTime;
            else return null;
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

            RecordId? cutoffTime = GetCutoffTime(dataSetData.DataSet);
            if (cutoffTime != null && dataSetData.Id >= cutoffTime.Value)
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
        /// Error message if one of the following is the case:
        ///
        /// * ReadOnly is set for the data source
        /// * ReadOnly is set for the dataset
        /// * CutoffTime is set for the data source
        /// * CutoffTime is set for the dataset
        /// </summary>
        private void CheckNotReadOnly(RecordId dataSetId)
        {
            if (ReadOnly != null && ReadOnly.Value)
                throw new Exception(
                    $"Attempting write operation for data source {DataSourceName} where ReadOnly flag is set.");

            var dataSetDetailData = GetDataSetDetailOrNull(dataSetId);
            if (dataSetDetailData != null && dataSetDetailData.ReadOnly != null && dataSetDetailData.ReadOnly.Value)
                throw new Exception(
                    $"Attempting write operation for dataset {dataSetId} where ReadOnly flag is set.");

            if (CutoffTime != null)
                throw new Exception(
                    $"Attempting write operation for data source {DataSourceName} where " +
                    $"CutoffTime is set. Historical view of the data cannot be written to.");

            if (dataSetDetailData != null && dataSetDetailData.CutoffTime != null)
                throw new Exception(
                    $"Attempting write operation for the dataset {dataSetId} where " +
                    $"CutoffTime is set. Historical view of the data cannot be written to.");
        }
    }
}
