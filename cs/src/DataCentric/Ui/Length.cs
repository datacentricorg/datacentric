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
    /// Provides length in relative and/or absolute units.
    /// </summary>
    public abstract class Length : Data
    {
        /// <summary>
        /// Relative length expressed in the units of:
        ///
        /// * If horizontal, width of the parent container
        /// * If vertical, height of the parent container
        ///
        /// If both Relative and Absolute length is specified,
        /// the length used is the greater of the two. This
        /// may be used to specify minimum size that a control
        /// needs to render properly.
        ///
        /// If neither Relative nor Absolute length is specified,
        /// the control is auto sized.
        /// </summary>
        public double? Relative { get; set; }

        /// <summary>
        /// Absolute length expressed in the units of:
        ///
        /// * If horizontal, a standard width of a table column
        /// * If vertical, a standard height of a table row
        ///
        /// If both Relative and Absolute length is specified,
        /// the length used is the greater of the two. This
        /// may be used to specify minimum size that a control
        /// needs to render properly.
        ///
        /// If neither Relative nor Absolute length is specified,
        /// the control is auto sized.
        /// </summary>
        public double? Absolute { get; set; }
    }
}
