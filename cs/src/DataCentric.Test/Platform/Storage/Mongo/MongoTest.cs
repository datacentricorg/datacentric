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
using System.Net.Mime;
using DataCentric;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using NodaTime;
using Xunit;

namespace DataCentric.Test
{
    /// <summary>Unit test for TemporalMongoDataSourceData.</summary>
    public class TemporalMongoTest
    {
        /// <summary>Smoke test.</summary>
        [Fact]
        public void Smoke()
        {
            using (var context = new MongoTestContext(this))
            {
                SaveBasicData(context);

                // Get dataset identifiers
                var dataSetA = context.GetDataSet("A", context.DataSet);
                var dataSetB = context.GetDataSet("B", context.DataSet);

                // Create keys
                var keyA0 = new MongoTestKey()
                {
                    RecordId = "A",
                    RecordIndex = 0
                };
                var keyB0 = new MongoTestKey()
                {
                    RecordId = "B",
                    RecordIndex = 0
                };

                // Verify the result of loading records from datasets A and B
                VerifyLoad(context, keyA0, "A");
                VerifyLoad(context, keyA0, "B");
                VerifyLoad(context, keyB0, "A");
                VerifyLoad(context, keyB0, "B");
            }
        }

        /// <summary>Test for the query across multiple datasets.</summary>
        [Fact]
        public void MultipleDataSetQuery()
        {
            using (var context = new MongoTestContext(this))
            {
                // Create datasets
                var dataSetA = context.CreateDataSet("A", context.DataSet);
                var dataSetB = context.CreateDataSet("B", new ObjectId[] { dataSetA }, context.DataSet);
                var dataSetC = context.CreateDataSet("C", new ObjectId[] { dataSetA }, context.DataSet);
                var dataSetD = context.CreateDataSet("D", new ObjectId[] { dataSetA, dataSetB, dataSetC }, context.DataSet);

                // Create initial version of the records
                SaveMinimalRecord(context, "A", "A", 0, 0);
                SaveMinimalRecord(context, "A", "B", 1, 0);
                SaveMinimalRecord(context, "A", "A", 2, 0);
                SaveMinimalRecord(context, "A", "B", 3, 0);
                SaveMinimalRecord(context, "B", "A", 4, 0);
                SaveMinimalRecord(context, "B", "B", 5, 0);
                SaveMinimalRecord(context, "B", "A", 6, 0);
                SaveMinimalRecord(context, "B", "B", 7, 0);
                SaveMinimalRecord(context, "C", "A", 8, 0);
                SaveMinimalRecord(context, "C", "B", 9, 0);
                SaveMinimalRecord(context, "D", "A", 10, 0);
                SaveMinimalRecord(context, "D", "B", 11, 0);

                // Create second version of some records
                SaveMinimalRecord(context, "A", "A", 0, 1);
                SaveMinimalRecord(context, "A", "B", 1, 1);
                SaveMinimalRecord(context, "A", "A", 2, 1);
                SaveMinimalRecord(context, "A", "B", 3, 1);
                SaveMinimalRecord(context, "B", "A", 4, 1);
                SaveMinimalRecord(context, "B", "B", 5, 1);
                SaveMinimalRecord(context, "B", "A", 6, 1);
                SaveMinimalRecord(context, "B", "B", 7, 1);

                // Create third version of even fewer records
                SaveMinimalRecord(context, "A", "A", 0, 2);
                SaveMinimalRecord(context, "A", "B", 1, 2);
                SaveMinimalRecord(context, "A", "A", 2, 2);
                SaveMinimalRecord(context, "A", "B", 3, 2);

                // Query for RecordId=B
                var query = context.GetQuery<MongoTestData>(dataSetD)
                    .Where(p => p.RecordId == "B")
                    .SortBy(p => p.RecordId)
                    .SortBy(p => p.RecordIndex)
                    .AsEnumerable();

                foreach (var obj in query)
                {
                    var dataSetId = context.LoadOrNull<DataSetData>(obj.DataSet).DataSetId;
                    context.Verify.Text($"Key={obj.Key} DataSet={dataSetId} Version={obj.Version}");
                }
            }
        }

