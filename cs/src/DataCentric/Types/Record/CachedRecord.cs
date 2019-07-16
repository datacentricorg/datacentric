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
using System.Data;
using MongoDB.Bson;

namespace DataCentric
{
    /// <summary>
    /// Reference to a cached record inside the key.
    ///
    /// The dataset is stored in a separate variable, not inside
    /// the record variable. This is to avoid the change in
    /// dataset value when it changes for the record.
    ///
    /// This reference is used in two cases:
    ///
    /// First, to avoid getting the record from storage multiple times.
    /// The first value loaded from storage will be cached in Record
    /// and returned on all subsequent calls for the same dataset
    /// without storage lookup.
    ///
    /// Second, to avoid accessing storage when two objects are
    /// created in memory, one having a property that is a key
    /// to the other. Use SetCachedRecord(record) method to assign
    /// an in-memory object to a key which will also set values
    /// of the elements of the key to the corresponding values
    /// of the record.
    /// </summary>
    public class CachedRecord
    {
        /// <summary>
        /// Cache dataset and record.
        ///
        /// Delete marker will be cached as null.
        /// </summary>
        public CachedRecord(ObjectId dataSet, RecordType record = null)
        {
            // Dataset for which the record is cached
            DataSet = dataSet;

            if (!record.Is<DeleteMarker>())
            {
                // Cache only if not a delete marker,
                // otherwise Record will remain null
                // after the constructor exits
                Record = record;
            }
        }

        /// <summary>Dataset for which the record is cached.</summary>
        public ObjectId DataSet { get; }

        /// <summary>
        /// Record passed to the constructor, or null for an
        /// empty cached record or a delete marker.
        /// </summary>
        public RecordType Record { get; }
    }
}
