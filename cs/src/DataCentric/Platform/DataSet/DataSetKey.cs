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

namespace DataCentric
{
    /// <summary>
    /// Dataset is a concept similar to a folder, applied to data in any
    /// data source including relational or document databases, OData
    /// endpoints, etc.
    ///
    /// Dataset is identified by ObjectId value of the record's DataSet
    /// element. This record contains dataset identifier (folder path) and
    /// other information about the dataset, including readonly flag etc.
    ///
    /// Datasets can be stored in other datasets. The dataset where dataset
    /// record is called parent dataset.
    ///
    /// Dataset has an Imports array which provides the list of ObjectIds of
    /// datasets where records are looked up if they are not found in the
    /// current dataset. The specific lookup rules are specific to the data
    /// source type and described in detail in the data source documentation.
    ///
    /// The parent dataset is not included in the list of Imports by
    /// default and must be included in the list of Imports explicitly.
    ///
    /// Some data source types do not support Imports. If such data
    /// source is used with a dataset where Imports array is not empty,
    /// an error will be raised.
    ///
    /// The root dataset uses ObjectId.Empty and does not have versions
    /// or its own DataSetData record. It is always last in the dataset
    /// lookup sequence. The root dataset cannot have Imports.
    /// </summary>
    [BsonSerializer(typeof(BsonKeySerializer<DataSetKey>))]
    public class DataSetKey : TypedKey<DataSetKey, DataSetData>
    {
        /// <summary>Dataset identifier.</summary>
        public string DataSetId { get; set; }

        /// <summary>Keys in which string id is the only element support implicit conversion from value.</summary>
        public static implicit operator DataSetKey(string value) { return new DataSetKey { DataSetId = value }; }

        /// <summary>
        /// By convention, Common is the default dataset in each data source.
        /// 
        /// It should have no imports, and is usually (but not always)
        /// included in the list of imports for other datasets. It should
        /// be used to store master records and other record types that
        /// should be visible from all other datasets.
        ///
        /// Similar to other datasets, the Common dataset is versioned.
        /// Records that should be stored in a non-versioned dataset, such
        /// as the dataset records themselves, the data source records,
        /// and a few other record types, should set DataSet property to
        /// ObjectId.Empty (root dataset).
        /// </summary>
        public static DataSetKey Common { get; } = new DataSetKey() {DataSetId = "Common"};
    }
}
