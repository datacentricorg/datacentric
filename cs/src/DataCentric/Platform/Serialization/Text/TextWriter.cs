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
using System.Text;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Reflection;

namespace DataCentric
{
    /// <summary>
    /// Implementation of ITextWriter that manages test indentation.
    ///
    /// When destination is not specified, System.Console.Out is used.
    /// </summary>
    public class CustomTextWriter : ITextWriter, IDisposable
    {
        private IContext context_;
        private TextWriter output_;
        private bool continuingLine_ = false;
        private int indent_ = 0;
        private const string singleIndentStop_ = "    ";

        //--- CONSTRUCTORS

        /// <summary>Create with the specified instance of System.IO.TextWriter as destination.</summary>
        public CustomTextWriter(IContext context, TextWriter output)
        {
            context_ = context;
            output_ = output;
        }

        /// <summary>Create with the specified instance of System.IO.Stream as destination.</summary>
        public CustomTextWriter(IContext context, Stream output)
        {
            context_ = context;
            output_ = new StreamWriter(output);
        }

        //--- METHODS

        /// <summary>
        /// Releases resources and calls base.Dispose().
        ///
        /// This method will NOT be called by the garbage
        /// collector, therefore instantiating it inside
        /// the ``using'' clause is essential to ensure
        /// that Dispose() method gets invoked.
        ///
        /// ATTENTION:
        ///
        /// Each class that overrides this method must
        ///
        /// (a) Specify IDisposable in interface list; and
        /// (b) Call base.Dispose() at the end of its own
        ///     Dispose() method.
        /// </summary>
        public virtual void Dispose()
        {
            // Flush all data to permanent storage
            Flush();

            // ose output stream
            output_.Close();

            // Dispose base
            // base.Dispose();
        }

        /// <summary>Write message with optional parameters to the output.
        /// Uses the same semantics with {N} tags for parameters as String.Format.</summary>
        public void Write(string message, params object[] messageParams)
        {
            // Write indent only when not continuing a line
            if (!continuingLine_)
            {
                for(int i = 0; i < indent_; i++) output_.Write(singleIndentStop_);
            }

            if (messageParams.Length > 0)
            {
                // Write message with parameter substitution if parameters are passed
                output_.Write(message, messageParams);
            }
            else
            {
                // Do not perform substitution if no parameters are specified
                // This will work even if the string contains {} characters
                output_.Write(message);
            }

            // Setting this flag prevents indent to be written inside the line
            continuingLine_ = true;
        }

        /// <summary>Write message with optional parameters, followed by end of line, to the output.
        /// Uses the same semantics with {N} tags for parameters as String.Format.</summary>
        public void WriteLine(string message, params object[] messageParams)
        {
            // Write message and then EOL using methods of the same class
            Write(message, messageParams);
            WriteLine();
        }

        /// <summary>Write EOL and reset indent.</summary>
        public void WriteLine()
        {
            output_.WriteLine();

            // earing this flag will cause indent to be written on the next line
            continuingLine_ = false;
        }

        /// <summary>Increase indent for subsequent lines
        /// by the specified number of stops.</summary>
        public void IncreaseIndent(int stops)
        {
            indent_ += stops;
        }

        /// <summary>Decrease indent for subsequent lines
        /// by the specified number of stops.</summary>
        public void DecreaseIndent(int stops)
        {
            indent_ -= stops;
            if (indent_ < 0) context_.Log.Error("DecreaseIndent call makes indent negative.");
        }

        /// <summary>Reset indent for subsequent lines to zero stops.</summary>
        public void ResetIndent()
        {
            indent_ = 0;
        }

        /// <summary>Flush buffer to permanent storage.
        /// This method does not reset indent.</summary>
        public void Flush()
        {
            output_.Flush();
        }

        /// <summary>Close the stream and release resources.
        /// After calling this method, any further calls to other methods
        /// must throw an exception.</summary>
        public void Close()
        {
            output_.Close();
            output_ = null;
        }
    }
}
