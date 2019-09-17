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


using System.Collections.Generic;
using CommandLine;

namespace DataCentric.Cli
{
    [Verb("run", HelpText = "Execute handler.")]
    public class RunVerbOptions
    {
        [Option('s', "source", Required = true, HelpText = "Source environment - folder for file storage and connection string for DB.")]
        public string Source { get; set; }

        [Option('e', "environment", Required = true, HelpText = "Parameter pointing to environment snapshot name.")]
        public string Environment { get; set; }

        [Option('d', "dataset", Required = true, HelpText = "Setting specifies data set name.")]
        public string Dataset { get; set; }

        [Option('k', "key", Required = true, HelpText = "Key of entity.")]
        public string Key { get; set; }

        [Option('t', "type", Required = true, HelpText = "Type handler belongs.")]
        public string Type { get; set; }

        [Option('h', "handler", Required = true, HelpText = "Handler name to execute.")]
        public string Handler { get; set; }

        [Option('a', "arguments", HelpText = "Space separated handler arguments in name=value format.")]
        public IEnumerable<string> Arguments { get; set; }
    }

    [Verb("test", HelpText = "Test runner.")]
    public class TestVerbOptions
    {
        [Option('p', "pattern", HelpText = "Pattern to filter tests.", Default = "*")]
        public string TestPattern { get; set; }
    }

    [Verb("extract", HelpText = "Extract type info from assemblies and convert to declarations.")]
    public class ExtractVerbOptions
    {
        [Option('a', "assembly", HelpText = "Paths to assemblies to extract types.")]
        public IEnumerable<string> Assemblies { get; set; }

        [Value(0, MetaName = "types", HelpText = "Regex for types to filter.")]
        public IEnumerable<string> Types { get; set; }

        [Option('o', "output", HelpText = "Output folder to save generated declarations.", Default = ".")]
        public string OutputFolder { get; set; }

        [Option('p', "project", HelpText = "Path to search for corresponding project location.")]
        public string ProjectPath { get; set; }

        [Option('l', "legacy", HelpText = "Generates declarations in legacy format.", Default = false)]
        public bool Legacy { get; set; }
    }

    [Verb("generate", HelpText = "Generate c++ header files from declarations.")]
    public class GenerateVerbOptions
    {
        [Option('i', "input", Required = true, HelpText = "Path to declarations location.")]
        public string InputFolder { get; set; }

        [Option('o', "output", Required = true, HelpText = "Path to save generated c++ classes.")]
        public string OutputFolder { get; set; }

        [Option('s', "settings", HelpText = "File with settings for generator.")]
        public string SettingsPath { get; set; }
    }

    [Verb("headers", HelpText = "Generate c++ header files c# assemblies.")]
    public class HeadersVerbOptions
    {
        [Option('a', "assembly", HelpText = "Paths to assemblies to extract types.")]
        public IEnumerable<string> Assemblies { get; set; }

        [Value(0, MetaName = "types", HelpText = "Regex for types to filter.")]
        public IEnumerable<string> Types { get; set; }

        [Option('p', "project", HelpText = "Path to search for corresponding project location.")]
        public string ProjectPath { get; set; }

        [Option('o', "output", HelpText = "Output folder to save generated c++ headers.", Default = ".")]
        public string OutputFolder { get; set; }

        [Option('s', "settings", HelpText = "File with settings for generator.")]
        public string SettingsPath { get; set; }
    }

    [Verb("exit", HelpText = "Exits from interactive shell.")]
    public class ExitVerbOptions
    {
    }
}
