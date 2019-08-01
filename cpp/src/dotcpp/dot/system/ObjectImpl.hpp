﻿/*
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
#include <dot/system/ptr.hpp>

namespace dot
{
    class object;
    class string_impl; class string;
    class type_impl; using type_t = ptr<type_impl>;

    /// <summary>
    /// All classes with reference semantics should derive from this type.
    /// It works with ptr to provide an emulation of reference semantics in C++.
    /// </summary>
    class DOT_CLASS object_impl : public reference_counter
    {
        template<typename T>
        friend class ptr;
        friend class object;

    public: // METHODS

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        ///
        /// Default implementation in object compares pointers. Derived classes
        /// can override this method to compare by value.
        /// </summary>
        virtual bool equals(object obj);

        /// <summary>
        /// Serves as the default hash function.
        ///
        /// Default implementation in object uses hash based on the pointer.
        /// Derived classes can override this method to provide value based hash.
        ///
        /// Methods Equals() and hash_code() must always be overriden together
        /// to avoid the situation when objects are equal but hash is not.
        /// </summary>
        virtual size_t hash_code();

        /// <summary>Gets the type_t of the current instance.</summary>
        virtual type_t type();

        /// <summary>Gets the type_t of the object_impl.</summary>
        static type_t typeof();

        /// <summary>
        /// string that represents the current object.
        ///
        /// Default implementation in object returns full name
        /// of the class by calling type().FullName. Derived types
        /// can override this method to provide custom conversion
        /// to string.
        /// </summary>
        virtual string to_string();
    };
}