        /// <summary>Test of CreateOrderedObjectId method.</summary>
        [Fact]
        public void CreateOrderedObjectId()
        {
            using (var context = new MongoTestContext(this))
            {
                for (int i = 0; i < 10_000; ++i)
                {
                    // Invoke 10,000 times to ensure there is no log report of non-increasing ObjectId
                    context.DataSource.CreateOrderedObjectId();
                }

                // Confirm no log output indicating non-increasing id from inside the for loop
                context.Verify.Text("Log should have no lines before this one.");
            }
        }

        /// <summary>Test delete marker.</summary>
        [Fact]
        public void Delete()
        {
            using (var context = new MongoTestContext(this))
            {
                SaveBasicData(context);

                // Get dataset identifiers
                var dataSetA = context.GetDataSet("A", context.DataSet);
                var dataSetB = context.GetDataSet("B", context.DataSet);

                // Create keys
                var keyA0 = new MongoTestKey()
                {
                    RecordId = "A",
                    RecordIndex = 0
                };
                var keyB0 = new MongoTestKey()
                {
                    RecordId = "B",
                    RecordIndex = 0
                };

                // Verify the result of loading records from datasets A and B
                context.Verify.Text("Initial load");
                VerifyLoad(context, keyA0, "A");
                VerifyLoad(context, keyA0, "B");
                VerifyLoad(context, keyB0, "A");
                VerifyLoad(context, keyB0, "B");

                context.Verify.Text("Query in dataset A");
                VerifyQuery<MongoTestData>(context, "A");
                context.Verify.Text("Query in dataset B");
                VerifyQuery<MongoTestData>(context, "B");

                context.Verify.Text("Delete A0 record in B dataset");
                context.Delete(keyA0, dataSetB);
                VerifyLoad(context, keyA0, "A");
                VerifyLoad(context, keyA0, "B");

                context.Verify.Text("Query in dataset A");
                VerifyQuery<MongoTestData>(context, "A");
                context.Verify.Text("Query in dataset B");
                VerifyQuery<MongoTestData>(context, "B");

                context.Verify.Text("Delete A0 record in A dataset");
                context.Delete(keyA0, dataSetA);
                VerifyLoad(context, keyA0, "A");
                VerifyLoad(context, keyA0, "B");

                context.Verify.Text("Query in dataset A");
                VerifyQuery<MongoTestData>(context, "A");
                context.Verify.Text("Query in dataset B");
                VerifyQuery<MongoTestData>(context, "B");

                context.Verify.Text("Delete B0 record in B dataset");
                context.Delete(keyB0, dataSetB);
                VerifyLoad(context, keyB0, "A");
                VerifyLoad(context, keyB0, "B");

                context.Verify.Text("Query in dataset A");
                VerifyQuery<MongoTestData>(context, "A");
                context.Verify.Text("Query in dataset B");
                VerifyQuery<MongoTestData>(context, "B");
            }
        }

        /// <summary>
        /// Test saving object of a different type for the same key.
        ///
        /// The objective of this test is to confirm that LoadOrNull
        /// for the wrong type will give an error, while query will
        /// skip the object of the wrong type even if there is an
        /// earlier version of the object with the same key that
        /// has a compatible type.
        /// </summary>
        [Fact]
        public void TypeChange()
        {
            using (var context = new MongoTestContext(this))
            {
                // Create datasets
                var dataSetA = context.CreateDataSet("A", context.DataSet);
                var dataSetB = context.CreateDataSet("B", new ObjectId[] { dataSetA }, context.DataSet);

                // Create records with minimal data
                SaveDerivedRecord(context, "A", "A", 0);
                SaveDerivedFromDerivedRecord(context, "B", "B", 0);

                // Create keys
                var keyA0 = new MongoTestKey()
                {
                    RecordId = "A",
                    RecordIndex = 0
                };
                var keyB0 = new MongoTestKey()
                {
                    RecordId = "B",
                    RecordIndex = 0
                };

                // Verify the result of loading records from datasets A and B
                context.Verify.Text("Initial load");
                VerifyLoad(context, keyA0, "A");
                VerifyLoad(context, keyA0, "B");
                VerifyLoad(context, keyB0, "A");
                VerifyLoad(context, keyB0, "B");

                context.Verify.Text("Query in dataset A for type MongoTestDerivedData");
                VerifyQuery<MongoTestDerivedData>(context, "A");
                context.Verify.Text("Query in dataset B for type MongoTestDerivedData");
                VerifyQuery<MongoTestDerivedData>(context, "B");

                context.Verify.Text("Change A0 record type in B dataset to C");
                SaveOtherDerivedRecord(context, "B", "A", 0);
                VerifyLoad(context, keyA0, "A");
                VerifyLoad(context, keyA0, "B");

                context.Verify.Text("Query in dataset A for type MongoTestDerivedData");
                VerifyQuery<MongoTestDerivedData>(context, "A");
                context.Verify.Text("Query in dataset B for type MongoTestDerivedData");
                VerifyQuery<MongoTestDerivedData>(context, "B");

                context.Verify.Text("Change A0 record type in A dataset to C");
                SaveOtherDerivedRecord(context, "A", "A", 0);
                VerifyLoad(context, keyA0, "A");
                VerifyLoad(context, keyA0, "B");

                context.Verify.Text("Query in dataset A for type MongoTestDerivedData");
                VerifyQuery<MongoTestDerivedData>(context, "A");
                context.Verify.Text("Query in dataset B for type MongoTestDerivedData");
                VerifyQuery<MongoTestDerivedData>(context, "B");

                context.Verify.Text("Change B0 record type in B dataset to C");
                SaveOtherDerivedRecord(context, "B", "B", 0);
                VerifyLoad(context, keyB0, "A");
                VerifyLoad(context, keyB0, "B");

                context.Verify.Text("Query in dataset A for type MongoTestDerivedData");
                VerifyQuery<MongoTestDerivedData>(context, "A");
                context.Verify.Text("Query in dataset B for type MongoTestDerivedData");
                VerifyQuery<MongoTestDerivedData>(context, "B");
            }
        }

