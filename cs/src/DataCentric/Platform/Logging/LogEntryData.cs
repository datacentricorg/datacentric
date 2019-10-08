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
using System.Collections.Generic;
using CsvHelper.Configuration.Attributes;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using NodaTime;

namespace DataCentric
{
    /// <summary>
    /// Records a single entry in a log.
    ///
    /// The log record serves as the key for querying log entries.
    /// To obtain the entire log, run a query for the Log element of
    /// the entry record, then sort the entry records by their ObjectId.
    ///
    /// Derive from this class to provide specialized log entry types
    /// that include additional data.
    /// </summary>
    public class LogEntryData : TypedRecord<LogEntryKey, LogEntryData>
    {
        /// <summary>
        /// Log for which the entry is recorded.
        ///
        /// To obtain the entire log, run a query for the Log element of
        /// the entry record, then sort the entry records by their ObjectId.
        /// </summary>
        [BsonRequired]
        public LogKey Log { get; set; }

        /// <summary>
        /// Minimal verbosity for which log entry will be displayed.
        /// </summary>
        [BsonRequired]
        public LogVerbosity? Verbosity { get; set; }

        /// <summary>
        /// Short, single-line title of the log entry.
        ///
        /// Line breaks in title will be replaced by spaces when the
        /// log entry is displayed.
        /// </summary>
        [BsonRequired]
        public string Title { get; set; }

        /// <summary>
        /// Optional single-line or multi-line description of the log entry.
        ///
        /// Line breaks, whitespace and other formatting in the description
        /// will be preserved when the log entry is displayed.
        /// </summary>
        public string Description { get; set; }

        //--- METHODS

        /// <summary>
        /// Returns verbosity followed by semicolon and then title
        /// with line breaks replaced by spaces, for example:
        ///
        /// Info: Sample Info Message
        /// </summary>
        public override string ToString()
        {
            string singleLineTitle = Title.Replace(Environment.NewLine, " ");
            string result = $"{Verbosity}: {singleLineTitle}";
            return result;
        }
    }
}
