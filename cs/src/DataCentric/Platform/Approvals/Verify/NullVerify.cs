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
using System.Diagnostics;
using System.IO;
using System.Linq.Expressions;
using System.Text;

namespace DataCentric
{
    /// <summary>Use this class to skip recording approval data.</summary>
    public class NullVerify : IVerify
    {
        /// <summary>Create from the execution context and folder path, without a linked log.\\
        /// Accepts path with regular path separator or in dot delimited (``namespace'') format.</summary>
        public NullVerify(IContext context)
        {
            Context = context;
        }

        /// <summary>Context of this approval log.</summary>
        public IContext Context { get; }

        /// <summary>Test class name.</summary>
        public string ClassName { get; }

        /// <summary>Test method name.</summary>
        public string MethodName { get; }

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
        public void Init(IContext context)
        {
            // Do nothing
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
            // Nothing to dispose

            // Uncomment except in root class of the hierarchy
            // base.Dispose();
        }

        /// <summary>Flush data to permanent storage.</summary>
        public void Flush()
        {
            // Not recording verify data, nothing to flush
        }
    }
}
