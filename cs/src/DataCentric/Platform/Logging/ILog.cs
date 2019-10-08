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
    /// <summary>
    /// Provides a unified API for writing log output to:
    ///
    /// * Console
    /// * String
    /// * File
    /// * Database
    /// * Logging frameworks such as log4net and other logging frameworks
    /// * Cloud logging services such as AWS CloudWatch
    /// </summary>
    public interface ILog : IDisposable
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
        IContext Context { get; }

        /// <summary>
        /// Minimal verbosity for which log entry will be displayed.
        /// </summary>
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
        void Entry(LogVerbosity verbosity, string message);

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
        void Entry(LogVerbosity verbosity, string title, string body);
    }

    /// <summary>Extension methods for ILog.</summary>
    public static class ILogExt
    {
        /// <summary>
        /// Record an error message to the log for any log verbosity.
        ///
        /// This method does not throw an exception; it is invoked
        /// to indicate an error when exception is not necessary,
        /// and it may also be invoked when the exception is caught.
        ///
        /// In a text log, first line of the message follows
        /// verbosity prefix after semicolon separator. Remaining
        /// lines of the message (if any) are recorded with 4 space
        /// indent, for example:
        ///
        /// Error: Message Line 1
        ///     Message Line 2
        ///     Message Line 3
        /// </summary>
        public static void Error(this ILog obj, string message)
        {
            // Published at any level of verbosity
            obj.Entry(LogVerbosity.Error, message);
        }

        /// <summary>
        /// Record an error message to the log for any log verbosity.
        ///
        /// This method does not throw an exception; it is invoked
        /// to indicate an error when exception is not necessary,
        /// and it may also be invoked when the exception is caught.
        ///
        /// In a text log, first line of the message follows
        /// verbosity prefix after semicolon separator. Remaining
        /// lines of the message (if any) are recorded with 4 space
        /// indent, for example:
        ///
        /// Error: Title Line 1
        ///     Title Line 2
        ///     Body Line 1
        ///     Body Line 2
        /// </summary>
        public static void Error(this ILog obj, string title, string body)
        {
            // Published at any level of verbosity
            obj.Entry(LogVerbosity.Error, title, body);
        }

        /// <summary>
        /// Record a warning message to the log if log verbosity
        /// is at least Warning.
        ///
        /// Warning messages should be used sparingly to avoid
        /// flooding log output with insignificant warnings.
        /// A warning message should never be generated inside
        /// a loop.
        ///
        /// In a text log, first line of the message follows
        /// verbosity prefix after semicolon separator. Remaining
        /// lines of the message (if any) are recorded with 4 space
        /// indent, for example:
        ///
        /// Warning: Message Line 1
        ///     Message Line 2
        ///     Message Line 3
        /// </summary>
        public static void Warning(this ILog obj, string message)
        {
            // Requires at least Warning verbosity
            obj.Entry(LogVerbosity.Warning, message);
        }

        /// <summary>
        /// Record a warning message to the log if log verbosity
        /// is at least Warning.
        ///
        /// Warning messages should be used sparingly to avoid
        /// flooding log output with insignificant warnings.
        /// A warning message should never be generated inside
        /// a loop.
        /// 
        /// In a text log, first line of the message follows
        /// verbosity prefix after semicolon separator. Remaining
        /// lines of the message (if any) are recorded with 4 space
        /// indent, for example:
        ///
        /// Warning: Title Line 1
        ///     Title Line 2
        ///     Body Line 1
        ///     Body Line 2
        /// </summary>
        public static void Warning(this ILog obj, string title, string body)
        {
            // Requires at least Warning verbosity
            obj.Entry(LogVerbosity.Warning, title, body);
        }

        /// <summary>
        /// Record an info message to the log if log verbosity
        /// is at least Info.
        ///
        /// Info messages should be used sparingly to avoid
        /// flooding log output with superfluous data. An info
        /// message should never be generated inside a loop.
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
        public static void Info(this ILog obj, string message)
        {
            // Requires at least Info verbosity
            obj.Entry(LogVerbosity.Info, message);
        }

        /// <summary>
        /// Record an info message to the log if log verbosity
        /// is at least Info.
        ///
        /// Info messages should be used sparingly to avoid
        /// flooding log output with superfluous data. An info
        /// message should never be generated inside a loop.
        /// 
        /// In a text log, first line of the message follows
        /// verbosity prefix after semicolon separator. Remaining
        /// lines of the message (if any) are recorded with 4 space
        /// indent, for example:
        ///
        /// Info: Title Line 1
        ///     Title Line 2
        ///     Body Line 1
        ///     Body Line 2
        /// </summary>
        public static void Info(this ILog obj, string title, string body)
        {
            // Requires at least Info verbosity
            obj.Entry(LogVerbosity.Info, title, body);
        }

        /// <summary>
        /// Record a verification message to the log if log verbosity
        /// is at least Verify.
        ///
        /// In a text log, first line of the message follows
        /// verbosity prefix after semicolon separator. Remaining
        /// lines of the message (if any) are recorded with 4 space
        /// indent, for example:
        ///
        /// Verify: Message Line 1
        ///     Message Line 2
        ///     Message Line 3
        /// </summary>
        public static void Verify(this ILog obj, string message)
        {
            // Requires at least Verify verbosity
            obj.Entry(LogVerbosity.Verify, message);
        }

        /// <summary>
        /// Record a verification message to the log if log verbosity
        /// is at least Verify.
        ///
        /// In a text log, first line of the message follows
        /// verbosity prefix after semicolon separator. Remaining
        /// lines of the message (if any) are recorded with 4 space
        /// indent, for example:
        ///
        /// Verify: Title Line 1
        ///     Title Line 2
        ///     Body Line 1
        ///     Body Line 2
        /// </summary>
        public static void Verify(this ILog obj, string title, string body)
        {
            // Requires at least Verify verbosity
            obj.Entry(LogVerbosity.Verify, title, body);
        }

        /// <summary>
        /// If condition is false, record an error message for any
        /// verbosity. If condition is true, record a verification
        /// message to the log if log verbosity is at least Verify.
        ///
        /// In a text log, first line of the message follows
        /// verbosity prefix after semicolon separator. Remaining
        /// lines of the message (if any) are recorded with 4 space
        /// indent, for example:
        ///
        /// Verify: Message Line 1
        ///     Message Line 2
        ///     Message Line 3
        /// </summary>
        public static void Assert(this ILog obj, bool condition, string message)
        {
            // Requires at least Verify verbosity if condition is true
            if (!condition) obj.Error(message);
            else obj.Verify(message);
        }

        /// <summary>
        /// If condition is false, record an error message for any
        /// verbosity. If condition is true, record a verification
        /// message to the log if log verbosity is at least Verify.
        ///
        /// In a text log, first line of the message follows
        /// verbosity prefix after semicolon separator. Remaining
        /// lines of the message (if any) are recorded with 4 space
        /// indent, for example:
        ///
        /// Verify: Title Line 1
        ///     Title Line 2
        ///     Body Line 1
        ///     Body Line 2
        /// </summary>
        public static void Assert(this ILog obj, bool condition, string title, string body)
        {
            // Requires at least Verify verbosity if condition is true
            if (!condition) obj.Error(title, body);
            else obj.Verify(title, body);
        }
    }
}
