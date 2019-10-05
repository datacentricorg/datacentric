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
    /// Context for use in handlers.
    /// </summary>
    public class Context : IContext
    {
        /// <summary>Get the default data source of the context.</summary>
        public IDataSource DataSource { get; set; }

        /// <summary>
        /// Returns default dataset of the context.
        ///
        /// Defaults to root dataset (ObjectId.Empty) if not set.
        /// </summary>
        public ObjectId DataSet { get; set; }

        /// <summary>Output folder root of the context's virtualized filesystem.</summary>
        public IOutputFolder Out { get; }

        /// <summary>Logging interface.</summary>
        public ILog Log { get; set; }

        /// <summary>Progress interface.</summary>
        public IProgress Progress { get; set; }

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
            // Uncomment except in root class of the hierarchy
            // base.Init();

            // Check that each property are set, error message otherwise
            if (DataSource == null) throw new Exception("Set context.DataSource property before calling context.Init().");
            if (Log == null) throw new Exception("Set context.Log property before calling context.Init().");
            if (Progress == null) throw new Exception("Set context.Progress property before calling context.Init().");

            // Call Init(this) for each property of the context
            DataSource.Init(this);
            Log.Init(this);
            Progress.Init(this);
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
            // Uncomment except in root class of the hierarchy
            // base.Dispose();
        }

        /// <summary>Flush data to permanent storage.</summary>
        public virtual void Flush()
        {
            // Call Flush() for each resources of the current context
            DataSource.Flush();
            Log.Flush();
            Progress.Flush();
        }
    }
}
