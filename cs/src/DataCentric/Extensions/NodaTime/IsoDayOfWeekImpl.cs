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
using System.Runtime.InteropServices.ComTypes;
using MongoDB.Bson;
using NodaTime;

namespace DataCentric
{
    /// <summary>Static helper methods for NodaTime.IsoDayOfWeek.</summary>
    public static class IsoDayOfWeekImpl
    {
        /// <summary>
        /// Converts the short (three-letter) string representation
        /// of the day of week to the original NodaTime enum value.
        /// This relies on the enums ShortDayOfWeek and IsoDayOfWeek
        /// sharing the same int representation.
        ///
        /// Sets result to default enum value (None) and returns
        /// false if the conversion fails.
        /// </summary>
        public static bool TryParse(string s, out IsoDayOfWeek result)
        {
            if (Enum.TryParse(s, out ShortDayOfWeek shortResult))
            {
                // Conversion succeeded, cast the result to the original
                // NodaTime.IsoDayOfWeek enum and return true.
                //
                // This relies on the enums ShortDayOfWeek and IsoDayOfWeek
                // sharing the same int representation.
                result = (IsoDayOfWeek) shortResult;
                return true;
            }
            else
            {
                // Conversion failed, set result to default value
                // for this enum type and return null. This is the
                // same behavior as the native Enum.Parse(s,result).
                result = default;
                return false;
            }
        }

        /// <summary>
        /// Converts the short (three-letter) string representation
        /// of the day of week to the original NodaTime enum value.
        ///
        /// Error message if the conversion fails.
        /// </summary>
        public static IsoDayOfWeek Parse(string s)
        {
            if (!TryParse(s, out IsoDayOfWeek result))
                throw new Exception($"String {s} cannot be converted to IsoDayOfWeek. The enum " +
                                    $"IsoDayOfWeek represents days of week using their full names, " +
                                    $"not three-letter abbreviations.");
            return result;
        }
    }
}
