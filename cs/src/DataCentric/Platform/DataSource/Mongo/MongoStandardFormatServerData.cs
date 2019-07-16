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
    /// Assembles MongoDB URI using the standard (``mongodb'') connection
    /// string format for a single server or a cluster.
    /// </summary>
    public sealed class MongoStandardFormatServerData : MongoServerData
    {
        /// <summary>
        /// MongoDB server hostname or the list of MongoDB cluster
        /// hostnames with optional port in ``host'' or ``host::port''
        /// format.
        /// </summary>
        public List<string> Hosts { get; set; }

        /// <summary>Get Mongo server URI without database name.</summary>
        public override string GetMongoServerUri()
        {
            if (Hosts == null || Hosts.Count == 0) throw new Exception(
                $"The list of hosts provided for MongoDB server {DbServerID} is null or empty.");

            string hostNames = String.Join(",", Hosts);
            string result = String.Concat("mongodb://", hostNames, "/");
            return result;
        }
    }
}
