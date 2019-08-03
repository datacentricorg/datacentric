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
    public interface IDataSource
    {
        //--- PROPERTIES

        /// <summary>Context for which this interface is defined.
        /// Use to access other interfaces of the same context.</summary>
        IContext Context { get; }
 
        //--- ELEMENTS

        /// <summary>Unique data source identifier.</summary>
        string DataSourceID { get; }

        //--- METHODS

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
        ObjectId CreateOrderedObjectId();

        /// <summary>
        /// Load record by its ObjectId.
        ///
        /// Return null if there is no record for the specified ObjectId;
        /// however an exception will be thrown if the record exists but
        /// is not derived from TRecord. 
        /// </summary>
        TRecord LoadOrNull<TRecord>(ObjectId id)
            where TRecord : RecordBase;

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
        TRecord ReloadOrNull<TKey, TRecord>(Key<TKey, TRecord> key, ObjectId loadFrom)
            where TKey : Key<TKey, TRecord>, new()
            where TRecord : Record<TKey, TRecord>;

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
        IQuery<TRecord> GetQuery<TRecord>(ObjectId loadFrom)
            where TRecord : RecordBase;

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
        void Save<TRecord>(TRecord record, ObjectId saveTo)
            where TRecord : RecordBase;

        /// <summary>
        /// Write a delete marker in deleteIn dataset for the specified key
        /// instead of actually deleting the record. This ensures that
        /// a record in another dataset does not become visible during
        /// lookup in a sequence of datasets.
        ///
        /// To avoid an additional roundtrip to the data store, the delete
        /// marker is written even when the record does not exist.
        /// </summary>
        void Delete<TKey, TRecord>(Key<TKey, TRecord> key, ObjectId deleteIn)
            where TKey : Key<TKey, TRecord>, new()
            where TRecord : Record<TKey, TRecord>;

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
        void DeleteDb();

        /// <summary>
        /// Return ObjectId for the latest dataset record with
        /// matching dataSetID string from in-memory cache. Try
        /// loading from storage only if not found in cache.
        ///
        /// Return ObjectId.Empty if not found.
        ///
        /// This method will return the value from in-memory
        /// cache even if it is no longer the latest version
        /// in the data store and will only load it from storage
        /// if not found in cache. Use LoadDataSet method to
        /// force reloading the dataset from storage.
        ///
        /// Error message if no matching dataSetID string is found
        /// or a delete marker is found instead.
        /// </summary>
        ObjectId GetDataSetOrEmpty(string dataSetID, ObjectId loadFrom);

        /// <summary>
        /// Save new version of the dataset.
        ///
        /// This method sets ID field of the argument to be the
        /// new ObjectId assigned to the record when it is saved.
        /// The timestamp of the new ObjectId is the current time.
        /// 
        /// This method updates in-memory cache to the saved dataset.
        /// </summary>
        void SaveDataSet(DataSetData dataSetData, ObjectId saveTo);
    }

    /// <summary>Extension methods for IDataSource.</summary>
    public static class IDataSourceEx
    {
        /// <summary>
        /// Load record by its ObjectId.
        ///
        /// Error message if there is no record for the specified ObjectId
        /// or if the record exists but is not derived from TRecord. 
        /// </summary>
        public static TRecord Load<TRecord>(this IDataSource obj, ObjectId id)
            where TRecord : RecordBase
        {
            var result = obj.LoadOrNull<TRecord>(id);
            if (result == null) throw new Exception(
                $"Record with ObjectId={id} is not found in data store {obj.DataSourceID}.");
            return result;
        }
 
        /// <summary>
        /// Load record from context.DataSource, overriding the dataset
        /// specified in the context with the value specified as the
        /// second parameter. The lookup occurs in the specified dataset
        /// and its imports, expanded to arbitrary depth with repetitions
        /// and cyclic references removed.
        ///
        /// ATTENTION - this method ignores context.DataSet
        /// because the second parameter loadFrom overrides it.
        /// 
        /// If Record property is set, its value is returned without
        /// performing lookup in the data store; otherwise the record
        /// is loaded from storage and cached in Record and the
        /// cached value is returned from subsequent calls.
        ///
        /// Once the record has been cached, the same version will be
        /// returned in subsequent calls with the same key instance.
        /// Create a new key or call earRecord() method to force
        /// reloading new version of the record from storage.
        /// 
        /// Error message if the record is not found or is a delete marker.
        /// </summary>
        public static TRecord Load<TKey, TRecord>(this IDataSource obj, Key<TKey, TRecord> key, ObjectId loadFrom)
            where TKey : Key<TKey, TRecord>, new()
            where TRecord : Record<TKey, TRecord>
        {
            return key.Load(obj.Context, loadFrom);
        }

        /// <summary>
        /// This method uses cached record inside the key if present,
        /// and only loads a record from storage if there is no cached
        /// record. To always retrieve a new record from storage, use
        /// the non-caching variant of this method:
        ///
        /// ReloadOrNull(key,dataSet)
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
        public static TRecord LoadOrNull<TKey, TRecord>(this IDataSource obj, Key<TKey, TRecord> key, ObjectId loadFrom)
            where TKey : Key<TKey, TRecord>, new()
            where TRecord : Record<TKey, TRecord>
        {
            // This method forwards to the implementation in Key(TKey, TRecord),
            // which in turn uses the non-caching variant of the same method,
            // in this class, ReloadOrNull(key,dataSet).
            //
            // While it would have been cleaner to keep all of the implementations
            // here, this method requires access to cachedRecord_ which is a private
            // field of Key(TKey, TRecord) that is not accessible to this class.
            return key.LoadOrNull(obj.Context, loadFrom);
        }

        /// <summary>
        /// Return ObjectId of the latest Common dataset.
        ///
        /// Common dataset is always stored in root dataset.
        /// </summary>
        public static ObjectId GetCommon(this IDataSource obj)
        {
            return obj.GetDataSet(DataSetKey.Common.DataSetID, ObjectId.Empty);
        }

        /// <summary>
        /// Return ObjectId for the latest dataset record with
        /// matching dataSetID string from in-memory cache. Try
        /// loading from storage only if not found in cache.
        ///
        /// Error message if not found.
        ///
        /// This method will return the value from in-memory
        /// cache even if it is no longer the latest version
        /// in the data store and will only load it from storage
        /// if not found in cache. Use LoadDataSet method to
        /// force reloading the dataset from storage.
        /// </summary>
        public static ObjectId GetDataSet(this IDataSource obj, string dataSetID, ObjectId loadFrom)
        {
            var result = obj.GetDataSetOrEmpty(dataSetID, loadFrom);
            if (result == ObjectId.Empty) throw new Exception(
                $"Dataset {dataSetID} is not found in data store {obj.DataSourceID}.");
            return result;
        }

        /// <summary>
        /// Create new version of the Common dataset. By convention,
        /// the Common dataset contains reference and configuration
        /// data and is included as import in all other datasets.
        ///
        /// The Common dataset is always stored in root dataset.
        ///
        /// This method sets ID field of the argument to be the
        /// new ObjectId assigned to the record when it is saved.
        /// The timestamp of the new ObjectId is the current time.
        /// 
        /// This method updates in-memory cache to the saved dataset.
        /// </summary>
        public static ObjectId CreateCommon(this IDataSource obj)
        {
            var result = new DataSetData() { DataSetID = DataSetKey.Common.DataSetID };

            // Save in root dataset
            obj.SaveDataSet(result, ObjectId.Empty); ;
            return result.ID;
        }

        /// <summary>
        /// Create new version of the dataset with the specified dataSetID.
        /// 
        /// This method updates in-memory cache to the saved dataset.
        /// </summary>
        public static ObjectId CreateDataSet(this IDataSource obj, string dataSetID, ObjectId saveTo)
        {
            // Delegate to the overload taking IEnumerable as second parameter
            return obj.CreateDataSet(dataSetID, (IEnumerable<ObjectId>)null, saveTo);
        }

        /// <summary>
        /// Create new version of the dataset with the specified dataSetID
        /// and imported dataset ObjectIds passed as an array, and return
        /// the new ObjectId assigned to the saved dataset.
        /// 
        /// This method updates in-memory cache to the saved dataset.
        /// </summary>
        public static ObjectId CreateDataSet(this IDataSource obj, string dataSetID, IEnumerable<ObjectId> importDataSets, ObjectId saveTo)
        {
            // Create dataset record
            var result = new DataSetData() { DataSetID = dataSetID };

            if (importDataSets != null)
            {
                // Add imports if second argument is not null
                result.Import = new List<ObjectId>();
                foreach (var importDataSet in importDataSets)
                {
                    result.Import.Add(importDataSet);
                }
            }

            // Save the record (this also updates the dictionaries)
            obj.SaveDataSet(result, saveTo);

            // Return ObjectId that was assigned to the
            // record inside the SaveDataSet method
            return result.ID;
        }
    }
}
