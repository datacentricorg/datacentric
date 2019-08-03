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
    public class DataSetData : Record<DataSetKey, DataSetData>
    {
        /// <summary>
        /// Unique dataset identifier.
        ///
        /// By convention, Common dataset must be stored in root dataset.
        /// Other datasets may be stored inside any dataset including
        /// the root dataset, Common dataset, or another dataset.
        /// </summary>
        [BsonRequired]
        public string DataSetID { get; set; }

        /// <summary>
        /// List of imported datasets.
        ///
        /// Record lookup occurs first in descending order of
        /// imported dataset ObjectIds, and then in the descending
        /// order of record ObjectIds within the first dataset that
        /// has at least one record. Both dataset and record ObjectIds
        /// are ordered chronologically to one second resolution,
        /// and are unique within the database server or cluster. 
        /// </summary>
        public List<ObjectId> Import { get; set; }

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

            if (!DataSetID.HasValue())
                throw new Exception(
                    $"DataSetID must be set before Init(context) " +
                    $"method of the dataset is called.");
            if (DataSetID == DataSetKey.Common.DataSetID && DataSet != ObjectId.Empty)
                throw new Exception(
                    $"By convention, Common dataset must be stored in root dataset. " +
                    $"Other datasets may be stored inside any dataset including " +
                    $"the root dataset, Common dataset, or another dataset.");

            if (Import != null && Import.Count > 0)
            {
                foreach (var importDataSet in Import)
                {
                    if (ID <= importDataSet)
                    {
                        if (ID == importDataSet)
                        {
                            throw new Exception(
                                $"Dataset {DataSetID} has an import with the same ObjectId={importDataSet} " +
                                $"as its own ObjectId. Each ObjectId must be unique.");
                        }
                        else
                        {
                            throw new Exception(
                                $"Dataset {DataSetID} has an import whose ObjectId={importDataSet} is greater " +
                                $"than its own ObjectId={ID}. The ObjectId of each import must be strictly " + 
                                $"less than the ObjectId of the dataset itself.");
                        }
                    }
                }
            }
        }
    }
}
