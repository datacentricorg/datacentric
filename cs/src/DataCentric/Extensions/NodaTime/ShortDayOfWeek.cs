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
    /// <summary>
    /// Equates the days of the week with their numerical value according to
    /// ISO-8601. This corresponds with System.DayOfWeek except for Sunday,
    /// which  is 7 in the ISO numbering and 0 in System.DayOfWeek.
    ///
    /// This class is the same as IsoDayOfWeek except it usees three-letter
    /// abbreviation for the day of week, e.g. Mon, Tue, etc.
    /// </summary>
    public enum ShortDayOfWeek
    {
        /// <summary>
        /// Value indicating no day of the week; this will never be returned
        /// by any IsoDayOfWeek property, and is not valid as an argument to
        /// any method.
        /// </summary>
        Empty,

        /// <summary>Value representing Monday (1).</summary>
        Mon,

        /// <summary>Value representing Tuesday (2).</summary>
        Tue,

        /// <summary>Value representing Wednesday (3).</summary>
        Wed,

        /// <summary>Value representing Thursday (4).</summary>
        Thu,

        /// <summary>Value representing Friday (5).</summary>
        Fri,

        /// <summary>Value representing Saturday (6).</summary>
        Sat,

        /// <summary>Value representing Sunday (7).</summary>
        Sun
    }

    /// <summary>Static and extension methods for ShortDayOfWeek.</summary>
    public static class ShortDayOfWeekEx
    {
        /// <summary>Return false if equal to the default constructed value.</summary>
        public static bool HasValue(this ShortDayOfWeek value)
        {
            return value != default;
        }

        /// <summary>Return false if null or equal to the default constructed value.</summary>
        public static bool HasValue(this ShortDayOfWeek? value)
        {
            return value.HasValue && value.HasValue();
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
