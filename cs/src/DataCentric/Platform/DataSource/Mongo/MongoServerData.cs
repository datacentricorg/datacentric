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
    /// Database server definition for MongoDB. This class is responsible
    /// for assembling MongoDB URI from hostname, port, and other parameters.
    ///
    /// Derived classes implement the API for the standard (``mongodb''),
    /// and seedlist (``mongodb+srv'') connection formats as well as for
    /// the default server running on localhost.
    /// </summary>
    public abstract class MongoServerData : DbServerData
    {
        /// <summary>Get Mongo server URI without database name.</summary>
        public abstract string GetMongoServerUri();
    }
}
