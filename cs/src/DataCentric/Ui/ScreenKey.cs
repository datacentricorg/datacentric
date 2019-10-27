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
using MongoDB.Bson.Serialization.Attributes;

namespace DataCentric
{
    /// <summary>
    /// Data representation of a user interface screen.
    /// </summary>
    [BsonSerializer(typeof(BsonKeySerializer<ScreenKey>))]
    public sealed class ScreenKey : TypedKey<ScreenKey, Screen>
    {
        /// <summary>
        /// Unique screen name.
        /// </summary>
        public string ScreenName { get; set; }

        /// <summary>
        /// Startup screen is the screen that is displayed initially when
        /// the application loads.
        /// </summary>
        public static ScreenKey Startup { get; } = new ScreenKey() {ScreenName = "Startup"};
    }
}
