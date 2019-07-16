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
using MongoDB.Bson;
using NodaTime;

namespace DataCentric
{
    /// <summary>
    /// Base class of a foreign key.
    ///
    /// The curiously recurring template pattern (CRTP) key class
    /// KeyFor(TKey,TRecord) is derived from this class.
    /// 
    /// Any elements of defined in the type specific key record
    /// become key tokens. Property Value and method ToString() of
    /// the key consists of key tokens with semicolon delimiter.
    /// </summary>
    public abstract class KeyType : DataType
    {
        /// <summary>
        /// String key consists of semicolon delimited primary key elements:
        ///
        /// KeyElement1;KeyElement2
        ///
        /// To avoid serialization format uncertainty, key elements
        /// can have any atomic type except Double.
        /// </summary>
        public string Value
        {
            get
            {
                var tokens = new List<string>();
                var elementInfoArray = DataInfo.GetOrCreate(GetType()).RootElements;
                foreach (var elementInfo in elementInfoArray)
                {
                    // Convert key element to string key token.
                    // 
                    // Note that string representation of certain types inside the key
                    // is not the same as what is returned by AsString().
                    //
                    // Specifically, LocalDate and LocalTime are represented as tokens
                    // in readable int format without delimiters (yyyymmdd and hhmmssfff)
                    // but use delimited ISO format (yyyy-mm-dd and hh:mm:ss.fff) when
                    // serialized using AsString().
                    var token = GetKeyToken(this, elementInfo);
                    tokens.Add(token);
                }

                string result = string.Join(";", tokens);
                return result;
            }
        }

        /// <summary>
        /// Convert key element to string key token.
        /// 
        /// Note that string representation of certain types inside the key
        /// is not the same as what is returned by AsString().
        ///
        /// Specifically, LocalDate and LocalTime are represented as tokens
        /// in readable int format without delimiters (yyyymmdd and hhmmssfff)
        /// but use delimited ISO format (yyyy-mm-dd and hh:mm:ss.fff) when
        /// serialized using AsString().
        /// </summary>
        internal static string GetKeyToken(object obj, PropertyInfo elementInfo)
        {
            string result;
            object element = elementInfo.GetValue(obj);
            switch (element)
            {
                case null:
                    throw new Exception($"Key element {elementInfo.Name} of type {obj.GetType().Name} is null. " +
                                        $"Null elements are not permitted in key.");
                case string stringValue:
                    // Return the string after checking that it does not contain semicolon delimiters
                    if (stringValue.Contains(";")) throw new Exception(
                        $"Key element {elementInfo.Name} of type {obj.GetType().Name} includes semicolon delimiter. " +
                        $"The use of this delimiter is reserved for separating key tokens.");
                    result = stringValue;
                    break;
                case double doubleValue:
                    // Double key elements are not permitted due to the risk of 
                    // key mismatch as a result of different serialization settings
                    // across multiple libraries and programming languages
                    throw new Exception(
                        $"Key element {elementInfo.Name} of type {obj.GetType().Name} has type Double. Elements " +
                        $"of this type cannot be part of key due to serialization format uncertainty.");
                case LocalDate dateValue:
                    // Serialize as readable int in yyyymmdd format, not in delimited ISO format
                    result = dateValue.ToIsoInt().ToString();
                    break;
                case LocalTime timeValue:
                    // Serialize as readable int in hhmmssfff format, not in delimited ISO format
                    result = timeValue.ToIsoInt().ToString();
                    break;
                case LocalMinute minuteValue:
                    // Serialize as readable int in hhmm format, not in delimited ISO format
                    result = minuteValue.ToIsoInt().ToString();
                    break;
                case LocalDateTime dateTimeValue:
                    // Serialize as readable long in yyyymmddhhmmssfff format, not in delimited ISO format
                    result = dateTimeValue.ToIsoLong().ToString();
                    break;
                case bool boolValue:
                case int intValue:
                case long longValue:
                case ObjectId objectIdValue:
                case Enum enumValue:
                case KeyType keyValue:
                    // Use AsString() for all remaining types including the key
                    //
                    // A token representing another key can contain a semicolon delimiter
                    // if the key being converted to the token is composite (has more than
                    // one key element). However a string key element cannot contain the
                    // semicolon delimiter.
                    result = element.AsString();
                    break;
                default:
                    // Argument type is unsupported, error message
                    throw new Exception(
                        $"Key element {elementInfo.Name} of type {obj.GetType().Name} has type {element.GetType()}" +
                        $"that is not one of the supported key element types. Available key element types are " +
                        $"string, double, bool, int, long, LocalDate, LocalTime, LocalMinute, LocalDateTime, LocalMinute, ObjectId, or Enum.");
            }

            return result;
        }

