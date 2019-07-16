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
    /// <summary>Extension methods for NodaTime.LocalDate.</summary>
    public static class LocalDateEx
    {
        /// <summary>Return false if equal to default constructed value.</summary>
        public static bool HasValue(this LocalDate value)
        {
            return value != default;
        }

        /// <summary>Convert LocalDate to to ISO 8601 8 digit int in yyyymmdd format.</summary>
        public static int ToIsoInt(this LocalDate value)
        {
            // If default constructed date is passed, error message
            if (value == LocalDateUtils.Empty) throw new Exception(
                $"Default constructed (empty) LocalDate {value} has been passed to ToIsoInt() method.");

            int result = value.Year * 10_000 + value.Month * 100 + value.Day;
            return result;
        }

        /// <summary>Convert LocalDate to ISO 8601 string in yyyy-mm-dd format.</summary>
        public static string ToIsoString(this LocalDate value)
        {
            // If default constructed date is passed, error message
            if (value == LocalDateUtils.Empty) throw new Exception(
                $"Default constructed (empty) LocalDate {value} has been passed to ToIsoString() method.");

            // LocalTime is serialized to ISO 8601 string in yyyy-mm-dd format.
            string result = LocalDateUtils.Pattern.Format(value);
            return result;
        }

        /// <summary>Convert LocalDate to variant.</summary>
        public static Variant ToVariant(this LocalDate value)
        {
            return new Variant(value);
        }
    }
}
