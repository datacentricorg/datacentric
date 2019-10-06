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
    public class StringLog : Log
    {
        private TextWriter textWriter_;

        //--- METHODS

        /// <summary>
        /// Set Context property and perform validation of the record's data,
        /// then initialize any fields or properties that depend on that data.
        ///
        /// This method may be called multiple times for the same instance,
        /// possibly with a different context parameter for each subsequent call.
        ///
        /// IMPORTANT - Every override of this method must call base.Init()
        /// first, and only then execute the rest of the override method's code.
        /// </summary>
        public override void Init(IContext context)
        {
            // Initialize base
            base.Init(context);

            textWriter_ = new StringWriter();
        }

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
            textWriter_.Close();
            textWriter_.Dispose();

            // Dispose base
            base.Dispose();
        }

        /// <summary>Flush data to permanent storage.</summary>
        public override void Flush()
        {
            textWriter_.Flush();
        }

        /// <summary>
        /// Append new entry to the log unless entry verbosity exceeds log verbosity.
        /// </summary>
        public override void Entry(LogVerbosity verbosity, string message)
        {
            // Do not record the log entry if entry verbosity exceeds log verbosity
            // Record all entries if log verbosity is not specified
            if (verbosity <= Verbosity)
            {
                var logEntry = new LogEntry(verbosity, message);
                string logString = logEntry.ToString();
                textWriter_.WriteLine(logString);
            }
        }

        /// <summary>Convert log output to string.</summary>
        public override string ToString()
        {
            return textWriter_.ToString();
        }
    }
}
