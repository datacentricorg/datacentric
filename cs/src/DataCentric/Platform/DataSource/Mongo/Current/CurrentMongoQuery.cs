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
using System.Linq.Expressions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace DataCentric
{
    /// <summary>
    /// Implements IQuery for Current MongoDB data source.
    /// 
    /// This implementation combines methods of IQueryable(TRecord) with
    /// additional constraints and ordering to retrieve the correct version
    /// of the record across multiple datasets.
    /// </summary>
    public class CurrentMongoQuery<TRecord> : IQuery<TRecord>
        where TRecord : Record
    {
        private readonly CurrentMongoCollection<TRecord> collection_;
        private readonly ObjectId loadFrom_;
        private readonly IQueryable<TRecord> queryable_;
        private readonly IOrderedQueryable<TRecord> orderedQueryable_;

        //--- CONSTRUCTORS

        /// <summary>
        /// Create query from collection and dataset.
        /// </summary>
        public CurrentMongoQuery(CurrentMongoCollection<TRecord> collection, ObjectId loadFrom)
        {
            collection_ = collection;
            loadFrom_ = loadFrom;

            // Create queryable from typed collection rather than base
            // collection so LINQ queries can be applied to properties
            // of the generic parameter type TRecord
            var queryable = collection_.TypedCollection.AsQueryable();

            // Without explicitly applying OfType, the match for _t
            // may or may not be added. This step ensures the match
            // for _t exists and is the first stage in the aggregation
            // pipeline
            queryable_ = queryable.OfType<TRecord>();
        }

        /// <summary>
        /// Create from collection for the base type Record, the lookup
        /// list of datasets, and IQueryable(TRecord).
        ///
        /// This constructor is private and is intended for use by the
        /// implementation of this class only.
        /// </summary>
        private CurrentMongoQuery(CurrentMongoCollection<TRecord> collection, ObjectId loadFrom, IQueryable<TRecord> queryable)
        {
            if (queryable == null)
                throw new Exception(
                    "Attempting to create a query from a null queryable.");

            collection_ = collection;
            loadFrom_ = loadFrom;
            queryable_ = queryable;
        }

        /// <summary>
        /// Create from collection for the base type Record, the lookup
        /// list of datasets, and IOrderedQueryable(TRecord).
        ///
        /// This constructor is private and is intended for use by the
        /// implementation of this class only.
        /// </summary>
        private CurrentMongoQuery(CurrentMongoCollection<TRecord> collection, ObjectId loadFrom, IOrderedQueryable<TRecord> orderedQueryable)
        {
            if (orderedQueryable == null)
                throw new Exception(
                    "Attempting to create a query from a null orderedQueryable.");

            collection_ = collection;
            loadFrom_ = loadFrom;
            orderedQueryable_ = orderedQueryable;
        }

        //--- PROPERTIES

        /// <summary>Context for which this interface is defined.
        /// Use to access other interfaces of the same context.</summary>
        public IContext Context { get => collection_.DataSource.Context; }

        //--- METHODS

        /// <summary>Filters a sequence of values based on a predicate.</summary>
        public IQuery<TRecord> Where(Expression<Func<TRecord, bool>> predicate)
        {
            if (queryable_ != null && orderedQueryable_ == null)
            {
                return new CurrentMongoQuery<TRecord>(collection_, loadFrom_, queryable_.Where(predicate));
            }
            else if (queryable_ == null && orderedQueryable_ != null)
            {
                throw new Exception(
                    "All Where(...) clauses of the query must precede " +
                    "SortBy(...) or SortByDescending(...) clauses of the same query.");
            }
            else
            {
                throw new Exception(
                    "Strictly one of queryable_ or orderedQueryable_ can " +
                    "have value, not both and not neither.");
            }
        }

        /// <summary>Sorts the elements of a sequence in ascending order according to the selected key.</summary>
        public IQuery<TRecord> SortBy<TProperty>(Expression<Func<TRecord, TProperty>> keySelector)
        {
            if (queryable_ != null && orderedQueryable_ == null)
            {
                // Apply dataset constraint before ordering
                var queryableWithDataSetConstraint = collection_.DataSource.ApplyFinalConstraints(queryable_, loadFrom_);

                // First SortBy clause, use OrderBy of queryable_
                return new CurrentMongoQuery<TRecord>(collection_, loadFrom_, queryableWithDataSetConstraint.OrderBy(keySelector));
            }
            else if (queryable_ == null && orderedQueryable_ != null)
            {
                // Subsequent SortBy clauses, use ThenBy of the orderedQueryable_
                return new CurrentMongoQuery<TRecord>(collection_, loadFrom_, orderedQueryable_.ThenBy(keySelector));
            }
            else
            {
                throw new Exception(
                    "Strictly one of queryable_ or orderedQueryable_ can " +
                    "have value, not both and not neither.");
            }
        }

        /// <summary>Sorts the elements of a sequence in descending order according to the selected key.</summary>
        public IQuery<TRecord> SortByDescending<TProperty>(Expression<Func<TRecord, TProperty>> keySelector)
        {
            if (queryable_ != null && orderedQueryable_ == null)
            {
                // Apply dataset constraint before ordering
                var queryableWithDataSetConstraint = collection_.DataSource.ApplyFinalConstraints(queryable_, loadFrom_);

                // First SortBy clause, use OrderBy of queryable_
                return new CurrentMongoQuery<TRecord>(collection_, loadFrom_, queryableWithDataSetConstraint.OrderByDescending(keySelector));
            }
            else if (queryable_ == null && orderedQueryable_ != null)
            {
                // Subsequent SortBy clauses, use ThenBy of the orderedQueryable_
                return new CurrentMongoQuery<TRecord>(collection_, loadFrom_, orderedQueryable_.ThenByDescending(keySelector));
            }
            else
            {
                throw new Exception(
                    "Strictly one of queryable_ or orderedQueryable_ can " +
                    "have value, not both and not neither.");
            }
        }

        /// <summary>Convert query to enumerable so iteration can be performed.</summary>
        public IEnumerable<TRecord> AsEnumerable()
        {
            IOrderedQueryable<TRecord> orderedQueryable = null;
            if (queryable_ != null && orderedQueryable_ == null)
            {
                // Apply dataset constraint before ordering
                var queryableWithDataSetConstraint = collection_.DataSource.ApplyFinalConstraints(queryable_, loadFrom_);

                // Perform ordering by Key, DataSet, and ID.
                //
                // Because we are created the ordered queryable for
                // the first time, begin from OrderBy, not ThenBy
                orderedQueryable = queryableWithDataSetConstraint
                    .OrderBy(p => p.Key) // _key
                    .ThenByDescending(p => p.DataSet) // _dataset
                    .ThenByDescending(p => p.Id); // _id
            }
            else if (queryable_ == null && orderedQueryable_ != null)
            {
                // Perform ordering by Key, DataSet, and ID.
                //
                // Because we are using an existing ordered
                // queryable, begin from ThenBy, not OrderBy
                orderedQueryable = orderedQueryable_
                    .ThenBy(p => p.Key) // _key
                    .ThenByDescending(p => p.DataSet) // _dataset
                    .ThenByDescending(p => p.Id); // _id
            }
            else
            {
                throw new Exception(
                    "Strictly one of queryable_ or orderedQueryable_ can " +
                    "have value, not both and not neither.");
            }

            // Cast to obtain access to Mongo specific methods
            IMongoQueryable mongoQueryable = (IMongoQueryable)orderedQueryable;

            // Obtain execution model and its stages
            AggregateQueryableExecutionModel<TRecord> queryableExecutionModel = (AggregateQueryableExecutionModel<TRecord>) mongoQueryable.GetExecutionModel();
            List<string> stages = queryableExecutionModel.Stages.Select(stage => stage.ToString()).ToList();

            // Check that first stage of the aggregation pipeline matches _t
            string typeMatchStage = stages[0];
            if (!typeMatchStage.Contains("{ \"$match\" : { \"_t\" :"))
                throw new Exception($"First aggregation pipeline stage does not match for _t: {typeMatchStage}");

            // Remove the first stage of the aggregation pipeline after checking it matches _t
            // The removed string has has format "{ \"$match\" : { \"_t\" : \"MyType\" } }"
            stages.RemoveAt(0);

            // Check that no further constraints on _t are present.
            // These constraints are not permitted because they will
            // cause an older object of the matching type to be found
            // even when there is a newer object of non-matching type
            // with the same key.
            foreach (string stage in stages)
            {
                if (stage.Contains("{ \"$match\" : { \"_t\" :"))
                    throw new Exception(
                        "Constraint on _t is not permitted as a query stage; " + 
                        "it should be applied only after the query is executed. " +
                        "Otherwise, an older object of the matching type will be found " +
                        "even when there is a newer object of non-matching type " +
                        "with the same key.");
            }

            // Create pipeline definition with removed match for _t
            PipelineDefinition<Record, Record> pipeline = stages.Select(m => BsonSerializer.Deserialize<BsonDocument>(m)).ToList();

            // Run the aggregation pipeline on the base collection (not typed collection)
            //
            // This ensures that the latest object is found
            // even when it is not derived from TRecord
            var cursor = collection_.BaseCollection.Aggregate(pipeline, new AggregateOptions {UseCursor=true});

            // Iterate over batches of documents returned by the cursor
            string currentKey = null;
            while (cursor.MoveNext())
            {
                // Create a list of query results for this cursor iteration
                // and a list of keys for each object in the result list
                List<TRecord> queryResultObjects_ = new List<TRecord>();
                List<string> queryResultKeys_ = new List<string>();

                // Iterate over documents in the current cursor batch and
                // populate the list of keys that match the filter
                foreach (var obj in cursor.Current)
                {
                    string objKey = obj.Key;
                    if (currentKey == objKey)
                    {
                        // The key was encountered before. Because the data is sorted by
                        // key and then by dataset and ID, this indicates that the object
                        // is not the latest and can be skipped
                        if (true)
                        {
                            // Enable this when debugging the query to report skipped records
                            // that are not the latest version in the latest dataset

                            // dataSource_.Context.Log.Warning(obj.Key);
                        }

                        // Continue to next record without returning
                        // the next item in the enumerable result
                        continue;
                    }
                    else
                    {
                        // The key was not encountered before, assign new value
                        currentKey = objKey;

                        // Skip if the result is a delete marker
                        if (obj.Is<DeleteMarker>()) continue;

                        // Attempt to cast to TRecord
                        var result = obj.As<TRecord>();

                        // Skip, do not throw, if the cast fails.
                        //
                        // This behavior is different from loading by ObjectId or string key
                        // using LoadOrNull method. In case of LoadOrNull, the API requires
                        // an error when wrong type is requested. Here, we want to proceed
                        // as though the record does not exist because the query is expected
                        // to skip over records of type not derived from TRecord.
                        if (result == null) continue;

                        // Add to the list of records and keys
                        queryResultObjects_.Add(result);
                        queryResultKeys_.Add(result.Key);
                    }
                }

                // The next step is to get objects for the list of keys without type restriction
                //
                // First, query base collection for records with key in the list
                IQueryable<Record> baseQueryable = collection_.BaseCollection.AsQueryable()
                    .Where(p => queryResultKeys_.Contains(p.Key));

                // Apply the same final constraints (list of datasets, savedBy, etc.)
                baseQueryable = collection_.DataSource.ApplyFinalConstraints(baseQueryable, loadFrom_);

                // Apply ordering to get last object in last dataset for the keys
                IOrderedQueryable<Record> baseOrderedQueryable = baseQueryable
                    .OrderBy(p => p.Key) // _key
                    .ThenByDescending(p => p.DataSet) // _dataset
                    .ThenByDescending(p => p.Id); // _id

                // Project to return only key and ObjectId. Note that some
                // of the returned records may be delete markers.
                var recordInfoQueryable = baseOrderedQueryable.Select(p => new RecordInfo
                    {Id = p.Id, DataSet = p.DataSet, Key = p.Key});

                // Populate dictionary of (Key, RecordInfo) pairs from base query
                // without a type restriction in the query, but limited only in the
                // keys returned by the typed query
                Dictionary<string, RecordInfo> recordInfoDict = new Dictionary<string, RecordInfo>();
                string currentBaseKey = null;
                foreach (var recordInfo in recordInfoQueryable)
                {
                    string objKey = recordInfo.Key;
                    if (currentBaseKey == objKey)
                    {
                        // The key was encountered before. Because the data is sorted by
                        // key and then by dataset and ID, this indicates that the object
                        // is not the latest and can be skipped
                        if (true)
                        {
                            // Enable this when debugging the query to report skipped records
                            // that are not the latest version in the latest dataset

                            // dataSource_.Context.Log.Warning(obj.Key);
                        }

                        // Continue to next record without returning
                        // the next item in the enumerable result
                        continue;
                    }
                    else
                    {
                        // The key was not encountered before, assign new value
                        currentBaseKey = objKey;

                        // Add to dictionary
                        recordInfoDict.Add(recordInfo.Key, recordInfo);
                    }
                }

                // Finally, iterate over the keys returned by the typed query and
                // yield return only those keys for which Id returned with type
                // restriction and query is the same as id returned without type
                // restriction or query
                foreach(var obj in queryResultObjects_)
                {
                    // Try to get record info for the key. It should always be
                    // present in the dictionary because typed query returns
                    // a subset of records in base query
                    if (!recordInfoDict.TryGetValue(obj.Key, out RecordInfo recordInfo))
                        throw new Exception(
                            $"Record with key {obj.Key} is returned by typed query but not base query.");

                    // Return only if Id matches; if Id does not match, the record
                    // returned by typed query was superseded by a record that does
                    // not match the query, or by a delete marker.
                    if (recordInfo.Id == obj.Id) yield return obj;
                    else continue;
                }
            }
        }

        /// <summary>
        /// Displays MongoDB aggregation pipeline JSON for server-executed queries
        /// starting from ``{aggregate...''.
        ///
        /// ATTENTION - if this method does not show aggregation pipeline JSON
        /// starting from ``{aggregate...'' and  instead shows class names, the query
        /// will not execute on the server and should be revised.
        /// </summary>
        public override string ToString()
        {
            // If in ordered pipeline phase, return orderedQueryable_;
            // otherwise return queryable_
            if (orderedQueryable_ != null) return orderedQueryable_.ToString();
            else return queryable_.ToString();
        }
    }
}
