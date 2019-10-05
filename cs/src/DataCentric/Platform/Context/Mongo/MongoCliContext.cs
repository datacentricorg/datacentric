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
        public MongoCliContext(DbNameKey db, DataStoreData dataStore, ObjectId dataSetId)
        {
            var dataSource = new TemporalMongoDataSourceData
            {
                DataSourceName = dataStore.DataStoreName + db.Value,
                // DataStore = dataStore, TODO - need to specify using key
                DbName = db
            };

            // Initialize and assign to property
            dataSource.Init(this);
            DataSource = dataSource;

            // Set dataset
            DataSet = dataSetId;
        }

        /// <summary>Get the default data source of the context.</summary>
        public IDataSource DataSource { get; }

        /// <summary>Returns default dataset of the context.</summary>
        public ObjectId DataSet { get; }

        // TODO: review hierarchy
        public IOutputFolder Out { get; }

        // TODO: review hierarchy
        public ILog Log { get; }

        // TODO: review hierarchy
        public IProgress Progress { get; }

        //--- METHODS

        /// <summary>Flush data to permanent storage.</summary>
        public void Flush()
        {
            // Do nothing
        }

        /// <summary>
        /// Releases resources and calls base.Dispose().
        ///
        /// This method will not be called by the garbage collector.
        /// It will only be executed if:
        ///
        /// * This class implements IDisposable; and
        /// * The class instance is created through the using clause
        ///
        /// IMPORTANT - Every override of this method must call base.Dispose()
        /// after executing its own code.
        /// </summary>
        public virtual void Dispose()
        {
            // Flush all buffers
            Flush();

            // Close the log
            Log.Close();

            // Uncomment except in root class of the hierarchy
            // base.Dispose();
        }
    }
}