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
using System.IO;
using System.Text;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using Newtonsoft.Json;
using Xunit;
using DataCentric;

namespace DataCentric.Test
{
    /// <summary>Unit test for JSON and BSON serialization.</summary>
    public class JsonWriterTest
    {
        /// <summary>JSON serialization using Newtonsoft writer.</summary>
        [Fact]
        public void NewtonsoftWriter()
        {
            using (var context = new UnitTestContext(this))
            {
                var stringBuilder = new StringBuilder();
                var stringWriter = new StringWriter(stringBuilder);
                var writer = new Newtonsoft.Json.JsonTextWriter(stringWriter);

                // Add root element with two attributes
                // FIXME writer.WritePropertyName("rootElement");
                writer.WriteStartObject();

                // Add value element using a single method call
                writer.WritePropertyName("valueElement1");
                writer.WriteValue("TestValue1");

                // Add value element with two attributes by creating element explicitly
                writer.WritePropertyName("valueElement2");
                writer.WriteStartObject();
                writer.WritePropertyName("valueElement3");
                writer.WriteValue("TestValue2");
                writer.WriteEndObject();

                // ose the previosly opened elements
                writer.WriteEndObject();

                // Output the result
                string result = stringBuilder.ToString();
                context.Verify.Text(result);
            }
        }
    }
}
