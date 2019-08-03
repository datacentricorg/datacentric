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
    /// Dataset is a concept similar to a folder, applied to
    /// data in any storage type including relational or
    /// document database, OData endpoint, etc.
    /// 
    /// Dataset is identified by ObjectId value of the
    /// record's DataSet element. This record contains
    /// dataset identifier (folder path) and other information
    /// about the dataset, including readonly flag etc.
    ///
    /// Datasets can be stored in other datasets, similar to
    /// folders. The dataset where dataset record is called
    /// parent dataset.
    ///
    /// Dataset also has Import array which provides the list
    /// of ObjectIds where data is looked up if it is not found
    /// in the current dataset. Record lookup occurs first in
    /// descending order of dataset ObjectIds, and then in the descending
    /// order of record ObjectIds within the first dataset that
    /// has at least one record. Both dataset and record ObjectIds
    /// are ordered chronologically to one second resolution,
    /// and are unique within the database server or cluster.
    ///
    /// The root dataset uses ObjectId.Empty and does not have versions
    /// or its own DataSetData record. It is always last in the dataset
    /// lookup sequence.
    /// </summary>
    [BsonSerializer(typeof(BsonKeySerializer<DataSetKey>))]
    public class DataSetKey : Key<DataSetKey, DataSetData>
    {
        /// <summary>Dataset identifier.</summary>
        public string DataSetID { get; set; }

        /// <summary>Keys in which string ID is the only element support implicit conversion from value.</summary>
        public static implicit operator DataSetKey(string value) { return new DataSetKey { DataSetID = value }; }

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
        public static DataSetKey Common { get; } = new DataSetKey() {DataSetID = "Common"};
    }
}
