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
    public class UnitTestContext : IContext, IVerifyable
    {
        /// <summary>
        /// Provides a unified API for an output folder located in a
        /// conventional filesystem or an alternative backing store
        /// such as S3.
        /// </summary>
        public IFolder OutputFolder { get; }

        /// <summary>Logging interface.</summary>
        public ILog Log { get; }

        /// <summary>Progress interface.</summary>
        public IProgress Progress { get; }

        /// <summary>Default data source of the context.</summary>
        public IDataSource DataSource { get; protected set; }

        /// <summary>Default dataset of the context.</summary>
        public ObjectId DataSet { get; protected set; }

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
            // Check that properties required by the unit test are set
            if (classInstance == null) throw new Exception("Method name passed to UnitTestContext is null.");
            if (methodName == null) throw new Exception("Method name passed to UnitTestContext is null.");
            if (sourceFilePath == null) throw new Exception("Source file path passed to UnitTestContext is null.");

            // Split file path into test folder path and source filename
            string testFolderPath = Path.GetDirectoryName(sourceFilePath);
            string sourceFileName = Path.GetFileName(sourceFilePath);

            // Test class path is the path to source file followed by
            // subfolder whose name is source file name without extension
            if (!sourceFileName.EndsWith(".cs")) throw new Exception($"Source filename '{sourceFileName}' does not end with '.cs'");
            string className = sourceFileName.Substring(0, sourceFileName.Length - 3);

            // Use log file name format assName.MethodName.approved.txt from ApprovalTests.NET.
            string logFileName = String.Join(".", className, methodName, "approved.txt");

            // All properties must be set before initialization is performed
            OutputFolder = new DiskFolder { FolderPath = testFolderPath };
            Log = new FileLog { LogFilePath = logFileName };
            Progress = new NullProgress();
            Verify = new LogVerify { ClassName = className, MethodName = methodName };

            // Initialize properties
            OutputFolder.Init(this);
            Log.Init(this);
            Progress.Init(this);
            Verify.Init(this);
        }

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
            // Uncomment except in root class of the hierarchy
            // base.Init();

            // Do nothing - initialization is performed in the constructor
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
            // Call Dispose() for each initialized property of the context
            // in the reverse order of initialization
            // TODO - Verify.Dispose();
            Progress.Dispose();
            Log.Dispose();
            // TODO - OutputFolder.Dispose();

            // Uncomment except in root class of the hierarchy
            // base.Dispose();
        }

        /// <summary>Flush data to permanent storage.</summary>
        public virtual void Flush()
        {
            // Call Flush() for each initialized property of the context
            // in the order of initialization
            // TODO - OutputFolder.Flush();
            Log.Flush();
            Progress.Flush();
            // TODO - Verify.Flush();
        }
    }
}
