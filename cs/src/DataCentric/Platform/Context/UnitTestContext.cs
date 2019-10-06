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
        private IVerify verify_;

        /// <summary>Approval testing interface.</summary>
        public IVerify Verify
        {
            get
            {
                if (verify_ == null) throw new Exception($"Verify property is not set in {GetType().Name}.");
                return verify_;
            }
            set
            {
                verify_ = value;
                verify_.Init(this);
            }
        }

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
            // TODO - if (verify_ != null) verify_.Dispose();

            // Dispose base
            base.Dispose();
        }

        /// <summary>Flush data to permanent storage.</summary>
        public virtual void Flush()
        {
            // Call Flush() for each initialized property of the context
            // in the order of initialization
            // TODO - if (verify_ != null) verify_.Flush();
        }
    }
}
