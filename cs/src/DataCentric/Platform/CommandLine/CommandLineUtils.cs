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
using System.Text;

namespace DataCentric
{
    /// <summary>Command line utilities.</summary>
    public static class CommandLineUtils //!! This class will be refactored
    {
        /// <summary>Quote and escapes command-line arguments..</summary>
        public static string QuoteAndEscapeCommandLineArgument(string argument)
        {
            // Reserve string builder with excess capacity for added characters.
            // The initial capacity can be exceeded with no errors.
            var result = new StringBuilder(argument.Length + 10);

            // Quote and escape the argument
            result.Append("\"");
            for (int i = 0; i < argument.Length; i++)
            {
                var ch = argument[i];
                switch (ch)
                {
                    case '\\':
                    case '\"':
                        result.Append('\\');
                        result.Append(ch);
                        break;

                    default:
                        result.Append(ch);
                        break;
                }
            }

            result.Append("\"");
            return result.ToString();
        }

        private const char Comma = '"';

        /// <summary>Unquote and enumerate arguments from source according to entry point rules.</summary>
        public static IEnumerable<string> ParseArguments(string source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            int quoteCount = 0;
            bool quoted = false;
            StringBuilder arg = new StringBuilder();

            foreach (char current in source)
            {
                if (current == Comma)
                {
                    quoteCount++;
                }
                else
                {
                    quoted ^= quoteCount % 2 == 1;
                    arg.Append(Comma, (quoteCount - (quoted ? 0 : 1)) / 2);

                    if (char.IsWhiteSpace(current) && !quoted)
                    {
                        if ((arg.Length > 0) || (quoteCount > 0))
                        {
                            yield return arg.ToString();
                            arg.Clear();
                        }
                    }
                    else
                    {
                        arg.Append(current);
                    }

                    quoteCount = 0;
                }
            }

            if ((arg.Length > 0) || (quoteCount > 0))
            {
                arg.Append(Comma, (quoteCount - (quoted ? 0 : 1)) / 2);
                yield return arg.ToString();
            }
        }
    }
}
