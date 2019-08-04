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
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using Xunit;
using DataCentric;
using MongoDB.Driver.Linq;


namespace DataCentric.Test
{
    /// <summary>Unit tests for the native functionality of the MongoDB driver.</summary>
    public class MongoPerformanceTest
    {
        private static int recordCount_ = 10; // 300_000;
        private static int dataSetCount_ = 2; //10;
        private static int versionCount_ = 2; // 10;
        private static int arraySize_ = 10; // 1_000;

        public class A
        {
            [BsonId]
            public string KeyElement { get; set; }
            public double DoubleElement { get; set; }
            public double IntElement { get; set; }
            public List<double> ArrayElement { get; set; }
            public int VersionElement { get; set; }
        }

        public class B
        {
            [BsonId]
            public ObjectId ID { get; set; }
            public ObjectId DataSet { get; set; }
            public string KeyElement { get; set; }
            public string StringElement1 { get; set; }
            public string StringElement2 { get; set; }
            public double DoubleElement { get; set; }
            public double IntElement { get; set; }
            public List<double> ArrayElement { get; set; }
            public int VersionElement { get; set; }
        }

        public class Cursor
        {
            [BsonId]
            public ObjectId ID { get; set; }
            public string KeyElement { get; set; }
        }

        /// <summary>Create DB instance.</summary>
        public IMongoDatabase GetDb(IContext context)
        {
            var result = context.DataSource.CastTo<MongoDataSourceBaseData>().Db;
            return result;
        }

        /// <summary>Insert N non-versioned instances.</summary>
        public void InsertRecordsA(IContext context)
        {
            var db = GetDb(context);

            List<A> records = new List<A>();
            for (int recordIndex = 0; recordIndex < recordCount_; ++recordIndex)
            {
                var rec = new A();
                rec.KeyElement = String.Concat("KeyPrefix", recordIndex);
                rec.DoubleElement = recordIndex;
                rec.IntElement = recordIndex;
                if (arraySize_ > 0)
                {
                    rec.ArrayElement = new List<double>();
                    for (int i = 0; i < arraySize_; ++i)
                    {
                        rec.ArrayElement.Add(i);
                    }
                }
                rec.VersionElement = 0;
                records.Add(rec);
            }

            // Unique index on _id is created automatically
            var collection = db.GetCollection<A>("A");
            var indexOptions = new CreateIndexOptions();
            var indexKeys = Builders<A>.IndexKeys.Descending(p => p.DoubleElement);
            var indexModel = new CreateIndexModel<A>(indexKeys, indexOptions);
            collection.Indexes.CreateOne(indexModel);
            collection.InsertMany(records);

            context.CastTo<IVerifyable>().Verify.Text($"Inserted {records.Count} records.");
        }

