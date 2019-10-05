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

        /// <summary>Indicates whether approval data is recorded by the context.
        /// Check to avoid performing expensive calculations that will not be recorded.</summary>
        public bool IsSet
        {
            // Return false for null approval class as the data is not recorded
            get { return false; }
        }

        //--- METHODS

        /// <summary>Flush data to permanent storage.</summary>
        public void Flush()
        {
            // Not recording approval data, nothing to flush
        }
    }
}
