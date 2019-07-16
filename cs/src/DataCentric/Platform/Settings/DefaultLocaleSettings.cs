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

namespace DataCentric
{
    /// <summary>Default locale settings are based on .NET universal locale.</summary>
    public class DefaultLocaleSettings : ILocaleSettings
    {
        /// <summary>Default locale settings are based on .NET universal locale.</summary>
        public DefaultLocaleSettings(ISettings settings)
        {
            Settings = settings;

            ListSeparator = ',';
            QuoteSymbol = '\"';
        }

        /// <summary>Provides access to other settings of the same root settings object.</summary>
        public ISettings Settings { get; }

        /// <summary>List separator for the universal locale is comma.</summary>
        public char ListSeparator { get; }

        /// <summary>Quote symbol for the universal locale is double quote.</summary>
        public char QuoteSymbol { get; }
    }
}
