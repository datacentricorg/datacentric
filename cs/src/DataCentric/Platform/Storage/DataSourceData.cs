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
using MongoDB.Bson.Serialization.Attributes;
using NodaTime;

namespace DataCentric
{
    /// <summary>
    /// Data source is a logical concept similar to database
    /// that can be implemented for a document DB, relational DB,
    /// key-value store, or filesystem.
    ///
    /// Data source API provides the ability to:
    ///
    /// (a) store and query datasets;
    /// (b) store records in a specific dataset; and
    /// (c) query record across a group of datasets.
    ///
    /// This record is stored in root dataset.
    /// </summary>
    public abstract class DataSourceData : RootRecord<DataSourceKey, DataSourceData>, IDataSource
    {
        private Dictionary<string, RecordId> dataSetDict_ { get; } = new Dictionary<string, RecordId>();
        private Dictionary<RecordId, HashSet<RecordId>> importDict_ { get; } = new Dictionary<RecordId, HashSet<RecordId>>();

        //--- ELEMENTS

        /// <summary>Unique data source name.</summary>
        [BsonRequired]
        public string DataSourceName { get; set; }

        /// <summary>
        /// This class enforces strict naming conventions
        /// for database naming. While format of the resulting database
        /// name is specific to data store type, it always consists
        /// of three tokens: InstanceType, InstanceName, and EnvName.
        /// The meaning of InstanceName and EnvName tokens depends on
        /// the value of InstanceType enumeration.
        /// </summary>
        public DbNameKey DbName { get; set; }

        /// <summary>
        /// Use this flag to mark data source as readonly.
        ///
        /// This value will be set to true if CutoffTime is
        /// true, because a data source that views the data
        /// for a cutoff time cannot modify its view.
        /// </summary>
        public bool? ReadOnly { get; set; }

        /// <summary>
        /// Records where _id is greater than SavedById will be
        /// ignored by the data source.
        /// </summary>
        public RecordId? SavedBy { get; set; }

        //--- METHODS

        /// <summary>
        /// Releases resources and calls base.Dispose().
        ///
        /// This method will not be called by the garbage collector.
        /// It will only be executed if:
        ///
        /// * This class implements IDisposable; and
        /// * The class instance is created through the using clause
        ///
        /// IMPORTANT - Every override of this method must call base.Dispose()
        /// after executing its own code.
        /// </summary>
        public virtual void Dispose()
        {
            // Uncomment except in root class of the hierarchy
            // base.Dispose();
        }

        /// <summary>Flush data to permanent storage.</summary>
        public abstract void Flush();

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
        public abstract RecordId CreateOrderedRecordId();

        /// <summary>
        /// Load record by its RecordId.
        ///
        /// Return null if there is no record for the specified RecordId;
        /// however an exception will be thrown if the record exists but
        /// is not derived from TRecord.
        /// </summary>
        public abstract TRecord LoadOrNull<TRecord>(RecordId id)
            where TRecord : Record;

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
        public abstract TRecord LoadOrNull<TKey, TRecord>(TypedKey<TKey, TRecord> key, RecordId loadFrom)
            where TKey : TypedKey<TKey, TRecord>, new()
            where TRecord : TypedRecord<TKey, TRecord>;

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
        public abstract IQuery<TRecord> GetQuery<TRecord>(RecordId loadFrom)
            where TRecord : Record;

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
        public abstract void Save<TRecord>(TRecord record, RecordId saveTo)
            where TRecord : Record;

        /// <summary>
        /// Write a DeletedRecord in deleteIn dataset for the specified key
        /// instead of actually deleting the record. This ensures that
        /// a record in another dataset does not become visible during
        /// lookup in a sequence of datasets.
        ///
        /// To avoid an additional roundtrip to the data store, the delete
        /// marker is written even when the record does not exist.
        /// </summary>
        public abstract void Delete<TKey, TRecord>(TypedKey<TKey, TRecord> key, RecordId deleteIn)
            where TKey : TypedKey<TKey, TRecord>, new()
            where TRecord : TypedRecord<TKey, TRecord>;

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
        public abstract void DeleteDb();

        //--- METHODS

        /// <summary>
        /// Error message if the data source is readonly.
        ///
        /// This method also provides an alert if CutoffTime
        /// is set but ReadOnly flag is not because  is not
        /// possible to write to a view of the data as of a
        /// past point in time.
        /// </summary>
        public void CheckNotReadOnly()
        {
            if (ReadOnly != null && ReadOnly.Value)
                throw new Exception(
                    $"Attempting write operation for readonly data source {DataSourceName}. " +
                    $"A data source is readonly if either (a) its ReadOnly flag is set, or (b) " +
                    $"one of SavedByTime or SavedById is set.");
            else if (SavedBy != null)
                throw new Exception(
                    $"Data source {DataSourceName} has CutoffTime set, but is not ReadOnly. " +
                    $"It is not possible to write to a view of the data as of a past point in time.");
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
        public RecordId? GetDataSetOrNull(string dataSetName, RecordId loadFrom)
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
        public void SaveDataSet(DataSetData dataSetData, RecordId saveTo)
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
                return new RecordId[] {RecordId.Empty};
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

            if (SavedBy != null && dataSetData.Id > SavedBy.Value)
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
    }
}
