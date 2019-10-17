using CommandLine;

namespace DataCentric.Cli
{
    [Verb("test", HelpText = "Test runner.")]
    public class TestOptions
    {
        [Option('p', "pattern", HelpText = "Pattern to filter tests.", Default = "*")]
        public string TestPattern { get; set; }
    }
}