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
    /// Data store provides the ability to import and export records
    /// individually or in bulk. It does not have the ability to
    /// load individual records by key or by query.
    /// </summary>
    [BsonSerializer(typeof(BsonKeySerializer<DataStoreKey>))]
    public class DataStoreKey : RootKeyFor<DataStoreKey, DataStoreData>
    {
        /// <summary>Unique data store identifier.</summary>
        public string DataStoreID { get; set; }

        //--- OPERATORS

        /// <summary>Keys in which string ID is the only element support implicit conversion from value.</summary>
        public static implicit operator DataStoreKey(string value) { return new DataStoreKey { DataStoreID = value }; }

        //--- STATIC

        /// <summary>
        /// By convention, Settings is the name of the data store for system settings.
        /// </summary>
        public static DataStoreKey Settings { get; } = new DataStoreKey() { DataStoreID = "Settings" };

        /// <summary>
        /// By convention, Configuration is the name of data store for configuring the analytics.
        /// </summary>
        public static DataStoreKey Configuration { get; } = new DataStoreKey() { DataStoreID = "Configuration" };
    }
}