        /// <summary>Test query.</summary>
        [Fact]
        public void ElementTypesQuery()
        {
            using (var context = new MongoTestContext(this))
            {
                // Saves data in A and B datasets, A is an import of B
                SaveCompleteData(context);

                // Look in B dataset
                var dataSetB = context.GetDataSetOrEmpty("B", context.DataSet);
                var testQuery = context.GetQuery<MongoTestDerivedData>(dataSetB)
                    .Where(p => p.DataElementList[0].DoubleElement3 == 1.0)
                    .Where(p => p.DataElementList[0].StringElement3 == "A0")
                    .Where(p => p.LocalDateElement < new LocalDate(2003, 5, 2))
                    .Where(p => p.LocalDateElement > new LocalDate(2003, 4, 30))
                    .Where(p => p.LocalDateElement == new LocalDate(2003, 5, 1))
                    .Where(p => p.LocalTimeElement < new LocalTime(10, 15, 31))
                    .Where(p => p.LocalTimeElement > new LocalTime(10, 15, 29))
                    .Where(p => p.LocalTimeElement == new LocalTime(10, 15, 30))
                    .Where(p => p.LocalDateTimeElement < new LocalDateTime(2003, 5, 1, 10, 15, 01))
                    .Where(p => p.LocalDateTimeElement > new LocalDateTime(2003, 5, 1, 10, 14, 59))
                    .Where(p => p.LocalDateTimeElement == new LocalDateTime(2003, 5, 1, 10, 15))
                    .Where(p => p.StringElement2 == String.Empty)
                    .Where(p => p.KeyElement == new MongoTestKey() {RecordId = "BB", RecordIndex = 2})
                    .SortBy(p => p.RecordId);

                foreach (var obj in testQuery.AsEnumerable())
                {
                    context.Verify.Text($"Key={obj.Key} Type={obj.GetType().Name}");
                }
            }
        }

