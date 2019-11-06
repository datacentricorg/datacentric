﻿/*
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
using System.Xml.Serialization;

namespace DataCentric.Cli
{
    /// <summary>Enum declaration.</summary>
    [Serializable]
    [XmlRoot]
    public class EnumDecl : IDecl
    {

        /// <summary>Module reference.</summary>
        public ModuleKey Module { get; set; }

        /// <summary>Enum name is unique when combined with module.</summary>
        public string Name { get; set; }

        /// <summary>Enum aliases.</summary>
        [XmlElement]
        public List<string> Aliases { get; set; }

        /// <summary>Enum label.</summary>
        public string Label { get; set; }

        /// <summary>Enum comment.</summary>
        public string Comment { get; set; }

        /// <summary>Category.</summary>
        public string Category { get; set; }

        /// <summary>Array of enum items.</summary>
        [XmlElement]
        public List<EnumItemDecl> Items { get; set; }
    }
}