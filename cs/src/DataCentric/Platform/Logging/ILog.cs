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
    public interface ILog
    {
        /// <summary>Context for which this interface is defined.
        /// Use to access other interfaces of the same context.</summary>
        IContext Context { get; }

        /// <summary>Log verbosity is the highest log entry type displayed.
        /// Verbosity can be modified at runtime to provide different levels of
        /// verbosity for different code segments.</summary>
        LogEntryType Verbosity { get; set; }

        /// <summary>Append new entry to the log if entry type is the same or lower than log verbosity.
        /// Entry subtype is an optional tag in dot delimited format (specify null if no subtype).</summary>
        void Append(LogEntryType entryType, string entrySubType, string message, params object[] messageParams);

        /// <summary>Flush log contents to permanent storage.</summary>
        void Flush();

        /// <summary>Close log and release handle to permanent storage.</summary>
        void Close();
    }

    /// <summary>Extension methods are provided for all message types except for error.
    /// Error message type must be handled by throwing exception.</summary>
    public static class ILogEx
    {
        /// <summary>Record an error message and return exception with the same message.
        /// The caller is expected to throw the exception: throw Log.Exception(message, messageParams).</summary>
        public static Exception Exception(this ILog obj, string message, params object[] messageParams)
        {
            // Requires at least Error verbosity
            obj.Append(LogEntryType.Error, String.Empty, message, messageParams);

            // Copy message parameters to the Data dictionary of the exception
            Exception e = new Exception(string.Format(message, messageParams));
            for (int i = 0; i < messageParams.Length; i++)
            {
                // Record with key equal to index of the parameter in messageParams array
                e.Data[i] = messageParams[i];
            }

            // The caller must throw the returned exception
            return e;
        }

        /// <summary>Record an error message and throw exception return by Log.Exception(...).</summary>
        public static void Error(this ILog obj, string message, params object[] messageParams)
        {
            // Exception is thrown irrespective of verbosity,
            // but the log entry requires at least Error verbosity
            throw obj.Exception(message, messageParams);
        }

        /// <summary>Record a warning.</summary>
        public static void Warning(this ILog obj, string message, params object[] messageParams)
        {
            // Requires at least Warning verbosity
            obj.Append(LogEntryType.Warning, String.Empty, message, messageParams);
        }

        /// <summary>Record a status message.</summary>
        public static void Status(this ILog obj, string message, params object[] messageParams)
        {
            // Requires at least Status verbosity
            obj.Append(LogEntryType.Status, String.Empty, message, messageParams);
        }
    }
}
