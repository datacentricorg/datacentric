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

        //--- METHODS

        /// <summary>
        /// Releases resources and calls base.Dispose().
        ///
        /// This method will not be called by the garbage collector.
        /// It will only be executed if:
        ///
        /// * This class implements IDisposable; and
        /// * The class instance is created through the using clause
        ///
        /// IMPORTANT - Every override of this method must call base.Dispose()
        /// after executing its own code.
        /// </summary>
        public override void Dispose()
        {
            stringWriter_.Close();
            stringWriter_.Dispose();

            // Dispose base
            base.Dispose();
        }

        /// <summary>Flush data to permanent storage.</summary>
        public override void Flush()
        {
            stringWriter_.Flush();
        }

        /// <summary>
        /// Append new entry to the log unless entry verbosity exceeds log verbosity.
        /// </summary>
        public override void Entry(LogVerbosity verbosity, string entrySubType, string message)
        {
            // Do not record the log entry if entry verbosity exceeds log verbosity
            // Record all entries if log verbosity is not specified
            if (verbosity <= Verbosity)
            {
                var logEntry = new LogEntry(verbosity, entrySubType, message);
                string logString = logEntry.ToString();
                stringWriter_.WriteLine(logString);
            }
        }

        /// <summary>Return multi-line log text as string.</summary>
        public override string ToString()
        {
            return stringWriter_.ToString();
        }
    }
}
