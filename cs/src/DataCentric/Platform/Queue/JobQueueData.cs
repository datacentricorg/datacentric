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
using CsvHelper.Configuration.Attributes;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace DataCentric
{
    /// <summary>
    /// The queue continuously monitors the dataset where it is
    /// running for job records. Once a job record is detected,
    /// the queue schedules the job for execution.
    ///
    /// The queue updates the job status record at least every
    /// time its status changes, and optionally more often to
    /// update its progress fraction and progress message. It
    /// also monitors the dataset where it is running for interrupt
    /// records.
    ///
    /// To execute the job, the queue invokes method Run() of
    /// the job record. Depending on the type of queue, it may be:
    ///
    /// * Executed in a different process or thread
    /// * Executed on a different machine
    /// * Executed in parallel or out of sequence
    ///
    /// The Run() method must be implemented defensively to ensure
    /// that the job runs successfully in all of these cases.
    /// </summary>
    public class JobQueueData : TypedRecord<JobQueueKey, JobQueueData>
    {
    }
}
