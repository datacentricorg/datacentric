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
    /// Provides a standard way to identify a database server.
    ///
    /// This record is stored in root dataset.
    /// </summary>
    [BsonSerializer(typeof(BsonKeySerializer<DbServerKey>))]
    public class DbServerKey : RootKeyFor<DbServerKey, DbServerData>
    {
        /// <summary>
        /// Unique database server identifier string.
        /// 
        /// This field is the user friendly name used to
        /// identify the server. It is not the server URI.
        /// </summary>
        public string DbServerID { get; set; }

        /// <summary>Keys in which string ID is the only element support implicit conversion from value.</summary>
        public static implicit operator DbServerKey(string value) { return new DbServerKey { DbServerID = value }; }

        /// <summary>
        /// By convention, Default is the Mongo server running on the default port of localhost.
        /// </summary>
        public static DbServerKey Default { get; } = new DbServerKey() { DbServerID = "Default" };
    }
}
