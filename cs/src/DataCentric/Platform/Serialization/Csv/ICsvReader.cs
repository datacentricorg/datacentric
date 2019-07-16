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

namespace DataCentric
{
    /// <summary>Provides functionality for reading multi-line CSV data.</summary>
    public interface ICsvReader
    {
        /// <summary>(ICsvReader) Returns true at the end of line.</summary>
        bool IsEol { get; }

        /// <summary>(ICsvReader) Returns true at the end of file.</summary>
        bool IsEof { get; }

        /// <summary>(ICsvReader) Read remaining tokens to the end of line and advance to next line.\\
        /// Returns empty list at the end of line, and error message at the end of file.</summary>
        string[] ReadLine();

        /// <summary>(ICsvReader) Read next token (error message at the end of line or file).</summary>
        string ReadValue();
    }
}
