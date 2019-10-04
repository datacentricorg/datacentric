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
using CsvHelper.Configuration.Attributes;
using MongoDB.Bson;
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
    public class DataSetData : TypedRecord<DataSetKey, DataSetData>
    {
        /// <summary>
        /// Unique dataset identifier.
        ///
        /// By convention, Common dataset must be stored in root dataset.
        /// Other datasets may be stored inside any dataset including
        /// the root dataset, Common dataset, or another dataset.
        /// </summary>
        [BsonRequired]
        [BsonElement("DataSetID")] // TODO - review for a possible rename
        public string DataSetId { get; set; }

        /// <summary>
        /// Flag indicating that the dataset is non-temporal even if the
        /// data source supports temporal data.
        ///
        /// For the data stored in datasets where NonTemporal == false, a
        /// temporal data source keeps permanent history of changes to each
        /// record within the dataset, and provides the ability to access
        /// the record as of the specified ObjectId, where ObjectId serves
        /// as a timeline (records created later have greater ObjectId than
        /// records created earlier).
        ///
        /// For the data stored in datasets where NonTemporal == true, the
        /// data source keeps only the latest version of the record. All
        /// child datasets of a non-temporal dataset must also be non-temporal.
        ///
        /// In a non-temporal data source, this flag is ignored as all
        /// datasets in such data source are non-temporal.
        /// </summary>
        [BsonRequired]
        public bool? NonTemporal { get; set; }

        //--- METHODS

        /// <summary>
        /// Set context and perform initialization or validation of object data.
        ///
        /// All derived classes overriding this method must call base.Init(context)
        /// before executing the the rest of the code in the method override.
        /// </summary>
        public override void Init(IContext context)
        {
            base.Init(context);

            if (!DataSetId.HasValue())
                throw new Exception(
                    $"DataSetId must be set before Init(context) " +
                    $"method of the dataset is called.");

            if (Id == DataSet)
            {
                // Error if the dataset specifies self as its own parent
                throw new Exception(
                    $"DataSet {Id} specifies self as its own parent.");
            }
            else if (Id < DataSet)
            {
                // Error if dataset Id is earlier than its parent
                throw new Exception(
                    $"DataSet {Id} is created earlier than its parent {DataSet}.");
            }
        }
    }
}
