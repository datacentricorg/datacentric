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
#include <dot/system/ptr.hpp>
#include <dc/platform/data_source/mongo/MongoServerData.hpp>

namespace dc
{
    class MongoDefaultServerDataImpl; using MongoDefaultServerData = dot::ptr<MongoDefaultServerDataImpl>;

    /// <summary>
    /// Returns MongoDB URI for the server running
    /// on the default port 27017 of localhost:
    ///
    /// mongodb://localhost/
    /// </summary>
    class DC_CLASS MongoDefaultServerDataImpl final : public MongoServerDataImpl
    {
        typedef MongoDefaultServerDataImpl self;

        friend MongoDefaultServerData new_MongoDefaultServerData();

    protected: // CONSTRUCTORS

        /// <summary>
        /// Assign the value of DbServerID that
        /// the default Mongo server uses by convention.
        /// </summary>
        MongoDefaultServerDataImpl();

    public: // METHODS

        /// <summary>Get Mongo server URI without database name.</summary>
        dot::string GetMongoServerUri() override;
    };

    inline MongoDefaultServerData new_MongoDefaultServerData() { return new MongoDefaultServerDataImpl; }
}
