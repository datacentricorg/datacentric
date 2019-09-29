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
using MongoDB.Bson.Serialization.Attributes;
using NodaTime;
using DataCentric;

namespace DataCentric.Test
{
    /// <summary>Key class that has all of the permitted nullable key elements included.</summary>
    [BsonSerializer(typeof(BsonKeySerializer<MongoKeyTestNullableSampleKey>))]
    public sealed class MongoKeyTestNullableSampleKey : TypedKey<MongoKeyTestNullableSampleKey, MongoKeyTestNullableSampleData>
    {
        public bool? BoolToken { get; set; }
        public int? IntToken { get; set; }
        public long? LongToken { get; set; }
        public LocalDate? LocalDateToken { get; set; }
        public LocalTime? LocalTimeToken { get; set; }
        public LocalMinute? LocalMinuteToken { get; set; }
        public LocalDateTime? LocalDateTimeToken { get; set; }
        public MongoTestEnum? EnumToken { get; set; }
    }
}
