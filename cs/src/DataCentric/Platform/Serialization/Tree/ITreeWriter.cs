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
}
