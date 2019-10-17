using CommandLine;

namespace DataCentric.Cli
{
    [Verb("csv2mongo", HelpText = "Test runner.")]
    public class CsvConvertOptions
    {
        [Option('p', "path", HelpText = "Pattern to filter tests.", Required = true)]
        public string CsvPath { get; set; }
    }
}