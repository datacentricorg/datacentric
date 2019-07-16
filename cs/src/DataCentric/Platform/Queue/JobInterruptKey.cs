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
    /// The queue to which the job is submitted continuously
    /// monitors the dataset where it is running for interrupt
    /// records. Once an interrupt record is detected, the action
    /// specified by this enum is performed for job specified by
    /// the interrupt record, and job status is updated accordingly.
    /// </summary>
    [BsonSerializer(typeof(BsonKeySerializer<JobInterruptKey>))]
    public class JobInterruptKey : KeyFor<JobInterruptKey, JobInterruptData>
    {
        /// <summary>Job to which the interrupt record applies.</summary>
        [BsonRequired]
        public JobKey Job { get; set; }
    }
}
