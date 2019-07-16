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
    /// Provides local date, time, and timezone in which
    /// they are defined.
    ///
    /// By convention, throughout this library all date, time,
    /// and minute properties and variables are in the local
    /// timezone; and all datetime properties and variables
    /// are in UTC timezone.
    /// </summary>
    public interface ILocalDateTime
    {
        /// <summary>
        /// Local date in the specified timezone.
        ///
        /// By convention, throughout this library all date, time,
        /// and minute properties and variables are in the local
        /// timezone; and all datetime properties and variables
        /// are in UTC timezone.
        ///
        /// Timezone is provided via TimeZone property
        /// of the base interface ITimeZone.
        /// </summary>
        LocalDate? Date { get; }

        /// <summary>
        /// Local time in the specified timezone.
        ///
        /// By convention, throughout this library all date, time,
        /// and minute properties and variables are in the local
        /// timezone; and all datetime properties and variables
        /// are in UTC timezone.
        ///
        /// Timezone is provided via TimeZone property
        /// of the base interface ITimeZone.
        /// </summary>
        LocalTime? Time { get; }

        /// <summary>
        /// Timezone used to convert between UTC and local
        /// date, time, minute, and datetime.
        ///
        /// By convention, throughout this library all date, time,
        /// and minute properties and variables are in the local
        /// timezone; and all datetime properties and variables
        /// are in UTC timezone.
        ///
        /// The IANA city timezone code has two slash-delimited tokens,
        /// the first referencing the country and the other the city, for
        /// example America/New_York.
        /// </summary>
        TimeZoneKey TimeZone { get; }
    }

    /// <summary>Extension methods for ILocalDateTime.</summary>
    public static class ILocalDateTimeEx
    {
    }
}
