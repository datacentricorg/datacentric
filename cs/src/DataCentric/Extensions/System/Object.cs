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
    /// <summary>Extension methods for System.Object.</summary>
    public static class ObjectExt
    {
        /// <summary>Checks if the value is null or has special value treated as empty.</summary>
        public static bool IsEmpty(this object obj)
        {
            switch(obj)
            {
                case null:
                    return true;
                case string stringValue:
                    return string.IsNullOrEmpty(stringValue);
                case double doubleValue:
                    return doubleValue == DoubleImpl.Empty;
                case bool boolValue:
                    // Non-nullable bool has no empty value
                    return false;
                case int intValue:
                    return intValue == IntImpl.Empty;
                case long longValue:
                    return longValue == LongImpl.Empty;
                case LocalDate dateValue:
                    // Special value of LocalDate treated as empty
                    return dateValue == LocalDateImpl.Empty;
                case LocalTime timeValue:
                    // Unlike LocalDate and LocalDateTime, the LocalTime class
                    // has no special value that can be treated as Empty.
                    // Its default constructed value is 00:00 (midnight).
                    return false;
                case LocalMinute minuteValue:
                    // Unlike LocalDate and LocalDateTime, the LocalMinute class
                    // has no special value that can be treated as Empty.
                    // Its default constructed value is 00:00 (midnight).
                    return false;
                case LocalDateTime dateTimeValue:
                    // Special value of LocalDateTime treated as empty
                    return dateTimeValue == LocalDateTimeImpl.Empty;
                case RecordId objectIdValue:
                    // Empty RecordId
                    return objectIdValue == RecordId.Empty;
                case Enum enumValue:
                    // Enum is never empty; all of its values are valid
                    return false;
                case Key keyValue:
                    // Return true if key is null or equals to its default (empty) value
                    return keyValue == null || keyValue.Equals(default);
                default:
                    // Error message for any other type
                    throw new Exception(
                        $"Method value.IsEmpty() is not supported for type {obj.GetType().Name}");
            }
        }

        /// <summary>
        /// Use in place of ToString() for alternate serialization of certain types.
        ///
        /// This method returns null when the argument is null or equal to a special
        /// value treated as empty.
        /// </summary>
        public static string AsString(this object obj)
        {
            if (!obj.IsEmpty())
            {
                switch (obj)
                {
                    case string stringValue:
                        return stringValue;
                    case double doubleValue:
                        return doubleValue.ToString();
                    case bool boolValue:
                        // Uses lowercase true and false as per JSON convention, rather
                        // than True and False return by the standard C# ToString()
                        if (boolValue) return "true";
                        else return "false";
                    case int intValue:
                        return intValue.ToString();
                    case long longValue:
                        return longValue.ToString();
                    case LocalDate dateValue:
                        // Return ISO 8601 string in yyyy-mm-dd format
                        return dateValue.ToIsoString();
                    case LocalTime timeValue:
                        // Return ISO 8601 string in hh:mm:ss.fff format
                        return timeValue.ToIsoString();
                    case LocalMinute minuteValue:
                        // Return ISO 8601 string in hh:mm format
                        return minuteValue.ToIsoString();
                    case LocalDateTime dateTimeValue:
                        // Return to ISO 8601 string in yyyy-mm-ddThh:mm::ss.fff format
                        return dateTimeValue.ToIsoString();
                    case IsoDayOfWeek isoDayOfWeekValue:
                        // Use three letter day of week format
                        return isoDayOfWeekValue.ToShortDayOfWeek().ToString();
                    default:
                        // In all other cases, return ToString()
                        return obj.ToString();
                }
            }
            else
            {
                // Return null when the argument is null or equal to a special value treated as empty
                return null;
            }
        }

        /// <summary>
        /// Syntactic sugar for ``obj is T'' that is more convenient
        /// to use in functional programming than the native syntax.
        /// </summary>
        public static bool Is<T>(this object obj)
            where T : class
        {
            return obj is T;
        }

        /// <summary>
        /// Syntactic sugar for ``obj as T'' that is more convenient
        /// to use in functional programming than the native syntax.
        ///
        /// Returns null if the conversion fails.
        /// </summary>
        public static T As<T>(this object obj)
            where T : class
        {
            return obj as T;
        }

        /// <summary>
        /// Syntactic sugar for ``(T)obj'' that is more convenient
        /// to use in functional programming than the native syntax.
        ///
        /// Error message if the cast fails.
        /// </summary>
        public static T CastTo<T>(this object obj)
            where T : class
        {
            if (obj is T result)
            {
                return result;
            }
            else
            {
                throw new Exception($"Cannot convert {obj.GetType().Name} to {typeof(T).Name}.");
            }
        }
    }
}
