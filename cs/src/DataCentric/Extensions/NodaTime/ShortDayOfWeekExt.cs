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

namespace DataCentric
{
    /// <summary>Static and extension methods for ShortDayOfWeek.</summary>
    public static class ShortDayOfWeekExt
    {
        /// <summary>Return false if equal to the default constructed value.</summary>
        public static bool HasValue(this ShortDayOfWeek value)
        {
            return value != default;
        }

        /// <summary>Return false if null or equal to the default constructed value.</summary>
        public static bool HasValue(this ShortDayOfWeek? value)
        {
            return value.HasValue && value.Value.HasValue();
        }

        /// <summary>Error message if equal to the default constructed value.</summary>
        public static void CheckHasValue(this ShortDayOfWeek value)
        {
            if (!value.HasValue()) throw new Exception("Required ShortDayOfWeek value is not set.");
        }

        /// <summary>Error message if null or equal to the default constructed value.</summary>
        public static void CheckHasValue(this ShortDayOfWeek? value)
        {
            if (!value.HasValue()) throw new Exception("Required ShortDayOfWeek value is not set.");
        }

        /// <summary>
        /// Convert to IsoDayOfWeek enum that uses full names for the days of week
        /// rather than the three-letter abbreviation used by ShortDayOfWeek.
        /// </summary>
        public static IsoDayOfWeek ToIsoDayOfWeek(this ShortDayOfWeek value)
        {
            return (IsoDayOfWeek) value;
        }

        /// <summary>
        /// Convert to IsoDayOfWeek enum that uses full names for the days of week
        /// rather than the three-letter abbreviation used by ShortDayOfWeek.
        /// </summary>
        public static IsoDayOfWeek? ToIsoDayOfWeek(this ShortDayOfWeek? value)
        {
            if (value != null) return (IsoDayOfWeek) value;
            else return null;
        }
    }
}
