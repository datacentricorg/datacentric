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
using System.Runtime.CompilerServices;
using MongoDB.Bson;

namespace DataCentric
{
    /// <summary>
    /// Context for use in test fixtures that require a data source.
    /// 
    /// It extends UnitTestContext by creating an empty test
    /// database specific to the test method, and deleting
    /// it after the test exits. The context creates Common
    /// dataset in the database and assigns its ObjectId to
    /// the DataSet property of the context.
    ///
    /// If the test sets KeepTestData = true, the data is retained
    /// after the text exits. This data will be cleared on the
    /// next launch of the test.
    ///
    /// For tests that do not require a data source, use UnitTestContext.
    /// </summary>
    public class DataTestContext<TDataSource> : UnitTestContext, IVerifyable, IDisposable
        where TDataSource : DataSourceData, IDataSource, new()
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
            if (methodName == null) throw new Exception("Method name passed to MongoTestContext is null.");
            if (sourceFilePath == null) throw new Exception("Source file path passed to MongoTestContext is null.");

            // Create and initialize data source with TEST instance type.
            //
            // This does not create the database until the data source
            // is actually used to access data.
            string mappedClassName = ClassInfo.GetOrCreate(classInstance).MappedClassName;

            // Create data source specified as generic argument
            var dataSource = new TDataSource();
            dataSource.DbName = new DbNameKey()
            {
                InstanceType = InstanceType.TEST,
                InstanceName = mappedClassName,
                EnvName = methodName
            };

            // Initialize and assign to property
            dataSource.Init(this);
            DataSource = dataSource;

            // Delete (drop) the database to clear the existing data
            DataSource.DeleteDb();

            // Create common dataset and assign it to DataSet property of this context
            DataSet = DataSource.CreateCommon();
        }

        //--- PROPERTIES

        /// <summary>Get the default data source of the context.</summary>
        public override IDataSource DataSource { get; }

        /// <summary>Returns ObjectId of the context dataset.</summary>
        public override ObjectId DataSet { get; }

        /// <summary>
        /// The data in test database is erased when the context is created
        /// irrespective of KeepTestData value. However it is only erased on
        /// Dispose() if the value of KeepTestData is true.
        ///
        /// The default value of KeepTestData is false.
        /// </summary>
        public bool KeepTestData { get; set; }

        //--- METHODS

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
        public override void Dispose()
        {
            if (!KeepTestData)
            {
                // Permanently delete the unit test database
                // unless KeepTestData is true
                DataSource.DeleteDb();
            }

            // Dispose base
            base.Dispose();
        }
    }
}
