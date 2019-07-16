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
    /// <summary>Provides functionality for deserializing
    /// the object from multi-line CSV data.</summary>
    public interface ICsvDeserializable
    {
        /// <summary>(ICsvSerializable) Deserialize from multi-line CSV data (locks the object).
        /// The object must be empty when this method is invoked.</summary>
        void DeserializeFrom(ICsvReader reader);
    }

    /// <summary>Extension methods for ICsvDeserializable.</summary>
    public static class ICsvDeserializableEx
    {
    }
}
