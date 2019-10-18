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
    /// <summary>Extension methods for NodaTime.Instant.</summary>
    public static class InstantExtensions
    {
        /// <summary>
        /// Return true unless equal to the default constructed value.
        ///
        /// Default constructed value is not a valid value for this type.
        /// </summary>
        public static bool HasValue(this Instant value)
        {
            return value != default;
        }

        /// <summary>Return false if null or equal to the default constructed value.</summary>
        public static bool HasValue(this Instant? value)
        {
            return value.HasValue && value.Value.HasValue();
        }

        /// <summary>Error message if equal to the default constructed value.</summary>
        public static void CheckHasValue(this Instant value)
        {
            if (!value.HasValue()) throw new Exception("Required datetime value is not set.");
        }

        /// <summary>Error message if null or equal to the default constructed value.</summary>
        public static void CheckHasValue(this Instant? value)
        {
            if (!value.HasValue()) throw new Exception("Required datetime value is not set.");
        }

        /// <summary>
        /// Return true if the Instant falls precisely on minute start.
        /// </summary>
        public static bool IsMinute(this Instant value)
        {
            // Check if the value has whole number of ticks per minute 
            return (value.ToUnixTimeTicks() % TimeSpan.TicksPerMinute) == 0;
        }

        /// <summary>
        /// Return true if the Instant falls precisely on second start.
        /// </summary>
        public static bool IsSecond(this Instant value)
        {
            // Check if the value has whole number of ticks per second 
            return (value.ToUnixTimeTicks() % TimeSpan.TicksPerSecond) == 0;
        }

        /// <summary>
        /// Return true if the Instant falls precisely on millisecond start.
        /// </summary>
        public static bool IsMillisecond(this Instant value)
        {
            // Check if the value has whole number of ticks per second 
            return (value.ToUnixTimeTicks() % TimeSpan.TicksPerMillisecond) == 0;
        }

        /// <summary>
        /// Convert Instant to ISO 8601 long with millisecond precision using yyyymmddhhmmssfff format in UTC.
        ///
        /// Error message if equal to the default constructed value.
        /// </summary>
        public static long ToIsoLong(this Instant value)
        {
            // If default constructed datetime is passed, error message
            if (value == InstantUtil.Empty) throw new Exception(
                $"Default constructed (empty) Instant {value} has been passed to ToIsoLong() method.");

            // Convert to zoned date time in UTC timezone, then take LocalDateTime component of the result
            var dateTimeInUtc = value.InUtc().LocalDateTime;

            // Convert the result to long
            long result = dateTimeInUtc.ToIsoLong();
            return result;
        }

        /// <summary>
        /// Convert Instant to ISO 8601 long with millisecond precision using yyyymmddhhmmssfff format in UTC.
        ///
        /// Return null if equal to the default constructed value.
        /// </summary>
        public static long? ToIsoLong(this Instant? value)
        {
            if (value.HasValue) return value.Value.ToIsoLong();
            else return null;
        }

        /// <summary>
        /// Use strict ISO 8601 datetime pattern to millisecond precision in UTC timezone:
        ///
        /// yyyy-mm-ddThh:mm::ss.fffZ
        ///
        /// Return String.Empty for the default constructed value.
        /// </summary>
        public static string ToIsoString(this Instant value)
        {
            // If default constructed datetime is passed, error message
            if (value != InstantUtil.Empty)
            {
                // Use strict ISO 8601 datetime pattern to millisecond precision without timezone
                string result = InstantUtil.Pattern.Format(value);
                return result;
            }
            else
            {
                return String.Empty;
            }
        }

        /// <summary>
        /// Use strict ISO 8601 datetime pattern to millisecond precision in UTC timezone:
        ///
        /// yyyy-mm-ddThh:mm::ss.fffZ
        ///
        /// Return String.Empty for null or the default constructed value.
        /// </summary>
        public static string ToIsoString(this Instant? value)
        {
            if (value.HasValue) return value.Value.ToIsoString();
            else return null;
        }

        /// <summary>
        /// Convert Instant to LocalDateTime in the specified timezone.
        ///
        /// Convert InstantUtil.Empty to LocalDateTimeUtil.Empty.
        ///
        /// Use timeZone = DateTimeZone.Utc for the UTC timezone.
        /// </summary>
        public static LocalDateTime ToLocalDateTime(this Instant value, DateTimeZone timeZone)
        {
            if (value == InstantUtil.Empty) return LocalDateTimeUtil.Empty;
            else return value.InZone(timeZone).LocalDateTime;
        }

        /// <summary>
        /// Convert Instant to LocalDateTime in the specified timezone.
        ///
        /// Converts InstantUtil.Empty to LocalDateTimeUtil.Empty and null to null.
        ///
        /// Use timeZone = DateTimeZone.Utc for the UTC timezone.
        /// </summary>
        public static LocalDateTime? ToLocalDateTime(this Instant? value, DateTimeZone timeZone)
        {
            if (value.HasValue) return value.Value.ToLocalDateTime(timeZone);
            else return null;
        }

        /// <summary>
        /// Convert to System.DateTime with Kind=Utc.
        ///
        /// Converts Instant.Empty to default constructed DateTime and null to null.
        /// </summary>
        public static DateTime? ToDateTime(this Instant? value)
        {
            if (value.HasValue) return value.Value.ToDateTime();
            else return null;
        }

        /// <summary>
        /// Convert to System.DateTime with Kind=Utc.
        ///
        /// Error message if equal to the default constructed value.
        /// </summary>
        public static DateTime ToDateTime(this Instant value)
        {
            if (value != default)
            {
                // If not default constructed value, convert to DateTime
                // with millisecond precision and Kind=Utc
                DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeMilliseconds(value.ToUnixTimeMilliseconds());
                DateTime result = dateTimeOffset.UtcDateTime;

                // Validate that Kind is set
                if (result.Kind != DateTimeKind.Utc) throw new Exception("DateTime.Kind is not UTC when converted from Instant.");

                return result;
            }
            else
            {
                // Converts Instant.Empty to default constructed DateTime
                return new DateTime();
            }

            value.CheckHasValue();
            return value.InUtc().ToDateTimeUtc();
        }

        /// <summary>
        /// Convert to the least possible value of \texttt{RecordId}
        /// with timestamp equal to \texttt{value}.
        ///
        /// Error message if equal to the default constructed value.
        /// </summary>
        public static RecordId ToRecordId(this Instant value)
        {
            value.CheckHasValue();
            return new RecordId((int)value.ToUnixTimeSeconds(), 0, 0, 0); // TODO - make ctor take Instant directly
        }
    }
}
