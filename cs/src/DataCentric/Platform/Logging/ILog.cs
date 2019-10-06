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
        /// Append a new single-line entry to the log.
        ///
        /// This method has no effect unless entry verbosity
        /// exceeds log verbosity.
        /// </summary>
        void Entry(LogVerbosity verbosity, string message);

        /// <summary>
        /// Append a new entry to the log that has single-line title
        /// and multi-line body. The body will be indented by one
        /// tab stop.
        ///
        /// This method has no effect unless entry verbosity
        /// exceeds log verbosity. 
        /// </summary>
        void Entry(LogVerbosity verbosity, string title, string body);
    }

    /// <summary>Extension methods for ILog.</summary>
    public static class ILogExt
    {
        /// <summary>
        /// Record a single-line error message.
        ///
        /// This method does not throw an exception; it is invoked
        /// to indicate an error when exception is not necessary,
        /// and it may also be invoked when the exception is caught.
        /// </summary>
        public static void Error(this ILog obj, string message)
        {
            // Published at any level of verbosity
            obj.Entry(LogVerbosity.Error, message);
        }

        /// <summary>
        /// Record an error message with a single-line title
        /// and multi-line body. The body will be indented by one
        /// tab stop.
        ///
        /// This method does not throw an exception; it is invoked
        /// to indicate an error when exception is not necessary,
        /// and it may also be invoked when the exception is caught.
        /// </summary>
        public static void Error(this ILog obj, string title, string body)
        {
            // Published at any level of verbosity
            obj.Entry(LogVerbosity.Error, title, body);
        }

        /// <summary>
        /// Record a single-line warning message.
        /// </summary>
        public static void Warning(this ILog obj, string message)
        {
            // Requires at least Warning verbosity
            obj.Entry(LogVerbosity.Warning, message);
        }

        /// <summary>
        /// Record a warning message with a single-line title
        /// and multi-line body. The body will be indented by one
        /// tab stop.
        /// </summary>
        public static void Warning(this ILog obj, string title, string body)
        {
            // Requires at least Warning verbosity
            obj.Entry(LogVerbosity.Warning, title, body);
        }

        /// <summary>
        /// Record a single-line information message.
        ///
        /// Information output should be used sparingly to avoid
        /// flooding log output with superfluous data. An information
        /// message should never be generated inside a loop.
        /// </summary>
        public static void Info(this ILog obj, string message)
        {
            // Requires at least Status verbosity
            obj.Entry(LogVerbosity.Info, message);
        }

        /// <summary>
        /// Record a information message with a single-line title
        /// and multi-line body. The body will be indented by one
        /// tab stop.
        ///
        /// Information output should be used sparingly to avoid
        /// flooding log output with superfluous data. An information
        /// message should never be generated inside a loop.
        /// </summary>
        public static void Info(this ILog obj, string title, string body)
        {
            // Requires at least Status verbosity
            obj.Entry(LogVerbosity.Info, title, body);
        }

        /// <summary>
        /// Record a single-line verification message.
        ///
        /// Verification messages are used in approval testing and
        /// are displayed at Verify or higher verbosity level, but
        /// not at the Info verbosity level.
        /// </summary>
        public static void Verify(this ILog obj, string message)
        {
            obj.Entry(LogVerbosity.Verify, message);
        }

        /// <summary>
        /// Record a verification message with a single-line title
        /// and multi-line body. The body will be indented by one
        /// tab stop.
        ///
        /// Verification messages are used in approval testing and
        /// are displayed at Verify or higher verbosity level, but
        /// not at the Info verbosity level.
        /// </summary>
        public static void Verify(this ILog obj, string title, string body)
        {
            obj.Entry(LogVerbosity.Verify, title, body);
        }

        /// <summary>
        /// Record a single-line error message if condition is false,
        /// and a single-line information message if condition is true.
        ///
        /// The information message is recorded only if
        /// log verbosity is at least Info.
        /// </summary>
        public static void Assert(this ILog obj, bool condition, string message)
        {
            if (!condition) obj.Error(message);
            else obj.Verify(message);
        }

        /// <summary>
        /// Record a multi-line error message if condition is false,
        /// and a multi-line information message if condition is true.
        ///
        /// The information message is recorded only if
        /// log verbosity is at least Info.
        /// </summary>
        public static void Assert(this ILog obj, bool condition, string title, string body)
        {
            if (!condition) obj.Error(title, body);
            else obj.Verify(title, body);
        }
    }
}
