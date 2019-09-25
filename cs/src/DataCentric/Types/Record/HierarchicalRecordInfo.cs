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
using CsvHelper.Configuration.Attributes;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace DataCentric
{
    /// <summary>
    /// This class is used to project the data identifying a non-temporal
    /// record stored in a hierarchical data source when running a database
    /// query. It contains the record's DataSet and Key, but not its Id.
    ///
    /// For large records, projecting to HierarchicalRecordInfo instead of
    /// loading the entire record makes the query significantly faster
    /// because only a small part of the record is transferred over the
    /// network.
    /// </summary>
    public sealed class HierarchicalRecordInfo
    {
        /// <summary>
        /// ObjectId of the dataset where the record is stored.
        ///
        /// For records stored in root dataset, the value of
        /// DataSet element should be ObjectId.Empty.
        /// </summary>
        public ObjectId DataSet { get; set; }

        /// <summary>
        /// String key consists of semicolon delimited primary key elements:
        ///
        /// KeyElement1;KeyElement2
        ///
        /// To avoid serialization format uncertainty, key elements
        /// can have any atomic type except Double.
        /// </summary>
        public string Key { get; set; }

        /// <summary>Return string representation of the record's key.</summary>
        public override string ToString() { return Key; }
    }
}
