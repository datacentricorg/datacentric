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
    /// <summary>Static helper class for LocalDate.</summary>
    public static class LocalDateImpl
    {
        /// <summary>Default constructed LocalDate is treated as empty.</summary>
        public static LocalDate Empty { get; } = default;

        /// <summary>Strict ISO 8601 date pattern yyyy-mm-dd.</summary>
        public static LocalDatePattern Pattern { get; } = LocalDatePattern.CreateWithInvariantCulture("uuuu'-'MM'-'dd");

        /// <summary>Parse string using standard ISO 8601 date pattern yyyy-mm-dd, throw if invalid format.
        /// No variations from the standard format are accepted and no delimiters can be changed or omitted.
        /// Specifically, ISO int-like string using yyyymmdd format without delimiters is not accepted.</summary>
        public static LocalDate Parse(string value)
        {
            // Parse using ISO 8601 pattern
            var parseResult = Pattern.Parse(value);
            var result = parseResult.GetValueOrThrow();

            // If default constructed date is passed, error message
            if (result == default) throw new Exception(
                $"String representation of default constructed date {value} " +
                $"passed to LocalDate.Parse(date) method.");

            return result;
        }

        /// <summary>Parse ISO 8601 8 digit int in yyyymmdd format, throw if invalid format.</summary>
        public static LocalDate ParseIsoInt(int value)
        {
            // Extract year, month, day
            int year = value / 100_00;
            value -= year * 100_00;
            int month = value / 100;
            value -= month * 100;
            int day = value;

            // Create LocalDate object
            var result = new LocalDate(year, month, day);
            return result;
        }
    }
}
