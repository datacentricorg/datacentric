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
using System.IO;
using System.Linq.Expressions;
using System.Text;

namespace DataCentric
{
    /// <summary>
    /// Accumulates log output in memory and converts it
    /// to string using ToString() method.
    /// </summary>
    public class StringLog : ILog
    {
        private TextWriter textWriter_;

        /// <summary>Create in-memory log for use in approval tests</summary>
        public StringLog(IContext context)
        {
            Context = context;
            textWriter_ = new StringWriter();
        }

        /// <summary>Context for which this interface is defined.
        /// Use to access other interfaces of the same context.</summary>
        public IContext Context { get; }

        /// <summary>Log verbosity is the highest log entry type displayed.
        /// Verbosity can be modified at runtime to provide different levels of
        /// verbosity for different code segments.</summary>
        public LogEntryType Verbosity { get; set; }

        /// <summary>Append new entry to the log if entry type is the same or lower than log verbosity.
        /// Entry subtype is an optional tag in dot delimited format (specify null if no subtype).</summary>
        public void Append(LogEntryType entryType, string entrySubType, string message, params object[] messageParams)
        {
            // Do not record the log entry if entry verbosity exceeds log verbosity
            // Record all entries if log verbosity is not specified
            if (Verbosity == LogEntryType.Empty || entryType <= Verbosity)
            {
                var logEntry = new LogEntry(entryType, entrySubType, message, messageParams);
                string logString = logEntry.ToString();
                textWriter_.WriteLine(logString);
            }
        }

        /// <summary>Flush log contents to permanent storage.</summary>
        public void Flush()
        {
            textWriter_.Flush();
        }

        /// <summary>Close log and release handle to permanent storage.</summary>
        public void Close()
        {
            // Do nothing
        }

        /// <summary>Convert log output to string.</summary>
        public override string ToString()
        {
            return textWriter_.ToString();
        }
    }
}
