// Copyright 2018-present MongoDB Inc.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#pragma once

#include <bsoncxx/document/view.hpp>
#include <bsoncxx/stdx/string_view.hpp>

#include <mongocxx/config/prelude.hpp>

namespace mongocxx {
MONGOCXX_INLINE_NAMESPACE_BEGIN

namespace events {

///
/// An event notification sent when the driver completes an "isMaster" command to check the status
/// of a server.
///
/// @see "ServerHeartbeatSucceededEvent" in
/// https://github.com/mongodb/specifications/blob/master/source/server-discovery-and-monitoring/server-discovery-and-monitoring-monitoring.rst
///
class MONGOCXX_API heartbeat_succeeded_event {
   public:
    MONGOCXX_PRIVATE explicit heartbeat_succeeded_event(const void* event);

    ///
    /// Destroys a heartbeat_succeeded_event.
    ///
    ~heartbeat_succeeded_event();

    ///
    /// Returns the server reply for the succeeded operation.
    ///
    /// @return The reply.
    ///
    bsoncxx::document::view reply() const;

    ///
    /// Returns the duration of the successful operation.
    ///
    /// @return The duration in microseconds.
    ///
    std::int64_t duration() const;

    ///
    /// Returns the host name.
    ///
    /// @return The host name.
    ///
    bsoncxx::stdx::string_view host() const;

    ///
    /// Returns the port.
    ///
    /// @return The port.
    ///
    std::uint16_t port() const;

   private:
    const void* _succeeded_event;
};

}  // namespace events
MONGOCXX_INLINE_NAMESPACE_END
}  // namespace mongocxx

#include <mongocxx/config/postlude.hpp>
