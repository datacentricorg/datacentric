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
using NodaTime;
using DataCentric;

namespace DataCentric.Test
{
    /// <summary>Unit tests for LocalDate.</summary>
    public class LocalDateTest
    {
        /// <summary>Test roundtrip serialization.</summary>
        [Fact]
        public void Roundtrip()
        {
            using (IUnitTestContext context = new UnitTestContext(this))
            {
                VerifyRoundtrip(context, new LocalDate(2003, 5, 1));
            }
        }

        private void VerifyRoundtrip(IUnitTestContext context, LocalDate value)
        {
            // Verify string serialization roundtrip
            string stringValue = value.AsString();
            LocalDate parsedStringValue = LocalDateUtils.Parse(stringValue);
            Assert.Equal(value, parsedStringValue);
            context.Verify.Text($"ISO 8601 format: {stringValue}");

            // Verify int serialization roundtrip
            int intValue = value.ToIsoInt();
            LocalDate parsedIntValue = LocalDateUtils.ParseIsoInt(intValue);
            Assert.Equal(value, parsedIntValue);
            context.Verify.Text($"Readable int format: {intValue}");
        }
    }
}
