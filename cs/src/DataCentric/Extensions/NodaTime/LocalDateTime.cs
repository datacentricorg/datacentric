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
    /// <summary>Extension methods for NodaTime.LocalDateTime.</summary>
    public static class LocalDateTimeExtensions
    {
        /// <summary>
        /// Return true unless equal to the default constructed value.
        ///
        /// Default constructed value is not a valid value for this type.
        /// </summary>
        public static bool HasValue(this LocalDateTime value)
        {
            return value != default;
        }

        /// <summary>Return false if null or equal to the default constructed value.</summary>
        public static bool HasValue(this LocalDateTime? value)
        {
            return value.HasValue && value.Value.HasValue();
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
        /// Convert LocalDateTime in the specified timezone to Instant.
        ///
        /// Converts LocalDateTimeUtil.Empty to InstantUtil.Empty.
        ///
        /// Use timeZone = DateTimeZone.Utc for the UTC timezone.
        /// </summary>
        public static Instant ToInstant(this LocalDateTime value, DateTimeZone timeZone)
        {
            if (value == LocalDateTimeUtil.Empty)
            {
                return InstantUtil.Empty;
            }
            else
            {
                // Use lenient conversion to avoid an error when during
                // the daylight savings time clock change the same local
                // datetime repeats twice. In this case the earlier of
                // the alternatives will be used.
                return value.InZoneLeniently(timeZone).ToInstant();
            }
        }

        /// <summary>
        /// Convert LocalDateTime in the specified timezone to Instant.
        ///
        /// Converts LocalDateTimeUtil.Empty to InstantUtil.Empty and null to null.
        ///
        /// Use timeZone = DateTimeZone.Utc for the UTC timezone.
        /// </summary>
        public static Instant? ToInstant(this LocalDateTime? value, DateTimeZone timeZone)
        {
            if (value.HasValue) return value.Value.ToInstant(timeZone);
            else return null;
        }

        /// <summary>
        /// Convert LocalDateTime to ISO 8601 long with millisecond precision using yyyymmddhhmmssfff format.
        ///
        /// Error message if equal to the default constructed value.
        /// </summary>
        public static long ToIsoLong(this LocalDateTime value)
        {
            // If default constructed datetime is passed, error message
            if (value == LocalDateTimeUtil.Empty) throw new Exception(
                $"Default constructed (empty) LocalDateTime {value} has been passed to ToIsoLong() method.");

            // LocalDateTime is serialized as readable ISO int64 in yyyymmddhhmmsssss format
            int isoDate = value.Year * 10_000 + value.Month * 100 + value.Day;
            int isoTime = value.Hour * 100_00_000 + value.Minute * 100_000 + value.Second * 1000 + value.Millisecond;
            long result = ((long)isoDate) * 100_00_00_000 + (long)isoTime;
            return result;
        }

        /// <summary>
        /// Convert LocalDateTime to ISO 8601 long with millisecond precision using yyyymmddhhmmssfff format.
        ///
        /// Return null if equal to the default constructed value.
        /// </summary>
        public static long? ToIsoLong(this LocalDateTime? value)
        {
            if (value.HasValue) return value.Value.ToIsoLong();
            else return null;
        }

        /// <summary>
        /// Use strict ISO 8601 datetime pattern to millisecond precision without timezone:
        ///
        /// yyyy-mm-ddThh:mm::ss.fff
        ///
        /// Return String.Empty for the default constructed value.
        /// </summary>
        public static string ToIsoString(this LocalDateTime value)
        {
            // If default constructed datetime is passed, error message
            if (value != LocalDateTimeUtil.Empty)
            {
                // Use strict ISO 8601 datetime pattern to millisecond precision without timezone
                string result = LocalDateTimeUtil.Pattern.Format(value);
                return result;
            }
            else
            {
                return String.Empty;
            }
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
        /// Convert to the least possible value of \texttt{RecordId}
        /// with timestamp equal to \texttt{value}.
        ///
        /// Error message if equal to the default constructed value.
        /// </summary>
        public static RecordId ToRecordId(this LocalDateTime value)
        {
            value.CheckHasValue();
            return new RecordId(value.ToUtcDateTime(), 0, 0, 0);
        }
    }
}
