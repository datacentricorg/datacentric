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
    /// <summary>
    /// Specifies the type of positioning method used for an element.
    /// </summary>
    public enum PositionTypeEnum
    {
        /// <summary>
        /// Control positioned relative to the position it would have
        /// if it were Static.
        /// </summary>
        Relative,

        /// <summary>
        /// Control positioned relative to its parent container.
        ///
        /// To position a control relative to the current scroll
        /// window rather than its parent container, use Fixed.
        /// </summary>
        Absolute
    }
}
