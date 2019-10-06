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
    /// Context for use in test fixtures that do not require MongoDB.
    ///
    /// It extends UnitTestContext with approval test functionality.
    /// Attempting to access DataSource using this context will cause
    /// an error.
    ///
    /// For tests that require MongoDB, use IDataTestDataContext.
    /// </summary>
    public class UnitTestContext : Context, IVerifyable
    {
        /// <summary>Unit test class instance.</summary>
        public object ClassInstance { get; }

        /// <summary>Unit test method name.</summary>
        public string MethodName { get; }

        /// <summary>
        /// Path to the source code of the unit test.
        ///
        /// Approval files and log output will be located in the same folder
        /// as the source code of the unit test.
        /// </summary>
        public string SourceFilePath { get; }

        /// <summary>Approval testing interface.</summary>
        public IVerify Verify { get; set; }

        /// <summary>
        /// Create with class name, method name, and source file path.
        ///
        /// When ``this'' is passed as the the only argument to the
        /// constructor, the latter two arguments are provided by
        /// the compiler.
        /// </summary>
        public UnitTestContext(
            object classInstance,
            [CallerMemberName] string methodName = null,
            [CallerFilePath] string sourceFilePath = null)
        {
            ClassInstance = classInstance;
            MethodName = methodName;
            SourceFilePath = sourceFilePath;
        }

        //--- METHODS

        /// <summary>
        /// Initialize the current context after its properties are set,
        /// and set default values for the properties that are not set.
        /// 
        /// Includes calling Init(this) for each property of the context.
        ///
        /// This method may be called multiple times for the same instance.
        ///
        /// IMPORTANT - Every override of this method must call base.Init()
        /// first, and only then execute the rest of the override method's code.
        /// </summary>
        public virtual void Init()
        {
            // As an exception to the general rule, properties of the base
            // must be set before base.Init() is called

            // Check that properties required by the unit test are set
            if (ClassInstance == null) throw new Exception("Method name passed to UnitTestContext is null.");
            if (MethodName == null) throw new Exception("Method name passed to UnitTestContext is null.");
            if (SourceFilePath == null) throw new Exception("Source file path passed to UnitTestContext is null.");

            // Split file path into test folder path and source filename
            string testFolderPath = Path.GetDirectoryName(SourceFilePath);
            string sourceFileName = Path.GetFileName(SourceFilePath);

            // Test class path is the path to source file followed by
            // subfolder whose name is source file name without extension
            if (!sourceFileName.EndsWith(".cs")) throw new Exception($"Source filename '{sourceFileName}' does not end with '.cs'");
            string className = sourceFileName.Substring(0, sourceFileName.Length - 3);

            // Use log file name format assName.MethodName.approved.txt from ApprovalTests.NET.
            string logFileName = String.Join(".", className, MethodName, "approved.txt");

            OutputFolder = new DiskFolder { FolderPath = testFolderPath };
            Log = new FileLog { LogFilePath = logFileName };
            Progress = new NullProgress();

            // Initialize base
            base.Init();

            // Set and initialize approval testing interface
            Verify = new LogVerify { ClassName = className, MethodName = MethodName };
            Verify.Init(this);
        }
    }
}
