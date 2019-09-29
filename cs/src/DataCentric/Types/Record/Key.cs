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
using System.Text;
using MongoDB.Bson;
using NodaTime;

namespace DataCentric
{
    /// <summary>
    /// Base class of a foreign key.
    ///
    /// The curiously recurring template pattern (CRTP) key class
    /// Key(TKey,TRecord) is derived from this class.
    ///
    /// Any elements of defined in the type specific key record
    /// become key tokens. Property Value and method ToString() of
    /// the key consists of key tokens with semicolon delimiter.
    /// </summary>
    public abstract class Key : Data
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
                var elementInfoArray = DataTypeInfo.GetOrCreate(GetType()).DataElements;
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
                    // Return the string after checking that it is not empty
                    // and that it does not itself contain semicolon delimiters
                    if (stringValue == String.Empty) throw new Exception(
                            $"String key element {elementInfo.Name} is empty." +
                            $"Empty elements are not permitted in key.");
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
                case Key keyValue:
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
        /// Populate key elements from semicolon delimited string.
        /// Elements that are themselves keys may use more than
        /// one token.
        ///
        /// If key AKey has two elements, B and C, where
        ///
        /// * B has type BKey which has two string elements, and
        /// * C has type string,
        ///
        /// the semicolon delimited key has the following format:
        ///
        /// BToken1;BToken2;CToken
        ///
        /// To avoid serialization format uncertainty, key elements
        /// can have any atomic type except Double.
        /// </summary>
        public void PopulateFrom(string value)
        {
            // Split key into tokens
            var tokens = value.Split(';');

            // Check that none of the tokens are empty
            foreach (string token in tokens)
            {
                if (string.IsNullOrEmpty(token))
                    throw new Exception($"Key {value} of key type {GetType().Name} contains an empty token.");
            }

            // Call the private method that uses array of tokens.
            // This method returns the number of tokens actually
            // used to parse the key.
            int tokenIndex = PopulateFrom(tokens, 0);

            // Verify that all tokens have been used, error message otherwise
            if (tokens.Length != tokenIndex)
            {
                throw new Exception($"Key with type {GetType().Name} requires {tokenIndex} tokens including " +
                                    $"any composite key elements, while key value {value} contains {tokens.Length} tokens.");
            }
        }

        //--- PRIVATE

