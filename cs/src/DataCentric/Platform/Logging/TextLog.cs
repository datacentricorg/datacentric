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
        private readonly string indentString_ = new String(' ', 4);
        private readonly string[] lineSeparators_ = new string[] {"\r\n", "\r", "\n"};

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
        /// Record a new entry to the log if log verbosity
        /// is the same or high as entry verbosity.
        ///
        /// In a text log, first line of the message follows
        /// verbosity prefix after semicolon separator. Remaining
        /// lines of the message (if any) are recorded with 4 space
        /// indent, for example:
        ///
        /// Info: Message Line 1
        ///     Message Line 2
        ///     Message Line 3
        /// </summary>
        public override void Entry(LogVerbosity verbosity, string message)
        {
            // Do not record the log entry if entry verbosity exceeds log verbosity
            // Record all entries if log verbosity is not specified
            if (verbosity <= Verbosity)
            {
                var logEntry = new LogEntry(verbosity, message);
                string logString = logEntry.ToString();

                // Split the log string into lines
                string[] logStringLines = logString.Split(lineSeparators_, StringSplitOptions.None);

                int lineCount = logStringLines.Length;
                for (int i = 0; i < lineCount; ++i)
                {
                    string logStringLine = logStringLines[i];
                    if (string.IsNullOrEmpty(logStringLine))
                    {
                        if (i < lineCount - 1)
                        {
                            // Write empty line unless the empty token is last, in which
                            // case it represents the trailing EOL and including it would
                            // create in a trailing empty line not present in the original
                            // log message
                            LogTextWriter.WriteLine();
                        }
                    }
                    else
                    {
                        // Write indent string for all lines except the first.
                        // The first line follows entry type on the same line
                        if (i > 0) LogTextWriter.Write(indentString_);

                        // Write output line
                        LogTextWriter.WriteLine(logStringLine);
                    }
                }
            }
        }

        /// <summary>
        /// Record a new entry to the log if log verbosity
        /// is the same or high as entry verbosity.
        ///
        /// In a text log, first line of the title follows verbosity
        /// prefix after semicolon separator. Remaining lines of the
        /// title (if any) and all lines of the body are recorded
        /// with 4 space indent, for example:
        ///
        /// Info: Title Line 1
        ///     Title Line 2
        ///     Body Line 1
        ///     Body Line 2
        /// </summary>
        public override void Entry(LogVerbosity verbosity, string title, string body)
        {
            // Do not record the log entry if entry verbosity exceeds log verbosity
            // Record all entries if log verbosity is not specified
            if (verbosity <= Verbosity)
            {
                // Message is title followed by line separator and then body
                string message = string.Concat(title, Environment.NewLine, body);
                Entry(verbosity, message);
            }
        }
    }
}
