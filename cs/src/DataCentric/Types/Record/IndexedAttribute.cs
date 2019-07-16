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

namespace DataCentric
{
    /// <summary>
    /// Use this attribute to specify that the element should
    /// be included in a database index for the record.
    ///
    /// If this attribute is defined with the same index name
    /// in both base and derived class(es), additional copies
    /// of the index will be created if GetCollection(T) is
    /// invoked for more than one class in the inheritance
    /// hierarchy.
    ///
    /// For example, if base class defines an index for
    /// Element1 and derived for Element2, and GetCollection(T)
    /// is called for both, one index will have Element1 and
    /// the other both Element1 and Element2.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public class IndexedAttribute : Attribute
    {
        /// <summary>
        /// Name of the index (optional).
        ///
        /// Use this optional property to specify to which
        /// index the attribute applies when more than one
        /// index is defined for a data type.
        ///
        /// Named indices can coexist with the default index
        /// that has no name.
        /// </summary>
        public string Index { get; set; } = String.Empty;

        /// <summary>
        /// Order of the element in the index (optional).
        ///
        /// Normally nullable would be used for an optional
        /// variable, however in .NET attributes cannot
        /// have nullable properties.
        ///
        /// The default order of elements in the index
        /// is the order of declaration within the class,
        /// and from base to derived in the inheritance
        /// hierarchy. When a different order is required,
        /// the order field can be used to override the
        /// default order.
        ///
        /// If any element in the index defines this property,
        /// it should be defined by all other elements
        /// within the same index. This property must be
        /// unique within the index.
        /// </summary>
        public int Order { get; set; } = IntUtils.Empty;
    }
}