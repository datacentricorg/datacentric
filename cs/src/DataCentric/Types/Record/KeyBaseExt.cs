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
using System.Linq;
using System.Reflection;
using MongoDB.Bson;
using NodaTime;

namespace DataCentric
{
    /// <summary>Extension methods for KeyBase.</summary>
    public static class KeyBaseExt
    {
        /// <summary>Deserialize record from XML using short
        /// class name without namespace for the root XML element.</summary>
        public static void ParseXml(this KeyBase obj, string xmlString)
        {
            IXmlReader reader = new XmlTreeReader(xmlString);

            // Root node of serialized XML must be the same as mapped class name without namespace
            var mappedFullName = ClassInfo.GetOrCreate(obj).MappedClassName;
            ITreeReader recordNodes = reader.ReadElement(mappedFullName);

            // Deserialize from XML nodes inside the root node
            obj.DeserializeFrom(recordNodes);
        }

        /// <summary>Serialize record to XML using short
        /// class name without namespace for the root XML element.</summary>
        public static string ToXml(this KeyBase obj)
        {
            // Get root XML element name using mapped final type of the object
            string rootName = ClassInfo.GetOrCreate(obj).MappedClassName;

            // Serialize to XML
            ITreeWriter writer = new XmlTreeWriter();
            writer.WriteStartDocument(rootName);
            obj.SerializeTo(writer);
            writer.WriteEndDocument(rootName);

            // Convert to string
            string result = writer.ToString();
            return result;
        }
    }
}
