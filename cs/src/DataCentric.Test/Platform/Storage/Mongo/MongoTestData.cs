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
using System.Collections.Generic;
using System.Linq;
using DataCentric;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using NodaTime;
using Xunit;

namespace DataCentric.Test
{
    /// <summary>Base data class.</summary>
    [Index("DoubleElement+LocalDateElement+EnumValue")]
    [Index("LocalDateElement")]
    [Index("RecordId-Version", "CustomIndexName")]
    [Index("-RecordIndex")]
    public class MongoTestData : TypedRecord<MongoTestKey, MongoTestData>
    {
        public string RecordId { get; set; }
        public int? RecordIndex { get; set; }
        public double? DoubleElement { get; set; }

        public LocalDate? LocalDateElement { get; set; }

        public LocalTime? LocalTimeElement { get; set; }
        public LocalMinute? LocalMinuteElement { get; set; }
        public LocalDateTime? LocalDateTimeElement { get; set; }
        public MongoTestEnum EnumValue { get; set; }
        public int? Version { get; set; }
    }
}