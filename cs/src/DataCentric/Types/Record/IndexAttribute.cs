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
    /// Use this class attribute to specify a database index
    /// for the class. More than one index attribute can
    /// be provided for the class.
    ///
    /// The index is specified as a definition string, which is
    /// a delimited list of elements included in the index
    /// with + or - separator between the elements.
    ///
    /// The separator acts as prefix indicating ascending (+)
    /// or descending (-) order of the element in the index.
    /// The separator is optional inf front of the first
    /// element in the index, if not specified the first
    /// element is sorted in ascending (+) order.
    ///
    /// Examples:
    ///
    /// * A is index on element A in ascending order;
    /// * -A is index on element A in descending order;
    /// * A+B-C is index on elements A and B in ascending
    ///   order and then element C in descending order.
    ///
    /// The names of the elements in the index definition
    /// are validated relative to element names of the
    /// class for which the index is defined. If the class
    /// does not have an an element with the name specified
    /// as part of the definition string, an error message
    /// is given.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class IndexElementsAttribute : Attribute
    {
        /// <summary>
        /// Create from the index definition string, which is a
        /// delimited list of elements included in the index
        /// with + or - separator between the elements.
        ///
        /// The separator acts as prefix indicating ascending (+)
        /// or descending (-) order of the element in the index.
        /// The separator is optional inf front of the first
        /// element in the index, if not specified the first
        /// element is sorted in ascending (+) order.
        ///
        /// Examples:
        ///
        /// * A is index on element A in ascending order;
        /// * -A is index on element A in descending order;
        /// * A+B-C is index on elements A and B in ascending
        ///   order and then element C in descending order.
        /// </summary>
        public IndexElementsAttribute(string definition)
        {
            Definition = definition;
        }

        /// <summary>
        /// Create from the index definition string, which is a
        /// delimited list of elements included in the index
        /// with + or - separator between the elements, and
        /// optional custom name for the index.
        ///
        /// By default, the delimited elements string (index
        /// definition) is used as index name. When the default
        /// name exceeds the maximum index name length, use
        /// this optional property to specify a shorter custom
        /// index name.
        ///
        /// The separator acts as prefix indicating ascending (+)
        /// or descending (-) order of the element in the index.
        /// The separator is optional inf front of the first
        /// element in the index, if not specified the first
        /// element is sorted in ascending (+) order.
        ///
        /// Examples:
        ///
        /// * A is index on element A in ascending order;
        /// * -A is index on element A in descending order;
        /// * A+B-C is index on elements A and B in ascending
        ///   order and then element C in descending order.
        /// </summary>
        public IndexElementsAttribute(string definition, string name)
        {
            Definition = definition;
            Name = name;
        }

        /// <summary>
        /// Delimited list of elements included in the index
        /// with + or - separator between the elements.
        ///
        /// The separator acts as prefix indicating ascending (+)
        /// or descending (-) order of the element in the index.
        /// The separator is optional inf front of the first
        /// element in the index, if not specified the first
        /// element is sorted in ascending (+) order.
        ///
        /// Examples:
        ///
        /// * A is index on element A in ascending order;
        /// * -A is index on element A in descending order;
        /// * A+B-C is index on elements A and B in ascending
        ///   order and then element C in descending order.
        /// </summary>
        public string Definition { get; set; }

        /// <summary>
        /// Name of the index (optional).
        ///
        /// By default, the delimited elements string (index
        /// definition) is used as index name. When the default
        /// name exceeds the maximum index name length, use
        /// this optional property to specify a shorter custom
        /// index name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Get a sorted dictionary of (definition, name) pairs
        /// for the inheritance chain of the specified type.
        /// </summary>
        public static SortedDictionary<string, string> GetIndexDict<TRecord>()
            where TRecord : Record
        {
            // The dictionary uses definition as key and name as value;
            // the name is the same as definition unless specified in
            // the attribute explicitly.
            var indexDict = new SortedDictionary<string, string>();

            // Create a list of index definition strings for the class,
            // including index definitions of its base classes, eliminating
            // duplicate definitions.
            Type classType = typeof(TRecord);
            while (classType != typeof(Record))
            {
                if (classType == null)
                    throw new Exception(
                        $"Data source cannot get collection for type {typeof(TRecord).Name} " +
                        $"because it is not derived from type Record.");

                // Get class attributes without inheritance
                var classAttributes = classType.GetCustomAttributes<IndexElementsAttribute>(false);
                foreach (var classAttribute in classAttributes)
                {
                    string definition = classAttribute.Definition;
                    string name = classAttribute.Name;

                    // Validate definition and specify default value for the name if null or empty
                    if (string.IsNullOrEmpty(definition))
                        throw new Exception("Empty index definition in IndexAttribute.");
                    if (string.IsNullOrEmpty(name)) name = definition;

                    // Remove + prefix from definition if specified
                    if (definition.StartsWith("+")) definition = definition.Substring(1, definition.Length - 1);

                    if (indexDict.TryGetValue(definition, out string existingName))
                    {
                        // Already included, check that the name matches, error message otherwise
                        if (name != existingName)
                            throw new Exception(
                                $"The same index definition {definition} is provided with two different " +
                                $"custom index names {name} and {existingName} in the inheritance chain " +
                                $"for class {typeof(TRecord).Name}.");
                    }
                    else
                    {
                        // Not yet included, add
                        indexDict.Add(definition, name);
                    }
                }

                // Continue to base type
                classType = classType.BaseType;
            }

            return indexDict;
        }

        /// <summary>
        /// Parse index definition to get an ordered list of
        /// (ElementName,SortOrder) tuples, where ElementName is
        /// name of the property for which the index is defined,
        /// and SortOrder is 1 for ascending, and -1 for descending.
        ///
        /// The parser will also validate that each element
        /// name exists in type TRecord.
        /// </summary>
        public static List<(string, int)> ParseIndexDefinition<TRecord>(string definition)
            where TRecord : Record
        {
            var result = new List<(string, int)>();

            // Determine sort order of the first element and remove the initial prefix
            int sortOrder = 1;
            if (definition.StartsWith("+"))
            {
                definition = definition.Substring(1, definition.Length - 1);
            }
            else if (definition.StartsWith("-"))
            {
                sortOrder = -1;
                definition = definition.Substring(1, definition.Length - 1);
            }

            int pos = 0;
            Type recordType = typeof(TRecord);
            while (true)
            {
                // Get next delimiter position
                int delimiterPos = definition.IndexOfAny(new[] {'+', '-'}, pos);

                // Element name is the characters from pos to delimiterPos-1 if the
                // next delimiter is found, and to the end of string otherwise
                string elementName = null;
                if (delimiterPos != -1) elementName = definition.Substring(pos, delimiterPos - pos);
                else elementName = definition.Substring(pos);

                // Validate element name
                if (elementName.Length == 0) throw new Exception($"Empty element name at position {pos} in index definition string {definition}.");

                // Check that element is present in TRecord as public property with both getter and setter
                var propertyInfo = recordType.GetProperty(elementName, BindingFlags.Public | BindingFlags.GetProperty | BindingFlags.SetProperty | BindingFlags.Instance);
                if (propertyInfo == null)
                    throw new Exception(
                        $"Public property {elementName} is not found in {recordType.Name}, or it is" +
                        $"not a public property with both getter and setter defined.");

                // Add element and its sort order to the result
                result.Add((elementName, sortOrder));

                // Exit if delimiter was not found, in this case the 
                // element we just added was the last element
                if (delimiterPos == -1) break;

                // Otherwise set sort order for the next element based on the delimiter
                char delimiter = definition[delimiterPos];
                if (delimiter == '+') sortOrder = 1;
                else if (delimiter == '-') sortOrder = -1;
                else throw new Exception($"Unknown delimiter {delimiter} in index definition string {definition}.");

                // Check that delimiter is not the last element in the string
                if (delimiterPos == definition.Length-1) throw new Exception($"Index definition string {definition} should not end with a delimiter.");

                // Advance to position after the delimiter
                pos = delimiterPos + 1;
            }

            return result;
        }
    }
}