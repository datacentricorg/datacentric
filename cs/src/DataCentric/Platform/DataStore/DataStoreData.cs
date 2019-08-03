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
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using NodaTime;

namespace DataCentric
{
    /// <summary>
    /// Data store represents a database server or similar concept for non-database
    /// storage. It is not the same as data source (database) as it only specifies
    /// the server, and each server can host multiple data sources (databases).
    ///
    /// Separating the data store from the data source helps separate server
    /// specifics such as URI, connection string, etc in data store from the
    /// definition of how the data is stored on the server, including the
    /// environment (which usually maps to database name) and data representation
    /// (basic or temporal).
    /// </summary>
    public abstract class DataStoreData : RootRecord<DataStoreKey, DataStoreData>
    {
        /// <summary>Unique data store identifier.</summary>
        [BsonRequired]
        public string DataStoreID { get; set; }
    }
}