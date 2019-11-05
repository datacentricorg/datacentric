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
    /// This class provides timezone conversion between UTC
    /// and local datetime for the specified city.
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
    /// Because CityName is used to look up timezone conventions,
    /// it must match either the string UTC or the code in IANA
    /// timezone database precisely. The IANA city timezone code
    /// has two slash-delimited tokens, the first referencing the
    /// country and the other the city, for example America/New_York.
    /// </summary>
    [Configurable]
    public class City : TypedRecord<CityKey, City>
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
        /// Because CityName is used to look up timezone conventions,
        /// it must match either the string UTC or the code in IANA
        /// timezone database precisely. The IANA city timezone code
        /// has two slash-delimited tokens, the first referencing the
        /// country and the other the city, for example America/New_York.
        /// </summary>
        [BsonRequired]
        public string CityName { get; set; }

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

            // Delegate to the method of the CityKey and cache the result in private field
            dateTimeZone_ = new CityKey() {CityName = CityName}.GetDateTimeZone();
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
        /// Because CityName is used to look up timezone conventions,
        /// it must match either the string UTC or the code in IANA
        /// timezone database precisely. The IANA city timezone code
        /// has two slash-delimited tokens, the first referencing the
        /// country and the other the city, for example America/New_York.
        /// </summary>
        public DateTimeZone GetDateTimeZone() { return dateTimeZone_; }

        //--- KEYS

        /// <summary>Special city code for UTC timezone.</summary>
        public static CityKey Utc { get; } = new CityKey() { CityName = "UTC" };

        /// <summary>New York City</summary>
        public static CityKey Nyc { get; } = new CityKey() { CityName = "NYC" };

        /// <summary>London</summary>
        public static CityKey London { get; } = new CityKey() { CityName = "London" };

        //--- STATIC

        /// <summary>
        /// This method will be invoked by context.Configure() for every
        /// class that is accessible by the executing assembly and marked
        /// with [Configurable] attribute.
        ///
        /// The method Configure(context) may be used to configure:
        ///
        /// * Reference data, and
        /// * In case of test mocks, test data
        ///
        /// The order in which Configure(context) method is invoked when
        /// multiple classes marked by [Configurable] attribute are present
        /// is undefined. The implementation of Configure(context) should
        /// not rely on any existing data, and should not invoke other
        /// Configure(context) method of other classes.
        ///
        /// The attribute [Configurable] is not inherited. To invoke
        /// Configure(context) method for multiple classes within the same
        /// inheritance chain, specify [Configurable] attribute for each
        /// class that provides Configure(context) method.
        /// </summary>
        public static void Configure(IContext context)
        {
            // Create ticker kind records
            var result = new List<City>
            {
                new City
                {
                    CityName = City.Utc.CityName
                },
                new City
                {
                    CityName = City.Nyc.CityName
                },
                new City
                {
                    CityName = City.London.CityName
                }
            };
            context.SaveMany(result);
        }
    }
}
