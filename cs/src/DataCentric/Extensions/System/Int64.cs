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
using System.Globalization;

namespace DataCentric
{
    /// <summary>Extension methods for System.Int64.</summary>
    public static class Int64Ext
    {
        /// <summary>Return false if equal to Long.Empty.</summary>
        public static bool HasValue(this long value)
        {
            return value != LongImpl.Empty;
        }

        /// <summary>Convert long to variant.</summary>
        public static Variant ToVariant(this long value) { return new Variant(value); }
    }
}
