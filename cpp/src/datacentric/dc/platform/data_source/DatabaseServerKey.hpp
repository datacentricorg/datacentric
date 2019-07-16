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
#include <dot/system/Ptr.hpp>
#include <dc/types/record/RecordFor.hpp>

namespace dc
{
    class DbServerKeyImpl; using DbServerKey = dot::Ptr<DbServerKeyImpl>;
    class DbServerDataImpl; using DbServerData = dot::Ptr<DbServerDataImpl>;

    inline DbServerKey new_DbServerKey();

    /// <summary>
    /// Provides a standard way to identify a database server.
    ///
    /// This record is stored in root dataset.
    /// </summary>
    class DC_CLASS DbServerKeyImpl : public RootKeyForImpl<DbServerKeyImpl, DbServerDataImpl>
    {
        typedef DbServerKeyImpl self;

    public: // PROPERTIES

        /// <summary>
        /// Unique database server identifier string.
        ///
        /// This field is the user friendly name used to
        /// identify the server. It is not the server URI.
        /// </summary>
        DOT_AUTO_PROP(dot::String, DbServerID)

    public: // STATIC

        /// <summary>
        /// By convention, Default is the Mongo server running on the default port of localhost.
        /// </summary>
        static DbServerKey Default;

    public: // CONSTRUCTORS

        /// <summary>Default constructor.</summary>
        DbServerKeyImpl() = default;

        /// <summary>Keys in which string ID is the only element support implicit conversion from value.</summary>
        DbServerKeyImpl(dot::String value);

        DOT_TYPE_BEGIN(".Runtime.Main", "DbServerKey")
            DOT_TYPE_PROP(DbServerID)
            DOT_TYPE_CTOR(new_DbServerKey)
        DOT_TYPE_END()
    };

    inline DbServerKey new_DbServerKey()
    {
        return new DbServerKeyImpl;
    }
}
