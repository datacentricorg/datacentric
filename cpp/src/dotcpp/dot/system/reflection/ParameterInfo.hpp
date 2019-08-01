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

#include <dot/system/reflection/MemberInfo.hpp>

namespace dot
{
    class ParameterInfoImpl; using ParameterInfo = ptr<ParameterInfoImpl>;

    /// <summary>
    /// Discovers the attributes of a parameter and provides access to parameter metadata.
    /// </summary>
    class ParameterInfoImpl : public virtual object_impl
    {
        friend ParameterInfo new_ParameterInfo(string , type_t, int);

        typedef ParameterInfoImpl self;

    public: // METHODS

        /// <summary>Gets the type of this parameter.</summary>
        type_t ParameterType; // TODO - convert to method

        /// <summary>Gets the name of this parameter.</summary>
        string Name;  // TODO - convert to method

        /// <summary>Gets the zero-based position of the parameter in the formal parameter list.</summary>
        int Position;  // TODO - convert to method

    private: // CONSTRUCTORS

        /// <summary>
        /// Create from parameter name, parameter type, and parameter position.
        ///
        /// This constructor is private. Use new_ParameterInfo(...)
        /// function with matching signature instead.
        /// </summary>
        ParameterInfoImpl(string name, type_t parameter_type, int position)
        {
            ParameterType = parameter_type;
            Name = name;
            Position = position;
        }
    };

    /// <summary>
    /// Create from parameter name, parameter type, and parameter position.
    /// </summary>
    inline ParameterInfo new_ParameterInfo(string name, type_t parameterType, int position)
    {
        return new ParameterInfoImpl(name, parameterType, position);
    }
}