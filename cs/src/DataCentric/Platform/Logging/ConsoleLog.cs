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
using System.Diagnostics;
using System.Text;

namespace DataCentric
{
    /// <summary>Logging to system console.</summary>
    public class ConsoleLog : Log
    {
        /// <summary>Flush data to permanent storage.</summary>
        public override void Flush()
        {
            // Do nothing as system console does not require buffer flush
        }

        /// <summary>Append new entry to the log if entry type is the same or lower than log verbosity.
        /// Entry subtype is an optional tag in dot delimited format (specify null if no subtype).</summary>
        public override void Append(LogEntryType entryType, string entrySubType, string message)
        {
            // Do not record the log entry if entry verbosity exceeds log verbosity
            // Record all entries if log verbosity is not specified
            if (entryType <= Verbosity)
            {
                var logEntry = new LogEntry(LogEntryType.Status, entrySubType, message);
                Console.WriteLine(logEntry.ToString());
            }
        }
    }
}