        /// <summary>
        /// Populate key elements from an array of tokens starting
        /// at the specified token index. Elements that are themselves
        /// keys may use more than one token.
        ///
        /// This method returns the index of the first unused token.
        /// The returned value is the same as the length of the tokens
        /// array if all tokens are used.
        ///
        /// If key AKey has two elements, B and C, where
        ///
        /// * B has type BKey which has two string elements, and
        /// * C has type string,
        ///
        /// the semicolon delimited key has the following format:
        ///
        /// BToken1;BToken2;CToken
        ///
        /// To avoid serialization format uncertainty, key elements
        /// can have any atomic type except Double.
        /// </summary>
        private int PopulateFrom(string[] tokens, int tokenIndex)
        {
            // Get key elements using reflection
            var elementInfoArray = DataTypeInfo.GetOrCreate(this).DataElements;

            // Check that there are enough remaining tokens in the key for each key element
            if (tokens.Length - tokenIndex < elementInfoArray.Length)
            {
                StringBuilder value = new StringBuilder();
                for (int i = tokenIndex; i < tokens.Length; i++)
                {
                    value.Append(tokens[i]);
                    if (i > tokenIndex) value.Append(";");
                }
                throw new Exception(
                    $"Key of type {GetType().Name} requires at least {elementInfoArray.Length} elements " +
                    $"{String.Join(";", elementInfoArray.Select(p => p.Name).ToArray())} while there are " +
                    $"only {tokens.Length - tokenIndex} remaining key tokens: {value}.");
            }

            // Iterate over element info elements, advancing tokenIndex by the required
            // number of tokens for each element. In case of embedded keys, the value of
            // tokenIndex is advanced by the recursive call to InitFromTokens method
            // of the embedded key.
            foreach (var elementInfo in elementInfoArray)
            {
                // Get element type
                Type elementType = elementInfo.PropertyType;

                // Convert string token to value depending on elementType
                if (elementType == typeof(string))
                {
                    string token = tokens[tokenIndex++];
                    elementInfo.SetValue(this, token);
                }
                else if (elementType == typeof(double) || elementType == typeof(double?))
                {
                    throw new Exception(
                        $"Key element {elementInfo.Name} has type Double. Elements of this type " +
                        $"cannot be part of key due to serialization format uncertainty.");
                }
                else if (elementType == typeof(bool) || elementType == typeof(bool?))
                {
                    string token = tokens[tokenIndex++];
                    bool tokenValue = bool.Parse(token);
                    elementInfo.SetValue(this, tokenValue);
                }
                else if (elementType == typeof(int) || elementType == typeof(int?))
                {
                    string token = tokens[tokenIndex++];
                    int tokenValue = int.Parse(token);
                    elementInfo.SetValue(this, tokenValue);
                }
                else if (elementType == typeof(long) || elementType == typeof(long?))
                {
                    string token = tokens[tokenIndex++];
                    long tokenValue = long.Parse(token);
                    elementInfo.SetValue(this, tokenValue);
                }
                else if (elementType == typeof(LocalDate) || elementType == typeof(LocalDate?))
                {
                    // Inside the key, LocalDate is represented as readable int in
                    // non-delimited yyyymmdd format, not as delimited ISO string.
                    //
                    // First parse the string to int, then convert int to LocalDate.
                    string token = tokens[tokenIndex++];
                    if (!Int32.TryParse(token, out int isoInt))
                    {
                        throw new Exception(
                            $"Element {elementInfo.Name} of key type {GetType().Name} has type LocalDate and value {token} " +
                            $"that cannot be converted to readable int in non-delimited yyyymmdd format.");
                    }

                    LocalDate tokenValue = LocalDateUtils.ParseIsoInt(isoInt);
                    elementInfo.SetValue(this, tokenValue);
                }
                else if (elementType == typeof(LocalTime) || elementType == typeof(LocalTime?))
                {
                    // Inside the key, LocalTime is represented as readable int in
                    // non-delimited hhmmssfff format, not as delimited ISO string.
                    //
                    // First parse the string to int, then convert int to LocalTime.
                    string token = tokens[tokenIndex++];
                    if (!Int32.TryParse(token, out int isoInt))
                    {
                        throw new Exception(
                            $"Element {elementInfo.Name} of key type {GetType().Name} has type LocalTime and value {token} " +
                            $"that cannot be converted to readable int in non-delimited hhmmssfff format.");
                    }

                    LocalTime tokenValue = LocalTimeUtils.ParseIsoInt(isoInt);
                    elementInfo.SetValue(this, tokenValue);
                }
                else if (elementType == typeof(LocalMinute) || elementType == typeof(LocalMinute?))
                {
                    // Inside the key, LocalMinute is represented as readable int in
                    // non-delimited hhmm format, not as delimited ISO string.
                    //
                    // First parse the string to int, then convert int to LocalTime.
                    string token = tokens[tokenIndex++];
                    if (!Int32.TryParse(token, out int isoInt))
                    {
                        throw new Exception(
                            $"Element {elementInfo.Name} of key type {GetType().Name} has type LocalMinute and value {token} " +
                            $"that cannot be converted to readable int in non-delimited hhmm format.");
                    }

                    LocalMinute tokenValue = LocalMinuteUtils.ParseIsoInt(isoInt);
                    elementInfo.SetValue(this, tokenValue);
                }
                else if (elementType == typeof(LocalDateTime) || elementType == typeof(LocalDateTime?))
                {
                    // Inside the key, LocalDateTime is represented as readable long in
                    // non-delimited yyyymmddhhmmssfff format, not as delimited ISO string.
                    //
                    // First parse the string to long, then convert int to LocalDateTime.
                    string token = tokens[tokenIndex++];
                    if (!Int64.TryParse(token, out long isoLong))
                    {
                        throw new Exception(
                            $"Element {elementInfo.Name} of key type {GetType().Name} has type LocalDateTime and value {token} " +
                            $"that cannot be converted to readable long in non-delimited yyyymmddhhmmssfff format.");
                    }

                    LocalDateTime tokenValue = LocalDateTimeUtils.ParseIsoLong(isoLong);
                    elementInfo.SetValue(this, tokenValue);
                }
                else if (elementType == typeof(ObjectId) || elementType == typeof(ObjectId?))
                {
                    string token = tokens[tokenIndex++];
                    ObjectId tokenValue = ObjectId.Parse(token);
                    elementInfo.SetValue(this, tokenValue);
                }
                else if (elementType.BaseType == typeof(Enum)) // TODO Support nullable Enum in key
                {
                    string token = tokens[tokenIndex++];
                    object tokenValue = Enum.Parse(elementType, token);
                    elementInfo.SetValue(this, tokenValue);
                }
                else if (typeof(Key).IsAssignableFrom(elementType))
                {
                    Key keyElement = (Key)Activator.CreateInstance(elementType);
                    tokenIndex = keyElement.PopulateFrom(tokens, tokenIndex);
                    elementInfo.SetValue(this, keyElement);
                }
                else
                {
                    // Field type is unsupported for a key, error message
                    throw new Exception(
                        $"Element {elementInfo.Name} of key type {GetType().Name} has type {elementType} that " +
                        $"is not one of the supported key element types. Available key element types are " +
                        $"string, bool, int, long, LocalDate, LocalTime, LocalMinute, LocalDateTime, Enum, or Key.");
                }
            }

            return tokenIndex;
        }
    }
}
