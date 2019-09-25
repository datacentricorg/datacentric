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
using MongoDB.Bson;

namespace DataCentric
{
    /// <summary>
    /// Base class of records stored in root dataset of the data store.
    ///
    /// This class overrides DataSet property to always return ObjectId.Empty.
    /// </summary>
    public abstract class RootRecord<TKey, TRecord> : TypedRecord<TKey, TRecord>
        where TKey : RootKey<TKey, TRecord>, new()
        where TRecord : RootRecord<TKey, TRecord>
    {
        /// <summary>
        /// Set context and perform initialization or validation of object data.
        ///
        /// All derived classes overriding this method must call base.Init(context)
        /// before executing the the rest of the code in the method override.
        /// </summary>
        public virtual void Init(IContext context)
        {
            // Initialize the base class
            base.Init(context);

            // For this base type of records stored in root dataset,
            // DataSet element has the value designated for the
            // root dataset: ObjectId.Empty.
            DataSet = ObjectId.Empty;
        }
    }
}