        /// <summary>Test polymorphic load and query.</summary>
        [Fact]
        public void PolymorphicQuery()
        {
            using (var context = new MongoTestContext(this))
            {
                // Saves data in A and B datasets, A is an import of B
                SaveCompleteData(context);

                // Look in B dataset
                var dataSetD = context.GetDataSetOrEmpty("D", context.DataSet);

                // Load record of derived types by base
                context.Verify.Text("Load all types by key to type A");
                {
                    var key = new MongoTestKey {RecordId = "A", RecordIndex = 0};
                    var obj = context.LoadOrNull(key, dataSetD);
                    context.Verify.Text($"    Key={obj.Key} Type={obj.GetType().Name}");
                }
                {
                    var key = new MongoTestKey { RecordId = "B", RecordIndex = 0 };
                    var obj = context.LoadOrNull(key, dataSetD);
                    context.Verify.Text($"    Key={obj.Key} Type={obj.GetType().Name}");
                }
                {
                    var key = new MongoTestKey { RecordId = "C", RecordIndex = 0 };
                    var obj = context.LoadOrNull(key, dataSetD);
                    context.Verify.Text($"    Key={obj.Key} Type={obj.GetType().Name}");
                }
                {
                    var key = new MongoTestKey { RecordId = "D", RecordIndex = 0 };
                    var obj = context.LoadOrNull(key, dataSetD);
                    context.Verify.Text($"    Key={obj.Key} Type={obj.GetType().Name}");
                }
                {
                    context.Verify.Text("Query by MongoTestData, unconstrained");
                    var query = context.GetQuery<MongoTestData>(dataSetD);
                    foreach (var obj in query.AsEnumerable())
                    {
                        context.Verify.Text($"    Key={obj.Key} Type={obj.GetType().Name}");
                    }
                }
                {
                    context.Verify.Text("Query by MongoTestDerivedData : MongoTestData which also picks up MongoTestDerivedFromDerivedData : MongoTestDerivedData, unconstrained");
                    var query = context.GetQuery<MongoTestDerivedData>(dataSetD);
                    foreach (var obj in query.AsEnumerable())
                    {
                        context.Verify.Text($"    Key={obj.Key} Type={obj.GetType().Name}");
                    }
                }
                {
                    context.Verify.Text("Query by MongoTestOtherDerivedData : MongoTestData, unconstrained");
                    var query = context.GetQuery<MongoTestOtherDerivedData>(dataSetD);
                    foreach (var obj in query.AsEnumerable())
                    {
                        context.Verify.Text($"    Key={obj.Key} Type={obj.GetType().Name}");
                    }
                }
                {
                    context.Verify.Text("Query by MongoTestDerivedFromDerivedData : MongoTestDerivedData, where MongoTestDerivedData : MongoTestData, unconstrained");
                    var query = context.GetQuery<MongoTestDerivedFromDerivedData>(dataSetD);
                    foreach (var obj in query.AsEnumerable())
                    {
                        context.Verify.Text($"    Key={obj.Key} Type={obj.GetType().Name}");
                    }
                }
            }
        }

        /// <summary>Test sorting.</summary>
        [Fact]
        public void Sort()
        {
            using (var context = new MongoTestContext(this))
            {
                // Saves data in A and B datasets, A is an import of B
                SaveCompleteData(context);

                // Look in B dataset
                var dataSetD = context.GetDataSetOrEmpty("D", context.DataSet);

                context.Verify.Text("Query by MongoTestData, sort by RecordIndex descending, then by DoubleElement ascending");
                var baseQuery = context.GetQuery<MongoTestData>(dataSetD)
                    .SortByDescending(p => p.RecordIndex)
                    .SortBy(p => p.DoubleElement);
                foreach (var obj in baseQuery.AsEnumerable())
                {
                    context.Verify.Text(
                        $"    RecordIndex={obj.RecordIndex} DoubleElement={obj.DoubleElement} " +
                        $"Key={obj.Key} Type={obj.GetType().Name}");
                }
            }
        }


