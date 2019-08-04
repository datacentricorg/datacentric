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
    /// <summary>Unit test for MongoDataSourceData.</summary>
    public class MongoTest
    {
        private string Prefix { get => GetType().Name; }

        /// <summary>Smoke test.</summary>
        [Fact]
        public void Smoke()
        {
            using (var context = new DataTestContext(this))
            {
                SaveBasicData(context);

                // Get dataset identifiers
                var dataSetA = context.GetDataSet("A", context.DataSet);
                var dataSetB = context.GetDataSet("B", context.DataSet);

                // Create keys
                var keyA0 = new MongoTestKey()
                {
                    RecordID = "A",
                    RecordIndex = 0
                };
                var keyB0 = new MongoTestKey()
                {
                    RecordID = "B",
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
            using (var context = new DataTestContext(this))
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

                // Query for RecordID=B
                var query = context.GetQuery<MongoTestData>(dataSetD)
                    .Where(p => p.RecordID == "B")
                    .SortBy(p => p.RecordID)
                    .SortBy(p => p.RecordIndex)
                    .AsEnumerable();

                foreach (var obj in query)
                {
                    var dataSetID = context.LoadOrNull<DataSetData>(obj.DataSet).DataSetID;
                    context.Verify.Text($"Key={obj.Key} DataSet={dataSetID} Version={obj.Version}");
                }
            }
        }

        /// <summary>Test of CreateOrderedObjectId method.</summary>
        [Fact]
        public void CreateOrderedObjectId()
        {
            using (var context = new DataTestContext(this))
            {
                for (int i = 0; i < 10_000; ++i)
                {
                    // Invoke 10,000 times to ensure there is no log report of non-increasing ObjectId
                    context.DataSource.CreateOrderedObjectId();
                }

                // Confirm no log output indicating non-increasing ID from inside the for loop
                context.Verify.Text("Log should have no lines before this one.");
            }
        }

        /// <summary>Test delete marker.</summary>
        [Fact]
        public void Delete()
        {
            using (var context = new DataTestContext(this))
            {
                SaveBasicData(context);

                // Get dataset identifiers
                var dataSetA = context.GetDataSet("A", context.DataSet);
                var dataSetB = context.GetDataSet("B", context.DataSet);

                // Create keys
                var keyA0 = new MongoTestKey()
                {
                    RecordID = "A",
                    RecordIndex = 0
                };
                var keyB0 = new MongoTestKey()
                {
                    RecordID = "B",
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
            using (var context = new DataTestContext(this))
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
                    RecordID = "A",
                    RecordIndex = 0
                };
                var keyB0 = new MongoTestKey()
                {
                    RecordID = "B",
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
            using (var context = new DataTestContext(this))
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
                    .Where(p => p.KeyElement == new MongoTestKey() {RecordID = "BB", RecordIndex = 2})
                    .SortBy(p => p.RecordID);

                foreach (var obj in testQuery.AsEnumerable())
                {
                    context.Verify.Text($"Key={obj.Key} Type={obj.GetType().Name.Replace(Prefix, String.Empty)}");
                }
            }
        }

        /// <summary>Test polymorphic load and query.</summary>
        [Fact]
        public void PolymorphicQuery()
        {
            using (var context = new DataTestContext(this))
            {
                // Saves data in A and B datasets, A is an import of B
                SaveCompleteData(context);

                // Look in B dataset
                var dataSetD = context.GetDataSetOrEmpty("D", context.DataSet);

                // Load record of derived types by base
                context.Verify.Text("Load all types by key to type A");
                {
                    var key = new MongoTestKey {RecordID = "A", RecordIndex = 0};
                    var obj = context.LoadOrNull(key, dataSetD);
                    context.Verify.Text($"    Key={obj.Key} Type={obj.GetType().Name.Replace(Prefix,String.Empty)}");
                }
                {
                    var key = new MongoTestKey { RecordID = "B", RecordIndex = 0 };
                    var obj = context.LoadOrNull(key, dataSetD);
                    context.Verify.Text($"    Key={obj.Key} Type={obj.GetType().Name.Replace(Prefix, String.Empty)}");
                }
                {
                    var key = new MongoTestKey { RecordID = "C", RecordIndex = 0 };
                    var obj = context.LoadOrNull(key, dataSetD);
                    context.Verify.Text($"    Key={obj.Key} Type={obj.GetType().Name.Replace(Prefix, String.Empty)}");
                }
                {
                    var key = new MongoTestKey { RecordID = "D", RecordIndex = 0 };
                    var obj = context.LoadOrNull(key, dataSetD);
                    context.Verify.Text($"    Key={obj.Key} Type={obj.GetType().Name.Replace(Prefix, String.Empty)}");
                }
                {
                    context.Verify.Text("Query by MongoTestData, unconstrained");
                    var query = context.GetQuery<MongoTestData>(dataSetD);
                    foreach (var obj in query.AsEnumerable())
                    {
                        context.Verify.Text($"    Key={obj.Key} Type={obj.GetType().Name.Replace(Prefix, String.Empty)}");
                    }
                }
                {
                    context.Verify.Text("Query by MongoTestDerivedData : MongoTestData which also picks up MongoTestDerivedFromDerivedData : MongoTestDerivedData, unconstrained");
                    var query = context.GetQuery<MongoTestDerivedData>(dataSetD);
                    foreach (var obj in query.AsEnumerable())
                    {
                        context.Verify.Text($"    Key={obj.Key} Type={obj.GetType().Name.Replace(Prefix, String.Empty)}");
                    }
                }
                {
                    context.Verify.Text("Query by MongoTestOtherDerivedData : MongoTestData, unconstrained");
                    var query = context.GetQuery<MongoTestOtherDerivedData>(dataSetD);
                    foreach (var obj in query.AsEnumerable())
                    {
                        context.Verify.Text($"    Key={obj.Key} Type={obj.GetType().Name.Replace(Prefix, String.Empty)}");
                    }
                }
                {
                    context.Verify.Text("Query by MongoTestDerivedFromDerivedData : MongoTestDerivedData, where MongoTestDerivedData : MongoTestData, unconstrained");
                    var query = context.GetQuery<MongoTestDerivedFromDerivedData>(dataSetD);
                    foreach (var obj in query.AsEnumerable())
                    {
                        context.Verify.Text($"    Key={obj.Key} Type={obj.GetType().Name.Replace(Prefix, String.Empty)}");
                    }
                }
            }
        }

        /// <summary>Test sorting.</summary>
        [Fact]
        public void Sort()
        {
            using (var context = new DataTestContext(this))
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
                        $"Key={obj.Key} Type={obj.GetType().Name.Replace(Prefix, String.Empty)}");
                }
            }
        }


        /// <summary>Test for SavedBy filtering.</summary>
        [Fact]
        public void SavedBy()
        {
            using (var context = new DataTestContext(this))
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
                    var loadedA0 = new MongoTestKey() {RecordID = "A", RecordIndex = 0}.LoadOrNull(context, dataSetB);
                    var loadedC0 = new MongoTestKey() {RecordID = "C", RecordIndex = 0}.LoadOrNull(context, dataSetB);

                    context.Verify.Text("Load records by string key without constraint");
                    if (loadedA0 != null) context.Verify.Text($"    Version found for key=A;0: {loadedA0.Version}");
                    if (loadedC0 != null) context.Verify.Text($"    Version found for key=C;0: {loadedC0.Version}");
                }

                // Query for all records
                if (true)
                {
                    var query = context.GetQuery<MongoTestData>(dataSetB)
                        .SortBy(p => p.RecordID)
                        .SortBy(p => p.RecordIndex)
                        .AsEnumerable();

                    context.Verify.Text("Query records without constraint");
                    foreach (var obj in query)
                    {
                        var dataSetID = context.LoadOrNull<DataSetData>(obj.DataSet).DataSetID;
                        context.Verify.Text($"    Key={obj.Key} DataSet={dataSetID} Version={obj.Version}");
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
                    var loadedA0 = new MongoTestKey() { RecordID = "A", RecordIndex = 0 }.LoadOrNull(context, dataSetB);
                    var loadedC0 = new MongoTestKey() { RecordID = "C", RecordIndex = 0 }.LoadOrNull(context, dataSetB);

                    context.Verify.Text("Load records by string key with SavedById constraint");
                    if (loadedA0 != null) context.Verify.Text($"    Version found for key=A;0: {loadedA0.Version}");
                    if (loadedC0 != null) context.Verify.Text($"    Version found for key=C;0: {loadedC0.Version}");
                }

                // Query for revised before the cutoff time
                if (true)
                {
                    var query = context.GetQuery<MongoTestData>(dataSetB)
                        .SortBy(p => p.RecordID)
                        .SortBy(p => p.RecordIndex)
                        .AsEnumerable();

                    context.Verify.Text("Query records with SavedById constraint");
                    foreach (var obj in query)
                    {
                        var dataSetID = context.LoadOrNull<DataSetData>(obj.DataSet).DataSetID;
                        context.Verify.Text($"    Key={obj.Key} DataSet={dataSetID} Version={obj.Version}");
                    }
                }

                // ear revision time constraint before exiting to avoid an error
                // about deleting readonly database. The error occurs because
                // revision time constraint makes the data source readonly.
                context.DataSource.CastTo<DataSourceData>().SavedById = null;
            }
        }

        /// <summary>Load the object and verify the outcome.</summary>
        private void VerifyLoad<TKey, TRecord>(IUnitTestContext context, Key<TKey, TRecord> key, string dataSetID)
            where TKey : Key<TKey, TRecord>, new()
            where TRecord : Record<TKey, TRecord>
        {
            // Get dataset and try loading the record
            var dataSet = context.GetDataSet(dataSetID, context.DataSet);
            TRecord record = key.LoadOrNull(context, dataSet);

            if (record == null)
            {
                // Not found
                context.Verify.Text($"Record {key} in dataset {dataSetID} not found.");
            }
            else
            {
                // Found, also checks that the key matches
                Assert.True(record.Key == key.ToString(),
                    $"Record found for key={key} in dataset {dataSetID} " +
                    $"has wrong key record.Key={record.Key}");
                context.Verify.Text(
                    $"Record {key} in dataset {dataSetID} found and " +
                    $"has Type={record.GetType().Name.Replace(Prefix, String.Empty)}.");
            }
        }

        /// <summary>Query over all records of the specified type in the specified dataset.</summary>
        private void VerifyQuery<TRecord>(IUnitTestContext context, string dataSetID)
            where TRecord : RecordBase
        {
            // Get dataset and query
            var dataSet = context.GetDataSet(dataSetID, context.DataSet);
            var query = context.GetQuery<TRecord>(dataSet);

            // Iterate over records
            foreach (var record in query.AsEnumerable())
            {
                context.Verify.Text(
                    $"Record {record.Key} returned by query in dataset {dataSetID} and " +
                    $"has Type={record.GetType().Name.Replace(Prefix, String.Empty)}.");
            }
        }

        /// <summary>Two datasets and two objects, one base and one derived.</summary>
        private void SaveBasicData(IUnitTestContext context)
        {
            // Create datasets
            var dataSetA = context.CreateDataSet("A", context.DataSet);
            var dataSetB = context.CreateDataSet("B", new ObjectId[] {dataSetA}, context.DataSet);

            // Create records with minimal data
            SaveBaseRecord(context, "A", "A", 0);
            SaveDerivedRecord(context, "B", "B", 0);
        }

        /// <summary>Two datasets and eight objects, split between base and derived.</summary>
        private void SaveCompleteData(IUnitTestContext context)
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
        private void SaveMultiDataSetData(IUnitTestContext context)
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
        private ObjectId SaveMinimalRecord(IUnitTestContext context, string dataSetID, string recordID, int recordIndex, int? version = null)
        {
            var rec = new MongoTestData();
            rec.RecordID = recordID;
            rec.RecordIndex = recordIndex;
            rec.Version = version;

            var dataSet = context.GetDataSet(dataSetID, context.DataSet);
            context.Save(rec, dataSet);

            return rec.ID;
        }

        /// <summary>Save base record</summary>
        private ObjectId SaveBaseRecord(IUnitTestContext context, string dataSetID, string recordID, int recordIndex)
        {
            var rec = new MongoTestData();
            rec.RecordID = recordID;
            rec.RecordIndex = recordIndex;
            rec.DoubleElement = 100.0;
            rec.LocalDateElement = new LocalDate(2003, 5, 1);
            rec.LocalTimeElement = new LocalTime(10, 15, 30); // 10:15:30
            rec.LocalMinuteElement = new LocalMinute(10, 15); // 10:15
            rec.LocalDateTimeElement = new LocalDateTime(2003, 5, 1, 10, 15); // 2003-05-01T10:15:00
            rec.EnumValue = MongoTestEnum.EnumValue2;

            var dataSet = context.GetDataSet(dataSetID, context.DataSet);
            context.Save(rec, dataSet);
            return rec.ID;
        }

        /// <summary>Save derived record</summary>
        private ObjectId SaveDerivedRecord(IUnitTestContext context, string dataSetID, string recordID, int recordIndex)
        {
            var rec = new MongoTestDerivedData();
            rec.RecordID = recordID;
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
            rec.KeyElement.RecordID = "BB";
            rec.KeyElement.RecordIndex = 2;

            // Key element list
            rec.KeyElementList = new List<MongoTestKey>();
            var keyList0 = new MongoTestKey();
            keyList0.RecordID = "B0";
            keyList0.RecordIndex = 3;
            rec.KeyElementList.Add(keyList0);
            var keyList1 = new MongoTestKey();
            keyList1.RecordID = "B1";
            keyList1.RecordIndex = 4;
            rec.KeyElementList.Add(keyList1);

            var dataSet = context.GetDataSet(dataSetID, context.DataSet);
            context.Save(rec, dataSet);
            return rec.ID;
        }

        /// <summary>Save other derived record.</summary>
        private ObjectId SaveOtherDerivedRecord(IUnitTestContext context, string dataSetID, string recordID, int recordIndex)
        {
            var rec = new MongoTestOtherDerivedData();
            rec.RecordID = recordID;
            rec.RecordIndex = recordIndex;
            rec.DoubleElement = 300.0;
            rec.LocalDateElement = new LocalDate(2003, 5, 1);
            rec.LocalTimeElement = new LocalTime(10, 15, 30); // 10:15:30
            rec.LocalMinuteElement = new LocalMinute(10, 15); // 10:15
            rec.LocalDateTimeElement = new LocalDateTime(2003, 5, 1, 10, 15); // 2003-05-01T10:15:00
            rec.OtherStringElement2 = String.Empty; // Test how empty value is recorded
            rec.OtherDoubleElement2 = 200.0;

            var dataSet = context.GetDataSet(dataSetID, context.DataSet);
            context.Save(rec, dataSet);
            return rec.ID;
        }

        /// <summary>Save record that is derived from derived.</summary>
        private ObjectId SaveDerivedFromDerivedRecord(IUnitTestContext context, string dataSetID, string recordID, int recordIndex)
        {
            var rec = new MongoTestDerivedFromDerivedData();
            rec.RecordID = recordID;
            rec.RecordIndex = recordIndex;
            rec.DoubleElement = 300.0;
            rec.LocalDateElement = new LocalDate(2003, 5, 1);
            rec.LocalTimeElement = new LocalTime(10, 15, 30); // 10:15:30
            rec.LocalMinuteElement = new LocalMinute(10, 15); // 10:15
            rec.LocalDateTimeElement = new LocalDateTime(2003, 5, 1, 10, 15); // 2003-05-01T10:15:00
            rec.OtherStringElement3 = String.Empty; // Test how empty value is recorded
            rec.OtherDoubleElement3 = 200.0;

            var dataSet = context.GetDataSet(dataSetID, context.DataSet);
            context.Save(rec, dataSet);
            return rec.ID;
        }
    }
}
