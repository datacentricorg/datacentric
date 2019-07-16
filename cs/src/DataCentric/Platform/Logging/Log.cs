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
    /// <summary>Abstract base class contains static methods
    /// and functionality common to all implementations.
    /// Deriving from this class is optional.</summary>
    public abstract class Log
    {
        private const int maxMessageParamLength_ = 255;

        /// <summary>Format message after truncating message parameters.</summary>
        public static string FormatMessage(string message, params object[] messageParams)
        {
            if (messageParams.Length > 0)
            {
                // Restrict the length of each message parameter to truncateArgToLength_ to avoid
                // log overflow in case a large string is passed as log entry argument.
                // The string is truncated by keeping head and tail and replacing the middle by ...
                string[] formattedMessageParams = new string[messageParams.Length];
                for (int i = 0; i < messageParams.Length; ++i)
                {
                    // Use AsString() instead of ToString() for custom formatting of certain types
                    object arg = messageParams[i];
                    string argString = arg.AsString();

                    // Restrict message length if more than max length
                    if (argString.Length > maxMessageParamLength_)
                    {
                        int substringLength = (maxMessageParamLength_ - 5) / 2;
                        string head = argString.Substring(0, substringLength);
                        string tail = argString.Substring(argString.Length - substringLength, substringLength);
                        argString = String.Concat(head, " ... ", tail);
                    }

                    formattedMessageParams[i] = argString;
                }

                string result = String.Format(message, formattedMessageParams);

                // If the message ends with four dots (....) because the last argument is truncated
                // while its token is followed by a dot (.), reduce to three dots at the end (...)
                if (result.EndsWith("....")) result = result.Substring(0, formattedMessageParams.Length - 1);

                return result;
            }
            else
            {
                // Do not perform substitution if no parameters are specified
                // This will work even if the string contains {} characters
                return message;
            }
        }
    }
}