        /// <summary>Test for SavedBy filtering.</summary>
        [Fact]
        public void SavedBy()
        {
            using (var context = new MongoTestContext(this))
            {
                // Create datasets
                var dataSetA = context.CreateDataSet("A", context.DataSet);
                var dataSetB = context.CreateDataSet("B", new ObjectId[] { dataSetA }, context.DataSet);

                // Create initial version of the records
                ObjectId objA0 = SaveMinimalRecord(context, "A", "A", 0, 0);
                ObjectId objB0 = SaveMinimalRecord(context, "B", "B", 0, 0);

                // Create second version of the records
                ObjectId objA1 = SaveMinimalRecord(context, "A", "A", 0, 1);
                ObjectId objB1 = SaveMinimalRecord(context, "B", "B", 0, 1);

                ObjectId cutoffObjectId = context.DataSource.CreateOrderedObjectId();

                // Create third version of the records
                ObjectId objA2 = SaveMinimalRecord(context, "A", "A", 0, 2);
                ObjectId objB2 = SaveMinimalRecord(context, "B", "B", 0, 2);

                // Create new records that did not exist before
                ObjectId objC0 = SaveMinimalRecord(context, "A", "C", 0, 0);
                ObjectId objD0 = SaveMinimalRecord(context, "B", "D", 0, 0);

                // Load each record by ObjectId
                context.Verify.Text("Load records by ObjectId without constraint");
                context.Verify.Value(context.LoadOrNull<MongoTestData>(objA0) != null, "    Found by ObjectId=A0");
                context.Verify.Value(context.LoadOrNull<MongoTestData>(objA1) != null, "    Found by ObjectId=A1");
                context.Verify.Value(context.LoadOrNull<MongoTestData>(objA2) != null, "    Found by ObjectId=A2");
                context.Verify.Value(context.LoadOrNull<MongoTestData>(objC0) != null, "    Found by ObjectId=C0");

                // Load each record by string key
                if (true)
                {
                    var loadedA0 = new MongoTestKey() {RecordId = "A", RecordIndex = 0}.LoadOrNull(context, dataSetB);
                    var loadedC0 = new MongoTestKey() {RecordId = "C", RecordIndex = 0}.LoadOrNull(context, dataSetB);

                    context.Verify.Text("Load records by string key without constraint");
                    if (loadedA0 != null) context.Verify.Text($"    Version found for key=A;0: {loadedA0.Version}");
                    if (loadedC0 != null) context.Verify.Text($"    Version found for key=C;0: {loadedC0.Version}");
                }

                // Query for all records
                if (true)
                {
                    var query = context.GetQuery<MongoTestData>(dataSetB)
                        .SortBy(p => p.RecordId)
                        .SortBy(p => p.RecordIndex)
                        .AsEnumerable();

                    context.Verify.Text("Query records without constraint");
                    foreach (var obj in query)
                    {
                        var dataSetId = context.LoadOrNull<DataSetData>(obj.DataSet).DataSetId;
                        context.Verify.Text($"    Key={obj.Key} DataSet={dataSetId} Version={obj.Version}");
                    }
                }

                // Set revision time constraint
                context.DataSource.CastTo<DataSourceData>().SavedById = cutoffObjectId;

                // Get each record by ObjectId
                context.Verify.Text("Load records by ObjectId with SavedById constraint");
                context.Verify.Value(context.LoadOrNull<MongoTestData>(objA0) != null, "    Found by ObjectId=A0");
                context.Verify.Value(context.LoadOrNull<MongoTestData>(objA1) != null, "    Found by ObjectId=A1");
                context.Verify.Value(context.LoadOrNull<MongoTestData>(objA2) != null, "    Found by ObjectId=A2");
                context.Verify.Value(context.LoadOrNull<MongoTestData>(objC0) != null, "    Found by ObjectId=C0");

                // Load each record by string key
                if (true)
                {
                    var loadedA0 = new MongoTestKey() { RecordId = "A", RecordIndex = 0 }.LoadOrNull(context, dataSetB);
                    var loadedC0 = new MongoTestKey() { RecordId = "C", RecordIndex = 0 }.LoadOrNull(context, dataSetB);

                    context.Verify.Text("Load records by string key with SavedById constraint");
                    if (loadedA0 != null) context.Verify.Text($"    Version found for key=A;0: {loadedA0.Version}");
                    if (loadedC0 != null) context.Verify.Text($"    Version found for key=C;0: {loadedC0.Version}");
                }

                // Query for revised before the cutoff time
                if (true)
                {
                    var query = context.GetQuery<MongoTestData>(dataSetB)
                        .SortBy(p => p.RecordId)
                        .SortBy(p => p.RecordIndex)
                        .AsEnumerable();

                    context.Verify.Text("Query records with SavedById constraint");
                    foreach (var obj in query)
                    {
                        var dataSetId = context.LoadOrNull<DataSetData>(obj.DataSet).DataSetId;
                        context.Verify.Text($"    Key={obj.Key} DataSet={dataSetId} Version={obj.Version}");
                    }
                }

                // Clear revision time constraint before exiting to avoid an error
                // about deleting readonly database. The error occurs because
                // revision time constraint makes the data source readonly.
                context.DataSource.CastTo<DataSourceData>().SavedById = null;
            }
        }

