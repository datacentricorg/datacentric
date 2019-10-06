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
using Xunit;
using DataCentric;

namespace DataCentric.Test
{
    /// <summary>Test the base class (fixture) for unit tests.</summary>
    public class FixtureTest
    {
        /// <summary>Unit test of logging.</summary>
        [Fact]
        public void Logging()
        {
            using (var context = new UnitTestContext(this))
            {
                // Log entries other than Exception
                context.Log.Warning("Warning log entry.");
                context.Log.Info("Status log entry.");

                // Verify entries
                context.Log.Verify("Result verify entry.");
                context.Log.Assert(true, "Assert when condition is true.");
                context.Log.Assert(false, "Assert when condition is false.");

                // File verify entry and saving
                // FIXME - check that file is saved when enabled
                context.Verify.File("FileName.txt", "File verify entry.");
            }
        }

        /// <summary>Test native .NET exception logging after context initialization.</summary>
        [Fact]
        public void NativeException()
        {
            using (var context = new UnitTestContext(this))
            {
                // The test checks that the entry preceding exception is recorded
                context.Log.Info("Normal status entry preceding exception");

                try
                {
                    // Exception is not recorded to log in unit test
                    throw new Exception("Test exception message.");
                }
                catch (Exception e)
                {
                    // The message is recorded by the catch only
                    context.Log.Verify($"Message={e.Message}");
                }
            }
        }
    }
}
