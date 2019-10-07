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

using System.Collections.Generic;
using MongoDB.Bson;

namespace DataCentric
{
    /// <summary>
    /// Mongo context to connect from cli.
    /// </summary>
    public class MongoCliContext : Context
    {
        /// <summary>
        /// Create with environment and source passed cli arguments.
        /// </summary>
        public MongoCliContext(DbNameKey db, string mongoServerUri, ObjectId dataSetId)
        {
            var dataSource = new TemporalMongoDataSourceData
            {
                DataSourceName = mongoServerUri + db.Value,
                DbName = db,
                MongoServerUri = mongoServerUri
            };

            // Set data source and dataset
            DataSource = dataSource;
            DataSet = dataSetId;

            // Initialize
            dataSource.Init(this);
        }
    }
}