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

namespace DataCentric.Cli
{
    /// <summary>List of values and immutable types, including string and binary.</summary>
    public enum AtomicType
    {
        /// <summary>None value is defined.</summary>
        EnumNone = -1,

        /// <summary>Bool value.</summary>
        Bool,

        /// <summary>Int value.</summary>
        Int,

        /// <summary>Long value.</summary>
        Long,

        /// <summary>Double value.</summary>
        Double,

        /// <summary>Date value.</summary>
        Date,

        /// <summary>DateTime value.</summary>
        DateTime,

        /// <summary>String value.</summary>
        String,

        /// <summary>Binary value.</summary>
        Binary,

        /// <summary>Key value.</summary>
        Key,

        /// <summary>Generic data value.</summary>
        Data,

        /// <summary>Variant value.</summary>
        Variant,

        /// <summary>Decimal value.</summary>
        Decimal,

        /// <summary>Time value.</summary>
        Time
    }
}