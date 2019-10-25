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
using MongoDB.Bson.Serialization.Attributes;

#pragma warning disable xUnit1013 // Public method should be marked as test

namespace DataCentric
{
    /// <summary>
    /// Makes possible for tests to be executed by:
    ///
    /// * Standard xUnit test runner; or
    /// * Via a DataCentric handler via CLI or in the front end
    ///
    /// This makes it possible to test not only inside the development
    /// environment but also on a deployed version of the application where
    /// access to the xUnit test runner is not available.
    ///
    /// This class also provides the abstract SetUp(context) method that
    /// is used to create test data by the tests in this class or as part
    /// of the test data set up by other classes.
    /// </summary>
    public abstract class TestData : TypedRecord<TestKey, TestData>, ITest
    {
        /// <summary>
        /// Unique test name.
        ///
        /// The name is set to the fully qualified test class name
        /// in the constructor of this class.
        /// </summary>
        [BsonRequired]
        public string TestName { get; set; }

        /// <summary>
        /// Test complexity level.
        ///
        /// Higher complexity results in more comprehensive testing at
        /// the expect of longer test running times.
        /// </summary>
        [BsonRequired]
        public TestComplexityEnum? Complexity { get; set; } = TestComplexityEnum.Smoke;

        //--- CONSTRUCTORS

        /// <summary>
        /// The constructor assigns test name.
        /// </summary>
        public TestData()
        {
            // This element is set to the fully qualified test class name
            // in the Init(context) method of the base class.
            TestName = GetType().FullName;
        }

        //--- METHODS

        /// <summary>
        /// Set Context property and perform validation of the record's data,
        /// then initialize any fields or properties that depend on that data.
        ///
        /// This method may be called multiple times for the same instance,
        /// possibly with a different context parameter for each subsequent call.
        ///
        /// IMPORTANT - Every override of this method must call base.Init()
        /// first, and only then execute the rest of the override method's code.
        /// </summary>
        public override void Init(IContext context)
        {
            // Initialize base
            base.Init(context);

            // This element is set to the fully qualified test class name
            // in the Init(context) method of the base class.
            TestName = GetType().FullName;
        }

        /// <summary>
        /// Get context for the test that depends on the test environment.
        ///
        /// When invoked inside xUnit test runner, Context will be null
        /// and a new copy of unit test runner will be created.
        ///
        /// When invoked inside DataCentric, Init(context) will be called
        /// before this method and will set Context. This method will then
        /// create a new dataset inside this Context for each test method.
        ///
        /// This method may be used by the unit tests in this class or as
        /// part of the test data set up by other classes.
        /// </summary>
        public IContext GetContext(
            [CallerMemberName] string methodName = null,
            [CallerFilePath] string sourceFilePath = null)
        {
            if (Context == null)
            {
                IContext result = new TemporalMongoTestContext(this, methodName, sourceFilePath);
                return result;
            }
            else
            {
                // Context is not null because Init(context) method was previously
                // called by DataCentric. Create create a new dataset for each test
                // method.
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Run all methods in this class that have [Fact] or [Theory] attribute.
        ///
        /// This method will run each of the test methods using its own instance
        /// of the test class in parallel.
        /// </summary>
        [Handler]
        public void RunAll()
        {
            // TODO - implement using reflection
            throw new NotImplementedException();
        }

        /// <summary>
        /// Creates test data in the default dataset of its argument context.
        ///
        /// This method may be used by the unit tests in this class or as
        /// part of the test data set up by other classes.
        /// </summary>
        public abstract void SetUp(IContext context);

        //--- PROTECTED

        /// <summary>
        /// Generates random seed using hashcode of full class name and method name
        /// passed as implicit parameter to this method.
        ///
        /// The purpose of this method is to provide the ability to set seed for
        /// randomly generated test data without using the same seed for different
        /// test classes and methods.
        /// </summary>
        protected int GetRandomSeed([CallerMemberName] string methodName = null)
        {
            if (string.IsNullOrEmpty(methodName))
                throw new Exception("Empty method name is passed to GetRandomSeed method.");

            // Get seed as hash of FullName.MethodName. Using hashcode of a well defined
            // string makes it possible to reproduce the same seed in a different programming
            // language for consistent test data across languages
            string fullClassName = string.Join(".", GetType().FullName, methodName);
            int result = fullClassName.GetHashCode();
            return result;
        }
    }
}
