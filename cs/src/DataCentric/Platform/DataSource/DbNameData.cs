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
using NodaTime;

namespace DataCentric
{
    /// <summary>
    /// This class enforces strict naming conventions
    /// for database naming. While format of the resulting database
    /// name is specific to data store type, it always consists
    /// of three tokens: InstanceType, InstanceName, and EnvName.
    /// The meaning of InstanceName and EnvName tokens depends on
    /// the value of InstanceType enumeration.
    ///
    /// This record is stored in root dataset.
    /// </summary>
    public abstract class DbNameData : RootRecord<DbNameKey, DbNameData>
    {
        /// <summary>
        /// Instance type enumeration.
        ///
        /// Some API functions are restricted based on the instance type. 
        /// </summary>
        [BsonRequired]
        public InstanceType InstanceType { get; set; }

        /// <summary>
        /// The meaning of instance name depends on the instance type.
        ///
        /// \begin{itemize}
        /// \item
        /// For PROD, UAT, and DEV instance types, instance name
        /// identifies the endpoint.
        ///
        /// \item
        /// For USER instance type, instance name is user alias.
        ///
        /// \item
        /// For TEST instance type, instance name is the name of
        /// the unit test class (test fixture).
        /// \end{itemize}
        /// </summary>
        [BsonRequired]
        public string InstanceName { get; set; }

        /// <summary>
        /// The meaning of environment name depends on the instance type.
        ///
        /// \begin{itemize}
        /// \item
        /// For PROD, UAT, DEV, and USER instance types, it is the
        /// name of the user environment selected in the client.
        ///
        /// \item
        /// For TEST instance type, it is the test method name.
        /// \end{itemize}
        /// </summary>
        [BsonRequired]
        public string EnvName { get; set; }
    }
}
