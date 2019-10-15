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
using System.Reflection;

namespace DataCentric
{
    /// <summary>
    /// Comma separated list of element(s) included in the simple
    /// or composite primary key of the class.
    ///
    /// Examples:
    ///
    /// * A is a simple primary key consisting of element A;
    /// * A, B is a complex primary key consisting of elements A, B.
    ///
    /// Empty definition string means the record is a singleton.
    ///
    /// Providing the definition for the class rather than for the
    /// element makes it possible to (a) define a singleton record
    /// which has an empty primary key and (b) use a common base class
    /// for multiple derived classes, where each derived class has
    /// its own primary key that may or may not involve elements of
    /// their shared base. Neither would not be possible if key elements
    /// were defined based on a property rather than class attribute.
    ///
    /// When collection interface is obtained from a data source,
    /// names of the elements in this definition are validated
    /// to match element names of the class for which the index is
    /// defined. If the class does not have an an element with the
    /// name specified as part of the definition string, an error
    /// message is given.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class KeyElementsAttribute : Attribute
    {
        /// <summary>
        /// Create from the key definition string.
        ///
        /// Comma separated list of element(s) included in the simple
        /// or composite primary key of the class.
        ///
        /// Examples:
        ///
        /// * A is a simple primary key consisting of element A;
        /// * A, B is a complex primary key consisting of elements A, B.
        ///
        /// Empty definition string means the record is a singleton.
        /// </summary>
        public KeyElementsAttribute(string definition)
        {
            Definition = definition;
        }

        /// <summary>
        /// Comma separated list of element(s) included in the simple
        /// or composite primary key of the class.
        ///
        /// Examples:
        ///
        /// * A is a simple primary key consisting of element A;
        /// * A, B is a complex primary key consisting of elements A, B.
        ///
        /// Empty definition string means the record is a singleton.
        /// </summary>
        public string Definition { get; set; }

        /// <summary>
        /// Parse KeyElements definition string to get a list of
        /// ElementNames, where each ElementName is name of a
        /// property included in the primary key.
        ///
        /// The parser will also validate that each element name
        /// exists in type TRecord or its base types.
        /// </summary>
        public static List<string> ParseDefinition<TRecord>(string definition)
            where TRecord : Record
        {
            var result = new List<string>();

            // Get record type to be used in element name validation
            Type recordType = typeof(TRecord);

            // Trim leading and trailing whitespace from the definition
            definition = definition.Trim();

            if (string.IsNullOrEmpty(definition))
            {
                // Empty key elements definition string means the record is a singleton.
                // The key for a singleton record has no elements and only one such record
                // can exist in each dataset.
                //
                // For a singleton, the list of key elements is empty
                return result;
            }
            else
            {
                // Parse comma separated index definition string into tokens
                // and iterate over each token
                string[] tokens = definition.Split(',');
                foreach (string token in tokens)
                {
                    // Trim leading and trailing whitespace from each token
                    string elementName = token.Trim();

                    // Check that element name is not empty
                    if (elementName.Length == 0)
                        throw new Exception(
                            $"Empty element name in comma separated key definition string {definition}.");

                    // Check that element name does not contain whitespace
                    if (elementName.Contains(" "))
                        throw new Exception(
                            $"Element name {elementName} in comma separated key definition string {definition} contains whitespace.");

                    // Check that element is present in TRecord as public property with both getter and setter
                    var propertyInfo = recordType.GetProperty(elementName,
                        BindingFlags.Public | BindingFlags.GetProperty | BindingFlags.SetProperty |
                        BindingFlags.Instance);
                    if (propertyInfo == null)
                        throw new Exception(
                            $"Property {elementName} not found in {recordType.Name} or its parents, " +
                            $"or is not a public property with both getter and setter defined.");

                    // Add element and its sort order to the result
                    result.Add(elementName);
                }

                return result;
            }
        }
    }
}