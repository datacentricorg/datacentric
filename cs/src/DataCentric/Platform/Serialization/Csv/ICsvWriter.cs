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

namespace DataCentric
{
    /// <summary>Provides functionality for writing multi-line CSV data.</summary>
    public interface ICsvWriter
    {
        /// <summary>(ICsvWriter) Number of lines written so far.</summary>
        int RowCount { get; }

        /// <summary>(ICsvWriter) Number of values in the longest row written so far.</summary>
        int ColCount { get; }

        /// <summary>(ICsvWriter) Write a single value escaping the list delimiter and double quotes.</summary>
        void WriteValue(string token);

        /// <summary>(ICsvWriter) Write multiple tokens escaping the list delimiter
        /// and double quotes into the current line, then advance to next line.</summary>
        void WriteLine(IEnumerable<string> tokens);

        /// <summary>(ICsvWriter) Advance to next line.</summary>
        void WriteLine();
    }
}
