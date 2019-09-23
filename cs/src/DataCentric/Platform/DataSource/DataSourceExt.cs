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
    /// <summary>Extension methods for IDataSource.</summary>
    public static class DataSourceExt
    {
        /// <summary>
        /// Load record by its ObjectId.
        ///
        /// Error message if there is no record for the specified ObjectId
        /// or if the record exists but is not derived from TRecord.
        /// </summary>
        public static TRecord Load<TRecord>(this IDataSource obj, ObjectId id)
            where TRecord : Record
        {
            var result = obj.LoadOrNull<TRecord>(id);
            if (result == null) throw new Exception(
                $"Record with ObjectId={id} is not found in data store {obj.DataSourceId}.");
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
        /// Error message if the record is not found or is a DeletedRecord.
        /// </summary>
        public static TRecord Load<TKey, TRecord>(this IDataSource obj, TypedKey<TKey, TRecord> key, ObjectId loadFrom)
            where TKey : TypedKey<TKey, TRecord>, new()
            where TRecord : TypedRecord<TKey, TRecord>
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
        /// if no records are found or if DeletedRecord is the first
        /// record.
        ///
        /// Return null if there is no record for the specified ObjectId;
        /// however an exception will be thrown if the record exists but
        /// is not derived from TRecord.
        /// </summary>
        public static TRecord LoadOrNull<TKey, TRecord>(this IDataSource obj, TypedKey<TKey, TRecord> key, ObjectId loadFrom)
            where TKey : TypedKey<TKey, TRecord>, new()
            where TRecord : TypedRecord<TKey, TRecord>
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
            return obj.GetDataSet(DataSetKey.Common.DataSetId, ObjectId.Empty);
        }

        /// <summary>
        /// Return ObjectId for the latest dataset record with
        /// matching dataSetId string from in-memory cache. Try
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
        public static ObjectId GetDataSet(this IDataSource obj, string dataSetId, ObjectId loadFrom)
        {
            var result = obj.GetDataSetOrEmpty(dataSetId, loadFrom);
            if (result == ObjectId.Empty) throw new Exception(
                $"Dataset {dataSetId} is not found in data store {obj.DataSourceId}.");
            return result;
        }

        /// <summary>
        /// Create new version of the Common dataset. By convention,
        /// the Common dataset contains reference and configuration
        /// data and is included as import in all other datasets.
        ///
        /// The Common dataset is always stored in root dataset.
        ///
        /// This method sets _id field of the argument to be the
        /// new ObjectId assigned to the record when it is saved.
        /// The timestamp of the new ObjectId is the current time.
        ///
        /// This method updates in-memory cache to the saved dataset.
        /// </summary>
        public static ObjectId CreateCommon(this IDataSource obj)
        {
            var result = new DataSetData() { DataSetId = DataSetKey.Common.DataSetId };

            // Save in root dataset
            obj.SaveDataSet(result, ObjectId.Empty); ;
            return result.Id;
        }

        /// <summary>
        /// Create new version of the dataset with the specified dataSetId.
        ///
        /// This method updates in-memory cache to the saved dataset.
        /// </summary>
        public static ObjectId CreateDataSet(this IDataSource obj, string dataSetId, ObjectId saveTo)
        {
            // Delegate to the overload taking IEnumerable as second parameter
            return obj.CreateDataSet(dataSetId, (IEnumerable<ObjectId>)null, saveTo);
        }

        /// <summary>
        /// Create new version of the dataset with the specified dataSetId
        /// and imported dataset ObjectIds passed as an array, and return
        /// the new ObjectId assigned to the saved dataset.
        ///
        /// This method updates in-memory cache to the saved dataset.
        /// </summary>
        public static ObjectId CreateDataSet(this IDataSource obj, string dataSetId, IEnumerable<ObjectId> importDataSets, ObjectId saveTo)
        {
            // Create dataset record
            var result = new DataSetData() { DataSetId = dataSetId };

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
            return result.Id;
        }
    }
}