        /// <summary>Test for the query across deleted entities.</summary>
        [Fact]
        public void QueryWithFilterOnDeletedRecord()
        {
            using (var context = new MongoTestContext(this))
            {
                context.KeepTestData = true;

                // Create datasets
                var dataSetA = context.CreateDataSet("A", context.DataSet);

                // Create initial version of the records
                context.Verify.Text("Write A;0 record to A dataset");
                SaveMinimalRecord(context, "A", "A", 0, 1);

                var keyA0 = new MongoTestKey
                {
                    RecordId = "A",
                    RecordIndex = 0
                };

                // Load from storage before deletion
                if (true)
                {
                    var loadedRecord = context.LoadOrNull(keyA0, dataSetA);
                    context.Verify.Assert(loadedRecord != null, "Record found before deletion.");
                }

                // Query before deletion
                if (true)
                {
                    var query = context.GetQuery<MongoTestData>(dataSetA)
                        .Where(p => p.Version == 1)
                        .AsEnumerable();
                    int recordCount = 0;
                    foreach (var obj in query)
                    {
                        var dataSetId = context.LoadOrNull<DataSetData>(obj.DataSet).DataSetId;
                        context.Verify.Text($"Query returned Key={obj.Key} DataSet={dataSetId} Version={obj.Version}");
                        recordCount++;
                    }

                    context.Verify.Text($"Query record count before deletion {recordCount}");
                }

                context.Verify.Text("Delete A;0 record in A dataset");
                context.Delete(keyA0, dataSetA);

                // Load from storage before deletion
                if (true)
                {
                    var loadedRecord = context.LoadOrNull(keyA0, dataSetA);
                    context.Verify.Assert(loadedRecord == null, "Record not found after deletion.");
                }

                // Query after deletion
                if (true)
                {
                    var query = context.GetQuery<MongoTestData>(dataSetA)
                        .Where(p => p.Version == 1)
                        .AsEnumerable();
                    int recordCount = 0;
                    foreach (var obj in query)
                    {
                        var dataSetId = context.LoadOrNull<DataSetData>(obj.DataSet).DataSetId;
                        context.Verify.Text($"Query returned Key={obj.Key} DataSet={dataSetId} Version={obj.Version}");
                        recordCount++;
                    }

                    context.Verify.Text($"Query record count after deletion {recordCount}");
                }
            }
        }

        /// <summary>Load the object and verify the outcome.</summary>
        private void VerifyLoad<TKey, TRecord>(IContext context, Key<TKey, TRecord> key, string dataSetId)
            where TKey : Key<TKey, TRecord>, new()
            where TRecord : Record<TKey, TRecord>
        {
            // Get dataset and try loading the record
            var dataSet = context.GetDataSet(dataSetId, context.DataSet);
            TRecord record = key.LoadOrNull(context, dataSet);

            if (record == null)
            {
                // Not found
                context.CastTo<IVerifyable>().Verify.Text($"Record {key} in dataset {dataSetId} not found.");
            }
            else
            {
                // Found, also checks that the key matches
                Assert.True(record.Key == key.ToString(),
                    $"Record found for key={key} in dataset {dataSetId} " +
                    $"has wrong key record.Key={record.Key}");
                context.CastTo<IVerifyable>().Verify.Text(
                    $"Record {key} in dataset {dataSetId} found and " +
                    $"has Type={record.GetType().Name}.");
            }
        }

        /// <summary>Query over all records of the specified type in the specified dataset.</summary>
        private void VerifyQuery<TRecord>(IContext context, string dataSetId)
            where TRecord : RecordBase
        {
            // Get dataset and query
            var dataSet = context.GetDataSet(dataSetId, context.DataSet);
            var query = context.GetQuery<TRecord>(dataSet);

            // Iterate over records
            foreach (var record in query.AsEnumerable())
            {
                context.CastTo<IVerifyable>().Verify.Text(
                    $"Record {record.Key} returned by query in dataset {dataSetId} and " +
                    $"has Type={record.GetType().Name}.");
            }
        }

        /// <summary>Two datasets and two objects, one base and one derived.</summary>
        private void SaveBasicData(IContext context)
        {
            // Create datasets
            var dataSetA = context.CreateDataSet("A", context.DataSet);
            var dataSetB = context.CreateDataSet("B", new ObjectId[] {dataSetA}, context.DataSet);

            // Create records with minimal data
            SaveBaseRecord(context, "A", "A", 0);
            SaveDerivedRecord(context, "B", "B", 0);
        }

