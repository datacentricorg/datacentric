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
    /// <summary>Unit test for Query.</summary>
    public class QueryTest
    {
        /// <summary>Query on all permitted nullable element types.</summary>
        [Fact]
        public void NullableElements()
        {
            using (var context = new TemporalMongoTestContext(this))
            {
                for (int recordIndex = 0; recordIndex < 8; ++recordIndex)
                {
                    int recordIndexMod2 = recordIndex % 2;
                    int recordIndexMod4 = recordIndex % 4;

                    var record = new NullableElementsSampleData();
                    record.RecordIndex = recordIndex;
                    record.DataSet = context.DataSet;
                    record.StringToken = "A" + recordIndexMod4.ToString();
                    record.BoolToken = recordIndexMod2 == 0;
                    record.IntToken = recordIndexMod4;
                    record.LongToken = recordIndexMod4;
                    record.LocalDateToken = new LocalDate(2003, 5, 1).PlusDays(recordIndexMod4);
                    record.LocalTimeToken = new LocalTime(10, 15, 30).PlusMinutes(recordIndexMod4);
                    record.LocalMinuteToken = new LocalMinute(10, recordIndexMod4);
                    record.LocalDateTimeToken = new LocalDateTime(2003, 5, 1, 10, 15).PlusDays(recordIndexMod4);
                    record.EnumToken = (SampleEnum)(recordIndexMod2 + 1);

                    context.Save(record, context.DataSet);
                }

                if (true)
                {
                    // Query for all records without restrictions,
                    // should return 4 out of 8 records because
                    // each record has two versions
                    var query = context.DataSource.GetQuery<NullableElementsSampleData>(context.DataSet);

                    context.Verify.Text("Unconstrained query");
                    foreach (var obj in query.AsEnumerable())
                    {
                        context.Verify.Text($"    {obj.Key} (record index {obj.RecordIndex}).");
                    }
                }

                if (true)
                {
                    // Query for all records without restrictions,
                    // should return 4 out of 8 records because
                    // each record has two versions

                    var query = context.DataSource.GetQuery<NullableElementsSampleData>(context.DataSet)
                        .Where(p => p.StringToken == "A1")
                        .Where(p => p.BoolToken == false)
                        .Where(p => p.IntToken == 1)
                        .Where(p => p.LongToken == 1)
                        .Where(p => p.LocalDateToken == new LocalDate(2003, 5, 1).PlusDays(1))
                        .Where(p => p.LocalTimeToken == new LocalTime(10, 15, 30).PlusMinutes(1))
                        .Where(p => p.LocalMinuteToken == new LocalMinute(10, 1))
                        .Where(p => p.LocalDateTimeToken == new LocalDateTime(2003, 5, 1, 10, 15).PlusDays(1))
                        .Where(p => p.EnumToken == (SampleEnum)2);

                    context.Verify.Text("Constrained query");
                    foreach (var obj in query.AsEnumerable())
                    {
                        context.Verify.Text($"    {obj.Key} (record index {obj.RecordIndex}).");
                    }
                }
            }
        }

        /// <summary>Query on all permitted non-nullable element types.</summary>
        [Fact]
        public void NonNullableElements()
        {
            using (var context = new TemporalMongoTestContext(this))
            {
                for (int recordIndex = 0; recordIndex < 8; ++recordIndex)
                {
                    int recordIndexMod2 = recordIndex % 2;
                    int recordIndexMod4 = recordIndex % 4;

                    var record = new NonNullableElementsSampleData();
                    record.RecordIndex = recordIndex;
                    record.DataSet = context.DataSet;
                    record.StringToken = "A" + recordIndexMod4.ToString();
                    record.BoolToken = recordIndexMod2 == 0;
                    record.IntToken = recordIndexMod4;
                    record.LongToken = recordIndexMod4;
                    record.LocalDateToken = new LocalDate(2003, 5, 1).PlusDays(recordIndexMod4);
                    record.LocalTimeToken = new LocalTime(10, 15, 30).PlusMinutes(recordIndexMod4);
                    record.LocalMinuteToken = new LocalMinute(10, recordIndexMod4);
                    record.LocalDateTimeToken = new LocalDateTime(2003, 5, 1, 10, 15).PlusDays(recordIndexMod4);
                    record.EnumToken = (SampleEnum) (recordIndexMod2 + 1);

                    context.Save(record, context.DataSet);
                }

                if (true)
                {
                    // Query for all records without restrictions,
                    // should return 4 out of 8 records because
                    // each record has two versions
                    var query = context.DataSource.GetQuery<NonNullableElementsSampleData>(context.DataSet);

                    context.Verify.Text("Unconstrained query");
                    foreach (var obj in query.AsEnumerable())
                    {
                        context.Verify.Text($"    {obj.Key} (record index {obj.RecordIndex}).");
                    }
                }

                if (true)
                {
                    // Query for all records without restrictions,
                    // should return 4 out of 8 records because
                    // each record has two versions

                    var query = context.DataSource.GetQuery<NonNullableElementsSampleData>(context.DataSet)
                        .Where(p => p.StringToken == "A1")
                        .Where(p => p.BoolToken == false)
                        .Where(p => p.IntToken == 1)
                        .Where(p => p.LongToken == 1)
                        .Where(p => p.LocalDateToken == new LocalDate(2003, 5, 1).PlusDays(1))
                        .Where(p => p.LocalTimeToken == new LocalTime(10, 15, 30).PlusMinutes(1))
                        .Where(p => p.LocalMinuteToken == new LocalMinute(10, 1))
                        .Where(p => p.LocalDateTimeToken == new LocalDateTime(2003, 5, 1, 10, 15).PlusDays(1))
                        .Where(p => p.EnumToken == (SampleEnum)2);

                    context.Verify.Text("Constrained query");
                    foreach (var obj in query.AsEnumerable())
                    {
                        context.Verify.Text($"    {obj.Key} (record index {obj.RecordIndex}).");
                    }
                }
            }
        }
    }
}
