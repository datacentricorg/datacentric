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
using System.ComponentModel.Design;
using MongoDB.Bson.Serialization.Attributes;
using NodaTime;
using Xunit;
using DataCentric;

namespace DataCentric.Test
{
    /// <summary>Unit test for JobQueue.</summary>
    public class JobQueueTest
    {
        public class SampleJobData : JobData
        {
            /// <summary>
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
                Context.CastTo<IUnitTestContext>().Verify.Text("Running");
            }
        }

        /// <summary>Test saving and loading back the job queue record using ObjectId based key.</summary>
        [Fact]
        public void Load()
        {
            using (IDataTestContext context = new DataTestContext(this))
            {
                context.KeepDb = true;

                // Create queue record and save, then get its ID
                var queue = new JobQueueData();
                context.Save(queue, context.DataSet);
                var queueId = queue.ID;

                // Create job record and save, then get its ID
                var job = new SampleJobData();
                job.Queue = queue.ToKey();
                context.Save(job, context.DataSet);
                var jobID = job.ID;

                // Load the records back
                var queueKey = new JobQueueKey {ID = queueId};
                var loadedQueue = queueKey.Load(context);
                var jobKey = new JobKey { ID = jobID };
                var loadedJob = jobKey.Load(context);

                // Check that ObjectId based key works correctly
                Assert.True(loadedJob.Queue.Value == loadedQueue.ToKey().Value);

                // Run the job
                loadedJob.Run();

                context.Verify.Text("Completed");
            }
        }
    }
}
