using CommandLine;

namespace DataCentric.Cli
{
    [Verb("generate", HelpText = "Generate c++ header files from declarations.")]
    public class GenerateOptions
    {
        [Option('i', "input", Required = true, HelpText = "Path to declarations location.")]
        public string InputFolder { get; set; }

        [Option('o', "output", Required = true, HelpText = "Path to save generated c++ classes.")]
        public string OutputFolder { get; set; }

        [Option('s', "settings", HelpText = "File with settings for generator.")]
        public string SettingsPath { get; set; }
    }
}