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
    /// Data store definition for the MongoDB server running on the
    /// local machine (localhost).
    ///
    /// If the port is not specified, the default Mongo port 27017
    /// will be used.
    /// </summary>
    public sealed class LocalMongoDataStoreData : MongoDataStoreData
    {
        /// <summary>
        /// Optional localhost port to connect to MongoDB server, for
        /// example 12345.
        ///
        /// If the value is not specified, the default Mongo port 27017
        /// will be used.
        /// </summary>
        public string Port { get; set; }

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
            if (!String.IsNullOrEmpty(Port))
            {
                string result = "mongodb://localhost:" + Port + "/";
                return result;
            }
            else
            {
                // Port not specified, connection string uses default port
                return "mongodb://localhost/";
            }
        }
    }
}
