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
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DataCentric
{
    /// <summary>Accumulates log data in memory as it arrives and provides
    /// the possibility to output it as multi-line text string.</summary>
    public class InMemoryLog : Log
    {
        private TextWriter stringWriter_ = new StringWriter();

        /// <summary>Create from the execution context and folder path, without a linked log.\\
        /// Accepts path with regular path separator or in dot delimited (``namespace'') format.</summary>
        public InMemoryLog(IContext context)
        {
            Init(context);
        }

        //--- METHODS

        /// <summary>Flush data to permanent storage.</summary>
        public override void Flush()
        {
            stringWriter_.Flush();
        }

        /// <summary>Append new entry to the log if entry type is the same or lower than log verbosity.
        /// Entry subtype is an optional tag in dot delimited format (specify null if no subtype).</summary>
        public override void Append(LogEntryType entryType, string entrySubType, string message, params object[] messageParams)
        {
            // Do not record the log entry if entry verbosity exceeds log verbosity
            // Record all entries if log verbosity is not specified
            if (Verbosity == LogEntryType.Empty || entryType <= Verbosity)
            {
                var logEntry = new LogEntry(entryType, entrySubType, message, messageParams);
                string logString = logEntry.ToString();
                stringWriter_.WriteLine(logString);
            }
        }

        /// <summary>Return multi-line log text as string.</summary>
        public override string ToString()
        {
            return stringWriter_.ToString();
        }

        /// <summary>Close log and release handle to permanent storage.</summary>
        public override void Close()
        {
            // Do nothing as string writer does not require closing the connection
        }
    }
}
