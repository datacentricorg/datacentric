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
using System.Runtime.CompilerServices;
using MongoDB.Bson;

namespace DataCentric
{
    /// <summary>
    /// Null implementation to use for contexts such as log-only
    /// context that do not support working with data.
    ///
    /// This implementation will raise an error if any of its
    /// methods are invoked.
    /// </summary>
    public class NullDataSourceData : DataSourceData
    {
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
            ErrorMessage();
            return ObjectId.Empty;
        }

        /// <summary>
        /// Load record by its ObjectId.
        ///
        /// Return null if there is no record for the specified ObjectId;
        /// however an exception will be thrown if the record exists but
        /// is not derived from TRecord. 
        /// </summary>
        public override TRecord LoadOrNull<TRecord>(ObjectId id)
        {
            ErrorMessage();
            return null;
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
        /// if no records are found or if delete marker is the first
        /// record.
        ///
        /// Return null if there is no record for the specified ObjectId;
        /// however an exception will be thrown if the record exists but
        /// is not derived from TRecord. 
        /// </summary>
        public override TRecord ReloadOrNull<TKey, TRecord>(KeyFor<TKey, TRecord> key, ObjectId loadFrom)
        {
            ErrorMessage();
            return null;
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
        public override IQuery<TRecord> GetQuery<TRecord>(ObjectId loadFrom)
        {
            ErrorMessage();
            return null;
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
        public override void Save<TRecord>(TRecord record, ObjectId saveTo)
        {
            ErrorMessage();
        }

        /// <summary>
        /// Write a delete marker in deleteIn dataset for the specified key
        /// instead of actually deleting the record. This ensures that
        /// a record in another dataset does not become visible during
        /// lookup in a sequence of datasets.
        ///
        /// To avoid an additional roundtrip to the data store, the delete
        /// marker is written even when the record does not exist.
        /// </summary>
        public override void Delete<TKey, TRecord>(KeyFor<TKey, TRecord> key, ObjectId deleteIn)
        {
            ErrorMessage();
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
            ErrorMessage();
        }

        /// <summary>Provides an error message if any of the data source methods are invoked.</summary>
        private void ErrorMessage([CallerMemberName] string callerMemberName = null)
        {
            throw new Exception($"Attempt to invoke method {callerMemberName} for a null data source.");
        }
    }
}
