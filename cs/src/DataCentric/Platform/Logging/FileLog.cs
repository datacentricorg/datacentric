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
    /// <summary>Writes log output to the specified text file as it arrives.</summary>
    public class FileLog : Log
    {
        private IndentedTextWriter indentedTextWriter_;

        //--- PROPERTIES

        /// <summary>Log file path relative to output folder root.</summary>
        public string LogFilePath { get; set; }

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

            // Create text writer for the file, then wrap it into
            // an indented writer using 4 space indent string
            var textWriter = context.OutputFolder.GetTextWriter(LogFilePath, FileWriteMode.Replace);
            indentedTextWriter_ = new IndentedTextWriter(textWriter, "    ");
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
            indentedTextWriter_.Close();
            indentedTextWriter_.Dispose();

            // Dispose base
            base.Dispose();
        }

        /// <summary>Flush data to permanent storage.</summary>
        public override void Flush()
        {
            indentedTextWriter_.Flush();
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

                // Set indent to the current indent of the log before writing the log entry string
                indentedTextWriter_.Indent = Indent;
                indentedTextWriter_.WriteLine(logString);
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

                // Set indent to the current indent of the log
                // before writing the title string
                indentedTextWriter_.Indent = Indent;
                indentedTextWriter_.WriteLine(logTitleString);

                // Increment indent by one tab stop before writing the body
                Indent++;
                indentedTextWriter_.Indent = Indent;

                indentedTextWriter_.WriteLine(body);

                // Restore the previous value
                Indent++;
                indentedTextWriter_.Indent = Indent;
            }
        }
    }
}
