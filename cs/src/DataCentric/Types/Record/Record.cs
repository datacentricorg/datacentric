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
using CsvHelper.Configuration.Attributes;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace DataCentric
{
    /// <summary>
    /// Base class of records stored in data source.
    /// </summary>
    [BsonDiscriminator(RootClass = true)]
    public abstract class Record : Data
    {
        /// <summary>
        /// Execution context provides access to key resources including:
        ///
        /// * Logging and error reporting
        /// * Cloud calculation service
        /// * Data sources
        /// * Filesystem
        /// * Progress reporting
        /// </summary>
        [BsonIgnore]
        [Ignore]
        public IContext Context { get; private set; }

        //--- ELEMENTS

        /// <summary>
        /// ObjectId of the record is specific to its version.
        ///
        /// For the record's history to be captured correctly, all
        /// update operations must assign a new ObjectId with the
        /// timestamp that matches update time.
        /// </summary>
        [BsonId]
        [BsonRequired]
        public ObjectId Id { get; set; }

        /// <summary>
        /// ObjectId of the dataset where the record is stored.
        ///
        /// For records stored in root dataset, the value of
        /// DataSet element should be ObjectId.Empty.
        /// </summary>
        [BsonElement("_dataset")]
        public ObjectId DataSet { get; set; }

        /// <summary>
        /// String key consists of semicolon delimited primary key elements:
        ///
        /// KeyElement1;KeyElement2
        ///
        /// To avoid serialization format uncertainty, key elements
        /// can have any atomic type except Double.
        /// </summary>
        [BsonElement("_key")]
        [BsonRequired]
        public abstract string Key { get; set; }

        //--- METHODS

        /// <summary>
        /// Set context and perform initialization or validation of object data.
        ///
        /// All derived classes overriding this method must call base.Init(context)
        /// before executing the the rest of the code in the method override.
        /// </summary>
        public virtual void Init(IContext context)
        {
            // The line below is an example of calling Init(context) method for base class.
            // It should be uncommented for all classes derived from this class directly
            // or indirectly that override Init(context)
            //
            // Initialize the base class
            // base.Init(context);

            // Check that argument is not null and assign to the Context property
            if (context == null) throw new Exception(
                $"Null context is passed to the Init(...) method for {GetType().Name}.");
            Context = context;
        }

        /// <summary>Return string representation of the record's key.</summary>
        public override string ToString() { return Key; }
    }
}
