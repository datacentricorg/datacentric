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
using MongoDB.Bson;
using NodaTime;

namespace DataCentric
{
    /// <summary>Extension methods for NodaTime.LocalDateTime.</summary>
    public static class LocalDateTimeEx
    {
        /// <summary>Return false if equal to the default constructed value.</summary>
        public static bool HasValue(this LocalDateTime value)
        {
            return value != default;
        }

        /// <summary>Return false if null or equal to the default constructed value.</summary>
        public static bool HasValue(this LocalDateTime? value)
        {
            return value.HasValue && value.HasValue();
        }

        /// <summary>Error message if equal to the default constructed value.</summary>
        public static void CheckHasValue(this LocalDateTime value)
        {
            if (!value.HasValue()) throw new Exception("Required datetime value is not set.");
        }

        /// <summary>Error message if null or equal to the default constructed value.</summary>
        public static void CheckHasValue(this LocalDateTime? value)
        {
            if (!value.HasValue()) throw new Exception("Required datetime value is not set.");
        }

        /// <summary>
        /// Convert LocalDateTime to ISO 8601 8 digit long in yyyymmddhhmmssfff format.
        ///
        /// Error message if equal to the default constructed value.
        /// </summary>
        public static long ToIsoLong(this LocalDateTime value)
        {
            // If default constructed datetime is passed, error message
            if (value == LocalDateTimeUtils.Empty) throw new Exception(
                $"Default constructed (empty) LocalDateTime {value} has been passed to ToIsoLong() method.");

            // LocalDateTime is serialized as readable ISO int64 in yyyymmddhhmmsssss format
            int isoDate = value.Year * 10_000 + value.Month * 100 + value.Day;
            int isoTime = value.Hour * 100_00_000 + value.Minute * 100_000 + value.Second * 1000 + value.Millisecond;
            long result = ((long)isoDate) * 100_00_00_000 + (long)isoTime;
            return result;
        }

        /// <summary>
        /// Convert LocalDateTime to ISO 8601 8 digit long in yyyymmddhhmmssfff format.
        ///
        /// Return null if equal to the default constructed value.
        /// </summary>
        public static long? ToIsoLong(this LocalDateTime? value)
        {
            if (value.HasValue) return value.Value.ToIsoLong();
            else return null;
        }

        /// <summary>
        /// Convert LocalDate to ISO 8601 string in yyyy-mm-ddThh:mm::ss.fff format.
        ///
        /// Error message if equal to the default constructed value.
        /// </summary>
        public static string ToIsoString(this LocalDateTime value)
        {
            // If default constructed datetime is passed, error message
            if (value == LocalDateTimeUtils.Empty) throw new Exception(
                $"Default constructed (empty) LocalDateTime {value} has been passed to ToIsoString() method.");

            // LocalDateTime is serialized to ISO 8601 string in yyyy-mm-ddThh:mm::ss.fff format.
            string result = LocalDateTimeUtils.Pattern.Format(value);
            return result;
        }

        /// <summary>
        /// Convert LocalDate to ISO 8601 string in yyyy-mm-ddThh:mm::ss.fff format.
        ///
        /// Return null if equal to the default constructed value.
        /// </summary>
        public static string ToIsoString(this LocalDateTime? value)
        {
            if (value.HasValue) return value.Value.ToIsoString();
            else return null;
        }

        /// <summary>
        /// Convert to System.DateTime with Kind=Utc.
        ///
        /// Error message if equal to the default constructed value.
        /// </summary>
        public static DateTime ToUtcDateTime(this LocalDateTime value)
        {
            value.CheckHasValue();
            return value.InUtc().ToDateTimeUtc();
        }

        /// <summary>
        /// Convert to System.DateTime with Kind=Utc if set and null otherwise.
        ///
        /// Return null if equal to the default constructed value.
        /// </summary>
        public static DateTime? ToUtcDateTime(this LocalDateTime? value)
        {
            if (value.HasValue) return value.Value.ToUtcDateTime();
            else return null;
        }

        /// <summary>
        /// Convert to the least possible value of \texttt{ObjectId}
        /// with timestamp equal to \texttt{value}.
        ///
        /// Error message if equal to the default constructed value.
        /// </summary>
        public static ObjectId ToObjectId(this LocalDateTime value)
        {
            value.CheckHasValue();
            return new ObjectId(value.ToUtcDateTime(), 0, 0, 0);
        }

        /// <summary>
        /// Convert to the least possible value of \texttt{ObjectId}
        /// with timestamp equal to \texttt{value}.
        ///
        /// Return null if equal to the default constructed value.
        /// </summary>
        public static ObjectId? ToObjectId(this LocalDateTime? value)
        {
            if (value.HasValue) return value.Value.ToObjectId();
            else return null;
        }
    }
}
