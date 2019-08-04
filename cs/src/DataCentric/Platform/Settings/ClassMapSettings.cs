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

namespace DataCentric
{
    /// <summary>
    /// Settings for mapping namespaces, class names, and method names.
    /// </summary>
    public static class ClassMapSettings
    {
        /// <summary>
        /// Namespace prefixes including dot separators ignored by class mapping (empty by default).
        ///
        /// Thread safety requires that this property is modified as a whole by
        /// assigning a new instance of List; values should not be appended to
        /// an existing list.
        /// </summary>
        public static List<string> IgnoredNamespacePrefixes { get; set; } = new List<string>();

        /// <summary>
        /// Namespace suffixes including dot separators ignored by class mapping (empty by default).
        ///
        /// Thread safety requires that this property is modified as a whole by
        /// assigning a new instance of List; values should not be appended to
        /// an existing list.
        /// </summary>
        public static List<string> IgnoredNamespaceSuffixes { get; set; } = new List<string>();

        /// <summary>
        /// Class name prefixes ignored by class mapping (empty by default).
        ///
        /// Thread safety requires that this property is modified as a whole by
        /// assigning a new instance of List; values should not be appended to
        /// an existing list.
        /// </summary>
        public static List<string> IgnoredClassNamePrefixes { get; set; } = new List<string>();

        /// <summary>
        /// Class name prefixes ignored by class mapping.
        ///
        /// Thread safety requires that this property is modified as a whole by
        /// assigning a new instance of List; values should not be appended to
        /// an existing list.
        /// </summary>
        public static List<string> IgnoredClassNameSuffixes { get; set; } = new List<string> { "Data", "Key" };

        /// <summary>
        /// Method name prefixes ignored by class mapping (empty by default).
        ///
        /// Thread safety requires that this property is modified as a whole by
        /// assigning a new instance of List; values should not be appended to
        /// an existing list.
        /// </summary>
        public static List<string> IgnoredMethodNamePrefixes { get; set; } = new List<string>();

        /// <summary>
        /// Method name prefixes ignored by class mapping (empty by default).
        ///
        /// Thread safety requires that this property is modified as a whole by
        /// assigning a new instance of List; values should not be appended to
        /// an existing list.
        /// </summary>
        public static List<string> IgnoredMethodNameSuffixes { get; set; } = new List<string>();
    }
}
