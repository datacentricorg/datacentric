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
    /// Helper class for the attributes.
    /// </summary>
    public static class AttributeUtil
    {
        /// <summary>
        /// Parse comma separated list of elements, for example definition
        /// string A, B becomes the list [A, B].
        ///
        /// The parser will check that each element exists in type TRecord
        /// or its base types, error message otherwise.
        /// </summary>
        public static List<string> ParseCommaSeparatedElements<TRecord>(string definition)
            where TRecord : Record
        {
            var result = new List<string>();

            // Get record type to be used in element name validation
            Type recordType = typeof(TRecord);

            // Trim leading and trailing whitespace from the definition
            definition = definition.Trim();

            if (string.IsNullOrEmpty(definition))
            {
                // Empty elements definition string is parsed to an empty list
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