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
    /// Provides information for positioning a control within its
    /// parent container or scroll window.
    /// </summary>
    public abstract class PositionData : Data
    {
        /// <summary>
        /// Specifies the type of positioning method used for an element.
        /// </summary>
        [BsonRequired]
        public PositionTypeEnum? Position { get; set; }

        /// <summary>
        /// Specify vertical position of the control counting from the top.
        ///
        /// Only one of Top or Bottom properties can be specified.
        /// </summary>
        public LengthData Top { get; set; }

        /// <summary>
        /// Specify vertical position of the control counting from the bottom.
        ///
        /// Only one of Top or Bottom properties can be specified.
        /// </summary>
        public LengthData Bottom { get; set; }

        /// <summary>
        /// Specify horizontal position of the control counting from the left.
        /// 
        /// Only one of Left or Right properties can be specified.
        /// </summary>
        public LengthData Left { get; set; }

        /// <summary>
        /// Specify horizontal position of the control counting from the right.
        ///
        /// Only one of Left or Right properties can be specified.
        /// </summary>
        public LengthData Right { get; set; }

        /// <summary>
        /// Sets the height of an element.
        ///
        /// If Height is not specified, the control will be auto sized.
        /// </summary>
        public LengthData Height { get; set; }

        /// <summary>
        /// Sets the width of an element.
        ///
        /// If Width is not specified, the control will be auto sized.
        /// </summary>
        public LengthData Width { get; set; }
    }
}
