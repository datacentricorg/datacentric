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
using System.Text;

namespace DataCentric
{
    /// <summary>Approval testing interface.</summary>
    public interface IVerify
    {
        /// <summary>Context for which this interface is defined.
        /// Use to access other interfaces of the same context.</summary>
        IContext Context { get; }

        /// <summary>Test class name.</summary>
        string ClassName { get; }

        /// <summary>Test method name.</summary>
        string MethodName { get; }

        /// <summary>Indicates whether approval data is recorded by the context.
        /// Check to avoid performing expensive calculations that will not be recorded.</summary>
        bool IsSet { get; }

        /// <summary>Flush approval log contents to permanent storage.</summary>
        void Flush();
    }
}
