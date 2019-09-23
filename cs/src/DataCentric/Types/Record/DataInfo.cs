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
using System.Linq;
using System.Net.Http.Headers;

namespace DataCentric
{
    /// <summary>Information about a data type obtained through reflection.</summary>
    public class DataInfo
    {
        [ThreadStatic] private static Dictionary<Type, DataInfo> dict_; // Do not initialize ThreadStatic here, initializer will run in first thread only

        //--- PROPERTIES

        /// <summary>
        /// Type of the class at the root of the inheritance chain, one
        /// level above Data, Key(TRecord), or Record(TKey).
        /// </summary>
        public Type RootType { get; }

        /// <summary>Kind of the root type (record, key, or element).</summary>
        public RootKind RootKind { get; }

        /// <summary>
        /// Inheritance chain from derived to base, ending
        /// with and inclusive of the RootType. This property
        /// is used to generate the value of _t BSON attribute.
        ///
        /// Root data type is the type that inherits directly
        /// from Key(TKey, TRecord).
        /// </summary>
        public string[] InheritanceChain { get; }

        /// <summary>
        /// Array of property info for all of the type's elements
        /// in the order of declaration.
        ///
        /// The elements are listed in the order of declaration
        /// within the class, and in the order from base to derived
        /// between the classes.
        /// </summary>
        public PropertyInfo[] DataElements { get; }

        /// <summary>
        /// Array of property info for the elements of the root
        /// data type in the order of declaration, including key
        /// elements and optionally also non-key elements.
        ///
        /// Root data type is the type that inherits directly
        /// from Key(TKey, TRecord).
        /// </summary>
        public PropertyInfo[] RootElements { get; }

        /// <summary>
        /// Dictionary of property info for the elements of the
        /// root data type indexed by element name, including key
        /// elements and optionally also non-key elements.
        ///
        /// Root data type is the type that inherits directly
        /// from Key(TKey, TRecord).
        /// </summary>
        public Dictionary<string, PropertyInfo> RootElementDict { get; }

        /// <summary>
        /// Get cached instance for the specified object, or create
        /// using  and add to thread static cache if does not exist.
        ///
        /// This object contains information about the data type
        /// including the list of its elements (public properties
        /// that have one of the supported data types).
        /// </summary>
        public static DataInfo GetOrCreate(object value)
        {
            return GetOrCreate(value.GetType());
        }

        /// <summary>
        /// Get cached instance for the specified type, or create
        /// using  and add to thread static cache if does not exist.
        ///
        /// This object contains information about the data type
        /// including the list of its elements (public properties
        /// that have one of the supported data types).
        /// </summary>
        public static DataInfo GetOrCreate(Type type)
        {
            // Check if thread static dictionary is already initialized
            if (dict_ == null)
            {
                // If not, initialize
                dict_ = new Dictionary<Type, DataInfo>();
            }

            // Check if a cached instance exists in dictionary
            if (dict_.TryGetValue(type, out DataInfo result))
            {
                // Return if found
                return result;
            }
            else
            {
                // Otherwise create and add to dictionary
                result = new DataInfo(type);
                dict_.Add(type, result);
                return result;
            }
        }

        /// <summary>
        /// Create from type.
        ///
        /// This constructor is private because it is only called
        /// from the GetOrCreate(...) method. Users should rely
        /// on GetOrCreate(...) method only which uses thread static
        /// cached value if any, and creates the instance only if
        /// it is not yet cached for the thread.
        /// </summary>
        private DataInfo(Type type)
        {
            // Populate the inheritance chain from parent to base,
            // stop when one of the base classes is reached or
            // there is no base class
            List<Type> inheritanceChain = new List<Type>();
            Type currentType = type;
            while (currentType.BaseType != null)
            {
                // Add type to the inheritance chain
                inheritanceChain.Add(currentType);

                string baseClassName = currentType.BaseType.Name;
                if (baseClassName == "Data")
                {
                    RootKind = RootKind.Element;
                    RootType = currentType;
                    break;
                }
                else if (baseClassName == "TypedKey`2"
                         || baseClassName == "RootKey`2"
                         || baseClassName == "Key")
                {
                    RootKind = RootKind.Key;
                    RootType = currentType;

                    if (inheritanceChain.Count > 1)
                        throw new Exception(
                            $"Key type {type.Name} is not derived directly from Key(TRecord). " +
                            $"Key classes cannot have an inheritance hierarchy, only data classes can.");

                    break;
                }
                else if (baseClassName == "TypedRecord`2"
                         || baseClassName == "RootRecord`2"
                         || baseClassName == "Record")
                {
                    RootKind = RootKind.Record;
                    RootType = currentType;
                    break;
                }

                currentType = currentType.BaseType;
            }

            // Error message if the type is not derived from one of the permitted base classes 
            if (RootKind == RootKind.Empty)
                throw new Exception(
                    $"Data type {type.Name} is not derived from Data, Key<TKey, TRecord>, or Record<TKey, TRecord>.");

            // Add elements in the order from from base to derived
            var rootElementList = new List<PropertyInfo>();
            var dataElementList = new List<PropertyInfo>();
            for (int i = inheritanceChain.Count - 1; i >= 0; --i)
            {
                Type inheritanceChainEntry = inheritanceChain[i];

                // Find all properties of this type that are public, not static,
                // declared in this type only, and have both getter and setter
                var propInfoArray = inheritanceChainEntry.GetProperties(
                        BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                    .Where(p => (p.CanRead && p.CanWrite));

                foreach (var propInfo in propInfoArray)
                {
                    // This line detects if the property is an override and skips it so we avoid
                    // duplicate definitions for the original declaration and the override
                    var propGetter = propInfo.GetMethod;
                    var propGetterBaseDefinition = propGetter.GetBaseDefinition();
                    if (!propGetter.Equals(propGetterBaseDefinition))
                    {
                        // This is an override, skip unless defined in types below root
                        var declaringTypeName = propGetterBaseDefinition.DeclaringType.Name;
                        if (declaringTypeName != "Data"
                            && declaringTypeName != "TypedKey`2"
                            && declaringTypeName != "RootKey`2"
                            && declaringTypeName != "Key"
                            && declaringTypeName != "TypedRecord`2"
                            && declaringTypeName != "RootRecord`2"
                            && declaringTypeName != "Record")
                        {
                            continue;
                        }
                    }

                    // DataElements has properties from all classes in the inheritance
                    // chain in the order of declaration, from base to derived
                    dataElementList.Add(propInfo);

                    // RootElements has properties only from the class that inherits
                    // directly from Key(TKey, TRecord)
                    if (i == inheritanceChain.Count - 1)
                    {
                        rootElementList.Add(propInfo);
                    }
                }
            }

            // Populate inheritance chain property with
            // names of types in the inheritance chain
            InheritanceChain = inheritanceChain.Select(p => p.Name).ToArray();

            // Populate root elements and data elements
            RootElements = rootElementList.ToArray();
            DataElements = dataElementList.ToArray();

            // Populate root element dictionary
            RootElementDict = new Dictionary<string, PropertyInfo>();
            foreach (var propertyInfo in RootElements)
            {
                RootElementDict.Add(propertyInfo.Name, propertyInfo);
            }
        }
    }
}
