﻿/*
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
    /// <summary>Type kind enumeration.</summary>
    public enum TypeKind
    {
        /// <summary>None value is defined.</summary>
        EnumNone = -1,

        /// <summary>Final type. No types can inherit from final type.</summary>
        Final,

        /// <summary>Abstract type. Abstract type can only be saved in storage as parent of another type.</summary>
        Abstract,

        /// <summary>Element type. Element type cannot be saved in storage directly or as parent of other type.</summary>
        Element
    }
}