/*
Copyright (C) 2013-present The DataCentric Authors.
Copyright (C) 2010-present MongoDB Inc.

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
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Security;
using System.Threading;
using MongoDB.Bson; // TODO - remove the remaining use of MongoDB so RecordId is fully portable
using NodaTime;

namespace DataCentric
{
    /// <summary>
    /// A portable, ordered 12-byte record identifier that begins from
    /// timestamp with one second resolution and has the following
    /// properties irrespective of the database type:
    /// 
    /// * Unique within each database table
    /// * Can be generated in strictly increasing order in a single thread
    /// * Can be generated in increasing order across multiple threads
    ///   or servers, but only up to one second resolution. Namely, two
    ///   values generated within the same second are not guaranteed to be
    ///   in an increasing order.
    ///
    /// The purpose of creating a portable version of Object Id is to have a
    /// unique, semi-ordered identifier that can be used not only with MongoDB
    /// but also with relational databases and other storage.
    ///
    /// Because RecordId does not require strict ordering across multiple
    /// threads or servers, it can be used with distributed, web scale
    /// databases where getting a strictly increasing auto-incremented
    /// identifier would cause a performance hit.
    ///
    /// For MongoDB, RecordId maps to ObjectID, and its implementation and
    /// the algorithm for unique generation is the same as ObjectId in MongoDB
    /// driver.
    ///
    /// For relational databases, RecordId may use the same algorithm or use
    /// mapping to a combination of timestamp and an auto incremented field,
    /// if available. 
    /// </summary>
    public struct RecordId : IComparable<RecordId>, IEquatable<RecordId>
    {
        private readonly int _a;
        private readonly int _b;
        private readonly int _c;

        //--- PROPERTIES

        /// <summary>Empty value.</summary>
        public static RecordId Empty { get; } = default(RecordId);

        /// <summar>Timestamp for which RecordId was created.</summary>
        public DateTime CreationTime
        {
            get { return BsonConstants.UnixEpoch.AddSeconds(_a); }
        }

        //--- CONSTRUCTORS

        /// <summary>Create from a byte array of size 12.</summary>
        public RecordId(byte[] bytes)
        {
            if (bytes == null || bytes.Length != 12)
                throw new Exception($"Bytes array passed to RecordId ctor must be 12 bytes long.");

            _a = (bytes[0] << 24) | (bytes[1] << 16) | (bytes[2] << 8) | bytes[3];
            _b = (bytes[4] << 24) | (bytes[5] << 16) | (bytes[6] << 8) | bytes[7];
            _c = (bytes[8] << 24) | (bytes[9] << 16) | (bytes[10] << 8) | bytes[11];
        }

        /// <summary>Create from datetime in UTC and the remaining bytes.</summary>
        public RecordId(DateTime creationTime, byte[] remainingBytes)
        {
            if (remainingBytes == null || remainingBytes.Length != 8)
                throw new Exception($"Remaining bytes array passed to RecordId ctor must be 8 bytes long.");

            _a = GetTimestampFromDateTime(creationTime);
            _b = (remainingBytes[0] << 24) | (remainingBytes[1] << 16) | (remainingBytes[2] << 8) | remainingBytes[3];
            _c = (remainingBytes[4] << 24) | (remainingBytes[5] << 16) | (remainingBytes[6] << 8) | remainingBytes[7];
        }

        /// <summary>
        /// Initializes a new instance of the RecordId class.
        /// </summary>
        /// <param name="timestamp">The timestamp (expressed as a DateTime).</param>
        /// <param name="machine">The machine hash.</param>
        /// <param name="pid">The PID.</param>
        /// <param name="increment">The increment.</param>
        public RecordId(DateTime timestamp, int machine, short pid, int increment)
            : this(GetTimestampFromDateTime(timestamp), machine, pid, increment)
        {
        }

        /// <summary>
        /// Initializes a new instance of the RecordId class.
        /// </summary>
        /// <param name="timestamp">The timestamp.</param>
        /// <param name="machine">The machine hash.</param>
        /// <param name="pid">The PID.</param>
        /// <param name="increment">The increment.</param>
        public RecordId(int timestamp, int machine, short pid, int increment)
        {
            if ((machine & 0xff000000) != 0)
            {
                throw new ArgumentOutOfRangeException("machine", "The machine value must be between 0 and 16777215 (it must fit in 3 bytes).");
            }
            if ((increment & 0xff000000) != 0)
            {
                throw new ArgumentOutOfRangeException("increment", "The increment value must be between 0 and 16777215 (it must fit in 3 bytes).");
            }

            _a = timestamp;
            _b = (machine << 8) | (((int)pid >> 8) & 0xff);
            _c = ((int)pid << 24) | increment;
        }

        /// <summary>Compares this RecordId to another RecordId.</summary>
        public int CompareTo(RecordId other)
        {
            int result = ((uint)_a).CompareTo((uint)other._a);
            if (result != 0) { return result; }
            result = ((uint)_b).CompareTo((uint)other._b);
            if (result != 0) { return result; }
            return ((uint)_c).CompareTo((uint)other._c);
        }

        /// <summary>True if the two RecordIds are equal.</summary>
        public bool Equals(RecordId rhs)
        {
            return
                _a == rhs._a &&
                _b == rhs._b &&
                _c == rhs._c;
        }

        /// <summary>True if the other object is an RecordId and equal to this one.</summary>
        public override bool Equals(object obj)
        {
            if (obj is RecordId)
            {
                return Equals((RecordId)obj);
            }
            else
            {
                return false;
            }
        }

        /// <summary>Gets the hash code.</summary>
        public override int GetHashCode()
        {
            int hash = 17;
            hash = 37 * hash + _a.GetHashCode();
            hash = 37 * hash + _b.GetHashCode();
            hash = 37 * hash + _c.GetHashCode();
            return hash;
        }

        /// <summary>Converts the RecordId to a byte array of length 12.</summary>
        public byte[] ToByteArray()
        {
            var bytes = new byte[12];
            bytes[0] = (byte)(_a >> 24);
            bytes[1] = (byte)(_a >> 16);
            bytes[2] = (byte)(_a >> 8);
            bytes[3] = (byte)(_a);
            bytes[4] = (byte)(_b >> 24);
            bytes[5] = (byte)(_b >> 16);
            bytes[6] = (byte)(_b >> 8);
            bytes[7] = (byte)(_b);
            bytes[8] = (byte)(_c >> 24);
            bytes[9] = (byte)(_c >> 16);
            bytes[10] = (byte)(_c >> 8);
            bytes[11] = (byte)(_c);
            return bytes;
        }

        /// <summary>
        /// Returns a string representation of the value using the following format:
        ///
        /// yyyy-mm-dd hh:mm:ss bytes[16]
        /// </summary>
        public override string ToString()
        {
            // First part of the serialized value is the timestamp
            string creationTimeString = this.ToLocalDateTime().AsString();

            // Second part of the serialized value is 16 byte hexadecimal string
            var c = new char[16];
            c[0] = BsonUtils.ToHexChar((_b >> 28) & 0x0f);
            c[1] = BsonUtils.ToHexChar((_b >> 24) & 0x0f);
            c[2] = BsonUtils.ToHexChar((_b >> 20) & 0x0f);
            c[3] = BsonUtils.ToHexChar((_b >> 16) & 0x0f);
            c[4] = BsonUtils.ToHexChar((_b >> 12) & 0x0f);
            c[5] = BsonUtils.ToHexChar((_b >> 8) & 0x0f);
            c[6] = BsonUtils.ToHexChar((_b >> 4) & 0x0f);
            c[7] = BsonUtils.ToHexChar(_b & 0x0f);
            c[8] = BsonUtils.ToHexChar((_c >> 28) & 0x0f);
            c[9] = BsonUtils.ToHexChar((_c >> 24) & 0x0f);
            c[10] = BsonUtils.ToHexChar((_c >> 20) & 0x0f);
            c[11] = BsonUtils.ToHexChar((_c >> 16) & 0x0f);
            c[12] = BsonUtils.ToHexChar((_c >> 12) & 0x0f);
            c[13] = BsonUtils.ToHexChar((_c >> 8) & 0x0f);
            c[14] = BsonUtils.ToHexChar((_c >> 4) & 0x0f);
            c[15] = BsonUtils.ToHexChar(_c & 0x0f);
            string hex = new string(c);

            // Serialized RecordId has the following format: yyyy-mm-dd hh:mm:ss bytes[16]
            string result = string.Join(" ", creationTimeString, hex);
            return result;
        }

        //--- STATIC

        /// <summary>
        /// Parses a string and creates a new RecordId.
        ///
        /// The string representation of RecordId has
        /// the following format:
        ///
        /// yyyy-mm-dd hh:mm:ss bytes[16]
        /// </summary>
        public static RecordId Parse(string s)
        {
            if (s == null)
            {
                throw new ArgumentNullException("s");
            }

            RecordId recId;
            if (TryParse(s, out recId))
            {
                return recId;
            }
            else
            {
                var message = string.Format("'{0}' is not a valid 24 digit hex string.", s);
                throw new FormatException(message);
            }
        }

        /// <summary>
        /// Tries to parse a string and create a new RecordId.
        ///
        /// The string representation of RecordId has
        /// the following format:
        ///
        /// yyyy-mm-dd hh:mm:ss bytes[16]
        /// </summary>
        public static bool TryParse(string s, out RecordId recId)
        {
            // Set to empty value in case the method exits early
            recId = default(RecordId);

            // RecordId is serialized using the following format yyyy-mm-dd hh:mm:ss 0000000000000000
            // Exit if the string length is not 36
            if (string.IsNullOrEmpty(s) || s.Length != 36) return false;

            // The next step is to tokenize the input string.
            // Exit unless the string has three tokens.
            string[] tokens = s.Split(' ');
            if (tokens.Length != 3) return false;

            // Concatenate and the first two tokens with T separator and then try to
            // parse them as LocalDateTime using strict format yyyy-mm-ddThh:mm:ss
            DateTime creationDateTime;
            string dateTimeString = string.Join("T", tokens[0], tokens[1]);
            if (!DateTime.TryParse(dateTimeString, null, DateTimeStyles.RoundtripKind, out creationDateTime))
            {
                return false;
            }

            // Try to parse the third token to a byte array
            byte[] bytes;
            if (!BsonUtils.TryParseHexString(tokens[2], out bytes)) return false;

            // Populate the first integer from timestamp
            // and the two remaining integers from the byte array
            recId = new RecordId(creationDateTime, bytes);
            return true;
        }

        //--- OPERATORS

        /// <summary>
        /// Converts UTC datetime to the smallest possible value of RecordId
        /// generated within the same second as the timestamp.
        ///
        /// By convention, all datetime values are assumed to be in UTC timezone.
        /// </summary>
        public static implicit operator RecordId(LocalDateTime rhs)
        {
            var utcDateTime = rhs.ToUtcDateTime();
            var result = new RecordId(utcDateTime, 0, 0, 0);
            return result;
        }

        /// <summary>True if the first RecordId is less than the second RecordId.</summary>
        public static bool operator <(RecordId lhs, RecordId rhs)
        {
            return lhs.CompareTo(rhs) < 0;
        }

        /// <summary>True if the first RecordId is less than or equal to the second RecordId.</summary>
        public static bool operator <=(RecordId lhs, RecordId rhs)
        {
            return lhs.CompareTo(rhs) <= 0;
        }

        /// <summary>True if the two RecordIds are equal.</summary>
        public static bool operator ==(RecordId lhs, RecordId rhs)
        {
            return lhs.Equals(rhs);
        }

        /// <summary>True if the two RecordIds are not equal.</summary>
        public static bool operator !=(RecordId lhs, RecordId rhs)
        {
            return !(lhs == rhs);
        }

        /// <summary>True if the first RecordId is greather than or equal to the second RecordId.</summary>
        public static bool operator >=(RecordId lhs, RecordId rhs)
        {
            return lhs.CompareTo(rhs) >= 0;
        }

        /// <summary>True if the first RecordId is greather than the second RecordId.</summary>
        public static bool operator >(RecordId lhs, RecordId rhs)
        {
            return lhs.CompareTo(rhs) > 0;
        }

        //--- PRIVATE

        private static int GetTimestampFromDateTime(DateTime timestamp)
        {
            var secondsSinceEpoch = (long)Math.Floor((BsonUtils.ToUniversalTime(timestamp) - BsonConstants.UnixEpoch).TotalSeconds);
            if (secondsSinceEpoch < int.MinValue || secondsSinceEpoch > int.MaxValue)
            {
                throw new ArgumentOutOfRangeException("timestamp");
            }
            return (int)secondsSinceEpoch;
        }
    }

    /// <summary>Extension methods for RecordId.</summary>
    public static class RecordIdExt
    {
        /// <summary>Return false if equal to the default constructed value.</summary>
        public static bool HasValue(this RecordId value)
        {
            return value != default;
        }

        /// <summary>Return false if null or equal to the default constructed value.</summary>
        public static bool HasValue(this RecordId? value)
        {
            return value.HasValue && value.HasValue();
        }

        /// <summary>Error message if equal to the default constructed value.</summary>
        public static void CheckHasValue(this RecordId value)
        {
            if (!value.HasValue()) throw new Exception("Required RecordId value is not set.");
        }

        /// <summary>Error message if null or equal to the default constructed value.</summary>
        public static void CheckHasValue(this RecordId? value)
        {
            if (!value.HasValue()) throw new Exception("Required RecordId value is not set.");
        }

        /// <summary>
        /// Convert RecordId to its creation time in UTC. This method has one second resolution.
        ///
        /// Error message if equal to the default constructed value.
        /// </summary>
        public static LocalDateTime ToLocalDateTime(this RecordId value)
        {
            value.CheckHasValue();

            var result = value.CreationTime.ToLocalDateTime();
            return result;
        }

        /// <summary>
        /// Convert RecordId to its creation time. This method has one second resolution.
        ///
        /// Return null if equal to the default constructed value.
        /// </summary>
        public static LocalDateTime? ToLocalDateTime(this RecordId? value)
        {
            if (value.HasValue) return value.Value.ToLocalDateTime();
            else return null;
        }
    }
}
