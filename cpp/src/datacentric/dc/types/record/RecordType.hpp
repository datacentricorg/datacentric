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

#pragma once

#include <dc/declare.hpp>
#include <dc/types/record/DataType.hpp>

namespace dc
{
    class record_type_impl; using record_type = dot::ptr<record_type_impl>;
    class IContextImpl; using IContext = dot::ptr<IContextImpl>;

    /// <summary>Record objects must derive from this type.</summary>
    class DC_CLASS record_type_impl : public virtual DataImpl
    {
        typedef record_type_impl self;

    public: // FIELDS

        /// <summary>
        /// ObjectId of the record is specific to its version.
        ///
        /// For the record's history to be captured correctly, all
        /// update operations must assign a new ObjectId with the
        /// timestamp that matches update time.
        /// </summary>
        ObjectId ID;

        /// <summary>
        /// ObjectId of the dataset where the record is stored.
        ///
        /// The records that may be stored in root dataset (including
        /// data source, database, database server, and Common dataset
        /// records) must override this property to avoid an error about
        /// dataset not being set for the record.
        /// </summary>
        ObjectId DataSet;

        /// <summary>Use context to access resources.</summary>
        IContext Context;

    public: // PROPERTIES

        /// <summary>
        /// dot::string key consists of semicolon delimited primary key elements:
        ///
        /// KeyElement1;KeyElement2
        ///
        /// To avoid serialization format uncertainty, key elements
        /// can have any atomic type except Double.
        /// </summary>
        virtual dot::string get_key() = 0;

    public: // METHODS

        /// <summary>
        /// Set context and perform fast initialization or validation
        /// of class data. Must first invoke base.Init(context).
        /// </summary>
        virtual void Init(IContext context);

        dot::string to_string() { return get_key(); }

        DOT_TYPE_BEGIN(".Runtime.Main", "record_type")
           // ->WithProperty("_id", &self::ID)
         //   ->WithProperty("_dataset", &self::DataSet)
          //  ->WithProperty("_key", &self::Key)
            DOT_TYPE_BASE(Data)
        DOT_TYPE_END()

    };
}
