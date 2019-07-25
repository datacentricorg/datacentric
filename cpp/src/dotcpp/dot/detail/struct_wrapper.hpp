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

namespace dot
{
    class ObjectImpl;
    class String;
    class Object;
    class ObjectImpl;

    namespace detail
    {
        /// <summary>Empty structure.</summary>
        class dummy_no_to_string : public virtual ObjectImpl
        {};

        /// <summary>
        /// Objects inherit this structure in case their inner struct class has method ToString
        /// so object also have these method.
        /// </summary>
        template <class W, class T>
        class obj_to_string : public virtual ObjectImpl
        {
        public:
            virtual String ToString() override { return static_cast<T*>(static_cast<W*>(this))->ToString(); }
        };

        /// <summary>Detects existance of ToString method.</summary>
        template<class T>
        struct has_to_string
        {
        private:
            static dummy_no_to_string detect(...);
            template<class U> static decltype(std::declval<U>().ToString()) detect(const U&);
        public:
            static constexpr bool value = !std::is_same<dummy_no_to_string, decltype(detect(std::declval<T>()))>::value;
            typedef std::integral_constant<bool, value> type;
        };

        /// <summary>For inheritance of ToString method.</summary>
        template<class W, class T>
        class inherit_to_string : public std::conditional<
                has_to_string<T>::value,
                obj_to_string<W, T>,
                dummy_no_to_string
            >::type
        {};

        /// <summary>Empty structure.</summary>
        class dummy_no_get_hashcode : public virtual ObjectImpl
        {};

        /// <summary>
        /// Objects inherit this structure in case their inner struct class has method GetHashCode
        /// so object also have these method.
        /// </summary>
        template <class W, class T>
        class obj_get_hashcode : public virtual ObjectImpl
        {
        public:
            virtual size_t GetHashCode() override { return static_cast<T*>(static_cast<W*>(this))->GetHashCode(); }
        };

        /// <summary>Detects existance of GetHashCode method.</summary>
        template<class T>
        struct has_get_hashcode
        {
        private:
            static dummy_no_get_hashcode detect(...);
            template<class U> static decltype(std::declval<U>().GetHashCode()) detect(const U&);
        public:
            static constexpr bool value = !std::is_same<dummy_no_get_hashcode, decltype(detect(std::declval<T>()))>::value;
            typedef std::integral_constant<bool, value> type;
        };

        /// <summary>For inheritance of GetHashCode method.</summary>
        template<class W, class T>
        class inherit_get_hashcode : public std::conditional<
            has_get_hashcode<T>::value,
            obj_get_hashcode<W, T>,
            dummy_no_get_hashcode
        >::type
        {};

        /// <summary>Empty structure.</summary>
        class dummy_no_equals : public virtual ObjectImpl
        {};

        /// <summary>
        /// Objects inherit this structure in case their inner struct class has method Equals
        /// so object also have these method.
        /// </summary>
        template <class W, class T>
        class obj_equals : public virtual ObjectImpl
        {
        public:
            virtual bool Equals(Object obj) override { return static_cast<T*>(static_cast<W*>(this))->Equals(obj); }
        };

        /// <summary>Detects existance of Equals method.</summary>
        template<class T>
        struct has_equals
        {
        private:
            static dummy_no_equals detect(...);
            template<class U> static decltype(std::declval<U>().Equals(std::declval<Object>())) detect(const U&);
        public:
            static constexpr bool value = !std::is_same<dummy_no_equals, decltype(detect(std::declval<T>()))>::value;
            typedef std::integral_constant<bool, value> type;
        };

        /// <summary>For inheritance of Equals method.</summary>
        template<class W, class T>
        class inherit_equals : public std::conditional<
            has_equals<T>::value,
            obj_equals<W, T>,
            dummy_no_equals
        >::type
        {};
    }
}