        /// <summary>Two datasets and eight objects, split between base and derived.</summary>
        private void SaveCompleteData(IContext context)
        {
            // Create datasets
            var dataSetA = context.CreateDataSet("A", context.DataSet);
            var dataSetB = context.CreateDataSet("B", new ObjectId[] { dataSetA }, context.DataSet);
            var dataSetC = context.CreateDataSet("C", new ObjectId[] { dataSetA }, context.DataSet);
            var dataSetD = context.CreateDataSet("D", new ObjectId[] { dataSetA, dataSetB, dataSetC }, context.DataSet);

            // Create records with minimal data
            SaveBaseRecord(context, "A", "A", 0);
            SaveDerivedRecord(context, "B", "B", 0);
            SaveOtherDerivedRecord(context, "C", "C", 0);
            SaveDerivedFromDerivedRecord(context, "D", "D", 0);
            SaveBaseRecord(context, "A", "A", 1);
            SaveDerivedRecord(context, "B", "B", 1);
            SaveOtherDerivedRecord(context, "C", "C", 1);
            SaveDerivedFromDerivedRecord(context, "D", "D", 1);
            SaveBaseRecord(context, "A", "A", 2);
            SaveDerivedRecord(context, "B", "B", 2);
            SaveOtherDerivedRecord(context, "C", "C", 2);
            SaveDerivedFromDerivedRecord(context, "D", "D", 2);
            SaveBaseRecord(context, "A", "A", 3);
            SaveDerivedRecord(context, "B", "B", 3);
            SaveOtherDerivedRecord(context, "C", "C", 3);
            SaveDerivedFromDerivedRecord(context, "D", "D", 3);
        }

        /// <summary>Minimal data in multiple datasets with overlapping imports.</summary>
        private void SaveMultiDataSetData(IContext context)
        {
            // Create datasets
            var dataSetA = context.CreateDataSet("A", context.DataSet);
            var dataSetB = context.CreateDataSet("B", new ObjectId[] {dataSetA}, context.DataSet);
            var dataSetC = context.CreateDataSet("C", new ObjectId[] {dataSetA}, context.DataSet);
            var dataSetD = context.CreateDataSet("D", new ObjectId[] {dataSetA, dataSetB, dataSetC}, context.DataSet);

            // Create records
            SaveMinimalRecord(context, "A", "A", 0);
            SaveMinimalRecord(context, "A", "A", 1);
            SaveMinimalRecord(context, "B", "B", 0);
            SaveMinimalRecord(context, "B", "B", 1);
            SaveMinimalRecord(context, "C", "C", 0);
            SaveMinimalRecord(context, "C", "C", 1);
            SaveMinimalRecord(context, "D", "D", 0);
            SaveMinimalRecord(context, "D", "D", 1);
        }

        /// <summary>Save record with minimal data for testing how the records are found. </summary>
        private ObjectId SaveMinimalRecord(IContext context, string dataSetId, string recordId, int recordIndex, int? version = null)
        {
            var rec = new MongoTestData();
            rec.RecordId = recordId;
            rec.RecordIndex = recordIndex;
            rec.Version = version;

            var dataSet = context.GetDataSet(dataSetId, context.DataSet);
            context.Save(rec, dataSet);

            return rec.Id;
        }

        /// <summary>Save base record</summary>
        private ObjectId SaveBaseRecord(IContext context, string dataSetId, string recordId, int recordIndex)
        {
            var rec = new MongoTestData();
            rec.RecordId = recordId;
            rec.RecordIndex = recordIndex;
            rec.DoubleElement = 100.0;
            rec.LocalDateElement = new LocalDate(2003, 5, 1);
            rec.LocalTimeElement = new LocalTime(10, 15, 30); // 10:15:30
            rec.LocalMinuteElement = new LocalMinute(10, 15); // 10:15
            rec.LocalDateTimeElement = new LocalDateTime(2003, 5, 1, 10, 15); // 2003-05-01T10:15:00
            rec.EnumValue = MongoTestEnum.EnumValue2;

            var dataSet = context.GetDataSet(dataSetId, context.DataSet);
            context.Save(rec, dataSet);
            return rec.Id;
        }

