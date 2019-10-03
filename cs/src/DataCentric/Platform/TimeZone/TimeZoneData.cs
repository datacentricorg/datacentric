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
using MongoDB.Bson;
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
    public class TimeZoneData : TypedRecord<TimeZoneKey, TimeZoneData>
    {
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

        /// <summary>
        /// NodaTime timezone object used for conversion between
        /// UTC and local date, time, minute, and datetime.
        ///
        /// This property is set in Init(...) method based on TimeZoneName.
        /// Because TimeZoneName is used to look up timezone conventions,
        /// it must match the code in IANA timezone database precisely.
        ///
        /// The IANA city timezone code has two slash-delimited tokens,
        /// the first referencing the country and the other the city, for
        /// example America/New_York.
        /// </summary>
        [BsonRequired]
        public DateTimeZone TimeZone { get; private set; }

        /// <summary>
        /// Set context and perform initialization or validation of object data.
        ///
        /// All derived classes overriding this method must call base.Init(context)
        /// before executing the the rest of the code in the method override.
        /// </summary>
        public override void Init(IContext context)
        {
            base.Init(context);

            // Check that TimeZoneName is set
            if (!TimeZoneName.HasValue()) throw new Exception("TimeZoneName is not set.");

            if (TimeZoneName != "UTC" && !TimeZoneName.Contains("/"))
                throw new Exception(
                    $"TimeZoneName={TimeZoneName} is not UTC and is not a forward slash  " +
                    $"delimited city timezone. Only (a) UTC timezone and (b) IANA TZDB " + 
                    $"city timezones such as America/New_York are permitted " +
                    $"as TimeZoneName values, but not three-symbol timezones without " +
                    $"delimiter such as EST or EDT that do not handle the switch " +
                    $"between winter and summer time automatically when winter time " +
                    $"is defined.");

            // Initialize TimeZone property
            TimeZone = DateTimeZoneProviders.Tzdb.GetZoneOrNull(TimeZoneName);

            // If still null after initialization, TimeZoneName
            // was not found in the IANA database of city codes
            if (TimeZone == null)
                throw new Exception($"TimeZoneName={TimeZoneName} not found in IANA TZDB timezone database.");
        }
    }
}
