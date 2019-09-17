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
using System.IO;
using System.Linq;
using System.Reflection;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using Newtonsoft.Json;

namespace DataCentric.Cli
{
    public static class CsvRecordsSerializer<T> where T : RecordBase
    {
        public static List<T> Deserialize(string input)
        {
            DefaultClassMap<T> mapping = CreateMapping();

            using (var reader = new StringReader(input))
            using (var csv = new CsvReader(reader))
            {
                csv.Configuration.RegisterClassMap(mapping);
                csv.Configuration.Delimiter = ",";
                return csv.GetRecords<T>().ToList();
            }
        }

        /// <summary>
        /// Create mapping rules for T.
        /// </summary>
        private static DefaultClassMap<T> CreateMapping()
        {
            DefaultClassMap<T> map = new DefaultClassMap<T>();
            Type type = typeof(T);

            // Get public properties with both getter and setter
            List<PropertyInfo> properties = type
                                           .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                                           .Where(property => property.GetGetMethod() != null && property.GetSetMethod() != null)
                                           .ToList();

            foreach (PropertyInfo property in properties)
            {
                Type propertyType = property.PropertyType;
                // Serialize data member to json
                if (propertyType.IsSubclassOf(typeof(Data)))
                {
                    Type jsonConverter = typeof(DataJsonConverter).MakeGenericType(propertyType);
                    ITypeConverter typeConverter = (ITypeConverter)Activator.CreateInstance(jsonConverter);
                    map.Map(type, property).TypeConverter(typeConverter);
                }
                // Serialize list member to json
                else if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(List<>))
                {
                    Type listArgument = propertyType.GetGenericArguments()[0];
                    Type listConverter = typeof(ListConverter).MakeGenericType(listArgument);
                    ITypeConverter typeConverter = (ITypeConverter)Activator.CreateInstance(listConverter);
                    map.Map(type, property).TypeConverter(typeConverter);
                }
                // Primitive types
                else
                {
                    map.Map(type, property);
                }
            }

            return map;
        }

        /// <summary>
        /// Use to map classes to json.
        /// </summary>
        private class DataJsonConverter : DefaultTypeConverter
        {
            public override object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
            {
                return JsonConvert.DeserializeObject<T>(text);
            }

            public override string ConvertToString(object value, IWriterRow row, MemberMapData memberMapData)
            {
                return JsonConvert.SerializeObject(value);
            }
        }

        /// <summary>
        /// Use to map lists to json
        /// </summary>
        private class ListConverter : DefaultTypeConverter
        {
            public override object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
            {
                return JsonConvert.DeserializeObject<List<T>>(text);
            }

            public override string ConvertToString(object value, IWriterRow row, MemberMapData memberMapData)
            {
                return JsonConvert.SerializeObject(value);
            }
        }
    }
}