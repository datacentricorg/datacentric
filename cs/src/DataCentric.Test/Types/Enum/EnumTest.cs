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
using Xunit;
using DataCentric;

namespace DataCentric.Test
{
    /// <summary>Enum type.</summary>
    public enum EnumTestSampleType
    {
        Empty,
        EnumValue1,
        EnumValue2,
        EnumValue3,
        EnumValue4,
        EnumValue5,
        EnumValue6,
        EnumValue7
    }

    /// <summary>Key class that has all of the permitted non-nullable key elements included.</summary>
    [BsonSerializer(typeof(BsonKeySerializer<EnumTestNonNullableSampleKey>))]
    public class EnumTestNonNullableSampleKey : Key<EnumTestNonNullableSampleKey, EnumTestNonNullableSampleData>
    {
        public EnumTestSampleType EnumID { get; set; }
    }

    /// <summary>Key class that has all of the permitted non-nullable key elements included.</summary>
    public class EnumTestNonNullableSampleData : Record<EnumTestNonNullableSampleKey, EnumTestNonNullableSampleData>
    {
        public EnumTestSampleType EnumID { get; set; }
        public EnumTestSampleType EnumValue { get; set; }
        public IsoDayOfWeek DayOfWeek { get; set; }
    }

    /// <summary>Key class that has all of the permitted nullable key elements included.</summary>
    [BsonSerializer(typeof(BsonKeySerializer<EnumTestNullableSampleKey>))]
    public class EnumTestNullableSampleKey : Key<EnumTestNullableSampleKey, EnumTestNullableSampleData>
    {
        public EnumTestSampleType? EnumID { get; set; }
    }

    /// <summary>Key class that has all of the permitted nullable key elements included.</summary>
    public class EnumTestNullableSampleData : Record<EnumTestNullableSampleKey, EnumTestNullableSampleData>
    {
        public EnumTestSampleType? EnumID { get; set; }
        public EnumTestSampleType? EnumValue { get; set; }
        public IsoDayOfWeek? DayOfWeek { get; set; }
    }

    /// <summary>Unit test for Query.</summary>
    public class EnumTest
    {
        /// <summary>>Key class that has all of the permitted non-nullable key elements included.</summary>
        [Fact]
        public void CompleteNonNullableQuery()
        {
            using (IDataTestContext context = new DataTestContext(this))
            {
                for (int recordIndex = 0; recordIndex < 8; ++recordIndex)
                {
                    int recordIndexMod8 = recordIndex % 8;

                    var record = new EnumTestNonNullableSampleData();
                    record.EnumID = (EnumTestSampleType)(recordIndexMod8);
                    record.EnumValue = (EnumTestSampleType)(recordIndexMod8);
                    record.DayOfWeek = (IsoDayOfWeek)recordIndexMod8;

                    context.Save(record, context.DataSet);
                }

                if (true)
                {
                    // Query for all records without restrictions,
                    // should return 8 records
                    var query = context.DataSource.GetQuery<EnumTestNonNullableSampleData>(context.DataSet);

                    context.Verify.Text("Unconstrained query");
                    foreach (var obj in query.AsEnumerable())
                    {
                        context.Verify.Text($"    Key={obj.Key} IsoDayOfWeek={obj.DayOfWeek}");
                    }
                }

                if (true)
                {
                    // Query for all records without restrictions,
                    // should return 1 out of 8 records

                    var query = context.DataSource.GetQuery<EnumTestNonNullableSampleData>(context.DataSet)
                        .Where(p => p.EnumID == (EnumTestSampleType) 1)
                        .Where(p => p.EnumValue == (EnumTestSampleType) 1)
                        .Where(p => p.DayOfWeek == (IsoDayOfWeek) 1);

                    context.Verify.Text("Constrained query");
                    foreach (var obj in query.AsEnumerable())
                    {
                        context.Verify.Text($"    Key={obj.Key} IsoDayOfWeek={obj.DayOfWeek}");
                    }
                }
            }
        }

        /// <summary>>Key class that has all of the permitted nullable key elements included.</summary>
        [Fact]
        public void CompleteNullableQuery()
        {
            using (IDataTestContext context = new DataTestContext(this))
            {
                for (int recordIndex = 0; recordIndex < 8; ++recordIndex)
                {
                    int recordIndexMod8 = recordIndex % 8;

                    var record = new EnumTestNullableSampleData();
                    record.EnumID = (EnumTestSampleType)(recordIndexMod8);
                    record.EnumValue = (EnumTestSampleType)(recordIndexMod8);
                    record.DayOfWeek = (IsoDayOfWeek)recordIndexMod8;

                    context.Save(record, context.DataSet);
                }

                if (true)
                {
                    // Query for all records without restrictions,
                    // should return 8 records
                    var query = context.DataSource.GetQuery<EnumTestNullableSampleData>(context.DataSet);

                    context.Verify.Text("Unconstrained query");
                    foreach (var obj in query.AsEnumerable())
                    {
                        context.Verify.Text($"    Key={obj.Key} IsoDayOfWeek={obj.DayOfWeek}");
                    }
                }

                if (true)
                {
                    // Query for all records without restrictions,
                    // should return 1 out of 8 records

                    var query = context.DataSource.GetQuery<EnumTestNullableSampleData>(context.DataSet)
                        .Where(p => p.EnumID == (EnumTestSampleType)1)
                        .Where(p => p.EnumValue == (EnumTestSampleType)1)
                        .Where(p => p.DayOfWeek == (IsoDayOfWeek)1);

                    context.Verify.Text("Constrained query");
                    foreach (var obj in query.AsEnumerable())
                    {
                        context.Verify.Text($"    Key={obj.Key} IsoDayOfWeek={obj.DayOfWeek}");
                    }
                }
            }
        }
    }
}
