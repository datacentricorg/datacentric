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
    /// <summary>Default implementation of ISettings that takes effect if settings are not initialized otherwise.</summary>
    public class DefaultSettings : ISettings
    {
        /// <summary>Create temporary context with logging to the current output.</summary>
        public DefaultSettings()
        {
            ClassMap = new DefaultClassMapSettings(this);
            Locale = new DefaultLocaleSettings(this);
        }

        /// <summary>Class map settings.</summary>
        public IClassMapSettings ClassMap { get; }

        /// <summary>Locale settings.</summary>
        public ILocaleSettings Locale { get; }
    }
}

