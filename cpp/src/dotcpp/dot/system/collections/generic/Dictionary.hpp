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

#include <unordered_map>
#include <dot/system/Exception.hpp>
#include <dot/system/collections/generic/IDictionary.hpp>
#include <dot/system/collections/generic/List.hpp>

namespace dot
{
    template <class TKey, class TValue> class DictionaryImpl;
    template <class TKey, class TValue> using Dictionary = Ptr<DictionaryImpl<TKey, TValue>>;

    /// <summary>Represents a collection of keys and values.</summary>
    template <class TKey, class TValue>
    class DictionaryImpl
        : public virtual ObjectImpl
        , public IDictionaryImpl<TKey, TValue>
        , public std::unordered_map<TKey, TValue>
    {
        typedef DictionaryImpl<TKey, TValue> self;
        typedef std::unordered_map<TKey, TValue> base;

        template <class TKey_, class TValue_>
        friend Dictionary<TKey_, TValue_> new_Dictionary();

    private: // CONSTRUCTORS

        /// <summary>
        /// Initializes a new instance of Dictionary.
        ///
        /// This constructor is private. Use new_Dictionary() function instead.
        /// </summary>
        DictionaryImpl() : base() {}

    public: // PROPERTIES

        /// <summary>Gets the number of key/value pairs contained in the Dictionary.</summary>
        DOT_IMPL_GET(int, Count, { return this->size(); })

        /// <summary>Gets a collection containing the keys in the Dictionary.</summary>
        DOT_IMPL_GET(ICollection<TKey>, Keys, {
            ICollection<TKey> list = new_List<TKey>();
            for (auto& x : *this) list->Add(x.first);
            return list;
        })

        /// <summary>Gets a collection containing the values in the Dictionary.</summary>
        DOT_IMPL_GET(ICollection<TValue>, Values, {
            ICollection<TValue> list = new_List<TValue>();
            for (auto& x : *this) list->Add(x.second);
            return list;
        })

    public: // METHODS

        /// <summary>Adds the specified key and value to the dictionary.</summary>
        virtual void Add(const TKey& key, const TValue& value) override
        {
            this->Add(KeyValuePair<TKey, TValue>(key, value));
        }

        /// <summary>Adds the specified value to the ICollection with the specified key.</summary>
        virtual void Add(const KeyValuePair<TKey, TValue>& keyValuePair) override
        {
            auto res = this->insert(keyValuePair);
            if (!res.second)
                throw new_Exception("An element with the same key already exists in the Dictionary");
        }

        /// <summary>Removes all keys and values from the Dictionary.</summary>
        virtual void Clear() override
        {
            this->clear();
        }

        /// <summary>Determines whether the ICollection contains a specific key and value.</summary>
        virtual bool Contains(const KeyValuePair<TKey, TValue>& keyValuePair) override
        {
            auto iter = this->find(keyValuePair.first);
            if (iter != end())
            {
                return std::equal_to<TValue>()(keyValuePair.second, iter->second);
            }
            return false;
        }

        /// <summary>Determines whether the Dictionary contains the specified key.</summary>
        virtual bool ContainsKey(const TKey& key) override
        {
            return this->find(key) != end();
        }

        /// <summary>Determines whether the Dictionary contains a specific value.</summary>
        virtual bool ContainsValue(const TValue& value)
        {
            for (auto& x : *this)
            {
                if (std::equal_to<TValue>()(x.second, value))
                    return true;
            }
            return false;
        }

        /// <summary>Returns an enumerator that iterates through the Dictionary.</summary>
        virtual IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() override
        {
           return new_Enumerator(base::begin(), base::end());
        }

        /// <summary>Returns random access begin iterator of the underlying std::unordered_map.</summary>
        typename base::iterator begin() { return base::begin(); }

        /// <summary>Returns random access end iterator of the underlying std::unordered_map.</summary>
        typename base::iterator end() { return base::end(); }

        /// <summary>Removes the value with the specified key from the Dictionary.</summary>
        virtual bool Remove(const TKey& key) override
        {
            return this->erase(key) != 0;
        }

        /// <summary>Removes a key and value from the dictionary.</summary>
        virtual bool Remove(const KeyValuePair<TKey, TValue>& keyValuePair) override
        {
            auto iter = this->find(keyValuePair.first);
            if (iter != end() && std::equal_to<TValue>()(keyValuePair.second, iter->second))
            {
                this->erase(iter);
                return true;
            }
            return false;
        }

        /// <summary>Gets the value associated with the specified key.</summary>
        bool TryGetValue(const TKey& key, TValue& value)
        {
            auto iter = this->find(key);
            if (iter != end())
            {
                value = iter->second;
                return true;
            }
            return false;
        }

    public: // OPERATORS

        /// <summary>Gets or sets the value associated with the specified key.</summary>
        virtual TValue& operator[](const TKey& key) override
        {
            return base::operator[](key);
        }
    };

    /// <summary>Initializes a new instance of Dictionary.</summary>
    template <class TKey, class TValue>
    inline Dictionary<TKey, TValue> new_Dictionary() { return new DictionaryImpl<TKey, TValue>(); }
}
