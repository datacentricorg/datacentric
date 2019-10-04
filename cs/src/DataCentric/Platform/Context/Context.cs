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

        /// <summary>Returns ObjectId of the context dataset.</summary>
        public ObjectId DataSet { get; set; }

        /// <summary>Output folder root of the context's virtualized filesystem.</summary>
        public IOutputFolder Out { get; }

        /// <summary>Logging interface.</summary>
        public ILog Log { get; set; }

        /// <summary>Progress interface.</summary>
        public IProgress Progress { get; set; }

        //--- METHODS

        /// <summary>
        /// Initialize the current context after its properties are set.
        ///
        /// The list of properties includes:
        ///
        /// * DataSource
        /// * DataSet
        /// * Log
        /// * Progress
        ///
        /// IMPORTANT - Every override of this method must call base.Init()
        /// first, and only then execute the rest of the override method's code.
        /// </summary>
        public virtual void Init()
        {
            // Check that all properties are set, error message otherwise
            if (DataSource == null) throw new Exception("Set context.DataSource property before calling context.Init().");
            if (DataSet == null) throw new Exception("Set context.DataSet property before calling context.Init().");
            if (Log == null) throw new Exception("Set context.Log property before calling context.Init().");
            if (Progress == null) throw new Exception("Set context.Progress property before calling context.Init().");
        }

        /// <summary>Flush context data to permanent storage.</summary>
        public virtual void Flush()
        {

        }

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
        public virtual void Dispose()
        {
            // Uncomment except in root class of the hierarchy
            // base.Dispose();
        }
    }
}
