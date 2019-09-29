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
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using NodaTime;
using Xunit;
using DataCentric;
using MongoDB.Bson;

namespace DataCentric.Test
{
    /// <summary>Unit test for key serialization in Mongo 2.1 format.</summary>
    public class MongoKeyTest
    {
        /// <summary>>Key class that has all of the permitted non-nullable key elements included.</summary>
        [Fact]
        public void CompleteNonNullableKey()
        {
            using (var context = new TemporalMongoTestContext(this))
            {
                var record = new NonNullableElementsSampleData();
                record.DataSet = context.DataSet;
                record.StringToken = "A";
                record.BoolToken = true;
                record.IntToken = 123;
                record.LongToken = 12345678912345;
                record.LocalDateToken = new LocalDate(2003, 5, 1);
                record.LocalTimeToken = new LocalTime(10, 15, 30); // 10:15:30
                record.LocalMinuteToken = new LocalMinute(10, 15); // 10:15
                record.LocalDateTimeToken = new LocalDateTime(2003, 5, 1, 10, 15); // 2003-05-01T10:15:00
                record.EnumToken = SampleEnum.EnumValue2;

                // Verify key serialization
                context.Verify.Text(record.Key);

                // Verify key creation
                var key = record.ToKey();
                context.Verify.Text(key.Value);

                // Save
                context.Save(record, context.DataSet);

                // Load from storage
                var loadedRecord = context.LoadOrNull(key, context.DataSet);
                context.Verify.Text(loadedRecord.Key);
            }
        }

        /// <summary>>Key class that has all of the permitted nullable key elements included.</summary>
        [Fact]
        public void CompleteNullableKey()
        {
            using (var context = new TemporalMongoTestContext(this))
            {
                var record = new NullableElementsSampleData();
                record.DataSet = context.DataSet;
                record.StringToken = "A";
                record.BoolToken = true;
                record.IntToken = 123;
                record.LongToken = 12345678912345;
                record.LocalDateToken = new LocalDate(2003, 5, 1);
                record.LocalTimeToken = new LocalTime(10, 15, 30); // 10:15:30
                record.LocalMinuteToken = new LocalMinute(10, 15); // 10:15
                record.LocalDateTimeToken = new LocalDateTime(2003, 5, 1, 10, 15); // 2003-05-01T10:15:00
                record.EnumToken = SampleEnum.EnumValue2;

                // Verify key serialization
                context.Verify.Text(record.Key);

                // Verify key creation
                var key = record.ToKey();
                context.Verify.Text(key.Value);

                // Save
                context.Save(record, context.DataSet);

                // Load from storage
                var loadedRecord = context.LoadOrNull(key, context.DataSet);
                context.Verify.Text(loadedRecord.Key);
            }
        }

        /// <summary>Test record with composite key.</summary>
        [Fact]
        public void CompositeKey()
        {
            using (var context = new TemporalMongoTestContext(this))
            {
                context.KeepTestData = true;

                var rec = new CompositeKeySampleData();
                rec.KeyElement1 = "abc";
                rec.KeyElement2 = new BaseSampleKey();
                rec.KeyElement2.RecordId = "def";
                rec.KeyElement2.RecordIndex = 123;
                rec.KeyElement3 = "xyz";

                // Verify key serialization
                string keyValue = rec.ToKey().ToString();
                context.Verify.Text($"Serialized key: {keyValue}");

                // Verify key deserialization
                var key = new CompositeKeySampleKey();
                key.PopulateFrom(keyValue);
                context.Verify.Text($"Deserialized key: {key}");
            }
        }
    }
}
