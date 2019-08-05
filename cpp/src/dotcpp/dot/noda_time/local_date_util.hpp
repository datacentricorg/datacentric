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

#include <dot/declare.hpp>
#include <dot/noda_time/local_date.hpp>

namespace dot
{
    class DOT_CLASS local_date_util
    {
    public: // STATIC

        /// <summary>Parse string using standard ISO 8601 date pattern yyyy-mm-dd, throw if invalid format.
        /// No variations from the standard format are accepted and no delimiters can be changed or omitted.
        /// Specifically, ISO int-like string using yyyymmdd format without delimiters is not accepted.</summary>
        static dot::local_date parse(dot::string value);

        /// <summary>Convert local_date to to ISO 8601 8 digit int in yyyymmdd format.</summary>
        static int to_iso_int(dot::local_date value);

        /// <summary>Parse ISO 8601 8 digit int in yyyymmdd format, throw if invalid format.</summary>
        static dot::local_date parse_iso_int(int value);
    };
}
