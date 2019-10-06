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
    /// <summary>Enumeration for the log entry type.</summary>
    public enum LogEntryType
    {
        /// <summary>
        /// Error is the default entry type, used when entry type is not set.
        ///
        /// Because this is the latest value in the enumeration,  errors are
        /// reported irrespective of the verbosity level.
        /// </summary>
        Error,

        /// <summary>Warning message.</summary>
        Warning,

        /// <summary>Status message.</summary>
        Status,

        /// <summary>Progress ratio or message.</summary>
        Progress,

        /// <summary>Approval test output.</summary>
        Verify
    }
}
