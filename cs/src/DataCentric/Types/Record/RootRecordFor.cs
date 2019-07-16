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
using MongoDB.Bson;

namespace DataCentric
{
    /// <summary>
    /// Base class of records stored in root dataset of the data store.
    ///
    /// This class overrides DataSet property to always return ObjectId.Empty.
    /// </summary>
    public abstract class RootRecordFor<TKey, TRecord> : RecordFor<TKey, TRecord>
        where TKey : RootKeyFor<TKey, TRecord>, new()
        where TRecord : RootRecordFor<TKey, TRecord>
    {
        /// <summary>
        /// ObjectId of the dataset where the record is stored.
        ///
        /// Records in root dataset must override this property to remove the error
        /// message that would otherwise be triggered when saving into root dataset.
        ///
        /// This override's getter always returns the root dataset and its setter
        /// does nothing. Accordingly, the records derived from this class will
        /// always be saved in root dataset.
        /// </summary>
        public override ObjectId DataSet { get => ObjectId.Empty; set { } }

        /// Always returns true for root records
        ///
        /// This method is needed because accessing dataset property
        /// before it is set throws an exception in order to avoid
        /// the called incorrectly assuming the object is in root dataset.
        /// </summary>
        public override bool DataSetHasValue() { return true; }
    }
}
