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
using System.Text;

namespace DataCentric
{
    /// <summary>Standard implementation of ICsvWriter.</summary>
    public class CsvWriter : ICsvWriter
    {
        private List<List<string>> rows_;
        private List<string> currentRow_;
        int colCount_;

        /// <summary>Create with empty document.</summary>
        public CsvWriter(IContext context)
        {
            // Create collection objects
            rows_ = new List<List<string>>();
            currentRow_ = null;

            // Set initial number of rows and columns
            colCount_ = 0;
        }

        /// <summary>(ICsvWriter) Number of lines written so far.</summary>
        public int RowCount { get { return rows_.Count; } }

        /// <summary>(ICsvWriter) Number of values in the longest row written so far.</summary>
        public int ColCount { get { return colCount_; } }

        /// <summary>(ICsvWriter) Write a single value escaping the list delimiter and double quotes.</summary>
        public void WriteValue(string token)
        {
            // Create row if not currently open
            if (currentRow_ == null)
            {
                currentRow_ = new List<string>();
                rows_.Add(currentRow_);
            }

            // Append token with escaped delimiter and quotes
            string escapedToken = CsvUtil.EscapeListSeparator(token);
            currentRow_.Add(escapedToken);

            // Increment col count if less than current row length
            if (colCount_ < currentRow_.Count) colCount_ = currentRow_.Count;
        }

        /// <summary>(ICsvWriter) Write multiple tokens escaping the list delimiter
        /// and double quotes into the current line, then advance to next line.</summary>
        public void WriteLine(IEnumerable<string> tokens)
        {
            foreach(string token in tokens)
            {
                WriteValue(token);
            }
            WriteLine();
        }

        /// <summary>(ICsvWriter) Advance to next line.</summary>
        public void WriteLine()
        {
            if (currentRow_ != null)
            {
                // Setting current row to null will cause a new row
                // to be created next time a token is added. The
                // current row is already added to the rows_ list.
                currentRow_ = null;
            }
            else
            {
                // Otherwise add empty row
                rows_.Add(new List<string>());
            }
        }

        /// <summary>Convert to string without checking that the document is complete.
        /// This permits the use of this method to inspect the document content during creation.
        /// Jagged rows are padded with empty strings until all rows have the same length.</summary>
        public override string ToString()
        {
            StringBuilder result = new StringBuilder();
            foreach (var row in rows_)
            {
                int pos = 0;
                int skippedEmptyTokenCount = 0;
                foreach (string token in row)
                {
                    if (string.IsNullOrEmpty(token))
                    {
                        // If token is null or empty, increment empty token count instead of adding
                        skippedEmptyTokenCount++;
                    }
                    else
                    {
                        // If the current token is not null, write the
                        // previously skipped empty tokens first
                        for (int i = 0; i < skippedEmptyTokenCount; ++i)
                        {
                            if (pos++ > 0) result.Append( Settings.Default.Locale.ListSeparator);
                        }

                        // Then write the current token
                        if (pos++ > 0) result.Append( Settings.Default.Locale.ListSeparator);
                        result.Append(token);
                    }
                }
                result.AppendLine();
            }
            return result.ToString();
        }
    }
}
