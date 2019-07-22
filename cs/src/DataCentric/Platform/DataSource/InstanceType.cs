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

namespace DataCentric
{
    /// <summary>
    /// Specifies instance type.
    ///
    /// Some API functions are restricted based on the instance type. 
    /// </summary>
    public enum InstanceType
    {
        /// <summary>Empty</summary>
        Empty,

        /// <summary>
        /// Production instance type.
        ///
        /// This instance type is used for live production data
        /// and has the most restrictions. For example, it
        /// does not allow a database to be deleted (dropped)
        /// through the API call.
        /// </summary>
        PROD,

        /// <summary>
        /// Shared user acceptance testing instance type.
        ///
        /// This instance type is used has some of the restrictions
        /// of the PROD instance type, including the restriction
        /// on deleting (dropping) the database through an API
        /// call.
        /// </summary>
        UAT,

        /// <summary>
        /// Shared development instance type.
        ///
        /// This instance type is shared but is free from most
        /// restrictions.
        /// </summary>
        DEV,

        /// <summary>
        /// Personal instance type of a specific user.
        /// 
        /// This instance type is not shared between users and is
        /// free from most restrictions.
        /// </summary>
        USER,

        /// <summary>
        /// Instance type is used for unit testing.
        /// 
        /// Databases for the test instance type are routinely
        /// cleared (deleted). They should not be used for any
        /// purpose other than unit tests.
        /// </summary>
        TEST
    }
}