        /// <summary>
        /// String key consists of semicolon delimited primary key elements:
        ///
        /// KeyElement1;KeyElement2
        ///
        /// To avoid serialization format uncertainty, key elements
        /// can have any atomic type except Double.
        /// </summary>
        public override string ToString() { return Value; }

        /// <summary>
        /// Populate primary key elements by parsing semicolon delimited string:
        ///
        /// KeyElement1;KeyElement2
        ///
        /// To avoid serialization format uncertainty, key elements
        /// can have any atomic type except Double.
        /// </summary>
        public void AssignString(string value)
        {
            // Split key into tokens
            var tokens = value.Split(';');

            // Check that the number of tokens matches the number of key elements
            var elementInfoArray = DataInfo.GetOrCreate(this).RootElements;
            if (tokens.Length != elementInfoArray.Length)
            {
                throw new Exception(
                    $"Key {value} consists of {tokens.Length} tokens while key of type {GetType().Name} " +
                    $"should have {elementInfoArray.Length} elements: " +
                    $"{String.Join(";",elementInfoArray.Select(p => p.Name).ToArray())}.");
            }

            int tokenIndex = 0;
            foreach (var elementInfo in elementInfoArray)
            {
                // Get field type
                object element = elementInfo.GetValue(this);
                Type elementType = elementInfo.PropertyType;

                // Get token and check that it is not empty
                string token = tokens[tokenIndex++];
                if (string.IsNullOrEmpty(token)) throw new Exception($"Key {value} of key type {GetType().Name} contains an empty token.");

                // Convert string token to value depending on fieldType
                object tokenValue = null;
                if (elementType == typeof(string))
                {
                    tokenValue = token;
                }
                else if (elementType == typeof(double) || elementType == typeof(double?))
                {
                    throw new Exception(
                        $"Key element {elementInfo.Name} has type Double. Elements of this type " +
                        $"cannot be part of key due to serialization format uncertainty.");
                }
                else if (elementType == typeof(bool) || elementType == typeof(bool?))
                {
                    tokenValue = bool.Parse(token);
                }
                else if (elementType == typeof(int) || elementType == typeof(int?))
                {
                    tokenValue = int.Parse(token);
                }
                else if (elementType == typeof(long) || elementType == typeof(long?))
                {
                    tokenValue = long.Parse(token);
                }
                else if (elementType == typeof(LocalDate) || elementType == typeof(LocalDate?))
                {
                    // Inside the key, LocalDate is represented as readable int in
                    // non-delimited yyyymmdd format, not as delimited ISO string.
                    //
                    // First parse the string to int, then convert int to LocalDate.
                    if (!Int32.TryParse(token, out int isoInt))
                    {
                        throw new Exception(
                            $"Element {elementInfo.Name} of key type {GetType().Name} has type LocalDate and value {token} " + 
                            $"that cannot be converted to readable int in non-delimited yyyymmdd format.");
                    }

                    tokenValue = LocalDateUtils.ParseIsoInt(isoInt);
                }
                else if (elementType == typeof(LocalTime) || elementType == typeof(LocalTime?))
                {
                    // Inside the key, LocalTime is represented as readable int in
                    // non-delimited hhmmssfff format, not as delimited ISO string.
                    //
                    // First parse the string to int, then convert int to LocalTime.
                    if (!Int32.TryParse(token, out int isoInt))
                    {
                        throw new Exception(
                            $"Element {elementInfo.Name} of key type {GetType().Name} has type LocalTime and value {token} " +
                            $"that cannot be converted to readable int in non-delimited hhmmssfff format.");
                    }

                    tokenValue = LocalTimeUtils.ParseIsoInt(isoInt);
                }
                else if (elementType == typeof(LocalMinute) || elementType == typeof(LocalMinute?))
                {
                    // Inside the key, LocalMinute is represented as readable int in
                    // non-delimited hhmm format, not as delimited ISO string.
                    //
                    // First parse the string to int, then convert int to LocalTime.
                    if (!Int32.TryParse(token, out int isoInt))
                    {
                        throw new Exception(
                            $"Element {elementInfo.Name} of key type {GetType().Name} has type LocalMinute and value {token} " +
                            $"that cannot be converted to readable int in non-delimited hhmm format.");
                    }

                    tokenValue = LocalMinuteUtils.ParseIsoInt(isoInt);
                }
                else if (elementType == typeof(LocalDateTime) || elementType == typeof(LocalDateTime?))
                {
                    // Inside the key, LocalDateTime is represented as readable long in
                    // non-delimited yyyymmddhhmmssfff format, not as delimited ISO string.
                    //
                    // First parse the string to long, then convert int to LocalDateTime.
                    if (!Int64.TryParse(token, out long isoLong))
                    {
                        throw new Exception(
                            $"Element {elementInfo.Name} of key type {GetType().Name} has type LocalDateTime and value {token} " +
                            $"that cannot be converted to readable long in non-delimited yyyymmddhhmmssfff format.");
                    }

                    tokenValue = LocalDateTimeUtils.ParseIsoLong(isoLong);
                }
                else if (elementType == typeof(ObjectId) || elementType == typeof(ObjectId?))
                {
                    tokenValue = ObjectId.Parse(token);
                }
                else if (elementType == typeof(Enum)) // TODO Support nullable Enum in key
                {
                    tokenValue = Enum.Parse(elementType, token);
                }
                else
                {
                    // Field type is unsupported for a key, error message
                    throw new Exception(
                        $"Element {elementInfo.Name} of key type {GetType().Name} has type {element.GetType()} that " +
                        $"is not one of the supported key element types. Available key element types are " +
                        $"string, bool, int, long, LocalDate, LocalTime, LocalMinute, LocalDateTime, or Enum.");
                }

                elementInfo.SetValue(this, tokenValue);
            }
        }
    }

    /// <summary>Extension methods for Key.</summary>
    public static class KeyEx
    {
        /// <summary>Deserialize record from XML using short
        /// class name without namespace for the root XML element.</summary>
        public static void ParseXml(this KeyType obj, string xmlString)
        {
            IXmlReader reader = new XmlReader(xmlString);

            // Root node of serialized XML must be the same as mapped class name without namespace
            var mappedFullName = ClassInfo.GetOrCreate(obj).MappedClassName;
            ITreeReader recordNodes = reader.ReadElement(mappedFullName);

            // Deserialize from XML nodes inside the root node
            obj.DeserializeFrom(recordNodes);
        }

        /// <summary>Serialize record to XML using short
        /// class name without namespace for the root XML element.</summary>
        public static string ToXml(this KeyType obj)
        {
            // Get root XML element name using mapped final type of the object
            string rootName = ClassInfo.GetOrCreate(obj).MappedClassName;

            // Serialize to XML
            ITreeWriter writer = new XmlWriter();
            writer.WriteStartDocument(rootName);
            obj.SerializeTo(writer);
            writer.WriteEndDocument(rootName);

            // Convert to string
            string result = writer.ToString();
            return result;
        }
    }
}
