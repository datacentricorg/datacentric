using System;
using System.Threading;
using Xunit.Runners;

namespace DataCentric.Cli
{
    public static class TestRunner
    {
        private static readonly object ConsoleLock = new object();

        // Use an event to know when we're done
        private static ManualResetEvent finished_;

        private static int result_;

        public static int Run(string testAssembly, string typeName)
        {


            // Start out assuming success; we'll set this to 1 if we get a failed test
            result_ = 0;

            using (var runner = AssemblyRunner.WithoutAppDomain($"{testAssembly}.dll"))
            {
                finished_ = new ManualResetEvent(false);

                runner.OnExecutionComplete = OnExecutionComplete;
                runner.OnTestFailed = OnTestFailed;
                runner.OnTestSkipped = OnTestSkipped;
                runner.OnTestPassed = OnTestPassed;

                runner.Start(typeName, parallel: false);

                finished_.WaitOne();
                finished_.Dispose();
            }

            return result_;
        }

        private static void OnTestPassed(TestPassedInfo info)
        {
            lock (ConsoleLock)
                Console.WriteLine("[PASSED] {0}: {1}", info.TestDisplayName, info.ExecutionTime);
        }

        private static void OnExecutionComplete(ExecutionCompleteInfo info)
        {
            finished_.Set();
        }

        private static void OnTestFailed(TestFailedInfo info)
        {
            lock (ConsoleLock)
            {
                Console.ForegroundColor = ConsoleColor.Red;

                Console.WriteLine("[FAIL] {0}: {1}", info.TestDisplayName, info.ExceptionMessage);
                if (info.ExceptionStackTrace != null)
                    Console.WriteLine(info.ExceptionStackTrace);

                Console.ResetColor();
            }

            result_ = 1;
        }

        private static void OnTestSkipped(TestSkippedInfo info)
        {
            lock (ConsoleLock)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("[SKIP] {0}: {1}", info.TestDisplayName, info.SkipReason);
                Console.ResetColor();
            }
        }
    }
}