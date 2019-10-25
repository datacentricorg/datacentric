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
    /// <summary>Test for HandlerMethod attribute.</summary>
    public class ViewerTestData : TestData
    {
        /// <summary>
        /// In this test, we invoke viewers in process to create baseline output
        /// that will be compared to the output of the CLI call.
        /// </summary>
        [Fact]
        public void Smoke()
        {
            using (var context = CreateMethodContext())
            {
                // Create base instance
                var baseSampleData = new BaseSampleData();
                baseSampleData.RecordName = "Smoke";
                baseSampleData.RecordIndex = 1;
                context.SaveOne(baseSampleData);

                // Invoke viewers
                baseSampleData.DefaultNamedViewer();
                baseSampleData.CustomNamedViewer();

                // Check for the results
                var defaultNamedView = context
                    .Load(new ViewKey {RecordId = baseSampleData.Id, ViewName = "DefaultNamedViewer"})
                    .CastTo<ViewSampleData>();
                context.Log.Verify(defaultNamedView.SampleViewString);
                var customNamedView = context
                    .Load(new ViewKey {RecordId = baseSampleData.Id, ViewName = "CustomName"})
                    .CastTo<ViewSampleData>();
                context.Log.Verify(customNamedView.SampleViewString);
            }
        }
    }
}
