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
    /// <summary>Approval data is recorded in context log under Verify entry type.</summary>
    public class LogVerify : IVerify
    {
        /// <summary>Context for which this interface is defined.
        /// Use to access other interfaces of the same context.</summary>
        public IContext Context { get; private set; }

        /// <summary>Test class name.</summary>
        public string ClassName { get; set; }

        /// <summary>Test method name.</summary>
        public string MethodName { get; set; }

        /// <summary>Indicates whether approval data is recorded by the context.
        /// Check to avoid performing expensive calculations that will not be recorded.</summary>
        public bool IsSet // TODO - rename to make the role of this method more clear
        {
            get
            {
                // Returns true if verbosity level is at least Verify
                var verbosity = Context.Log.Verbosity;
                return verbosity == LogEntryType.Empty || verbosity >= LogEntryType.Verify;
            }
        }

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
        public virtual void Init(IContext context)
        {
            // Uncomment except in root class of the hierarchy
            // base.Init(context);

            // Check that argument is not null and assign to the Context property
            if (context == null) throw new Exception($"Null context is passed to the Init(...) method for {GetType().Name}.");
            Context = context;
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
            // Flushes the log where verify output is written
            Context.Log.Flush();

            // Uncomment except in root class of the hierarchy
            // base.Flush();
        }
    }
}
