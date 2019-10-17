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
}