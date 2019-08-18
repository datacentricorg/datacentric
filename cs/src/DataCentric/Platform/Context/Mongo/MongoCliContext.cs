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
    public class MongoCliContext : IContext
    {
        /// <summary>
        /// Create with environment and source passed cli arguments.
        /// </summary>
        public MongoCliContext(DbNameKey db, DataStoreData dataStore)
        {
            var dataSource = new MongoDataSourceData
            {
                DataSourceId = dataStore.DataStoreId + db.Value,
                DataStore = dataStore,
                DbName = db
            };

            // Initialize and assign to property
            dataSource.Init(this);
            DataSource = dataSource;

            DataSet = DataSource.GetCommon();
        }

        /// <summary>Get the default data source of the context.</summary>
        public IDataSource DataSource { get; }

        /// <summary>Returns ObjectId of the context dataset.</summary>
        public ObjectId DataSet { get; }

        // TODO: review hierarchy
        public IOutputFolder Out { get; }

        // TODO: review hierarchy
        public ILog Log { get; }

        // TODO: review hierarchy
        public IProgress Progress { get; }

        // TODO: review hierarchy
        public void Flush()
        {
        }

        /// <summary>
        /// Releases resources and calls base.Dispose().
        ///
        /// This method will NOT be called by the garbage
        /// collector, therefore instantiating it inside
        /// the ``using'' clause is essential to ensure
        /// that Dispose() method gets invoked.
        ///
        /// ATTENTION:
        ///
        /// Each class that overrides this method must
        ///
        /// (a) Specify IDisposable in interface list; and
        /// (b) Call base.Dispose() at the end of its own
        ///     Dispose() method.
        /// </summary>
        public virtual void Dispose()
        {
            // Flush all buffers
            Flush();

            // Close the log
            Log.Close();
        }
    }
}