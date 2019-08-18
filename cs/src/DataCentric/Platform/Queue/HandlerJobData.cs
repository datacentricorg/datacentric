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
using MongoDB.Bson.Serialization.Attributes;

namespace DataCentric
{
    /// <summary>
    /// Executes the specified handler when Run() is invoked.
    /// </summary>
    public class HandlerJobData : JobData
    {
        /// <summary>
        /// ObjectId of the record whose handler will be called
        /// by the Run() method of this job.
        /// </summary>
        [BsonRequired]
        public ObjectId TargetId { get; set; }

        /// <summary>
        /// Name of the handler method that will be called by
        /// the Run() method of this job.
        /// </summary>
        [BsonRequired]
        public string TargetHandler { get; set; }

        //--- METHODS

        /// <summary>
        /// Executes the specified handler.
        /// 
        /// This method is executed by the queue to run the job.
        /// Depending on the type of queue, it may be
        ///
        /// * Executed in a different process or thread
        /// * Executed on a different machine
        /// * Executed in parallel or out of sequence
        ///  
        /// This method should be implemented defensively to
        /// ensure that the job runs successfully in all of
        /// these cases.
        /// </summary>
        public override void Run()
        {
            // Load record by its ObjectId, error message if not found
            var record = Context.DataSource.Load<RecordBase>(TargetId);

            // Get handler method info using string handler name 
            var type = record.GetType();
            var methodInfo = type.GetMethod(TargetHandler);

            // If handler with the name specified in TargetHandler
            // is not found, methodInfo will be null
            if (methodInfo == null)
                throw new Exception($"Handler {TargetHandler} not found in record type {type.Name}.");

            // Invoke the handler. No parameters are specified
            // and no return value is expected as handler return
            // type is always void.
            methodInfo.Invoke(record, null);
        }
    }
}
