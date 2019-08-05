/*
Copyright (C) 2015-present The DotCpp Authors.

This file is part of .C++, a native C++ implementation of
popular .NET class library APIs developed to facilitate
code reuse between C# and C++.

    http://github.com/dotcpp/dotcpp (source)
    http://dotcpp.org (documentation)

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
#include <dot/system/objectimpl.hpp>

namespace dot
{
    /// <summary>Wrapper around bool to make it convertible to object (boxing).</summary>
    class bool_impl : public virtual object_impl
    {
        friend object;
        bool value_;

    public: // CONSTRUCTORS

        /// <summary>Create from value (box).</summary>
        bool_impl(bool value) : value_(value) {}

    public: //  CONSTANTS

        /// <summary>Sentinel value representing uninitialized state.</summary>
        static constexpr int empty = INT32_MIN;

        /// <summary>Represents the Boolean value false as a string. This field is read-only.</summary>
        static const string false_string;

        /// <summary>Represents the Boolean value true as a string. This field is read-only.</summary>
        static const string true_string;

    public: // STATIC

        /// <summary>Converts the specified string representation of a logical value to its Boolean equivalent.</summary>
        static bool parse(string s);

    public: // METHODS

        /// <summary>Returns a value indicating whether this instance is equal to a specified object.</summary>
        bool equals(object obj) override;

        /// <summary>Returns the hash code for this instance.</summary>
        virtual size_t hash_code() override;

        /// <summary>Converts the value of this instance to its equivalent string representation (either "True" or "False").</summary>
        virtual string to_string() override;

        static type_t typeof();
        virtual type_t type() override;
    };

}
