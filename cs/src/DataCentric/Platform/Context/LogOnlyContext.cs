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
using MongoDB.Bson;

namespace DataCentric
{
    /// <summary>Log only context provides logging to the output but no other functionality.
    /// It does not support loading or saving objects to a cache or file storage.
    /// Progress is reported only if progress interface is passed to the constructor.</summary>
    public class LogOnlyContext : IContext, IDisposable
    {
        /// <summary>Create with logging to the output, progress messages are ignored.
        /// Log and progress destinations can be modified after construction.</summary>
        public LogOnlyContext()
        {
            DataSource = new NullDataSourceData();
            DataSet = ObjectId.Empty;
            Out = new DiskOutputFolder(this, "test/out");
            Log = new ConsoleLog(this);
            Progress = new LogProgress(this);
        }

        //--- PROPERTIES

        /// <summary>Get interface to the context data source.</summary>
        public IDataSource DataSource { get; }

        /// <summary>Returns default dataset of the context.</summary>
        public ObjectId DataSet { get; }

        /// <summary>Output folder root of the context's virtualized filesystem.</summary>
        public IOutputFolder Out { get; }

        /// <summary>Logging interface.</summary>
        public ILog Log { get; }

        /// <summary>Progress interface.</summary>
        public IProgress Progress { get; }

        /// <summary>Approval testing interface.</summary>
        public IVerify Verify { get; }

        //--- METHODS

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

        /// <summary>Flush data to permanent storage.</summary>
        public void Flush()
        {
            // Flush to permanent storage
            Log.Flush();
            Verify.Flush();
            Progress.Flush();
        }
    }
}
