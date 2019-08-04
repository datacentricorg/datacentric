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
using System.IO;
using System.Linq;
using Xunit;
using DataCentric;

namespace DataCentric.Test
{
    /// <summary>Unit tests for TextWriter.</summary>
    public class IndentedTextWriterText
    {
        /// <summary>Writing files to disk storage.</summary>
        [Fact]
        public void Writing()
        {
            using (var context = new UnitTestContext(this))
            {
                // Create text writer
                var fileNames = new string[]
                {
                    "FileWithoutExtension",
                    "FileWithStandardExtension.txt",
                    "FileWithNonStandardExtension.text"
                };

                foreach (var fileName in fileNames)
                {
                    TextWriter writer = context.Out.CreateTextWriter(fileName, FileWriteMode.Replace);
                    writer.WriteLine("Sample line");
                    writer.Flush();
                }

                context.Verify.Text("Completed");
            }
        }
    }
}
