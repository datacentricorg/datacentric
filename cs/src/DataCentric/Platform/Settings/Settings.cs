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
    /// <summary>Provides low level settings that cannot be stored as a record because
    /// they are needed for the storage and activator frameworks to work.</summary>
    public static class Settings
    {
        [ThreadStatic] private static ISettings default_; // Do not initialize ThreadStatic here, initializer will run in first thread only

        /// <summary>Default settings.</summary>
        public static ISettings Default
        {
            get
            {
                if (default_ == null)
                {
                    // If default context is not yet initialized, initialize it to output context
                    // Console context provides logging to the output but no other functionality.
                    // It does not support loading or saving objects to in-memory cache or storage.
                    default_ = new DefaultSettings();
                }

                return default_;
            }
            set { default_ = value; }
        }
    }
}

