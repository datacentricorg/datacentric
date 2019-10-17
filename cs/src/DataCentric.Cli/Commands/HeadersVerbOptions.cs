using System.Collections.Generic;
using CommandLine;

namespace DataCentric.Cli
{
    [Verb("headers", HelpText = "Generate c++ header files c# assemblies.")]
    public class HeadersOptions
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
}