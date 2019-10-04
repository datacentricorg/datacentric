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
using NodaTime.Extensions;

namespace DataCentric
{
    /// <summary>Extension methods for MongoDB.Bson.ObjectId.</summary>
    public static class ObjectIdExt
    {
        /// <summary>Return false if equal to the default constructed value.</summary>
        public static bool HasValue(this ObjectId value)
        {
            return value != default;
        }

        /// <summary>Return false if null or equal to the default constructed value.</summary>
        public static bool HasValue(this ObjectId? value)
        {
            return value.HasValue && value.HasValue();
        }

        /// <summary>Error message if equal to the default constructed value.</summary>
        public static void CheckHasValue(this ObjectId value)
        {
            if (!value.HasValue()) throw new Exception("Required ObjectId value is not set.");
        }

        /// <summary>Error message if null or equal to the default constructed value.</summary>
        public static void CheckHasValue(this ObjectId? value)
        {
            if (!value.HasValue()) throw new Exception("Required ObjectId value is not set.");
        }

        /// <summary>
        /// Convert ObjectId to its creation time. This method has one second resolution.
        ///
        /// Error message if equal to the default constructed value.
        /// </summary>
        public static LocalDateTime ToLocalDateTime(this ObjectId value)
        {
            value.CheckHasValue();

            var result = value.CreationTime.ToLocalDateTime();
            return result;
        }

        /// <summary>
        /// Convert ObjectId to its creation time. This method has one second resolution.
        ///
        /// Return null if equal to the default constructed value.
        /// </summary>
        public static LocalDateTime? ToLocalDateTime(this ObjectId? value)
        {
            if (value.HasValue) return value.Value.ToLocalDateTime();
            else return null;
        }
    }
}
