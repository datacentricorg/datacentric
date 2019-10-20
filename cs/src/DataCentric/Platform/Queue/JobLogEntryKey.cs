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
using MongoDB.Bson.Serialization.Attributes;

namespace DataCentric
{
    /// <summary>
    /// Contains a single log entry generated by the job.
    ///
    /// To obtain the entire log, use a query to match Job element of
    /// the log entry record to the job and sort the records in the
    /// order of their TemporalId.
    /// </summary>
    [BsonSerializer(typeof(BsonKeySerializer<JobLogEntryKey>))]
    public sealed class JobLogEntryKey : TypedKey<JobLogEntryKey, JobLogEntryData>
    {
        /// <summary>
        /// Defining element Id here includes the record's TemporalId
        /// in its key. Because TemporalId of the record is specific
        /// to its version, this is equivalent to using an auto-
        /// incrementing column as part of the record's primary key
        /// in a relational database.
        ///
        /// For the record's history to be captured correctly, all
        /// update operations must assign a new TemporalId with the
        /// timestamp that matches update time.
        /// </summary>
        public TemporalId Id { get; set; }
    }
}
