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
using System.Collections;
using NodaTime;

namespace DataCentric
{
    /// <summary>
    /// Abstract base class to data structures.
    /// </summary>
    public abstract class Data : ITreeSerializable, ITreeDeserializable
    {
        /// <summary>Serialize by writing into ITreeWriter.</summary>
        public void SerializeTo(ITreeWriter writer)
        {
            // Write start tag
            writer.WriteStartDict();

            // Iterate over the list of elements
            var innerElementInfoList = DataTypeInfo.GetOrCreate(GetType()).DataElements;
            foreach (var innerElementInfo in innerElementInfoList)
            {
                // Get element name and value
                string innerElementName = innerElementInfo.Name;
                object innterElementValue = innerElementInfo.GetValue(this);

                // Serialize based on type of element value
                switch (innterElementValue)
                {
                    case null:
                        // Do not serialize null value
                        break;
                    case string stringValue:
                    case double doubleValue:
                    case bool boolValue:
                    case int intValue:
                    case long longValue:
                    case LocalDate dateValue:
                    case LocalTime timeValue:
                    case LocalMinute minuteValue:
                    case LocalDateTime dateTimeValue:
                    case Enum enumValue:
                        // Embedded as string value
                        writer.WriteValueElement(innerElementName, innterElementValue);
                        break;
                    case IEnumerable enumerableElement:
                        // Embedded enumerable such as array or list
                        enumerableElement.SerializeTo(innerElementName, writer);
                        break;
                    case Data dataElement:
                        if (dataElement is Key)
                        {
                            // Embedded as string key
                            writer.WriteValueElement(innerElementName, innterElementValue);
                            break;
                        }
                        else
                        {
                            // Embedded as data
                            writer.WriteStartElement(innerElementName);
                            dataElement.SerializeTo(writer);
                            writer.WriteEndElement(innerElementName);
                        }
                        break;
                    case RecordId idElement:
                        // Do not serialize
                        break;
                    default:
                        // Argument type is unsupported, error message
                        throw new Exception($"Element type {innerElementInfo.PropertyType} is not supported for tree serialization.");
                }
            }

            // Write end tag
            writer.WriteEndDict();
        }

        /// <summary>Deserialize from data in ITreeReader.</summary>
        public void DeserializeFrom(ITreeReader reader)
        {
            // Do nothing if the selected XML node is empty
            if (reader == null) return;

            // Iterate over the list of elements
            var elementInfoList = DataTypeInfo.GetOrCreate(GetType()).DataElements;
            foreach (var elementInfo in elementInfoList)
            {
                // Get element name and value
                string elementName = elementInfo.Name;
                Type elementType = elementInfo.PropertyType;

                // First check for each of the supported value types
                if (elementType == typeof(string))
                {
                    ITreeReader innerXmlNode = reader.ReadElement(elementName);
                    string token = innerXmlNode.ReadValue();
                    elementInfo.SetValue(this, token);
                }
                else if (elementType == typeof(double) || elementType == typeof(double?))
                {
                    ITreeReader innerXmlNode = reader.ReadElement(elementName);
                    string token = innerXmlNode.ReadValue();
                    var value = double.Parse(token);
                    elementInfo.SetValue(this, value);
                }
                else if (elementType == typeof(bool) || elementType == typeof(bool?))
                {
                    ITreeReader innerXmlNode = reader.ReadElement(elementName);
                    string token = innerXmlNode.ReadValue();
                    var value = bool.Parse(token);
                    elementInfo.SetValue(this, value);
                }
                else if (elementType == typeof(int) || elementType == typeof(int?))
                {
                    ITreeReader innerXmlNode = reader.ReadElement(elementName);
                    string token = innerXmlNode.ReadValue();
                    var value = int.Parse(token);
                    elementInfo.SetValue(this, value);
                }
                else if (elementType == typeof(long) || elementType == typeof(long?))
                {
                    ITreeReader innerXmlNode = reader.ReadElement(elementName);
                    string token = innerXmlNode.ReadValue();
                    var value = long.Parse(token);
                    elementInfo.SetValue(this, value);
                }
                else if (elementType == typeof(LocalDate) || elementType == typeof(LocalDate?))
                {
                    ITreeReader innerXmlNode = reader.ReadElement(elementName);
                    string token = innerXmlNode.ReadValue();
                    var value = LocalDateImpl.Parse(token);
                    elementInfo.SetValue(this, value);
                }
                else if (elementType == typeof(LocalTime) || elementType == typeof(LocalTime?))
                {
                    ITreeReader innerXmlNode = reader.ReadElement(elementName);
                    string token = innerXmlNode.ReadValue();
                    var value = LocalTimeImpl.Parse(token);
                    elementInfo.SetValue(this, value);
                }
                else if (elementType == typeof(LocalMinute) || elementType == typeof(LocalMinute?))
                {
                    ITreeReader innerXmlNode = reader.ReadElement(elementName);
                    string token = innerXmlNode.ReadValue();
                    var value = LocalMinuteImpl.Parse(token);
                    elementInfo.SetValue(this, value);
                }
                else if (elementType == typeof(LocalDateTime) || elementType == typeof(LocalDateTime?))
                {
                    ITreeReader innerXmlNode = reader.ReadElement(elementName);
                    string token = innerXmlNode.ReadValue();
                    var value = LocalDateTimeImpl.Parse(token);
                    elementInfo.SetValue(this, value);
                }
                else if (elementType.IsSubclassOf(typeof(Enum)))
                {
                    ITreeReader innerXmlNode = reader.ReadElement(elementName);
                    string token = innerXmlNode.ReadValue();
                    var value = Enum.Parse(elementType, token);
                    elementInfo.SetValue(this, value);
                }
                else
                {
                    // If none of the supported atomic types match, use the activator
                    // to create and empty instance of a complex type and populate it
                    var element = Activator.CreateInstance(elementType);
                    switch (element)
                    {
                        case IList listElement:
                            listElement.DeserializeFrom(elementName, reader);
                            break;
                        case Data dataElement:
                            var keyElement = dataElement as Key;
                            if (keyElement != null)
                            {
                                // Deserialize key from value node containing semicolon delimited string
                                ITreeReader keyNodeValue = reader.ReadElement(elementName);
                                string token = keyNodeValue.ReadValue();
                                // Parse semicolon delimited string to populate key elements
                                keyElement.PopulateFrom(token);
                            }
                            else
                            {
                                // Deserialize embedded data object from the list of nodes
                                ITreeReader dataNodes = reader.ReadElement(elementName);
                                dataElement.DeserializeFrom(dataNodes);
                            }
                            break;
                        case RecordId idElement:
                            // Do not serialize
                            break;
                        default:
                            // Error message if the type does not match any of the value or reference types
                            throw new Exception($"Serialization is not supported for type {elementType}.");
                    }

                    // Assign the populated key to the property
                    elementInfo.SetValue(this, element);
                }
            }
        }
    }
}
