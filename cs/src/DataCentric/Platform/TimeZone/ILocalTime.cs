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
using NodaTime;

namespace DataCentric
{
    /// <summary>
    /// Provides local time and the timezone in which it is defined.
    ///
    /// By convention, throughout this library datetime is always in UTC
    /// while date, time, and minute are always in a specific timezone.
    ///
    /// Only the following timezones can be defined:
    ///
    /// * UTC timezone; and 
    /// * IANA city timezones such as America/New_York
    ///
    /// Other 3-letter regional timezones such as EST or EDT are
    /// not permitted because they do not handle the transition
    /// between winter and summer time automatically for those
    /// regions where winter time is defined.
    /// </summary>
    public interface ILocalTime
    {
        /// <summary>
        /// Local time in the specified timezone.
        ///
        /// By convention, throughout this library datetime is always in UTC
        /// while date, time, and minute are always in a specific timezone.
        /// </summary>
        LocalTime? Time { get; }

        /// <summary>
        /// Timezone used to convert between UTC datetime and local time.
        /// 
        /// By convention, throughout this library datetime is always in UTC
        /// while date, time, and minute are always in a specific timezone.
        /// </summary>
        TimeZoneKey TimeZone { get; }
    }

    /// <summary>Extension methods for ILocalTime.</summary>
    public static class ILocalTimeEx
    {
    }
}
