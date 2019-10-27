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

using System;
using System.Collections.Generic;
using System.Text;

namespace DataCentric
{
    /// <summary>
    /// Specifies the type of the layout item.
    /// </summary>
    public enum LayoutItemTypeEnum
    {
        /// <summary>
        /// The controls inside are arranged vertically 
        /// and separated by resizers.
        /// </summary>
        Column,

        /// <summary>
        /// Leaf layout item. 
        /// Should be placed in Stack only.
        /// </summary>
        Component,

        /// <summary>
        /// Root layout item.
        /// </summary>
        Root,

        /// <summary>
        /// The controls inside are arranged horizontally  
        /// and separated by resizers.
        /// </summary>
        Row,

        /// <summary>
        /// Parent item of single or multiple Components. 
        /// Current layout item should be placed in Column, Row or Root only.
        /// </summary>
        Stack
    }
}
