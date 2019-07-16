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

namespace DataCentric
{
    /// <summary>
    /// Specifies the action requested by the interrupt record.
    ///
    /// The queue to which the job is submitted continuously
    /// monitors the dataset where it is running for interrupt
    /// records. Once an interrupt record is detected, the action
    /// specified by this enum is performed for job specified by
    /// the interrupt record, and job status is updated accordingly.
    /// </summary>
    public enum JobInterruptType
    {
        /// <summary>
        /// Indicates that value is not set.
        /// </summary>
        Empty,

        /// <summary>
        /// Request to cancel the job.
        ///
        /// After an interrupt record with Cancel interrupt type is detected
        /// by the queue, the job is given 10 seconds to shut down gracefully
        /// (soft cancellation); after that time it is terminated by the queue
        /// (hard cancellation). The code should be written defensively to
        /// continue without error in case of either soft or hard cancellation.
        /// </summary>
        Cancel
    }
}
