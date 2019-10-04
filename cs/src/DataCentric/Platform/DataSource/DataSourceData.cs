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
using MongoDB.Bson;
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
        //--- FIELDS

        /// <summary>
        /// Dictionary of dataset ObjectIds stored under string dataSetName.
        /// </summary>
        private Dictionary<string, ObjectId> dataSetDict_ { get; } = new Dictionary<string, ObjectId>();

        /// <summary>
        /// Dictionary of the expanded list of import ObjectIds for each dataset ObjectId,
        /// including imports of imports to unlimited depth with cyclic references and
        /// duplicates removed.
        /// </summary>
        private Dictionary<ObjectId, HashSet<ObjectId>> importDict_ { get; } = new Dictionary<ObjectId, HashSet<ObjectId>>();

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
        /// Identifies the data store used by this data source.
        ///
        /// Data store represents a database server or similar concept for non-database
        /// storage. It is not the same as data source (database) as it only specifies
        /// the server, and each server can host multiple data sources (databases).
        ///
        /// Separating the data store from the data source helps separate server
        /// specifics such as URI, connection string, etc in data store from the
        /// definition of how the data is stored on the server, including the
        /// environment (which usually maps to database name) and data representation
        /// (basic or temporal).
        /// </summary>
        public DataStoreKey DataStore { get; set; }

        /// <summary>
        /// Use this flag to mark dataset as readonly, but use either
        /// IsReadOnly() or CheckNotReadonly() method to determine the
        /// readonly status because the dataset may be readonly for
        /// two reasons:
        ///
        /// * ReadOnly flag is true; or
        /// * One of SavedByTime or SavedById is set
        /// </summary>
        public bool ReadOnly { get; set; }

        //--- ABSTRACT

        /// <summary>
        /// Returns true if the data source is readonly,
        /// which may be because ReadOnly flag is true,
        /// or due to other flags (e.g. SavedBy) defined
        /// in derived types.
        /// </summary>
        public abstract bool IsReadOnly();

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
        public abstract ObjectId CreateOrderedObjectId();

        /// <summary>
        /// Load record by its ObjectId.
        ///
        /// Return null if there is no record for the specified ObjectId;
        /// however an exception will be thrown if the record exists but
        /// is not derived from TRecord.
        /// </summary>
        public abstract TRecord LoadOrNull<TRecord>(ObjectId id)
            where TRecord : Record;

        /// <summary>
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
        /// if no records are found or if DeletedRecord is the first
        /// record.
        ///
        /// Return null if there is no record for the specified ObjectId;
        /// however an exception will be thrown if the record exists but
        /// is not derived from TRecord.
        /// </summary>
        public abstract TRecord LoadOrNull<TKey, TRecord>(TypedKey<TKey, TRecord> key, ObjectId loadFrom)
            where TKey : TypedKey<TKey, TRecord>, new()
            where TRecord : TypedRecord<TKey, TRecord>;

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
        public abstract IQuery<TRecord> GetQuery<TRecord>(ObjectId loadFrom)
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
        /// This method guarantees that ObjectIds will be in strictly increasing
        /// order for this instance of the data source class always, and across
        /// all processes and machine if they are not created within the same
        /// second.
        /// </summary>
        public abstract void Save<TRecord>(TRecord record, ObjectId saveTo)
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
        public abstract void Delete<TKey, TRecord>(TypedKey<TKey, TRecord> key, ObjectId deleteIn)
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
        /// Error message if the data source is readonly,
        /// which may be for the following reasons:
        ///
        /// * ReadOnly flag is true; or
        /// * One of SavedByTime or SavedById is set
        /// </summary>
        public void CheckNotReadOnly()
        {
            if (IsReadOnly())
                throw new Exception(
                    $"Attempting write operation for readonly data source {DataSourceName}. " +
                    $"A data source is readonly if either (a) its ReadOnly flag is set, or (b) " +
                    $"one of SavedByTime or SavedById is set.");
        }

        /// <summary>
        /// Get ObjectId of the dataset with the specified name.
        ///
        /// All of the previously requested dataSetIds are cached by
        /// the data source. To load the latest version of the dataset
        /// written by a separate process, clear the cache first by
        /// calling DataSource.ClearDataSetCache() method.
        ///
        /// Returns null if not found.
        /// </summary>
        public ObjectId? GetDataSetOrNull(string dataSetName, ObjectId loadFrom)
        {
            if (dataSetDict_.TryGetValue(dataSetName, out ObjectId result))
            {
                // Check if already cached, return if found
                return result;
            }
            else
            {
                // Otherwise load from storage (this also updates the dictionaries)
                DataSetKey dataSetKey = new DataSetKey() { DataSetName = dataSetName };
                DataSetData dataSetData = this.LoadOrNull(dataSetKey, loadFrom);

                // If not found, return ObjectId.Empty
                if (dataSetData == null) return null;

                // If found, cache result in ObjectId dictionary
                dataSetDict_[dataSetName] = dataSetData.Id;

                // Build and cache dataset lookup list if not found
                if (!importDict_.TryGetValue(dataSetData.Id, out HashSet<ObjectId> importSet))
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
        /// new ObjectId assigned to the record when it is saved.
        /// The timestamp of the new ObjectId is the current time.
        ///
        /// This method updates in-memory cache to the saved dataset.
        /// </summary>
        public void SaveDataSet(DataSetData dataSetData, ObjectId saveTo)
        {
            // Save dataset to storage. This updates its Id
            // to the new ObjectId created during save
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
        /// SavedByTime/SavedById if specified, or their imports (including
        /// even those imports that are earlier than the constraint).
        /// </summary>
        public IEnumerable<ObjectId> GetDataSetLookupList(ObjectId loadFrom)
        {
            // Root dataset has no imports (there is not even a record
            // where these imports can be specified).
            //
            // Return list containing only the root dataset (ObjectId.Empty) and exit
            if (loadFrom == ObjectId.Empty)
            {
                return new ObjectId[] {ObjectId.Empty};
            }

            if (importDict_.TryGetValue(loadFrom, out HashSet<ObjectId> result))
            {
                // Check if the lookup list is already cached, return if yes
                return result;
            }
            else
            {
                // Otherwise load from storage (returns null if not found)
                DataSetData dataSetData = LoadOrNull<DataSetData>(loadFrom);

                if (dataSetData == null) throw new Exception($"Dataset with ObjectId={loadFrom} is not found.");
                if (dataSetData.DataSet != ObjectId.Empty) throw new Exception($"Dataset with ObjectId={loadFrom} is not stored in root dataset.");

                // Build the lookup list
                result = BuildDataSetLookupList(dataSetData);

                // Add to dictionary and return
                importDict_.Add(loadFrom, result);
                return result;
            }
        }

        //--- PROTECTED

        /// <summary>
        /// Records where _id is greater than the returned value will be
        /// ignored by the data source.
        ///
        /// This element is set based on either SavedByTime and SavedById
        /// elements that are alternates; only one of them can be specified.
        /// </summary>
        protected abstract ObjectId? GetSavedBy();

        //--- PRIVATE

        /// <summary>
        /// Builds hashset of import datasets for specified dataset data,
        /// including imports of imports to unlimited depth with cyclic
        /// references and duplicates removed. This method uses cached lookup
        /// list for the import datasets but not for the argument dataset.
        ///
        /// The list will not include datasets that are after the value of
        /// SavedByTime/SavedById if specified, or their imports (including
        /// even those imports that are earlier than the constraint).
        ///
        /// This overload of the method will return the result hashset.
        ///
        /// This private helper method should not be used directly.
        /// It provides functionality for the public API of this class.
        /// </summary>
        private HashSet<ObjectId> BuildDataSetLookupList(DataSetData dataSetData)
        {
            // Delegate to the second overload
            var result = new HashSet<ObjectId>();
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
        /// SavedByTime/SavedById if specified, or their imports (including
        /// even those imports that are earlier than the constraint).
        ///
        /// This overload of the method will return the result hashset.
        ///
        /// This private helper method should not be used directly.
        /// It provides functionality for the public API of this class.
        /// </summary>
        private void BuildDataSetLookupList(DataSetData dataSetData, HashSet<ObjectId> result)
        {
            // Return if the dataset is null or has no imports
            if (dataSetData == null) return;

            // Error message if dataset has no Id or Key set
            dataSetData.Id.CheckHasValue();
            dataSetData.Key.CheckHasValue();

            var savedBy = GetSavedBy();
            if (savedBy != null && dataSetData.Id > savedBy.Value)
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
                            $"Dataset {dataSetData.Key} with ObjectId={dataSetData.Id} includes itself in the list of its imports.");

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
