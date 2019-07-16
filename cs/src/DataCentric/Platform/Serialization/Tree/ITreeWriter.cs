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
using System.Xml;

namespace DataCentric
{
    /// <summary>Interface for writing tree data.</summary>
    public interface ITreeWriter
    {
        /// <summary>Write start document tags. This method
        /// should be called only once for the entire document.</summary>
        void WriteStartDocument(string rootElementName);

        /// <summary>Write end document tag. This method
        /// should be called only once for the entire document.
        /// The root element name passed to this method must match the root element
        /// name passed to the preceding call to WriteStartDocument(...).</summary>
        void WriteEndDocument(string rootElementName);

        /// <summary>Write element start tag. Each element may contain
        /// a single dictionary, a single value, or multiple array items.</summary>
        void WriteStartElement(string elementName);

        /// <summary>Write element end tag. Each element may contain
        /// a single dictionary, a single value, or multiple array items.
        /// The element name passed to this method must match the element name passed
        /// to the matching WriteStartElement(name) call at the same indent level.</summary>
        void WriteEndElement(string elementName);

        /// <summary>Write dictionary start tag. A call to this method
        /// must follow WriteStartElement(name).</summary>
        void WriteStartDict();

        /// <summary>Write dictionary end tag. A call to this method
        /// must be followed by WriteEndElement(name).</summary>
        void WriteEndDict();

        /// <summary>Write start tag for an array. A call to this method
        /// must follow WriteStartElement(name).</summary>
        void WriteStartArray();

        /// <summary>Write end tag for an array. A call to this method
        /// must be followed by WriteEndElement(name).</summary>
        void WriteEndArray();

        /// <summary>Write start tag for an array item. A call to this method
        /// must follow either WriteStartArray() or WriteEndArrayItem().</summary>
        void WriteStartArrayItem();

        /// <summary>Write end tag for an array item. A call to this method
        /// must be followed by either WriteEndArray() or WriteStartArrayItem().</summary>
        void WriteEndArrayItem();

        /// <summary>Write value start tag. A call to this method
        /// must follow WriteStartElement(name).</summary>
        void WriteStartValue();

        /// <summary>Write value end tag. A call to this method
        /// must be followed by WriteEndElement(name).</summary>
        void WriteEndValue();

        /// <summary>Write atomic value. Value type
        /// will be inferred from object.GetType().</summary>
        void WriteValue(object value);
    }

    /// <summary>Extension methods for ITreeWriter.</summary>
    public static class ITreeWriterEx
    {
        /// <summary>WriteStartElement(...) followed by WriteStartDict().</summary>
        public static void WriteStartDictElement(this ITreeWriter obj, string elementName)
        {
            obj.WriteStartElement(elementName);
            obj.WriteStartDict();
        }

        /// <summary>WriteEndDict(...) followed by WriteEndElement(...).</summary>
        public static void WriteEndDictElement(this ITreeWriter obj, string elementName)
        {
            obj.WriteEndDict();
            obj.WriteEndElement(elementName);
        }

        /// <summary>WriteStartElement(...) followed by WriteStartArray().</summary>
        public static void WriteStartArrayElement(this ITreeWriter obj, string elementName)
        {
            obj.WriteStartElement(elementName);
            obj.WriteStartArray();
        }

        /// <summary>WriteEndArray(...) followed by WriteEndElement(...).</summary>
        public static void WriteEndArrayElement(this ITreeWriter obj, string elementName)
        {
            obj.WriteEndArray();
            obj.WriteEndElement(elementName);
        }

        /// <summary>WriteStartArrayItem(...) followed by WriteStartDict().</summary>
        public static void WriteStartDictArrayItem(this ITreeWriter obj)
        {
            obj.WriteStartArrayItem();
            obj.WriteStartDict();
        }

        /// <summary>WriteEndDict(...) followed by WriteEndArrayItem(...).</summary>
        public static void WriteEndDictArrayItem(this ITreeWriter obj)
        {
            obj.WriteEndDict();
            obj.WriteEndArrayItem();
        }

        /// <summary>Write an element with no inner nodes.
        /// Element type is inferred by calling obj.GetType().</summary>
        public static void WriteValueElement(this ITreeWriter obj, string elementName, object value)
        {
            // Do not serialize null or empty value
            if (!value.IsEmpty())
            {
                obj.WriteStartElement(elementName);
                obj.WriteStartValue();
                obj.WriteValue(value);
                obj.WriteEndValue();
                obj.WriteEndElement(elementName);
            }
        }

        /// <summary>Write an array item with no inner nodes.
        /// Element type is inferred by calling obj.GetType().</summary>
        public static void WriteValueArrayItem(this ITreeWriter obj, object value)
        {
            // Writes null or empty value as BSON null
            obj.WriteStartArrayItem();
            obj.WriteStartValue();
            obj.WriteValue(value);
            obj.WriteEndValue();
            obj.WriteEndArrayItem();
        }

        /// <summary>Write a single array item.</summary>
        public static void WriteArrayItem(this ITreeWriter obj, object value)
        {
            // Will serialize null or empty value
            obj.WriteStartArrayItem();
            obj.WriteStartValue();
            obj.WriteValue(value);
            obj.WriteEndValue();
            obj.WriteEndArrayItem();
        }

        /// <summary>Write an array of elements with no inner nodes.
        /// Element type is inferred by calling obj.GetType().</summary>
        public static void WriteValueArray(this ITreeWriter obj, string elementName, IEnumerable<object> values)
        {
            obj.WriteStartArrayElement(elementName);
            foreach (object value in values)
            {
                obj.WriteArrayItem(value);
            }
            obj.WriteEndArrayElement(elementName);
        }
    }
}
