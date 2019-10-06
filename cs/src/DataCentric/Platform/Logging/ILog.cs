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
    /// <summary>Log interface.</summary>
    public interface ILog : IDisposable
    {
        /// <summary>Context for which this interface is defined.
        /// Use to access other interfaces of the same context.</summary>
        IContext Context { get; }

        /// <summary>Log verbosity is the highest log entry type displayed.
        /// Verbosity can be modified at runtime to provide different levels of
        /// verbosity for different code segments.</summary>
        LogVerbosity Verbosity { get; set; }

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
        void Init(IContext context);

        /// <summary>Flush data to permanent storage.</summary>
        void Flush();

        /// <summary>
        /// Append new entry to the log unless entry verbosity exceeds log verbosity.
        /// </summary>
        void Entry(LogVerbosity verbosity, string entrySubType, string message);
    }

    /// <summary>Extension methods for ILog.</summary>
    public static class ILogExt
    {
        /// <summary>Record an error message and throw exception return by Log.Exception(...).</summary>
        public static void Error(this ILog obj, string message)
        {
            // Published at any level of verbosity
            obj.Entry(LogVerbosity.Error, String.Empty, message);
        }

        /// <summary>Record a warning.</summary>
        public static void Warning(this ILog obj, string message)
        {
            // Requires at least Warning verbosity
            obj.Entry(LogVerbosity.Warning, String.Empty, message);
        }

        /// <summary>Record a status message.</summary>
        public static void Status(this ILog obj, string message)
        {
            // Requires at least Status verbosity
            obj.Entry(LogVerbosity.Status, String.Empty, message);
        }
    }
}
