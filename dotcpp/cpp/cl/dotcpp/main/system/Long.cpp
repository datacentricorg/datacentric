/*
Copyright (C) 2003-present CompatibL

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

#include <cl/dotcpp/main/implement.hpp>
#include <cl/dotcpp/main/system/Long.hpp>
#include <cl/dotcpp/main/system/String.hpp>
#include <cl/dotcpp/main/system/Type.hpp>

namespace cl
{
    bool LongImpl::Equals(Object obj)
    {
        if (this == &(*obj)) return true;

        if (obj.is<Ptr<LongImpl>>())
        {
            return value_ == obj.as<Ptr<LongImpl>>()->value_;
        }

        return false;
    }

    size_t LongImpl::GetHashCode()
    {
        return std::hash<int64_t>()(value_);
    }

    String LongImpl::ToString()
    {
        return std::to_string(value_);
    }

    Type LongImpl::typeof()
    {
        return cl::typeof<int64_t>();
    }

    Type LongImpl::GetType()
    {
        return typeof();
    }
}
