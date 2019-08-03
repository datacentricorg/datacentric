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
using System.Xml;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace DataCentric
{
    /// <summary>Serializes record as BSON document.</summary>
    public class BsonRecordSerializer<TRecord> : SerializerBase<TRecord> where TRecord : KeyBase, new()
    {
        /// <summary>Null value is handled via [BsonIgnoreIfNull] attribute and is not expected here.</summary>
        public override TRecord Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            // Create instance to which BSON will be deserialized
            TRecord result = new TRecord();
            ITreeWriter writer = new DataWriter((Data) result);

            // Checks that type matches
            IBsonReader reader = context.Reader;
            reader.ReadStartDocument();
            string mappedBsonClassName = "DerivedCollectionType"; //context.Reader.FindStringElement("-t");
            writer.WriteStartDocument(mappedBsonClassName);
            DeserializeDocument(reader, writer);
            reader.ReadEndDocument();
            writer.WriteEndDocument(mappedBsonClassName);
            return result;
        }

        /// <summary>Null value is handled via [BsonIgnoreIfNull] attribute and is not expected here.</summary>
        public static void DeserializeDocument(IBsonReader reader, ITreeWriter writer)
        {
            // Each document is a dictionary at root level
            writer.WriteStartDict();

            // Loop over elements until end of file
            // Normally exit from loop occurs in break statement below
            while (!reader.IsAtEndOfFile())
            {
                var bsonType = reader.ReadBsonType();
                if (bsonType == BsonType.EndOfDocument)
                {
                    // Stop reading elements if type indicates end of document
                    // This is where the loop over elements normally ends
                    break;
                }

                // Read element name and value
                string elementName = reader.ReadName();
                if (bsonType == BsonType.Null)
                {
                    reader.ReadNull();
                }
                else if (bsonType == BsonType.ObjectId)
                {
                    ObjectId value = reader.ReadObjectId();
                    // TODO - handle ID and DataSet fields
                    // writer.WriteValue(elementName, value);
                }
                else if (bsonType == BsonType.String)
                {
                    string value = reader.ReadString();

                    if (elementName == "_t")
                    {
                        // TODO Handle assName
                    }
                    else if (elementName == "Key")
                    {
                        // TODO Handle Key
                    }
                    else
                    {
                        writer.WriteValueElement(elementName, value);
                    }
                }
                else if (bsonType == BsonType.Double)
                {
                    double value = reader.ReadDouble();
                    writer.WriteValueElement(elementName, value);
                }
                else if (bsonType == BsonType.Boolean)
                {
                    bool value = reader.ReadBoolean();
                    writer.WriteValueElement(elementName, value);
                }
                else if (bsonType == BsonType.Int32)
                {
                    int value = reader.ReadInt32();
                    writer.WriteValueElement(elementName, value);
                }
                else if (bsonType == BsonType.Int64)
                {
                    long value = reader.ReadInt64();
                    writer.WriteValueElement(elementName, value);
                }
                else if (bsonType == BsonType.Document)
                {
                    // Read BSON stream for the embedded data element
                    IByteBuffer documentBuffer = reader.ReadRawBsonDocument();
                    IBsonReader documentReader = new BsonBinaryReader(new ByteBufferStream(documentBuffer));

                    // Deserialize embedded data element
                    documentReader.ReadStartDocument();
                    writer.WriteStartElement(elementName);
                    DeserializeDocument(documentReader, writer);
                    documentReader.ReadEndDocument();
                    writer.WriteEndElement(elementName);
                }
                else if (bsonType == BsonType.Array)
                {
                    // Array is accessed as a document BSON type inside array BSON,
                    // type, where document element name is serialized array index.
                    // Deserialization of sparse arrays is currently not supported.

                    // Array reader
                    IByteBuffer arrayBuffer = reader.ReadRawBsonArray();
                    IBsonReader arrayReader = new BsonBinaryReader(new ByteBufferStream(arrayBuffer));

                    // Document reader inside array reader
                    IByteBuffer arrayDocumentBuffer = arrayReader.ReadRawBsonDocument();
                    IBsonReader arrayDocumentReader = new BsonBinaryReader(new ByteBufferStream(arrayDocumentBuffer));

                    // We can finally deserialize array here
                    // This method checks that array is not sparse
                    arrayDocumentReader.ReadStartDocument();
                    writer.WriteStartArrayElement(elementName);
                    DeserializeArray(arrayDocumentReader, writer);
                    writer.WriteEndArrayElement(elementName);
                    arrayDocumentReader.ReadEndDocument();
                }
                else throw new Exception(
                    $"Deserialization of BSON type {bsonType} is not supported.");
            }

            // Each document is a dictionary at root level
            writer.WriteEndDict();
        }

        /// <summary>Null value is handled via [BsonIgnoreIfNull] attribute and is not expected here.</summary>
        public static void DeserializeArray(IBsonReader reader, ITreeWriter writer)
        {
            // Loop over elements until end of file
            // Normally exit from loop occurs in break statement below
            int arrayIndex = 0;
            while (!reader.IsAtEndOfFile())
            {
                var bsonType = reader.ReadBsonType();
                if (bsonType == BsonType.EndOfDocument)
                {
                    // Stop reading elements if type indicates end of document
                    // This is where the loop over elements normally ends
                    break;
                }

                string elementName = reader.ReadName();
                int elementIndex = int.Parse(elementName);
                if (elementIndex != arrayIndex++) throw new Exception(
                    "Deserialization of BSON sparse arrays is not supported.");

                if (bsonType == BsonType.Null)
                {
                    // Unlike for dictionaries, in case of arrays we write null item values
                    reader.ReadNull();
                    writer.WriteValueArrayItem(null);
                }
                else if (bsonType == BsonType.String)
                {
                    string value = reader.ReadString();
                    writer.WriteValueArrayItem(value);
                }
                else if (bsonType == BsonType.Double)
                {
                    double value = reader.ReadDouble();
                    writer.WriteValueArrayItem(value);
                }
                else if (bsonType == BsonType.Boolean)
                {
                    bool value = reader.ReadBoolean();
                    writer.WriteValueArrayItem(value);
                }
                else if (bsonType == BsonType.Int32)
                {
                    int value = reader.ReadInt32();
                    writer.WriteValueArrayItem(value);
                }
                else if (bsonType == BsonType.Int64)
                {
                    long value = reader.ReadInt64();
                    writer.WriteValueArrayItem(value);
                }
                else if (bsonType == BsonType.Document)
                {
                    // Read BSON stream for the embedded data element
                    IByteBuffer documentBuffer = reader.ReadRawBsonDocument();
                    IBsonReader documentReader = new BsonBinaryReader(new ByteBufferStream(documentBuffer));

                    // Deserialize embedded data element
                    documentReader.ReadStartDocument();
                    writer.WriteStartArrayItem();
                    DeserializeDocument(documentReader, writer);
                    documentReader.ReadEndDocument();
                    writer.WriteEndArrayItem();
                }
                else if (bsonType == BsonType.Array)
                {
                    throw new Exception($"Deserializaion of an array inside another array is not supported.");
                }
                else
                    throw new Exception(
                        $"Deserialization of BSON type {bsonType} inside an array is not supported.");
            }
        }

        /// <summary>Null value is handled via [BsonIgnoreIfNull] attribute and is not expected here.</summary>
        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, TRecord value)
        {
            // ass name without namespace
            string className = value.GetType().Name;

            var bsonWriter = new BsonWriter(context.Writer);
            bsonWriter.WriteStartDocument(className);
            value.SerializeTo(bsonWriter);
            bsonWriter.WriteEndDocument(className);
        }
    }
}
