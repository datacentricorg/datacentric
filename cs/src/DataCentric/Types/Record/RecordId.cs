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
    /// A 16-byte ordered record identifier that consists of two parts:
    ///
    /// * Int64 representing creation time recorded as the number of
    ///   Unix ticks since the epoch (1970).
    /// * Int64 that is randomized using machine-specific information.
    ///
    /// RecordId has the same size as GUID and can be stored in a
    /// data type designed for GUID.
    ///
    /// RecordId is unique in the absence of collision for its randomized
    /// part, which by design has extremely low probability. It has the
    /// following ordering guarantees:
    ///
    /// * When generated in the same process, RecordIds are strictly ordered
    ///   irrespective of how fast they are generated.
    /// * When generated independently, RecordIds are ordered if generated
    ///   more than one operating system clock event apart. While the
    ///   underlying tick data type has 100ns resolution, the operating
    ///   system clock more typically has one event per 10-20 ms.
    ///
    /// Because RecordId does not rely on auto-incremented database field,
    /// it can be used with distributed, web scale databases where getting
    /// a strictly increasing auto-incremented identifier would cause a
    /// performance hit.
    /// </summary>
    public struct RecordId : IComparable<RecordId>, IEquatable<RecordId>
    {
        private static readonly int __staticMachine = (GetMachineHash() + GetAppDomainId()) & 0x00ffffff;
        private static readonly short __staticPid = GetPid();
        private static int __staticIncrement = (new Random()).Next();

        private readonly int _a;
        private readonly int _b;
        private readonly int _c;

        //--- PROPERTIES

        /// <summary>Empty value.</summary>
        public static RecordId Empty { get; } = default(RecordId);

        /// <summary>Creation time of the current object.</summary>
        public Instant CreationTime
        {
            get => Instant.FromUnixTimeSeconds(_a);
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
        public RecordId(Instant creationTime, byte[] remainingBytes)
        {
            long secondsSinceEpoch = creationTime.ToUnixTimeSeconds();
            if (secondsSinceEpoch < int.MinValue || secondsSinceEpoch > int.MaxValue)
                throw new Exception(
                    $"CreationTime={creationTime} is out of range that can be represented by RecordId.");

            if (remainingBytes == null || remainingBytes.Length != 8)
                throw new Exception(
                    $"Remaining bytes array passed to RecordId ctor must be 8 bytes long.");

            _a = (int) secondsSinceEpoch;
            _b = (remainingBytes[0] << 24) | (remainingBytes[1] << 16) | (remainingBytes[2] << 8) | remainingBytes[3];
            _c = (remainingBytes[4] << 24) | (remainingBytes[5] << 16) | (remainingBytes[6] << 8) | remainingBytes[7];
        }

        /// <summary>Create from datetime in UTC and randomization parameters.</summary>
        public RecordId(Instant creationTime, int machine, short pid, int increment)
        {
            long secondsSinceEpoch = creationTime.ToUnixTimeSeconds();
            if (secondsSinceEpoch < int.MinValue || secondsSinceEpoch > int.MaxValue)
                throw new Exception(
                    $"CreationTime={creationTime} is out of range that can be represented by RecordId.");

            if ((machine & 0xff000000) != 0) throw new Exception($"The machine value {machine} must be between 0 and 16777215 (it must fit in 3 bytes).");
            if ((increment & 0xff000000) != 0) throw new Exception($"The increment value {increment} must be between 0 and 16777215 (it must fit in 3 bytes).");

            _a = (int) secondsSinceEpoch;
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
            string creationTimeString = this.CreationTime.AsString();

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

            // RecordId is serialized using the following format:
            //
            // yyyy-mm-ddThh:mm:ss.fffZhhhhhhhhhhhhhhhh
            //
            // where h represents a hexadecimal digit (total of 16)
            string result = creationTimeString + hex;
            return result;
        }

        //--- STATIC

        /// <summary>Generates a new RecordId with a unique value.</summary>
        public static RecordId GenerateNewId()
        {
            Instant creationTime = DateTime.UtcNow.ToInstant();

            // Only use low order 3 bytes
            int increment = Interlocked.Increment(ref __staticIncrement) & 0x00ffffff;
            return new RecordId(creationTime, __staticMachine, __staticPid, increment);
        }

        /// <summary>
        /// Parses a string and creates a new RecordId.
        ///
        /// RecordId is serialized using the following format:
        ///
        /// yyyy-mm-ddThh:mm:ss.fffZhhhhhhhhhhhhhhhh
        ///
        /// where each h represents a hexadecimal digit (total of 16).
        /// </summary>
        public static RecordId Parse(string value)
        {
            RecordId result;
            if (TryParse(value, out result))
            {
                return result;
            }
            else
            { 
                throw new Exception(
                    $"RecordId={value} does not consist of ISO timestamp in UTC (Z) " +
                    $"timezone followed by hexadecimal string of length 16.");
            }
        }

        /// <summary>
        /// Tries to parse a string and create a new RecordId.
        ///
        /// RecordId is serialized using the following format:
        ///
        /// yyyy-mm-ddThh:mm:ss.fffZhhhhhhhhhhhhhhhh
        ///
        /// where each h represents a hexadecimal digit (total of 16).
        /// </summary>
        public static bool TryParse(string value, out RecordId recId)
        {
            // Return empty RecordId for null or empty string
            if (string.IsNullOrEmpty(value))
            {
                recId = RecordId.Empty;
                return true;
            }

            // Set to empty value in case the method exits early
            recId = default(RecordId);

            // RecordId is serialized using the following format:
            //
            // yyyy-mm-ddThh:mm:ss.fffZhhhhhhhhhhhhhhhh
            //
            // where each h represents a hexadecimal digit (total of 16)
            //
            // Exit if the string length is not 37
            if (value.Length != 40) return false;

            // Parse creation time using strict format yyyy-mm-ddThh:mm:ss
            Instant creationTime;
            string creationTimeString = value.Substring(0, 24);
            if (!InstantUtil.TryParse(creationTimeString, out creationTime))
            {
                return false;
            }

            // Try to parse the third token to a byte array
            byte[] bytes;
            string bytesString = value.Substring(24, 16);
            if (!BsonUtils.TryParseHexString(bytesString, out bytes)) return false;

            // Populate the first integer from timestamp
            // and the two remaining integers from the byte array
            recId = new RecordId(creationTime, bytes);
            return true;
        }

        //--- OPERATORS

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

        // private static methods
        private static int GetAppDomainId()
        {
#if NETSTANDARD1_5 || NETSTANDARD1_6
            return 1;
#else
            return AppDomain.CurrentDomain.Id;
#endif
        }

        /// <summary>
        /// Gets the current process id.  This method exists because of how CAS operates on the call stack, checking
        /// for permissions before executing the method.  Hence, if we inlined this call, the calling method would not execute
        /// before throwing an exception requiring the try/catch at an even higher level that we don't necessarily control.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int GetCurrentProcessId()
        {
            return Process.GetCurrentProcess().Id;
        }

        private static int GetMachineHash()
        {
            // Use instead of Dns.HostName so it will work offline.
            // Use first 3 bytes of hash.
            return 0x00ffffff & Environment.MachineName.GetHashCode(); 
        }

        private static short GetPid()
        {
            try
            {
                // Use low order two bytes only
                return (short)GetCurrentProcessId();
            }
            catch (SecurityException)
            {
                return 0;
            }
        }

        private static void FromByteArray(byte[] bytes, int offset, out int a, out int b, out int c)
        {
            a = (bytes[offset] << 24) | (bytes[offset + 1] << 16) | (bytes[offset + 2] << 8) | bytes[offset + 3];
            b = (bytes[offset + 4] << 24) | (bytes[offset + 5] << 16) | (bytes[offset + 6] << 8) | bytes[offset + 7];
            c = (bytes[offset + 8] << 24) | (bytes[offset + 9] << 16) | (bytes[offset + 10] << 8) | bytes[offset + 11];
        }
    }

    /// <summary>Extension methods for RecordId.</summary>
    public static class RecordIdExtensions
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
    }
}
