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
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using NodaTime;

namespace DataCentric
{
    /// <summary>
    /// Serializes RecordId by converting it to RecordId.
    /// </summary>
    public class BsonRecordIdSerializer : SerializerBase<RecordId>
    {
        /// <summary>
        /// Deserialize RecordId by creating it from RecordId.
        ///
        /// The serializer accepts empty value.
        /// </summary>
        public override RecordId Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            // Convert via byte array
            ObjectId objId = context.Reader.ReadObjectId();
            RecordId result = new RecordId(objId.ToByteArray());
            return result;
        }

        /// <summary>
        /// Serialize RecordId by converting it to RecordId.
        ///
        /// The serializer accepts empty value.
        /// </summary>
        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, RecordId value)
        {
            // Convert via byte array
            ObjectId objId = new ObjectId(value.ToByteArray());
            context.Writer.WriteObjectId(objId);
        }
    }
}
