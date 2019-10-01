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
    /// Extension methods for IContext.
    ///
    /// This class permits the methods of IDataSource to be called for
    /// IContext by forwarding the implementation to IContext.DataSource.
    /// </summary>
    public static class ContextExt
    {
        //--- METHODS

        /// <summary>
        /// Load record by its ObjectId.
        ///
        /// Return null if there is no record for the specified ObjectId;
        /// however an exception will be thrown if the record exists but
        /// is not derived from TRecord.
        /// </summary>
        public static TRecord LoadOrNull<TRecord>(this IContext obj, ObjectId id)
            where TRecord : Record
        {
            return obj.DataSource.LoadOrNull<TRecord>(id);
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
        /// if no records are found or if DeletedRecord is the first
        /// record.
        ///
        /// Return null if there is no record for the specified ObjectId;
        /// however an exception will be thrown if the record exists but
        /// is not derived from TRecord.
        /// </summary>
        public static TRecord LoadOrNull<TKey, TRecord>(this IContext obj, TypedKey<TKey, TRecord> key, ObjectId loadFrom)
            where TKey : TypedKey<TKey, TRecord>, new()
            where TRecord : TypedRecord<TKey, TRecord>
        {
            return obj.DataSource.LoadOrNull(key, loadFrom);
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
        /// if no records are found or if DeletedRecord is the first
        /// record.
        ///
        /// Return null if there is no record for the specified ObjectId;
        /// however an exception will be thrown if the record exists but
        /// is not derived from TRecord.
        /// </summary>
        public static TRecord ReloadOrNull<TKey, TRecord>(this IContext obj, TypedKey<TKey, TRecord> key, ObjectId loadFrom)
            where TKey : TypedKey<TKey, TRecord>, new()
            where TRecord : TypedRecord<TKey, TRecord>
        {
            return obj.DataSource.ReloadOrNull(key, loadFrom);
        }

        /// <summary>
        /// Get query for the specified type in the dataset of the context.
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
        public static IQuery<TRecord> GetQuery<TRecord>(this IContext obj)
            where TRecord : Record
        {
            return obj.DataSource.GetQuery<TRecord>(obj.DataSet);
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
        public static IQuery<TRecord> GetQuery<TRecord>(this IContext obj, ObjectId loadFrom)
            where TRecord : Record
        {
            return obj.DataSource.GetQuery<TRecord>(loadFrom);
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
        public static void Save<TRecord>(this IContext obj, TRecord record)
            where TRecord : Record
        {
            // All Save methods ignore the value of record.DataSet before the
            // Save method is called. When dataset is not specified explicitly,
            // the value of dataset from the context, not from the record, is used.
            // The reason for this behavior is that the record may be stored from
            // a different dataset than the one where it is used.
            obj.DataSource.Save(record, obj.DataSet);
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
        public static void Save<TRecord>(this IContext obj, TRecord record, ObjectId saveTo)
            where TRecord : Record
        {
            obj.DataSource.Save(record, saveTo);
        }

        /// <summary>
        /// Write a DeletedRecord for the dataset of the context and the specified
        /// key instead of actually deleting the record. This ensures that
        /// a record in another dataset does not become visible during
        /// lookup in a sequence of datasets.
        ///
        /// To avoid an additional roundtrip to the data store, the delete
        /// marker is written even when the record does not exist.
        /// </summary>
        public static void Delete<TKey, TRecord>(this IContext obj, TypedKey<TKey, TRecord> key)
            where TKey : TypedKey<TKey, TRecord>, new()
            where TRecord : TypedRecord<TKey, TRecord>
        {
            // Delete in the dataset of the context
            obj.DataSource.Delete(key, obj.DataSet);
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
        public static void Delete<TKey, TRecord>(this IContext obj, TypedKey<TKey, TRecord> key, ObjectId deleteIn)
            where TKey : TypedKey<TKey, TRecord>, new()
            where TRecord : TypedRecord<TKey, TRecord>
        {
            obj.DataSource.Delete(key, deleteIn);
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
        public static void DeleteDb(this IContext obj)
        {
            obj.DataSource.DeleteDb();
        }

        /// <summary>
        /// Return ObjectId of the latest Common dataset.
        ///
        /// Common dataset is always stored in root dataset.
        /// </summary>
        public static ObjectId GetCommon(this IContext obj)
        {
            return obj.DataSource.GetCommon();
        }

        /// <summary>
        /// Return ObjectId for the latest dataset record with
        /// matching dataSetId string from in-memory cache. Try
        /// loading from storage only if not found in cache.
        ///
        /// This overload of the GetDataSet method does not
        /// specify the loadFrom parameter explicitly and instead
        /// uses context.DataSet for its value.
        ///
        /// Error message if not found.
        ///
        /// This method will return the value from in-memory
        /// cache even if it is no longer the latest version
        /// in the data store and will only load it from storage
        /// if not found in cache. Use LoadDataSet method to
        /// force reloading the dataset from storage.
        /// </summary>
        public static ObjectId GetDataSet(this IContext obj, string dataSetId)
        {
            return obj.DataSource.GetDataSet(dataSetId, obj.DataSet);
        }

        /// <summary>
        /// Return ObjectId for the latest dataset record with
        /// matching dataSetId string from in-memory cache. Try
        /// loading from storage only if not found in cache.
        ///
        /// This overload of the GetDataSet method specifies
        /// the loadFrom parameter explicitly.
        ///
        /// Error message if not found.
        ///
        /// This method will return the value from in-memory
        /// cache even if it is no longer the latest version
        /// in the data store and will only load it from storage
        /// if not found in cache. Use LoadDataSet method to
        /// force reloading the dataset from storage.
        /// </summary>
        public static ObjectId GetDataSet(this IContext obj, string dataSetId, ObjectId loadFrom)
        {
            return obj.DataSource.GetDataSet(dataSetId, loadFrom);
        }

        /// <summary>
        /// Return ObjectId for the latest dataset record with
        /// matching dataSetId string from in-memory cache. Try
        /// loading from storage only if not found in cache.
        ///
        /// This overload of the GetDataSetOrEmpty method does not
        /// specify the loadFrom parameter explicitly and instead
        /// uses context.DataSet for its value.
        ///
        /// Return ObjectId.Empty if not found.
        ///
        /// This method will return the value from in-memory
        /// cache even if it is no longer the latest version
        /// in the data store and will only load it from storage
        /// if not found in cache. Use LoadDataSet method to
        /// force reloading the dataset from storage.
        ///
        /// Error message if no matching dataSetId string is found
        /// or a DeletedRecord is found instead.
        /// </summary>
        public static ObjectId GetDataSetOrEmpty(this IContext obj, string dataSetId)
        {
            return obj.DataSource.GetDataSetOrEmpty(dataSetId, obj.DataSet);
        }

        /// <summary>
        /// Return ObjectId for the latest dataset record with
        /// matching dataSetId string from in-memory cache. Try
        /// loading from storage only if not found in cache.
        ///
        /// This overload of the GetDataSetOrEmpty method specifies
        /// the loadFrom parameter explicitly.
        ///
        /// Return ObjectId.Empty if not found.
        ///
        /// This method will return the value from in-memory
        /// cache even if it is no longer the latest version
        /// in the data store and will only load it from storage
        /// if not found in cache. Use LoadDataSet method to
        /// force reloading the dataset from storage.
        ///
        /// Error message if no matching dataSetId string is found
        /// or a DeletedRecord is found instead.
        /// </summary>
        public static ObjectId GetDataSetOrEmpty(this IContext obj, string dataSetId, ObjectId loadFrom)
        {
            return obj.DataSource.GetDataSetOrEmpty(dataSetId, loadFrom);
        }

        /// <summary>
        /// Create new version of the Common dataset.
        ///
        /// Create new version of the Common dataset. By convention,
        /// the Common dataset contains reference and configuration
        /// data and is included as import in all other datasets.
        ///
        /// The Common dataset is always stored in root dataset.
        ///
        /// This method sets Id element of the argument to be the
        /// new ObjectId assigned to the record when it is saved.
        /// The timestamp of the new ObjectId is the current time.
        ///
        /// This method updates in-memory cache to the saved dataset.
        /// </summary>
        public static ObjectId CreateCommon(this IContext obj)
        {
            return obj.DataSource.CreateCommon();
        }

        /// <summary>
        /// Create new version of the dataset with the specified dataSetId
        /// and no import datasets.
        ///
        /// This overload of the CreateDataSet method does not
        /// specify the saveTo parameter explicitly and instead
        /// uses context.DataSet for its value.
        ///
        /// This method updates in-memory cache to the saved dataset.
        /// </summary>
        public static ObjectId CreateDataSet(this IContext obj, string dataSetId)
        {
            return obj.DataSource.CreateDataSet(dataSetId, obj.DataSet);
        }

        /// <summary>
        /// Create new version of the dataset with the specified dataSetId
        /// and no import datasets.
        ///
        /// This overload of the CreateDataSet method specifies
        /// the saveTo parameter explicitly.
        ///
        /// This method updates in-memory cache to the saved dataset.
        /// </summary>
        public static ObjectId CreateDataSet(this IContext obj, string dataSetId, ObjectId saveTo)
        {
            return obj.DataSource.CreateDataSet(dataSetId, saveTo);
        }

        /// <summary>
        /// Save new version of the dataset.
        ///
        /// This overload of the SaveDataSet method does not
        /// specify the saveTo parameter explicitly and instead
        /// uses context.DataSet for its value.
        ///
        /// This method sets Id element of the argument to be the
        /// new ObjectId assigned to the record when it is saved.
        /// The timestamp of the new ObjectId is the current time.
        ///
        /// This method updates in-memory cache to the saved dataset.
        /// </summary>
        public static void SaveDataSet(this IContext obj, DataSetData dataSetData)
        {
            obj.DataSource.SaveDataSet(dataSetData, obj.DataSet);
        }

        /// <summary>
        /// Save new version of the dataset.
        ///
        /// This overload of the SaveDataSet method specifies
        /// the saveTo parameter explicitly.
        ///
        /// This method sets Id element of the argument to be the
        /// new ObjectId assigned to the record when it is saved.
        /// The timestamp of the new ObjectId is the current time.
        ///
        /// This method updates in-memory cache to the saved dataset.
        /// </summary>
        public static void SaveDataSet(this IContext obj, DataSetData dataSetData, ObjectId saveTo)
        {
            obj.DataSource.SaveDataSet(dataSetData, saveTo);
        }
    }
}
