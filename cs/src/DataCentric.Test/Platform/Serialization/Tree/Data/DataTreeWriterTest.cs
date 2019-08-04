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
using System.Collections.Generic;
using DataCentric;
using Xunit;

namespace DataCentric.Test
{
    /// <summary>Unit tests for DataTreeWriter.</summary>
    public class DataTreeWriterTest
    {
        /// <summary>Test for serialization of a basic data structure.</summary>
        [Fact]
        public void Basic()
        {
            using (var context = new UnitTestContext(this))
            {
                var obj = new BaseTypeSampleData();
                obj.SampleID = "ABC";
                obj.DoubleElement = 1.0;

                string xmlString = obj.ToXml();
                string mappedClassName = ClassInfo.GetOrCreate(obj).MappedClassName;
                string testName = "basic";
                context.Verify.File("Original.xml", xmlString);

                var copy = new BaseTypeSampleData();
                var copyWriter = new DataTreeWriter(copy);
                copyWriter.WriteStartDocument(mappedClassName);
                ((ITreeSerializable) obj).SerializeTo(copyWriter);
                copyWriter.WriteEndDocument(mappedClassName);

                string copyString = copy.ToXml();
                context.Verify.File("Copy.xml", copyString);
                context.Verify.Assert(xmlString == copyString, "Serialization roundtrip");
            }
        }

        /// <summary>Test for serialization of a complete data structure.</summary>
        [Fact]
        public void Complete()
        {
            using (var context = new UnitTestContext(this))
            {
                var obj = new DerivedTypeSampleData();
                obj.SampleID = "AAA";
                obj.DoubleElement = 1.0;
                obj.IntElement = 1;
                obj.KeyElement = new BaseTypeSampleKey();
                obj.KeyElement.SampleID = "BBB";

                obj.NonNullableIntList = new List<int>();
                obj.NonNullableIntList.Add(100);
                obj.NonNullableIntList.Add(200);

                obj.NullableIntList = new List<int?>();
                obj.NullableIntList.Add(100);
                obj.NullableIntList.Add(null);
                obj.NullableIntList.Add(300);

                obj.StringList = new List<string>();
                obj.StringList.Add("AAAA");
                obj.StringList.Add("BBBB");

                obj.KeyList = new List<BaseTypeSampleKey>();
                var keyListElement1 = new BaseTypeSampleKey();
                keyListElement1.SampleID = "BBB";
                obj.KeyList.Add(keyListElement1);
                var keyListElement2 = new BaseTypeSampleKey();
                keyListElement2.SampleID = "BBB";
                obj.KeyList.Add(keyListElement2);

                obj.DataElement = new ElementTypeSampleData();
                obj.DataElement.SampleID = "CCC";
                obj.DataElement.DoubleElement = 2.0;

                obj.DataList = new List<ElementTypeSampleData>();
                var dataListItem1 = new ElementTypeSampleData();
                dataListItem1.SampleID = "DDD";
                dataListItem1.DoubleElement = 3.0;
                obj.DataList.Add(dataListItem1);
                var dataListItem2 = new ElementTypeSampleData();
                dataListItem2.SampleID = "DDD";
                dataListItem2.DoubleElement = 4.0;
                obj.DataList.Add(dataListItem2);

                string mappedClassName = ClassInfo.GetOrCreate(obj).MappedClassName;

                var copy = new DerivedTypeSampleData();
                var copyWriter = new DataTreeWriter(copy);
                copyWriter.WriteStartDocument(mappedClassName);
                ((ITreeSerializable) obj).SerializeTo(copyWriter);
                copyWriter.WriteEndDocument(mappedClassName);

                string xmlString = obj.ToXml();
                context.Verify.File("Original.xml", xmlString);

                string copyString = copy.ToXml();
                context.Verify.File("Copy.xml", copyString);
                context.Verify.Assert(xmlString == copyString, "Serialization roundtrip");
            }
        }
    }
}
