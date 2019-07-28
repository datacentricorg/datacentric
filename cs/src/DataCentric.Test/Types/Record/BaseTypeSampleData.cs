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
using DataCentric;
using CsvHelper.Configuration.Attributes;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace DataCentric.Test
{
    /// <summary>Base type sample for unit testing.</summary>
    public class BaseTypeSampleData : RecordFor<BaseTypeSampleKey, BaseTypeSampleData>
    {
        /// <summary>Unique identifier.</summary>
        public string SampleID { get; set; }

        /// <summary>Double element of base type.</summary>
        public double? DoubleElement { get; set; }

        /// <summary>Method of base type.</summary>
        public double GetDoubleElement()
        {
            return DoubleElement.Value;
        }
    }
}
