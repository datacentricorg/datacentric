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
using CsvHelper.Configuration.Attributes;
using MongoDB.Bson.Serialization.Attributes;
using NodaTime;

namespace DataCentric
{
    /// <summary>
    /// This interface provides timezone for the conversion between
    /// datetime (by convention, always in UTC) and date, time,
    /// and minute (by convention, always in a specific timezone).
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
    ///
    /// Because TimeZoneName is used to look up timezone conventions,
    /// it must match either the string UTC or the code in IANA
    /// timezone database precisely. The IANA city timezone code
    /// has two slash-delimited tokens, the first referencing the
    /// country and the other the city, for example America/New_York.
    /// </summary>
    public class TimeZone : TypedRecord<TimeZoneKey, TimeZone>
    {
        private DateTimeZone dateTimeZone_;

        /// <summary>
        /// Unique timezone name.
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
        ///
        /// Because TimeZoneName is used to look up timezone conventions,
        /// it must match either the string UTC or the code in IANA
        /// timezone database precisely. The IANA city timezone code
        /// has two slash-delimited tokens, the first referencing the
        /// country and the other the city, for example America/New_York.
        /// </summary>
        [BsonRequired]
        public string TimeZoneName { get; set; }

        //--- METHODS

        /// <summary>
        /// Set Context property and perform validation of the record's data,
        /// then initialize any fields or properties that depend on that data.
        ///
        /// This method must work when called multiple times for the same instance,
        /// possibly with a different context parameter for each subsequent call.
        ///
        /// All overrides of this method must call base.Init(context) first, then
        /// execute the rest of the code in the override.
        /// </summary>
        public override void Init(IContext context)
        {
            // Initialize base before executing the rest of the code in this method
            base.Init(context);

            // Delegate to the method of the TimeZoneKey and cache the result in private field
            dateTimeZone_ = new TimeZoneKey() {TimeZoneName = TimeZoneName}.GetDateTimeZone();
        }

        /// <summary>
        /// Returns NodaTime timezone object used for conversion between
        /// UTC and local date, time, minute, and datetime.
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
        ///
        /// Because TimeZoneName is used to look up timezone conventions,
        /// it must match either the string UTC or the code in IANA
        /// timezone database precisely. The IANA city timezone code
        /// has two slash-delimited tokens, the first referencing the
        /// country and the other the city, for example America/New_York.
        /// </summary>
        public DateTimeZone GetDateTimeZone() { return dateTimeZone_; }
    }
}
