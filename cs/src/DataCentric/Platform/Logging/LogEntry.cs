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

namespace DataCentric
{
    /// <summary>Log entry consists of formatted message and the original message parameter objects.</summary>
    public class LogEntry
    {
        private string entryText_;

        /// <summary>Truncates message parameters before formatting the message.</summary>
        public LogEntry(LogEntryType entryType, string entrySubType, string message, params object[] messageParams)
        {
            // Truncates message parameters before formatting the message.
            string formattedMessage = LogImpl.FormatMessage(message, messageParams);


            string prefix = entryType.ToString();
            if (!string.IsNullOrEmpty(entrySubType))
            {
                // Append dot delimited subtype if specified
                prefix = string.Concat(prefix, ".", entrySubType);
            }

            // Format entry text
            entryText_ = string.Concat(prefix, ": ", formattedMessage);
        }

        /// <summary>Formatted message with truncated parameter strings.
        /// Use MessageParams to access the original parameter objects.</summary>
        public override string ToString() { return entryText_; }
    }
}
