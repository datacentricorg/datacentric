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
    public abstract class TypedKey<TKey, TRecord> : Key
        where TKey : TypedKey<TKey, TRecord>, new()
        where TRecord : TypedRecord<TKey, TRecord>
    {
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
        /// Create a new key or call ClearCachedRecord() method to force
        /// reloading new version of the record from storage.
        /// 
        /// Error message if the record is not found or is a DeletedRecord.
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
        /// Create a new key or call ClearCachedRecord() method to force
        /// reloading new version of the record from storage.
        /// 
        /// Error message if the record is not found or is a DeletedRecord.
        /// </summary>
        public TRecord Load(IContext context, ObjectId loadFrom)
        {
            // This method will return null if the record is
            // not found or the found record is a DeletedRecord
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
        /// Create a new key or call ClearCachedRecord() method to force
        /// reloading new version of the record from storage.
        /// 
        /// Return null if the record is not found or is a DeletedRecord.
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
        /// Create a new key or call ClearCachedRecord() method to force
        /// reloading new version of the record from storage.
        /// 
        /// Return null if the record is not found or is a DeletedRecord.
        /// </summary>
        public TRecord LoadOrNull(IContext context, ObjectId loadFrom)
        {
            // This method will return null if the record is
            // not found or the found record is a DeletedRecord
            TRecord result = context.ReloadOrNull(this, loadFrom);

            // If not null, check that the key matches (even if DeletedRecord)
            if (result != null && Value != result.Key)
            {
                if (result.Is<DeletedRecord>())
                    throw new Exception(
                        $"Delete marker with Type={result.GetType().Name} stored " +
                        $"for Key={Value} has a non-matching Key={result.Key}.");
                else
                    throw new Exception(
                        $"Record with Type={result.GetType().Name} stored " +
                        $"for Key={Value} has a non-matching Key={result.Key}.");
            }

            return result;
        }

        /// <summary>
        /// Write a DeletedRecord for the dataset of the context and the specified
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
        /// Write a DeletedRecord in deleteIn dataset for the specified key
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
        /// Populate key elements by taking them from the matching
        /// elements of the argument record.
        /// </summary>
        public void PopulateFrom(TypedRecord<TKey, TRecord> record)
        {
            // Assign elements of the record to the matching elements
            // of the key. This will also make string representation
            // of the key return the proper value for the record.
            //
            // Get PropertyInfo arrays for TKey and TRecord
            var rootTypeName = DataTypeInfo.GetOrCreate(GetType()).RootType.Name;
            var dataElementInfoDict = DataTypeInfo.GetOrCreate(typeof(TRecord)).DataElementDict;
            var keyElementInfoArray = DataTypeInfo.GetOrCreate(typeof(TKey)).DataElements;

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