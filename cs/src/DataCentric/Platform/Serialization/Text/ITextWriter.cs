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
using System.Xml;

namespace DataCentric
{
    /// <summary>Interface used to write multi-line text.</summary>
    public interface ITextWriter : IDisposable
    {
        /// <summary>Write message with optional parameters to the output.
        /// Uses the same semantics with {N} tags for parameters as String.Format.</summary>
        void Write(string message, params object[] messageParams);

        /// <summary>Write message with optional parameters, followed by end of line, to the output.
        /// Uses the same semantics with {N} tags for parameters as String.Format.</summary>
        void WriteLine(string message, params object[] messageParams);

        /// <summary>Write EOL and reset indent.</summary>
        void WriteLine();

        /// <summary>Increase indent for subsequent lines
        /// by the specified number of stops.</summary>
        void IncreaseIndent(int stops);

        /// <summary>Decrease indent for subsequent lines
        /// by the specified number of stops.</summary>
        void DecreaseIndent(int stops);

        /// <summary>Reset indent for subsequent lines to zero stops.</summary>
        void ResetIndent();

        /// <summary>Flush buffer to permanent storage.
        /// This method does not reset indent.</summary>
        void Flush();

        /// <summary>Close the stream and release resources.
        /// After calling this method, any further calls to other methods
        /// must throw an exception.</summary>
        void Close();
    }
}
