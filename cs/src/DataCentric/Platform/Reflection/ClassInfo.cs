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

namespace DataCentric
{
    /// <summary>Provides the result of applying class map settings to a class.</summary>
    public class ClassInfo
    {
        [ThreadStatic] private static Dictionary<Type, ClassInfo> dict_; // Do not initialize ThreadStatic here, initializer will run in first thread only

        //--- PROPERTIES

        /// <summary>Type for which class info is provided.</summary>
        public Type Type { get; }

        /// <summary>Namespace before mapping.</summary>
        public string RawNamespace { get; }

        /// <summary>Namespace after mapping.</summary>
        public string MappedNamespace { get; }

        /// <summary>Class name without namespace before mapping.</summary>
        public string RawClassName { get; }

        /// <summary>Class name without namespace after mapping.</summary>
        public string MappedClassName { get; }

        /// <summary>Fully qualified class name before mapping.</summary>
        public string RawFullName { get; }

        /// <summary>Fully qualified class name after mapping.</summary>
        public string MappedFullName { get; }

        /// <summary>Returns fully qualified class name.</summary>
        public override string ToString() { return MappedFullName; }

        // STATIC

        /// <summary>
        /// Get cached instance for the specified object, or create
        /// using settings from ClassMapSettings
        /// and add to thread static cache if does not exist.
        ///
        /// This object contains information about the
        /// class including its name, namespace, etc.
        /// </summary>
        public static ClassInfo GetOrCreate(object value)
        {
            return GetOrCreate(value.GetType());
        }

        /// <summary>
        /// Get cached instance for the specified type, or create
        /// using settings from ClassMapSettings
        /// and add to thread static cache if does not exist.
        ///
        /// This object contains information about the
        /// class including its name, namespace, etc.
        /// </summary>
        public static ClassInfo GetOrCreate(Type type)
        {
            // Check if thread static dictionary is already initialized
            if (dict_ == null)
            {
                // If not, initialize
                dict_ = new Dictionary<Type, ClassInfo>();
            }

            // Check if a cached instance exists in dictionary
            if (dict_.TryGetValue(type, out ClassInfo result))
            {
                // Return if found
                return result;
            }
            else
            {
                // Otherwise create and add to dictionary
                result = new ClassInfo(type);
                dict_.Add(type, result);
                return result;
            }
        }

        /// <summary>
        /// Create using settings from ClassMapSettings.
        ///
        /// This constructor is private because it is only called
        /// from the GetOrCreate(...) method. Users should rely
        /// on GetOrCreate(...) method only which uses thread static
        /// cached value if any, and creates the instance only if
        /// it is not yet cached for the thread.
        /// </summary>
        private ClassInfo(Type type)
        {
            // Set type, raw full name, class name, and namespace
            Type = type;
            RawFullName = type.FullName;
            RawClassName = type.Name;
            RawNamespace = type.Namespace;

            // Remove ignored class name prefix
            MappedClassName = RawClassName;
            foreach (var ignoredTypeNamePrefix in ClassMapSettings.IgnoredClassNamePrefixes)
            {
                if (MappedClassName.StartsWith(ignoredTypeNamePrefix))
                {
                    MappedClassName = MappedClassName.Remove(0, ignoredTypeNamePrefix.Length);

                    // Break to prevent more than one prefix removed
                    break;
                }
            }

            // Remove ignored class name suffix
            foreach (var ignoredTypeNameSuffix in ClassMapSettings.IgnoredClassNameSuffixes)
            {
                if (MappedClassName.EndsWith(ignoredTypeNameSuffix))
                {
                    MappedClassName = MappedClassName.Substring(0, MappedClassName.Length - ignoredTypeNameSuffix.Length);

                    // Break to prevent more than one prefix removed
                    break;
                }
            }

            // Remove ignored namespace prefix
            MappedNamespace = RawNamespace;
            foreach (var ignoredModuleNamePrefix in ClassMapSettings.IgnoredNamespacePrefixes)
            {
                if (MappedNamespace.StartsWith(ignoredModuleNamePrefix))
                {
                    MappedNamespace = MappedNamespace.Remove(0, ignoredModuleNamePrefix.Length);

                    // Break to prevent more than one prefix removed
                    break;
                }
            }

            // Remove ignored namespace suffix
            foreach (var ignoredModuleNameSuffix in ClassMapSettings.IgnoredNamespaceSuffixes)
            {
                if (MappedNamespace.EndsWith(ignoredModuleNameSuffix))
                {
                    MappedNamespace = MappedNamespace.Substring(0, MappedNamespace.Length - ignoredModuleNameSuffix.Length);

                    // Break to prevent more than one prefix removed
                    break;
                }
            }

            // Create mapped full name by combining mapped namespace and mapped class name
            MappedFullName = string.Join(".", MappedNamespace, MappedClassName);
        }
    }
}
