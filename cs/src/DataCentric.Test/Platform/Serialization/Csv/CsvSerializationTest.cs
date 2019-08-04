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
using Xunit;
using DataCentric;

namespace DataCentric.Test
{
    /// <summary>Unit tests for CsvSerialization.</summary>
    public class CsvSerializationTest
    {
        /// <summary>Serialization with empty cells.</summary>
        [Fact]
        public void EmptyCells()
        {
            using (var context = new UnitTestContext(this))
            {
                CsvWriter writer = new CsvWriter(context);

                // Write complete line
                writer.WriteValue("A11");
                writer.WriteValue("A12");
                writer.WriteLine();

                // Write line step by step
                string[] tokens = new string[] {"A21", "A22", "A23"};
                writer.WriteLine(tokens);

                // Write row with empty trailing tokens
                // which should be ignored
                writer.WriteValue("A31");
                writer.WriteValue(String.Empty);
                writer.WriteValue(String.Empty);
                writer.WriteValue(String.Empty);
                writer.WriteValue(String.Empty);
                writer.WriteLine();

                // Write row with empty token in the midddle
                writer.WriteValue("A41");
                writer.WriteValue(String.Empty);
                writer.WriteValue("A43");
                writer.WriteLine();

                // Write row with leading empty tokens
                writer.WriteValue(String.Empty);
                writer.WriteValue(String.Empty);
                writer.WriteValue("A53");
                writer.WriteLine();

                // Check the row and column count
                int rowCount = writer.RowCount;
                int colCount = writer.ColCount;

                // Save and log the result
                context.Verify.File("Matrix.csv", writer.ToString());
            }
        }

        /// <summary>Serialization with escaped characters such as list separator and quote.</summary>
        [Fact]
        public void EscapedCharacters()
        {
            using (var context = new UnitTestContext(this))
            {
                CsvWriter writer = new CsvWriter(context);

                // Write line with list separator
                writer.WriteValue("A11" + Settings.Default.Locale.ListSeparator + "A11");
                writer.WriteValue("A12");
                writer.WriteLine();

                // Write line with string already surrounded by quotes
                writer.WriteValue("\"A21\"");
                writer.WriteValue("A22");
                writer.WriteLine();

                // Write line with strings which include quotes inside
                // with and without list separator
                writer.WriteValue("A31\"A31\"A31");
                writer.WriteValue("A32\"A32,A32\"A32");
                writer.WriteLine();

                // Save and log the result
                context.Verify.File("Matrix.csv", writer.ToString());
            }
        }

        /// <summary>Test writing CSV matrix.</summary>
        [Fact]
        public void Writing()
        {
            using (var context = new UnitTestContext(this))
            {
                CsvWriter writer = new CsvWriter(context);

                // Write complete line
                writer.WriteValue("A11");
                writer.WriteValue("A12");
                writer.WriteLine();

                // Write line step by step
                string[] tokens = new string[] {"A21", "A22", "A23"};
                writer.WriteLine(tokens);

                // Write row with empty trailing tokens
                // which should be ignored
                writer.WriteValue("A31");
                writer.WriteValue(String.Empty);
                writer.WriteValue(String.Empty);
                writer.WriteValue(String.Empty);
                writer.WriteValue(String.Empty);
                writer.WriteLine();

                // Write row with empty token in the midddle
                writer.WriteValue("A41");
                writer.WriteValue(String.Empty);
                writer.WriteValue("A43");
                writer.WriteLine();

                // Write row with leading empty tokens
                writer.WriteValue(String.Empty);
                writer.WriteValue(String.Empty);
                writer.WriteValue("A53");
                writer.WriteLine();

                // Check the row and column count
                int rowCount = writer.RowCount;
                int colCount = writer.ColCount;

                // Output the result
                string result = writer.ToString();

                // Save and log the result
                context.Verify.File("Matrix.csv", writer.ToString());
            }
        }

        /// <summary>Test parsing-serialization roundtrip.</summary>
        [Fact]
        public void Roundtrip()
        {
            using (var context = new UnitTestContext(this))
            {
                // Parsing to serialization roundtrip for CSV matrix without serialization
                string eol = StringUtils.Eol;
                string csvTableStr = "1,2,3" + eol + "4,5,6" + eol + "7,8,9" + eol;

                // Test roundtrip
                ValueTableData matrix = new ValueTableData();
                matrix.ParseCsv(TableLayout.NoHeaders, AtomicType.Int, csvTableStr);
                string csvTableToString = matrix.ToString();
                context.Verify.Assert(csvTableStr == csvTableToString, "CSV string parsing and serialization roundtrip.");
            }
        }
    }
}
