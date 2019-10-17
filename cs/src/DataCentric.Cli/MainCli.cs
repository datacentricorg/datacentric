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
using CommandLine;

namespace DataCentric.Cli
{
    /// <summary>
    /// Entry point for the Runtime command line interface (CLI).
    /// </summary>
    public static class MainCli
    {
        /// <summary>
        /// Entry point method for CLI.
        /// </summary>
        public static int Main(string[] args)
        {
            // Interactive mode: commands separated by line
            if (args.Length == 0)
            {
                while (true)
                {
                    Console.Write("$ ");
                    args = Console.ReadLine()?.Split(' ');
                    ParserResult<object> parseInteractiveResult = Parser.Default.ParseArguments<RunOptions,
                        ExtractOptions,
                        TestOptions,
                        GenerateOptions,
                        HeadersOptions,
                        CsvConvertOptions,
                        ExitOptions>(args);

                    if (parseInteractiveResult is Parsed<object> parsedInteractive)
                    {
                        switch (parsedInteractive.Value)
                        {
                            case RunOptions runOptions:
                                runOptions.Execute();
                                break;
                            case ExtractOptions extractOptions:
                                extractOptions.Execute();
                                break;
                            case TestOptions testOptions:
                                testOptions.Execute();
                                break;
                            case GenerateOptions generateOptions:
                                generateOptions.Execute();
                                break;
                            case HeadersOptions headersOptions:
                                headersOptions.Execute();
                                break;
                            case CsvConvertOptions convertOptions:
                                convertOptions.Execute();
                                break;
                            case ExitOptions _:
                                return 0;
                            default:
                                return -1;
                        }
                    }
                    // Exit and show help if command is not recognized
                    else if (parseInteractiveResult is NotParsed<object>)
                    {
                        return -1;
                    }
                }
            }

            // Single command mode
            ParserResult<object> parseResult = Parser.Default.ParseArguments<RunOptions,
                ExtractOptions,
                TestOptions,
                GenerateOptions,
                HeadersOptions,
                CsvConvertOptions,
                ExitOptions>(args);

            if (parseResult is Parsed<object> parsed)
            {
                switch (parsed.Value)
                {
                    case RunOptions runOptions:
                        runOptions.Execute();
                        break;
                    case ExtractOptions extractOptions:
                        extractOptions.Execute();
                        break;
                    case TestOptions testOptions:
                        testOptions.Execute();
                        break;
                    case GenerateOptions generateOptions:
                        generateOptions.Execute();
                        break;
                    case HeadersOptions headersOptions:
                        headersOptions.Execute();
                        break;
                    case CsvConvertOptions convertOptions:
                        convertOptions.Execute();
                        break;
                    case ExitOptions _:
                        return 0;
                }
            }

            return -1;
        }
    }
}
