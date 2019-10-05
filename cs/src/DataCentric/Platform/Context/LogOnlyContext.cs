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
    public class LogOnlyContext : Context
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

            // Initialize
            Init();
        }
    }
}
