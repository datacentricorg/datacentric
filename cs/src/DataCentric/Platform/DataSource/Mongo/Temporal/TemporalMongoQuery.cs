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
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Linq.Expressions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace DataCentric
{
    /// <summary>
    /// Implements IQuery for temporal MongoDB data source.
    /// 
    /// This implementation combines methods of IQueryable(TRecord) with
    /// additional constraints and ordering to retrieve the correct version
    /// of the record across multiple datasets.
    /// </summary>
    public class TemporalMongoQuery<TRecord> : IQuery<TRecord>
        where TRecord : Record
    {
        private readonly TemporalMongoCollection<TRecord> collection_;
        private readonly ObjectId loadFrom_;
        private readonly IQueryable<TRecord> queryable_;
        private readonly IOrderedQueryable<TRecord> orderedQueryable_;

        //--- CONSTRUCTORS

        /// <summary>
        /// Create query from collection and dataset.
        /// </summary>
        public TemporalMongoQuery(TemporalMongoCollection<TRecord> collection, ObjectId loadFrom)
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
        private TemporalMongoQuery(TemporalMongoCollection<TRecord> collection, ObjectId loadFrom, IQueryable<TRecord> queryable)
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
        private TemporalMongoQuery(TemporalMongoCollection<TRecord> collection, ObjectId loadFrom, IOrderedQueryable<TRecord> orderedQueryable)
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
                return new TemporalMongoQuery<TRecord>(collection_, loadFrom_, queryable_.Where(predicate));
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
                return new TemporalMongoQuery<TRecord>(collection_, loadFrom_, queryableWithDataSetConstraint.OrderBy(keySelector));
            }
            else if (queryable_ == null && orderedQueryable_ != null)
            {
                // Subsequent SortBy clauses, use ThenBy of the orderedQueryable_
                return new TemporalMongoQuery<TRecord>(collection_, loadFrom_, orderedQueryable_.ThenBy(keySelector));
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
                return new TemporalMongoQuery<TRecord>(collection_, loadFrom_, queryableWithDataSetConstraint.OrderByDescending(keySelector));
            }
            else if (queryable_ == null && orderedQueryable_ != null)
            {
                // Subsequent SortBy clauses, use ThenBy of the orderedQueryable_
                return new TemporalMongoQuery<TRecord>(collection_, loadFrom_, orderedQueryable_.ThenByDescending(keySelector));
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
            IQueryable<TRecord> typedQueryable = null;
            if (queryable_ != null && orderedQueryable_ == null)
            {
                // Apply final constraints to queryable_ and assign to typedQueryable
                typedQueryable = collection_.DataSource.ApplyFinalConstraints(queryable_, loadFrom_);
            }
            else if (queryable_ == null && orderedQueryable_ != null)
            {
                // Assign orderedQueryable_ to typedQueryable, final constraints already applied
                typedQueryable = orderedQueryable_;
            }
            else
            {
                throw new Exception(
                    "Strictly one of queryable_ or orderedQueryable_ can " +
                    "have value, not both and not neither.");
            }

            // Project to record info instead of returning the entire record
            var projectedQueryable = typedQueryable.Select(p => new RecordInfo {Id = p.Id, DataSet = p.DataSet, Key = p.Key});

            // Iterate over the projected query collecting the greatest
            // (i.e., latest) Id for each key in an ordered dictionary
            var orderedDictionary = new OrderedDictionary();
            foreach (var recordInfo in projectedQueryable)
            {
                ObjectId currentId = (ObjectId) orderedDictionary[recordInfo.Key];
            }

            IDictionaryEnumerator orderedEnumerator = orderedDictionary.GetEnumerator();
            int batchSize = 1000;
            while (true)
            {
                int batchIndex = 0;
                var batchKeys = new List<string>();
                var batchIds = new List<ObjectId>();
                while(orderedEnumerator.MoveNext())
                {
                    // Populate lists of batch keys and batch ids for the current batch
                    batchKeys.Add((string)orderedEnumerator.Key);
                    batchIds.Add((ObjectId)orderedEnumerator.Key);

                    // Exit the loop when batch size is reached
                    if (++batchIndex == batchSize)
                    {
                        break;
                    }
                }

                // The next step is to get objects for the list of keys without type restriction
                //
                // First, query base collection for records with key in the list
                IQueryable<Record> baseQueryable = collection_.BaseCollection.AsQueryable()
                    .Where(p => batchKeys.Contains(p.Key));

                // Apply the same final constraints (list of datasets, savedBy, etc.)
                baseQueryable = collection_.DataSource.ApplyFinalConstraints(baseQueryable, loadFrom_);

                // Apply ordering to get last object in last dataset for the keys
                IOrderedQueryable<Record> baseOrderedQueryable = baseQueryable
                    .OrderBy(p => p.Key) // _key
                    .ThenByDescending(p => p.DataSet) // _dataset
                    .ThenByDescending(p => p.Id); // _id

                // Populate dictionary of (Key, Record) pairs, keeping
                // only the latest record in the latest dataset
                var objDict = new Dictionary<string, Record>();
                string currentBaseKey = null;
                foreach (var obj in baseOrderedQueryable)
                {
                    string objKey = obj.Key;
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
                        objDict.Add(objKey, obj);
                    }
                }

                // Finally, iterate over the keys in the ordered array of 
                // batch keys and yield return only those objects whose Id 
                // matches the Ids returned by the query. If the Ids do not
                // match, then the object returned by query is not the latest
                // and should be skipped.
                for(int resultIndex = 0; resultIndex < batchKeys.Count; ++resultIndex)
                {
                    // Get Key and Id from the result of the typed query
                    string batchKey = batchKeys[resultIndex];
                    ObjectId batchId = batchIds[resultIndex];

                    // Get object returned by base query for this key
                    // This object should not be null
                    var baseResult = objDict[batchKey];

                    // Skip if the result is a DeletedRecord
                    if (baseResult.Is<DeletedRecord>()) continue;

                    // Skip if Id does not match; this indicates that
                    // the object returned by the query is not the 
                    // latest object in the latest dataset for this key.
                    if (baseResult.Id != batchId) continue;

                    // Cast to the requested type; this should 
                    // succeed if the Id matches
                    TRecord result = baseResult.CastTo<TRecord>();
                    yield return result;
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
