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

namespace DataCentric
{
    /// <summary>
    /// Specialization of DataTestContext for TemporalMongoDataSourceData.
    /// 
    /// DataTestContext is the context for use in test fixtures that
    /// require a data source, parameterized by data source type.
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
    public class TemporalMongoTestContext : DataTestContext<TemporalMongoDataSourceData>
    {
        /// <summary>
        /// Create with class name, method name, and source file path.
        ///
        /// When ``this'' is passed as the the only argument to the
        /// constructor, the latter two arguments are provided by
        /// the compiler.
        /// </summary>
        public TemporalMongoTestContext(
            object classInstance,
            [CallerMemberName] string methodName = null,
            [CallerFilePath] string sourceFilePath = null)
            :
            base(classInstance, methodName, sourceFilePath)
        {
        }
    }
}
