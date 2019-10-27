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
    /// The job executes a method of the specified record using:
    ///
    /// * Name of the collection where the record is stored
    /// * TemporalId of the record
    /// * Name of the method to be executed
    /// * List of parameter names (optional)
    /// * List of serialized parameter values (optional)
    ///
    /// The invoked method must return void.
    ///
    /// A job can execute any public method of a class that returns void.
    /// There is no requirement to mark the method by [Handler] or
    /// [Viewer] attribute.
    /// 
    /// After a job record is created, it is detected and scheduled for
    /// execution by the queue specified by the record.Queue element.
    ///
    /// The queue updates the JobProgress record at least every time its
    /// status changes, and optionally more often to update its progress
    /// fraction and progress message. It also monitors the dataset where
    /// it is running for JobCancellation records and writes log entries
    /// to the log specified by the queue.
    ///
    /// To run the job, the queue executes the Run() method of this class
    /// which in turn invokes method with MethodName in the referenced record.
    /// Depending on the type of queue, MethodName may be executed:
    ///
    /// * In a different process or thread than the one that created the job
    /// * On a different machine than the one where the job was created
    /// * In parallel or out of sequence relative to other jobs
    ///
    /// The job submitter must ensure that the specified method will have
    /// access to the resources it needs and will be able to run successfully
    /// in each of these cases.
    /// </summary>
    public sealed class Job : TypedRecord<JobKey, Job>
    {
        /// <summary>Queue to which the job is submitted.</summary>
        [BsonRequired]
        public JobQueueKey Queue { get; set; }

        /// <summary>
        /// Name of the collection where the referenced record is stored.
        ///
        /// Referenced record is the record in this collection whose
        /// TemporalId is RecordId.
        /// </summary>
        [BsonRequired]
        public string CollectionName { get; set; }

        /// <summary>
        /// TemporalId of the referenced record.
        ///
        /// This key is specific to the version of the referenced record.
        /// When a new record is created for the same key, the view will
        /// continue referencing the original version of the record where
        /// Id=RecordId.
        /// </summary>
        public TemporalId RecordId { get; set; }

        /// <summary>
        /// Name of the method of the referenced record executed by the job.
        ///
        /// Referenced record is the record in collection with CollectionName
        /// whose TemporalId is RecordId.
        /// </summary>
        [BsonRequired]
        public string MethodName { get; set; }

        /// <summary>
        /// Invokes method with MethodName in the referenced record.
        ///
        /// Depending on the type of queue, MethodName may be executed:
        ///
        /// * In a different process or thread than the one that created the job
        /// * On a different machine than the one where the job was created
        /// * In parallel or out of sequence relative to other jobs
        ///
        /// The job submitter must ensure that the specified method will have
        /// access to the resources it needs and will be able to run successfully
        /// in each of these cases.
        /// </summary>
        public void Run()
        {
            // TODO Not implemented yet, code below is a stub
            throw new NotImplementedException();

            // Load record by its TemporalId, error message if not found
            var record = Context.DataSource.Load<Record>(RecordId);

            // Get handler method info using string handler name
            var type = record.GetType();
            var methodInfo = type.GetMethod(MethodName);

            // If handler with the name specified in TargetHandler
            // is not found, methodInfo will be null
            if (methodInfo == null)
                throw new Exception($"Handler {MethodName} not found in record type {type.Name}.");

            // Invoke the handler. No parameters are specified
            // and no return value is expected as handler return
            // type is always void.
            methodInfo.Invoke(record, null);
        }
    }
}
