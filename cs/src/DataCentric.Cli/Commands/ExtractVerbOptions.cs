using System.Collections.Generic;
using CommandLine;

namespace DataCentric.Cli
{
    [Verb("extract", HelpText = "Extract type info from assemblies and convert to declarations.")]
    public class ExtractOptions
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
}