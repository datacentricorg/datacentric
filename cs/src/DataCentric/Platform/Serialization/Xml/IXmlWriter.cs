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
    /// <summary>Interface for writing XML data, extends ITreeWriter
    /// that supports XML elements to add support for XML attributes.</summary>
    public interface IXmlWriter : ITreeWriter
    {
        /// <summary>Add XML attribute to the current element.
        /// One or multiple call(s) to this method must immediately follow
        /// WriteStartElement(name) before any other calls are made.</summary>
        void WriteAttribute(string attributeName, string attributeValue);
    }
}
