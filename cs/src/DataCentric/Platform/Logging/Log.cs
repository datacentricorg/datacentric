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
    /// <summary>
    /// Implements ILog and sets default verbosity.
    /// </summary>
    public abstract class Log : ILog
    {
        /// <summary>
        /// Execution context provides access to key resources including:
        ///
        /// * Logging and error reporting
        /// * Cloud calculation service
        /// * Data sources
        /// * Filesystem
        /// * Progress reporting
        /// </summary>
        public IContext Context { get; private set; }

        /// <summary>
        /// Minimal verbosity for which log entry will be displayed.
        /// </summary>
        public LogVerbosity Verbosity { get; set; }

        //--- CONSTRUCTORS

        /// <summary>
        /// Set default verbosity level to Warning.
        ///
        /// The verbosity level can subsequently be modified
        /// using ILog interface.
        /// </summary>
        protected Log()
        {
            Verbosity = LogVerbosity.Warning;
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
        public abstract void Flush();

        /// <summary>
        /// Record a new entry to the log if log verbosity
        /// is the same or high as entry verbosity.
        ///
        /// In a text log, first line of the message follows
        /// verbosity prefix after semicolon separator. Remaining
        /// lines of the message (if any) are recorded with 4 space
        /// indent, for example:
        ///
        /// Info: Message Line 1
        ///     Message Line 2
        ///     Message Line 3
        /// </summary>
        public abstract void Entry(LogVerbosity verbosity, string message);

        /// <summary>
        /// Record a new entry to the log if log verbosity
        /// is the same or high as entry verbosity.
        ///
        /// In a text log, first line of the title follows verbosity
        /// prefix after semicolon separator. Remaining lines of the
        /// title (if any) and all lines of the body are recorded
        /// with 4 space indent, for example:
        ///
        /// Info: Title Line 1
        ///     Title Line 2
        ///     Body Line 1
        ///     Body Line 2
        /// </summary>
        public abstract void Entry(LogVerbosity verbosity, string title, string body);
    }
}
