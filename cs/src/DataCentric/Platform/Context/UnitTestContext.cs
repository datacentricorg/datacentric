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
    public class UnitTestContext : Context, IUnitTestContext
    {
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

            Out =  new DiskOutputFolder(this, testFolderPath);
            Log = new FileLog(this, logFileName);
            Progress = new NullProgress(this);
            Verify = new LogVerify(this, className, methodName);
        }

        /// <summary>Get the default data source of the context.</summary>
        public virtual DataSourceData DataSource
        {
            get { throw new Exception("Class UnitTestContext does not provide access to DataSource. Use DataTestContext instead."); }
        }

        /// <summary>Returns ObjectId of the context dataset.</summary>
        public virtual ObjectId DataSet
        {
            get { throw new Exception("Class UnitTestContext does not provide access to DataSet. Use DataTestContext instead."); }
        }

        /// <summary>Output folder root of the context's virtualized filesystem.</summary>
        public IOutputFolder Out { get; }

        /// <summary>Logging interface.</summary>
        public ILog Log { get; }

        /// <summary>Progress interface.</summary>
        public IProgress Progress { get; }

        /// <summary>Approval testing interface.</summary>
        public IVerify Verify { get; }

        /// <summary>Flush context data to permanent storage.</summary>
        public override void Flush()
        {
            // Flush to permanent storage
            Log.Flush();
            Verify.Flush();
            Progress.Flush();
        }

        /// <summary>
        /// Releases log and calls base.Dispose().
        /// </summary>
        public override void Dispose()
        {
            // base.Dispose() should be called first, since it flushes and uses log
            base.Dispose();

            // ose the log to avoid resource conflicts.
            Log.Close();
        }
    }
}
