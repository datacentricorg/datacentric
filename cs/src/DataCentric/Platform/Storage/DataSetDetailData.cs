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
using CsvHelper.Configuration.Attributes;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace DataCentric
{
    /// <summary>
    /// Provides the ability to change data associated with the dataset
    /// without changing the dataset record, which is immutable in a
    /// temporal data source.
    ///
    /// The reason dataset record is immutable is that any change to the
    /// the dataset record in a temporal data source results in creation
    /// of a record with new RecordId, which is treated as a new dataset.
    ///
    /// The DataSetDetail record uses RecordId of the referenced dataset
    /// as its primary key. It is located in the parent of the referenced
    /// dataset alongside the reference dataset record itself, so it it not
    /// affected by its own settings, such as ImportsCutoff or ReadOnly
    /// flag.
    /// </summary>
    public abstract class DataSetDetailData : TypedRecord<DataSetDetailKey, DataSetDetailData>
    {
        /// <summary>
        /// RecordId of the referenced dataset.
        /// </summary>
        [BsonRequired]
        public RecordId DataSetId { get; set; }

        /// <summary>
        /// If specified, write operations to the referenced dataset
        /// will result in an error.
        /// </summary>
        public bool? ReadOnly { get; set; }

        /// <summary>
        /// If specified, data from the list of imported datasets is
        /// queried for RecordId strictly less than the provided value.
        ///
        /// This has the effect of ``freezing'' the state of imports
        /// as of the specified time (RecordId), thereby isolating
        /// the current dataset from changes to the data in imported
        /// datasets that occur after the specified time (RecordId).
        /// </summary>
        public RecordId? ImportsCutoff { get; set; }
    }
}
