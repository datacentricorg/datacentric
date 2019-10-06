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
using System.CodeDom.Compiler;
using System.IO;

namespace DataCentric
{
    /// <summary>
    /// Provides support for text log output in:
    ///
    /// * ConsoleLog
    /// * StringLog
    /// * FileLog
    /// </summary>
    public abstract class TextLog : Log
    {
        private const int indentSize_ = 4;

        //--- PROPERTIES

        /// <summary>
        /// Text writer
        /// </summary>
        protected TextWriter LogTextWriter { private get; set; }

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
            LogTextWriter.Close();
            LogTextWriter.Dispose();

            // Dispose base
            base.Dispose();
        }

        /// <summary>Flush data to permanent storage.</summary>
        public override void Flush()
        {
            LogTextWriter.Flush();
        }

        /// <summary>
        /// Append a new single-line entry to the log.
        ///
        /// This method has no effect unless entry verbosity
        /// exceeds log verbosity.
        /// </summary>
        public override void Entry(LogVerbosity verbosity, string message)
        {
            // Do not record the log entry if entry verbosity exceeds log verbosity
            // Record all entries if log verbosity is not specified
            if (verbosity <= Verbosity)
            {
                var logEntry = new LogEntry(verbosity, message);
                string logString = logEntry.ToString();

                // Do not write indent for a single-line entry
                LogTextWriter.WriteLine(logString);
            }
        }

        /// <summary>
        /// Append a new entry to the log that has single-line title
        /// and multi-line body. The body will be indented by one
        /// tab stop.
        ///
        /// This method has no effect unless entry verbosity
        /// exceeds log verbosity. 
        /// </summary>
        public override void Entry(LogVerbosity verbosity, string title, string body)
        {
            // Do not record the log entry if entry verbosity exceeds log verbosity
            // Record all entries if log verbosity is not specified
            if (verbosity <= Verbosity)
            {
                var logTitleEntry = new LogEntry(verbosity, title);
                string logTitleString = logTitleEntry.ToString();

                // Do not write indent for the title
                LogTextWriter.WriteLine(logTitleString);

                // Create an indented writer for long entry body and set indent size
                var indentedWriter = new IndentedTextWriter(LogTextWriter, new String(' ', indentSize_));

                // Increment indent by one tab stop before writing the body
                indentedWriter.Indent++;
                indentedWriter.WriteLine(body);
            }
        }
    }
}