        /// <summary>Save derived record</summary>
        private ObjectId SaveDerivedRecord(IContext context, string dataSetId, string recordId, int recordIndex)
        {
            var rec = new MongoTestDerivedData();
            rec.RecordId = recordId;
            rec.RecordIndex = recordIndex;
            rec.DoubleElement = 300.0;
            rec.LocalDateElement = new LocalDate(2003, 5, 1);
            rec.LocalTimeElement = new LocalTime(10, 15, 30); // 10:15:30
            rec.LocalMinuteElement = new LocalMinute(10, 15); // 10:15
            rec.LocalDateTimeElement = new LocalDateTime(2003, 5, 1, 10, 15); // 2003-05-01T10:15:00
            rec.StringElement2 = String.Empty; // Test how empty value is recorded
            rec.DoubleElement2 = 200.0;

            // String collections
            rec.ArrayOfString = new string[] { "A", "B", "C" };
            rec.ListOfString = new List<string>() { "A", "B", "C" };

            // Double collections
            rec.ArrayOfDouble = new double[] { 1.0, 2.0, 3.0 };
            rec.ListOfDouble = new List<double>() { 1.0, 2.0, 3.0 };
            rec.ListOfNullableDouble = new List<double?>();
            rec.ListOfNullableDouble.Add(10.0);
            rec.ListOfNullableDouble.Add(null);
            rec.ListOfNullableDouble.Add(30.0);

            // Data element
            rec.DataElement = new MongoTestElementData();
            rec.DataElement.DoubleElement3 = 1.0;
            rec.DataElement.StringElement3 = "AA";

            // Data element list
            rec.DataElementList = new List<MongoTestElementData>();
            var elementList0 = new MongoTestElementData();
            elementList0.DoubleElement3 = 1.0;
            elementList0.StringElement3 = "A0";
            rec.DataElementList.Add(elementList0);
            var elementList1 = new MongoTestElementData();
            elementList1.DoubleElement3 = 2.0;
            elementList1.StringElement3 = "A1";
            rec.DataElementList.Add(elementList1);

            // Key element
            rec.KeyElement = new MongoTestKey();
            rec.KeyElement.RecordId = "BB";
            rec.KeyElement.RecordIndex = 2;

            // Key element list
            rec.KeyElementList = new List<MongoTestKey>();
            var keyList0 = new MongoTestKey();
            keyList0.RecordId = "B0";
            keyList0.RecordIndex = 3;
            rec.KeyElementList.Add(keyList0);
            var keyList1 = new MongoTestKey();
            keyList1.RecordId = "B1";
            keyList1.RecordIndex = 4;
            rec.KeyElementList.Add(keyList1);

            var dataSet = context.GetDataSet(dataSetId, context.DataSet);
            context.Save(rec, dataSet);
            return rec.Id;
        }

        /// <summary>Save other derived record.</summary>
        private ObjectId SaveOtherDerivedRecord(IContext context, string dataSetId, string recordId, int recordIndex)
        {
            var rec = new MongoTestOtherDerivedData();
            rec.RecordId = recordId;
            rec.RecordIndex = recordIndex;
            rec.DoubleElement = 300.0;
            rec.LocalDateElement = new LocalDate(2003, 5, 1);
            rec.LocalTimeElement = new LocalTime(10, 15, 30); // 10:15:30
            rec.LocalMinuteElement = new LocalMinute(10, 15); // 10:15
            rec.LocalDateTimeElement = new LocalDateTime(2003, 5, 1, 10, 15); // 2003-05-01T10:15:00
            rec.OtherStringElement2 = String.Empty; // Test how empty value is recorded
            rec.OtherDoubleElement2 = 200.0;

            var dataSet = context.GetDataSet(dataSetId, context.DataSet);
            context.Save(rec, dataSet);
            return rec.Id;
        }

        /// <summary>Save record that is derived from derived.</summary>
        private ObjectId SaveDerivedFromDerivedRecord(IContext context, string dataSetId, string recordId, int recordIndex)
        {
            var rec = new MongoTestDerivedFromDerivedData();
            rec.RecordId = recordId;
            rec.RecordIndex = recordIndex;
            rec.DoubleElement = 300.0;
            rec.LocalDateElement = new LocalDate(2003, 5, 1);
            rec.LocalTimeElement = new LocalTime(10, 15, 30); // 10:15:30
            rec.LocalMinuteElement = new LocalMinute(10, 15); // 10:15
            rec.LocalDateTimeElement = new LocalDateTime(2003, 5, 1, 10, 15); // 2003-05-01T10:15:00
            rec.OtherStringElement3 = String.Empty; // Test how empty value is recorded
            rec.OtherDoubleElement3 = 200.0;

            var dataSet = context.GetDataSet(dataSetId, context.DataSet);
            context.Save(rec, dataSet);
            return rec.Id;
        }
    }
}
