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
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.IdGenerators;

namespace DataCentric
{
    /// <summary>
    /// Context for use in test fixtures that require MongoDB.
    /// 
    /// It extends UnitTestContext by creating an empty test
    /// database specific to the test method, and deleting
    /// it after the test exits. The context creates Common
    /// dataset in the database and assigns its ObjectId to
    /// the DataSet property of the context.
    ///
    /// If the test sets KeepDb = true, the data is retained
    /// after the text exits. This data will be cleared on the
    /// next launch of the test.
    ///
    /// For tests that do not require MongoDB, use UnitTestDataContext.
    /// </summary>
    public class DataTestContext : UnitTestContext, IDataTestContext
    {
        /// <summary>
        /// Create with class name, method name, and source file path.
        ///
        /// When ``this'' is passed as the the only argument to the
        /// constructor, the latter two arguments are provided by
        /// the compiler.
        /// </summary>
        public DataTestContext(
            object classInstance,
            [CallerMemberName] string methodName = null,
            [CallerFilePath] string sourceFilePath = null)
            :
            base(classInstance, methodName, sourceFilePath)
        {
            if (methodName == null) throw new Exception("Method name passed to DataTestContext is null.");
            if (sourceFilePath == null) throw new Exception("Source file path passed to DataTestContext is null.");

            // Create and initialize data source with TEST instance type.
            //
            // This does not create the database until the data source
            // is actually used to access data.
            string mappedClassName = ClassInfo.GetOrCreate(classInstance).MappedClassName;
            var dataSource = new MongoDataSourceData()
            {
                DataStore = new LocalMongoDataStoreData(),
                DbName = new DbNameKey()
                {
                    InstanceType = InstanceType.TEST,
                    InstanceName = mappedClassName,
                    EnvName = methodName
                }
            };

            // Initialize and assign to property
            dataSource.Init(this);
            DataSource = dataSource;

            // Delete (drop) the database to clear the existing data
            DataSource.DeleteDb();

            // Create common dataset and assign it to DataSet property of this context
            DataSet = DataSource.CreateCommon();
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
        /// Each class must call base.Dispose() at the end
        /// of its own Dispose() method.
        /// </summary>
        public override void Dispose()
        {
            if (!KeepDb)
            {
                // Permanently delete the unit test database
                // unless KeepDb is true
                DataSource.DeleteDb();
            }

            base.Dispose();
        }

        /// <summary>Get the default data source of the context.</summary>
        public override IDataSource DataSource { get; }

        /// <summary>Returns ObjectId of the context dataset.</summary>
        public override ObjectId DataSet { get; }

        /// <summary>
        /// Test database is dropped on Dispose() unless
        /// KeepDb property is set to true.
        /// </summary>
        public bool KeepDb { get; set; }
    }
}
