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
    /// <summary>Extension methods for System.DateTime.</summary>
    public static class DateTimeEx
    {
        /// <summary>Return false if equal to the default constructed value.</summary>
        public static bool HasValue(this DateTime value)
        {
            return value != default;
        }

        /// <summary>Return false if null or equal to the default constructed value.</summary>
        public static bool HasValue(this DateTime? value)
        {
            return value.HasValue && value.HasValue();
        }

        /// <summary>
        /// Error message if equal to the default constructed value or if \texttt{Kind != Utc}.
        /// </summary>
        public static void CheckHasValue(this DateTime value)
        {
            if (!value.HasValue()) throw new Exception("Required DateTime value is not set.");
            if (value.Kind != DateTimeKind.Utc) throw new Exception("DateTime.Kind is not UTC.");
        }

        /// <summary>
        /// Error message if null or equal to the default constructed value or if \texttt{Kind != Utc}.
        /// </summary>
        public static void CheckHasValue(this DateTime? value)
        {
            if (!value.HasValue()) throw new Exception("Required DateTime value is not set.");
            if (value.Value.Kind != DateTimeKind.Utc) throw new Exception("DateTime.Kind is not UTC.");
        }

        /// <summary>
        /// Convert System.DateTime with Kind=Utc to LocalDateTime.
        ///
        /// Error message if equal to the default constructed value.
        /// </summary>
        public static LocalDateTime ToLocalDateTime(this DateTime value)
        {
            value.CheckHasValue();
            return LocalDateTime.FromDateTime(value);
        }

        /// <summary>
        /// Convert System.DateTime with Kind=Utc to LocalDateTime if set and null otherwise.
        ///
        /// Return null if equal to the default constructed value.
        /// </summary>
        public static LocalDateTime? ToLocalDateTime(this DateTime? value)
        {
            if (value.HasValue) return value.Value.ToLocalDateTime();
            else return null;
        }
    }
}