        /// <summary>Insert M copies of each of N versioned instances B.</summary>
        public void InsertRecordsB(IContext context)
        {
            var db = GetDb(context);
            var collection = db.GetCollection<B>("B");
            if (false)
            {
                var indexOptions = new CreateIndexOptions();
                var indexKeys = Builders<B>.IndexKeys.Descending(p => p.DataSet).Descending(p => p.ID);
                var indexModel = new CreateIndexModel<B>(indexKeys, indexOptions);
                collection.Indexes.CreateOne(indexModel);
            }
            if (true)
            {
                var indexOptions = new CreateIndexOptions();
                var indexKeys = Builders<B>.IndexKeys.Ascending(p => p.KeyElement).Descending(p => p.DataSet).Descending(p => p.ID);
                var indexModel = new CreateIndexModel<B>(indexKeys, indexOptions);
                collection.Indexes.CreateOne(indexModel);
            }
            if (true)
            {
                var indexOptions = new CreateIndexOptions();
                var indexKeys = Builders<B>.IndexKeys
                    .Ascending(p => p.StringElement1)
                    .Ascending(p => p.StringElement2)
                    .Ascending(p => p.DoubleElement)
                    .Ascending(p => p.IntElement)
                    // .Ascending(p => p.KeyElement)
                    .Descending(p => p.DataSet).Descending(p => p.ID);
                var indexModel = new CreateIndexModel<B>(indexKeys, indexOptions);
                collection.Indexes.CreateOne(indexModel);
            }

            List<B> records = new List<B>();
            for (int dataSetIndex = 0; dataSetIndex < dataSetCount_; ++dataSetIndex)
            {
                ObjectId dataSet = ObjectId.GenerateNewId();
                for (int versionIndex = 0; versionIndex < versionCount_; ++versionIndex)
                {
                    for (int recordIndex = 0; recordIndex < recordCount_; ++recordIndex)
                    {
                        var rec = new B();
                        rec.ID = ObjectId.GenerateNewId();
                        rec.DataSet = dataSet;
                        rec.KeyElement = String.Concat("KeyPrefix", recordIndex);
                        rec.StringElement1 = (recordIndex % 2).ToString();
                        rec.StringElement2 = (recordIndex % 3).ToString();
                        rec.DoubleElement = recordIndex;
                        rec.IntElement = recordIndex;
                        if (arraySize_ > 0)
                        {
                            rec.ArrayElement = new List<double>();
                            for (int i = 0; i < arraySize_; ++i)
                            {
                                rec.ArrayElement.Add(i);
                            }
                        }
                        rec.VersionElement = versionIndex;
                        records.Add(rec);
                    }
                }
            }
            collection.InsertMany(records);

            context.CastTo<IVerifyable>().Verify.Text($"Inserted {records.Count} record versions.");
        }

        /// <summary>Insert N non-versioned instances.</summary>
        [Fact]
        public void InsertA()
        {
            using (var context = new DataTestContext(this))
            {
                InsertRecordsA(context);
            }
        }

        /// <summary>Insert M copies of each of N versioned instances B.</summary>
        [Fact]
        public void InsertB()
        {
            using (var context = new DataTestContext(this))
            {
                InsertRecordsB(context);
            }
        }

        /// <summary>Run FindOne for each of the non-versioned instances.</summary>
        [Fact]
        public void FindOneA()
        {
            using (var context = new DataTestContext(this))
            {
                InsertRecordsA(context);

                var db = GetDb(context);
                var collection = db.GetCollection<A>("A");
                int count = 0;
                double sum = 0.0;
                for (int recordIndex = 0; recordIndex < recordCount_; ++recordIndex)
                {
                    string key = String.Concat("KeyPrefix", recordIndex);
                    var obj = collection.Find(p => p.KeyElement == key).SingleOrDefault();

                    count++;
                    sum += obj.DoubleElement;
                }

                context.Verify.Text($"Found {count} records.");
                context.Verify.Value(sum, "Sum(DoubleElement)");
            }
        }

        /// <summary>Run FindOne for last version of each of the versioned instances B.</summary>
        [Fact]
        public void FindOneB()
        {
            using (var context = new DataTestContext(this))
            {
                InsertRecordsB(context);

                var db = GetDb(context);
                var collection = db.GetCollection<B>("B");

                int count = 0;
                double sum = 0.0;
                for (int recordIndex = 0; recordIndex < recordCount_; ++recordIndex)
                {
                    string key = String.Concat("KeyPrefix", recordIndex);
                    var query = collection.AsQueryable().Where(p => p.KeyElement == key).OrderByDescending(p => p.DataSet).ThenByDescending(p => p.ID);
                    var obj = query.FirstOrDefault();

                    count++;
                    sum += obj.DoubleElement;
                }

                context.Verify.Text($"Found {count} records.");
                context.Verify.Value(sum, "Sum(DoubleElement)");
            }
        }

