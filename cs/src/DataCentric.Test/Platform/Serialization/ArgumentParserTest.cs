using DataCentric;
using Xunit;

namespace DataCentric.Test
{
    public static class ArgumentParserTest
    {
        private static void CheckArguments(string source, params string[] args)
        {
            Assert.Equal(CommandLineUtils.ParseArguments(source), args);
        }

        [Fact]
        public static void ParseArgumentsTest()
        {
            CheckArguments("a", "a");
            CheckArguments("a b", "a", "b");
            CheckArguments("  a   b ", "a", "b");
            CheckArguments("\"a\"", "a");
            CheckArguments("\"a\" b", "a", "b");
            CheckArguments(" \"a \" b", "a ", "b");
            CheckArguments(" \"a \"b", "a b");
            CheckArguments("a \"\"", "a", "");
            CheckArguments(" \"a\"b\" c", "ab c");
            CheckArguments("\"a\"\" \" b ", "a\" ", "b");
            CheckArguments("a \"\" \" b ", "a", "", " b ");
            CheckArguments("a\"\"b \"\"\" \" b", "ab", "\" ", "b");
        }
    }
}
