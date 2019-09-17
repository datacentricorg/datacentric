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
    /// Data store definition for the MongoDB server running on a cluster.
    ///
    /// If the port is not specified, the default Mongo port 27017
    /// will be used.
    /// </summary>
    public sealed class ClusterMongoDataStoreData : MongoDataStoreData
    {
        /// <summary>
        /// MongoDB server hostname or the list of MongoDB cluster hostnames
        /// with optional port in either ``host'' or ``host::port'' format.
        /// </summary>
        [BsonRequired]
        public List<string> Hosts { get; set; }

        /// <summary>
        /// MongoDB server connection string
        /// </summary>
        public string ConnectionString { get; set; }

        //--- METHODS

        /// <summary>
        /// Get Mongo server URI.
        ///
        /// The data store specifies the Mongo server but not the specific
        /// database on the server. Accordingly, the server URI returned by
        /// this method does not include the database and only specifies
        /// the server.
        /// </summary>
        public override string GetMongoServerUri()
        {
            if (ConnectionString.HasValue())
                return ConnectionString;

            if (Hosts == null || Hosts.Count == 0) throw new Exception(
                $"The list of hosts provided for MongoDB server {DataStoreId} is null or empty.");

            string hostNames = String.Join(",", Hosts);
            string result = String.Concat("mongodb://", hostNames, "/");
            return result;
        }
    }
}
