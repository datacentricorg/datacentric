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
    /// Executes the specified viewer when Run() is invoked
    /// and records viewer output in ViewerOutputData record.
    /// </summary>
    public class ViewerJobData : JobData
    {
        /// <summary>
        /// Executes the specified viewer when Run() is invoked
        /// and records viewer output in ViewerOutput record.
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
            throw new NotImplementedException();
        }
    }
}
