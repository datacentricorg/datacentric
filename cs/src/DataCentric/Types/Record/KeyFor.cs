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
using System.Reflection;
using MongoDB.Bson;

namespace DataCentric
{
    /// <summary>
    /// Base class of a foreign key.
    ///
    /// Generic parameters TKey, TRecord represent a variation on
    /// the curiously recurring template pattern (CRTP). They make
    /// it possible to get key type from the record and vice versa. 
    /// 
    /// Any elements of defined in the class derived from this one
    /// become key tokens. Property Value and method ToString() of
    /// the key consists of key tokens with semicolon delimiter.
    /// </summary>
    public abstract class KeyFor<TKey, TRecord> : KeyType
        where TKey : KeyFor<TKey, TRecord>, new()
        where TRecord : RecordFor<TKey, TRecord>
    {
        /// <summary>
        /// Cached reference to a record inside the key.
        ///
        /// This reference is used in two cases:
        ///
        /// First, to avoid getting the record from storage multiple times.
        /// The first value loaded from storage will be cached in Record
        /// and returned on all subsequent calls for the same dataset
        /// without storage lookup.
        ///
        /// Second, to avoid accessing storage when two objects are
        /// created in memory, one having a property that is a key
        /// to the other. Use SetCachedRecord(record) method to assign
        /// an in-memory object to a key which will also set values
        /// of the elements of the key to the corresponding values
        /// of the record.
        /// </summary>
        private CachedRecord cachedRecord_;

        //--- METHODS

        /// <summary>
        /// Load record from context.DataSource. The lookup occurs in
        /// context.DataSet and its imports, expanded to arbitrary
        /// depth with repetitions and cyclic references removed.
        ///
        /// If Record property is set, its value is returned without
        /// performing lookup in the data store; otherwise the record
        /// is loaded from storage and cached in Record and the
        /// cached value is returned from subsequent calls.
        ///
        /// Once the record has been cached, the same version will be
        /// returned in subsequent calls with the same key instance.
        /// Create a new key or call earRecord() method to force
        /// reloading new version of the record from storage.
        /// 
        /// Error message if the record is not found or is a delete marker.
        /// </summary>
        public TRecord Load(IContext context)
        {
            return Load(context, context.DataSet);
        }

        /// <summary>
        /// Load record from context.DataSource, overriding the dataset
        /// specified in the context with the value specified as the
        /// second parameter. The lookup occurs in the specified dataset
        /// and its imports, expanded to arbitrary depth with repetitions
        /// and cyclic references removed.
        ///
        /// ATTENTION - this method ignores context.DataSet
        /// because the second parameter loadFrom overrides it.
        /// 
        /// If Record property is set, its value is returned without
        /// performing lookup in the data store; otherwise the record
        /// is loaded from storage and cached in Record and the
        /// cached value is returned from subsequent calls.
        ///
        /// Once the record has been cached, the same version will be
        /// returned in subsequent calls with the same key instance.
        /// Create a new key or call earRecord() method to force
        /// reloading new version of the record from storage.
        /// 
        /// Error message if the record is not found or is a delete marker.
        /// </summary>
        public TRecord Load(IContext context, ObjectId loadFrom)
        {
            // This method will return null if the record is
            // not found or the found record is a delete marker
            var result = LoadOrNull(context, loadFrom);

            // Error message if null, otherwise return
            if (result == null) throw new Exception(
                $"Record with key {this} is not found in dataset with ObjectId={loadFrom}.");

            return result;
        }

        /// <summary>
        /// Load record from context.DataSource. The lookup occurs in
        /// context.DataSet and its imports, expanded to arbitrary
        /// depth with repetitions and cyclic references removed.
        ///
        /// If Record property is set, its value is returned without
        /// performing lookup in the data store; otherwise the record
        /// is loaded from storage and cached in Record and the
        /// cached value is returned from subsequent calls.
        ///
        /// Once the record has been cached, the same version will be
        /// returned in subsequent calls with the same key instance.
        /// Create a new key or call earRecord() method to force
        /// reloading new version of the record from storage.
        /// 
        /// Return null if the record is not found or is a delete marker.
        /// </summary>
        public TRecord LoadOrNull(IContext context)
        {
            return LoadOrNull(context, context.DataSet);
        }

        /// <summary>
        /// Load record from context.DataSource, overriding the dataset
        /// specified in the context with the value specified as the
        /// second parameter. The lookup occurs in the specified dataset
        /// and its imports, expanded to arbitrary depth with repetitions
        /// and cyclic references removed.
        ///
        /// ATTENTION - this method ignores context.DataSet
        /// because the second parameter loadFrom overrides it.
        /// 
        /// If Record property is set, its value is returned without
        /// performing lookup in the data store; otherwise the record
        /// is loaded from storage and cached in Record and the
        /// cached value is returned from subsequent calls.
        ///
        /// Once the record has been cached, the same version will be
        /// returned in subsequent calls with the same key instance.
        /// Create a new key or call earRecord() method to force
        /// reloading new version of the record from storage.
        /// 
        /// Return null if the record is not found or is a delete marker.
        /// </summary>
        public TRecord LoadOrNull(IContext context, ObjectId loadFrom)
        {
            // First check if the record has been
            // cached for the same dataset as the
            // argument to this method. Note that
            // the dataset of the record may not
            // be the same as the dataset where
            // the record is looked up.
            if (cachedRecord_ != null && cachedRecord_.DataSet == loadFrom)
            {
                // If cached for the argument dataset, return the cached
                // value unless it is empty, in which case return null
                if (cachedRecord_.Record == null) return null;
                else return cachedRecord_.Record.CastTo<TRecord>();
            }
            else
            {
                // Before doing anything else, clear the cached record
                // This will ensure that the previous cached copy is 
                // no longer present even in case of an exception in the
                // following code, or if it does not set the new cached
                // record value.
                ClearCachedRecord();

                // This method will return null if the record is
                // not found or the found record has Deleted flag set
                TRecord result = context.ReloadOrNull(this, loadFrom);

                if (result == null || result.Is<DeleteMarker>())
                {
                    // If not null, it is a delete marker;
                    // check that has a matching key
                    if (result != null && Value != result.Key)
                        throw new Exception(
                            $"Delete marker with Type={result.GetType().Name} stored " +
                            $"for Key={Value} has a non-matching Key={result.Key}.");

                    // Record not found or is a delete marker,
                    // cache an empty record and return null
                    cachedRecord_ = new CachedRecord(loadFrom);
                    return null;
                }
                else
                {
                    // Check that the found record has a matching key
                    if (Value != result.Key)
                        throw new Exception(
                            $"Record with Type={result.GetType().Name} stored " +
                            $"for Key={Value} has a non-matching Key={result.Key}.");

                    // Cache the record; the ctor of CachedRecord
                    // will cache null if the record is a delete marker
                    cachedRecord_ = new CachedRecord(loadFrom, result);

                    // Return the result after caching it inside the key
                    return result;
                }
            }
        }

        /// <summary>
        /// Write a delete marker for the dataset of the context and the specified
        /// key instead of actually deleting the record. This ensures that
        /// a record in another dataset does not become visible during
        /// lookup in a sequence of datasets.
        ///
        /// To avoid an additional roundtrip to the data store, the delete
        /// marker is written even when the record does not exist.
        /// </summary>
        public void Delete(IContext context)
        {
            // Delete in the dataset of the context
            context.DataSource.Delete(this, context.DataSet);
        }

        /// <summary>
        /// Write a delete marker in deleteIn dataset for the specified key
        /// instead of actually deleting the record. This ensures that
        /// a record in another dataset does not become visible during
        /// lookup in a sequence of datasets.
        ///
        /// To avoid an additional roundtrip to the data store, the delete
        /// marker is written even when the record does not exist.
        /// </summary>
        public void Delete(IContext context, ObjectId deleteIn)
        {
            context.DataSource.Delete(this, deleteIn);
        }

        /// <summary>
        /// Return true if the key holds a cached record,
        /// irrespective of whether or not that cached
        /// record is null.
        /// </summary>
        public bool HasCachedRecord()
        {
            return cachedRecord_ != null;
        }

        /// <summary>
        /// Use SetCachedRecord(record, dataSet) method to cache a
        /// reference to the record inside the key.
        ///
        /// This reference is used in two cases:
        ///
        /// First, to avoid getting the record from storage multiple times.
        /// The first value loaded from storage will be cached in Record
        /// and returned on all subsequent calls for the same dataset
        /// without storage lookup.
        ///
        /// Second, to avoid accessing storage when two objects are
        /// created in memory, one having a property that is a key
        /// to the other. Use SetCachedRecord(record) method to assign
        /// an in-memory object to a key which will also set values
        /// of the elements of the key to the corresponding values
        /// of the record.
        ///
        /// The parameter dataSet is separate from record because
        /// the record may be saved in a dataset that is different
        /// from the dataset for which it is looked up, e.g. it could
        /// be an imported dataset. The cached reference is stored with
        /// the dataset where the object has been looked up, not the
        /// one where it is stored.
        /// </summary>
        public void SetCachedRecord(RecordFor<TKey, TRecord> record, ObjectId dataSet)
        {
            // Before doing anything else, clear the cached record
            // This will ensure that the previous cached copy is 
            // no longer present even in case of an exception in the
            // following code, or if it does not set the new cached
            // record value.
            ClearCachedRecord();

            // Assign key elements to match the record
            AssignKeyElements(record);

            // Cache self inside the key
            cachedRecord_ = new CachedRecord(dataSet, record);
        }

        /// <summary>
        /// ear the previously cached record so that a
        /// new value can be loaded from storage or set using
        /// SetCachedRecord(record).
        /// </summary>
        public void ClearCachedRecord()
        {
            cachedRecord_ = null;
        }

        /// <summary>Assign key elements from record to key.</summary>
        public void AssignKeyElements(RecordFor<TKey, TRecord> record)
        {
            // Assign elements of the record to the matching elements
            // of the key. This will also make string representation
            // of the key return the proper value for the record.
            //
            // Get PropertyInfo arrays for TKey and TRecord
            var rootTypeName = DataInfo.GetOrCreate(GetType()).RootType.Name;
            var dataElementInfoDict = DataInfo.GetOrCreate(typeof(TRecord)).RootElementDict;
            var keyElementInfoArray = DataInfo.GetOrCreate(typeof(TKey)).RootElements;

            // Check that TRecord has the same or greater number of elements
            // as TKey (all elements of TKey must also be present in TRecord)
            if (dataElementInfoDict.Count < keyElementInfoArray.Length) throw new Exception(
                 $"Root data type {rootTypeName} has fewer elements than key type {typeof(TKey).Name}.");

            // Iterate over the key elements 
            foreach (var keyElementInfo in keyElementInfoArray)
            {
                if (!dataElementInfoDict.TryGetValue(keyElementInfo.Name, out var dataElementInfo))
                {
                    throw new Exception(
                        $"Element {keyElementInfo.Name} of key type {typeof(TKey).Name} " +
                        $"is not found in the root data type {rootTypeName}.");
                }

                if (keyElementInfo.PropertyType != dataElementInfo.PropertyType)
                    throw new Exception(
                        $"Element {typeof(TKey).Name} has type {keyElementInfo.PropertyType.Name} which does not " +
                        $"match the type {dataElementInfo.PropertyType.Name} of the corresponding element in the " +
                        $"root data type {rootTypeName}.");

                // Read from the record and assign to the key
                object elementValue = dataElementInfo.GetValue(record);
                keyElementInfo.SetValue(this, elementValue);
            }
        }
    }
}