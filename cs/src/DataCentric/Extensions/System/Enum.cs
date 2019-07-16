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

namespace DataCentric
{
    /// <summary>Static helper class for System.Enum.</summary>
    public static class EnumEx
    {
        /// <summary>Return true if not null and not equal to the default constructed value.</summary>
        public static bool HasValue(this Enum value)
        {
            return value != null;
        }

        /// <summary>Error message if equal to the default constructed value.</summary>
        public static void CheckHasValue(this Enum value)
        {
            if (!value.HasValue()) throw new Exception("Required enum value is not set.");
        }

        /// <summary>Convert Enum to variant.</summary>
        public static Variant ToVariant(this Enum value) { return new Variant(value); }
    }
}