        /// <summary>Run one step query for each of the non-versioned instances.</summary>
        [Fact]
        public void OneStepQueryA()
        {
            using (var context = new DataTestContext(this))
            {
                InsertRecordsA(context);

                var db = GetDb(context);
                var collection = db.GetCollection<A>("A");
                int count = 0;
                double sum = 0.0;

                var query = collection.AsQueryable();
                    // .Where(p => p.DoubleElement < 10);
                foreach (var obj in query)
                {
                    count++;
                    sum += obj.DoubleElement;
                }

                context.Verify.Text($"Query returned {count} records.");
                context.Verify.Value(sum, "Sum(DoubleElement)");
            }
        }

        /// <summary>Run one step query for each of the versioned instances B.</summary>
        [Fact]
        public void OneStepQueryB()
        {
            using (var context = new DataTestContext(this))
            {
                InsertRecordsB(context);

                var db = GetDb(context);
                var collection = db.GetCollection<B>("B");
                int count = 0;
                double sum = 0.0;

                var query = collection.AsQueryable()
                    .Where(p => p.StringElement1 == "1")
                    .Where(p => p.StringElement2 == "2")
                    // .Where(p => p.IntElement >= 38*recordCount_/100)
                    // .Where(p => p.DoubleElement < 40*recordCount_ / 100)
                    // .Where(p => p.IntElement >= 2 * recordCount_ / 100)
                    // .Where(p => p.IntElement < 40 * recordCount_ / 100)
                    .OrderBy(p => p.StringElement1)
                    .ThenBy(p => p.StringElement2)
                    .ThenBy(p => p.DoubleElement)
                    .ThenBy(p => p.IntElement)
                    .ThenByDescending(p => p.DataSet)
                    .ThenByDescending(p => p.ID);

                HashSet<string> keys = new HashSet<string>();
                foreach (var obj in query)
                {
                    if (keys.Add(obj.KeyElement))
                    {
                        sum += obj.DoubleElement;
                        count++;
                    }
                }

                context.Verify.Text($"Query returned {count} records.");
                context.Verify.Value(sum, "Sum(DoubleElement)");
            }
        }

        /// <summary>Run two step query for each of the versioned instances B.</summary>
        [Fact]
        public void TwoStepQueryB()
        {
            using (var context = new DataTestContext(this))
            {
                InsertRecordsB(context);

                var db = GetDb(context);
                var collection = db.GetCollection<B>("B");
                double sum = 0.0;

                var query = collection.AsQueryable()
                    .Where(p => p.StringElement1 == "1")
                    .Where(p => p.StringElement2 == "2")
                    // .Where(p => p.IntElement >= 38*recordCount_/100)
                    // .Where(p => p.DoubleElement < 40*recordCount_ / 100)
                    // .Where(p => p.IntElement >= 2 * recordCount_ / 100)
                    // .Where(p => p.IntElement < 40 * recordCount_ / 100)
                    .OrderBy(p => p.StringElement1)
                    .ThenBy(p => p.StringElement2)
                    .ThenBy(p => p.DoubleElement)
                    .ThenBy(p => p.IntElement)
                    .ThenByDescending(p => p.DataSet)
                    .ThenByDescending(p => p.ID)
                    .Select(p => new Cursor {ID = p.ID, KeyElement = p.KeyElement});

                // Get ObjectIds of the query results
                int objectIdCount = 0;
                HashSet<string> keys = new HashSet<string>();
                List<ObjectId> objectIds = new List<ObjectId>();
                foreach (var obj in query)
                {
                    if (keys.Add(obj.KeyElement))
                    {
                        objectIds.Add(obj.ID);
                        objectIdCount++;
                    }
                }

                // Iterate over ObjectIds
                int recordCount = 0;
                var recordQuery = collection.AsQueryable()
                    .Where(p => objectIds.Contains(p.ID));
                foreach (var record in recordQuery)
                {
                    sum += record.DoubleElement;
                    recordCount++;
                }

                context.Verify.Text($"Query returned {recordCount} records from {objectIdCount} ObjectIds.");
                context.Verify.Value(sum, "Sum(DoubleElement)");
            }
        }
    }
}
