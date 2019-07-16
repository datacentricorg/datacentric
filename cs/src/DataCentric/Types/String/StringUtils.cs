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

namespace DataCentric
{
    /// <summary>Static helper class for String.</summary>
    public static class StringUtils
    {
        /// <summary>Constant representing the OS-specific end of line (newline) character.</summary>
        public static string Eol { get { return Environment.NewLine; } }

        /// <summary>Returns the specified number of OS-specific end of line (newline) characters.</summary>
        public static string Eols(int count)
        {
            StringBuilder result = new StringBuilder();
            for (int i = 0; i < count; ++i) result.Append(Eol);
            return result.ToString();
        }
    }
}
