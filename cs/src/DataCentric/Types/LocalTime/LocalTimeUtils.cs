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
using NodaTime;
using NodaTime.Text;

namespace DataCentric
{
    /// <summary>
    /// Static helper class for LocalTime.
    ///
    /// Unlike LocalDate and LocalDateTime, the LocalTime class
    /// has no special value that can be treated as Empty.
    /// Its default constructed value is 00:00 (midnight).
    /// </summary>
    public static class LocalTimeUtils
    {
        /// <summary>Strict ISO 8601 time pattern with fractional seconds to millisecond precision.</summary>
        public static LocalTimePattern Pattern { get; } = LocalTimePattern.CreateWithInvariantCulture("HH':'mm':'ss.FFF");

        /// <summary>Parse string using standard ISO 8601 time pattern hh:mm:ss.fff, throw if invalid format.
        /// No variations from the standard format are accepted and no delimiters can be changed or omitted.
        /// Specifically, ISO int-like string using hhmmssfff format without delimiters is not accepted.</summary>
        public static LocalTime Parse(string value)
        {
            var parseResult = Pattern.Parse(value);
            var result = parseResult.GetValueOrThrow();
            return result;
        }

        /// <summary>Parse ISO 8601 9 digit int in hhmmssfff format, throw if invalid format.</summary>
        public static LocalTime ParseIsoInt(int value)
        {
            // Extract hour, minute, second, and millisecond
            int hour = value / 100_00_000;
            value -= hour * 100_00_000;
            int minute = value / 100_000;
            value -= minute * 100_000;
            int second = value / 1000;
            value -= second * 1000;
            int millisecond = value;

            // Create new LocalTime object, validates values on input
            var result = new LocalTime(hour, minute, second, millisecond);
            return result;
        }
    }
}
