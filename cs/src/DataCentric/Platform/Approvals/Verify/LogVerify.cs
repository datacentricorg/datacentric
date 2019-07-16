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

namespace DataCentric
{
    /// <summary>Approval data is recorded in Context.Log under Approval entry type.</summary>
    public class LogVerify : IVerify
    {
        /// <summary>Create from the execution context and folder path.</summary>
        public LogVerify(IContext context, string className, string methodName)
        {
            Context = context;
            ClassName = className;
            MethodName = methodName;
        }

        /// <summary>Context for which this interface is defined.
        /// Use to access other interfaces of the same context.</summary>
        public IContext Context { get; }

        /// <summary>Test class name.</summary>
        public string ClassName { get; }

        /// <summary>Test method name.</summary>
        public string MethodName { get; }

        /// <summary>Indicates whether approval data is recorded by the context.
        /// Check to avoid performing expensive calculations that will not be recorded.</summary>
        public bool IsSet
        {
            get
            {
                // Returns true if verbosity level is at least Verify
                var verbosity = Context.Log. Verbosity;
                return verbosity == LogEntryType.Empty || verbosity >= LogEntryType.Verify;
            }
        }

        /// <summary>Append new log entry with Verify type if log verbosity is at least Verify.
        /// Entry subtype is an optional tag in dot delimited format (specify null if no subtype).</summary>
        public void Append(string entrySubType, string message, params object[] messageParams)
        {
            Context.Log.Append(LogEntryType.Verify, entrySubType, message, messageParams);
        }

        /// <summary>Flush approval log contents to permanent storage.</summary>
        public void Flush()
        {
            Context.Log.Flush();
        }
    }
}
