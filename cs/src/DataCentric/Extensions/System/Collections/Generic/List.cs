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

namespace DataCentric
{
    /// <summary>Extension methods for List.</summary>
    public static class ListEx
    {
        /// <summary>
        /// Add value to the collection and return self.
        ///
        /// This extension method is used by the Fluent API.
        ///</summary>
        public static List<TValue> Append<TValue>(this List<TValue> obj, TValue value)
        {
            obj.Add(value);
            return obj;
        }
    }
}
