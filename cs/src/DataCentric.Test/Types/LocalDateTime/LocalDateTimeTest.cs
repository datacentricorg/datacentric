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
using NodaTime;

namespace DataCentric.Test
{
    /// <summary>Unit tests for LocalDateTime.</summary>
    public class LocalDateTimeTest
    {
        /// <summary>Test roundtrip serialization.</summary>
        [Fact]
        public void Roundtrip()
        {
            using (var context = new UnitTestContext(this))
            {
                VerifyRoundtrip(context, new LocalDateTime(2003, 5, 1, 10, 15, 30));
                VerifyRoundtrip(context, new LocalDateTime(2003, 5, 1, 10, 15, 30, 5));
            }
        }

        private void VerifyRoundtrip(IUnitTestContext context, LocalDateTime value)
        {
            // To be used in assert message
            string nameAsString = value.AsString();

            // Verify string serialization roundtrip
            string stringValue = value.AsString();
            LocalDateTime parsedStringValue = LocalDateTimeUtils.Parse(stringValue);
            context.Verify.Assert(value == parsedStringValue, $"String roundtrip for {nameAsString}");

            // Verify long serialization roundtrip
            long longValue = value.ToIsoLong();
            LocalDateTime parsedLongValue = LocalDateTimeUtils.ParseIsoLong(longValue);
            context.Verify.Assert(value == parsedLongValue, $"Long roundtrip for {nameAsString}");
        }
    }
}
