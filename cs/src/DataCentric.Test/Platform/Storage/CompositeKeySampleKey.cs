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
using System.Linq;
using DataCentric;
using MongoDB.Bson.Serialization.Attributes;
using NodaTime;
using Xunit;

namespace DataCentric.Test
{
    /// <summary>Data type sample with composite key.</summary>
    [BsonSerializer(typeof(BsonKeySerializer<CompositeKeySampleKey>))]
    public sealed class CompositeKeySampleKey : TypedKey<CompositeKeySampleKey, CompositeKeySample>
    {
        /// <summary>Sample element.</summary>
        public string KeyElement1 { get; set; }

        /// <summary>Sample element.</summary>
        public BaseSampleKey KeyElement2 { get; set; }

        /// <summary>Sample element.</summary>
        public string KeyElement3 { get; set; }
    }
}