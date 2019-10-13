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
    /// Attribute for identifying methods that are viewers.
    ///
    /// Viewers are methods of a Record that produce output
    /// displayed along with the record in the user interface,
    /// e.g. on a tab of the screen associated with the Record.
    ///
    /// A viewer method must:
    ///
    /// * Take parameters that are either atomic types or
    ///   classes derived from Data;
    /// * Return void; and
    /// * Create a view record associated with the record
    ///   for which it is invoked.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class ViewerAttribute : Attribute
    {
    }
}