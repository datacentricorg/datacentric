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
    /// <summary>
    /// Test for the data type where key fields are not the first in the record,
    /// and key fields are not in the same order in the record as in the key.
    /// </summary>
    public class MongoNonAlignedTest
    {
        /// <summary>Smoke test.</summary>
        [Fact]
        public void Smoke()
        {
            using (var context = new HierarchicalMongoTestContext(this))
            {
                var record = new MongoNonAlignedTestData();
                record.DataSet = context.DataSet;
                record.KeyElement1 = "A";
                record.KeyElement2 = "B";
                record.InitialElement1 = 1;
                record.InitialElement1 = 2;

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
    }
}
