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

#define RAPIDJSON_HAS_STDSTRING 1
#include <dc/implement.hpp>
#include <dc/serialization/JsonWriter.hpp>
#include <dot/system/Array1D.hpp>
#include <dot/system/Enum.hpp>
#include <dot/system/String.hpp>
#include <dot/system/Object.hpp>
#include <dot/system/Type.hpp>
#include <dot/system/collections/IObjectEnumerable.hpp>
#include <dot/noda_time/LocalDate.hpp>
#include <dot/noda_time/LocalTime.hpp>
#include <dot/noda_time/LocalMinute.hpp>
#include <dot/noda_time/LocalDateTime.hpp>
#include <dc/platform/data_source/mongo/ObjectId.hpp>
#include <dc/types/local_date/LocalDate.hpp>
#include <dc/types/local_time/LocalTime.hpp>
#include <dc/types/local_minute/LocalMinute.hpp>
#include <dc/types/local_date_time/LocalDateTime.hpp>

namespace dc
{
    JsonWriterImpl::JsonWriterImpl()
        : jsonWriter_(buffer_)
        , currentState_(TreeWriterState::Empty)
    {}

    void JsonWriterImpl::WriteStartDocument(dot::string rootElementName)
    {
        // Push state and name into the element stack. Writing the actual start tag occurs inside
        // one of WriteStartDict, WriteStartArrayItem, or WriteStartValue calls.
        elementStack_.push({ rootElementName, currentState_ });

        if (currentState_ == TreeWriterState::Empty && elementStack_.size() == 1)
        {
            currentState_ = TreeWriterState::DocumentStarted;
        }
        else
            throw dot::exception(
                "A call to WriteStartDocument(...) must be the first call to the tree writer.");
    }

    void JsonWriterImpl::WriteEndDocument(dot::string rootElementName)
    {
        // Check state transition matrix
        if (currentState_ == TreeWriterState::DictCompleted && elementStack_.size() == 1)
        {
            currentState_ = TreeWriterState::DocumentCompleted;
        }
        else
            throw dot::exception(
                "A call to WriteEndDocument(...) does not follow  WriteEndElement(...) at at root level.");

        // Pop the outer element name and state from the element stack
        dot::string currentElementName;
        std::pair<dot::string, TreeWriterState> top = elementStack_.top();
        elementStack_.pop();
        currentElementName = top.first;
        currentState_ = top.second;

        // Check that the current element name matches the specified name. Writing the actual end tag
        // occurs inside one of WriteStartDict, WriteStartArrayItem, or WriteStartValue calls.
        if (rootElementName != currentElementName)
            throw dot::exception(dot::string::Format(
                "WriteEndDocument({0}) follows WriteStartDocument({1}), root element name mismatch.", rootElementName, currentElementName));
    }

    void JsonWriterImpl::WriteStartElement(dot::string elementName)
    {
        // Push state and name into the element stack. Writing the actual start tag occurs inside
        // one of WriteStartDict, WriteStartArrayItem, or WriteStartValue calls.
        elementStack_.push({ elementName, currentState_ });

        if (currentState_ == TreeWriterState::DocumentStarted) currentState_ = TreeWriterState::ElementStarted;
        else if (currentState_ == TreeWriterState::ElementCompleted) currentState_ = TreeWriterState::ElementStarted;
        else if (currentState_ == TreeWriterState::DictStarted) currentState_ = TreeWriterState::ElementStarted;
        else if (currentState_ == TreeWriterState::DictArrayItemStarted) currentState_ = TreeWriterState::ElementStarted;
        else
            throw dot::exception(
                "A call to WriteStartElement(...) must be the first call or follow WriteEndElement(prevName).");

        // Write "elementName" :
        jsonWriter_.Key(*elementStack_.top().first);
    }

    void JsonWriterImpl::WriteEndElement(dot::string elementName)
    {
        // Check state transition matrix
        if (currentState_ == TreeWriterState::ElementStarted) currentState_ = TreeWriterState::ElementCompleted;
        else if (currentState_ == TreeWriterState::DictCompleted) currentState_ = TreeWriterState::ElementCompleted;
        else if (currentState_ == TreeWriterState::ValueCompleted) currentState_ = TreeWriterState::ElementCompleted;
        else if (currentState_ == TreeWriterState::ArrayCompleted) currentState_ = TreeWriterState::ElementCompleted;
        else throw dot::exception(
            "A call to WriteEndElement(...) does not follow a matching WriteStartElement(...) at the same indent level.");

        // Pop the outer element name and state from the element stack
        //(currentElementName, currentState_) = elementStack_.Pop();
        dot::string currentElementName;
        std::pair<dot::string, TreeWriterState> top = elementStack_.top();
        elementStack_.pop();
        currentElementName = top.first;
        currentState_ = top.second;

        // Check that the current element name matches the specified name. Writing the actual end tag
        // occurs inside one of WriteStartDict, WriteStartArrayItem, or WriteStartValue calls.
        if (elementName != currentElementName)
            throw dot::exception(
                dot::string::Format("EndComplexElement({0}) follows StartComplexElement({1}), element name mismatch.", elementName, currentElementName));

        // Nothing to write here but array closing bracket was written above
    }

    void JsonWriterImpl::WriteStartDict()
    {
        // Save initial state to be used below
        TreeWriterState prevState = currentState_;

        // Check state transition matrix
        if (currentState_ == TreeWriterState::DocumentStarted) currentState_ = TreeWriterState::DictStarted;
        else if (currentState_ == TreeWriterState::ElementStarted) currentState_ = TreeWriterState::DictStarted;
        else if (currentState_ == TreeWriterState::ArrayItemStarted) currentState_ = TreeWriterState::DictArrayItemStarted;
        else
            throw dot::exception(
                "A call to WriteStartDict() must follow WriteStartElement(...) or WriteStartArrayItem().");

        // Write {
        jsonWriter_.StartObject();

        // If prev state is DocumentStarted, write _t tag
        //if (prevState == TreeWriterState::DocumentStarted)
        //{
        //    dot::string rootElementName = elementStack_.top().first;
        //    if (!rootElementName->EndsWith("Key"))  // TODO remove it
        //        this->WriteValueElement("_t", rootElementName);
        //}
    }

    void JsonWriterImpl::WriteEndDict()
    {
        // Check state transition matrix
        if (currentState_ == TreeWriterState::DictStarted) currentState_ = TreeWriterState::DictCompleted;
        else if (currentState_ == TreeWriterState::DictArrayItemStarted) currentState_ = TreeWriterState::DictArrayItemCompleted;
        else if (currentState_ == TreeWriterState::ElementCompleted) currentState_ = TreeWriterState::DictCompleted;
        else
            throw dot::exception(
                "A call to WriteEndDict(...) does not follow a matching WriteStartDict(...) at the same indent level.");

        // Write }
        jsonWriter_.EndObject();
    }

    void JsonWriterImpl::WriteStartArray()
    {
        // Check state transition matrix
        if (currentState_ == TreeWriterState::ElementStarted) currentState_ = TreeWriterState::ArrayStarted;
        else
            throw dot::exception(
                "A call to WriteStartArray() must follow WriteStartElement(...).");

        // Write [
        jsonWriter_.StartArray();
    }

    void JsonWriterImpl::WriteEndArray()
    {
        // Check state transition matrix
        if (currentState_ == TreeWriterState::ArrayStarted) currentState_ = TreeWriterState::ArrayCompleted;
        else if (currentState_ == TreeWriterState::ArrayItemCompleted) currentState_ = TreeWriterState::ArrayCompleted;
        else
            throw dot::exception(
                "A call to WriteEndArray(...) does not follow WriteEndArrayItem(...).");

        // Write ]
        jsonWriter_.EndArray();
    }

    void JsonWriterImpl::WriteStartArrayItem()
    {
        // Check state transition matrix
        if (currentState_ == TreeWriterState::ArrayStarted) currentState_ = TreeWriterState::ArrayItemStarted;
        else if (currentState_ == TreeWriterState::ArrayItemCompleted) currentState_ = TreeWriterState::ArrayItemStarted;
        else throw dot::exception(
            "A call to WriteStartArrayItem() must follow WriteStartElement(...) or WriteEndArrayItem().");

        // Nothing to write here
    }

    void JsonWriterImpl::WriteEndArrayItem()
    {
        // Check state transition matrix
        if (currentState_ == TreeWriterState::ArrayItemStarted) currentState_ = TreeWriterState::ArrayItemCompleted;
        else if (currentState_ == TreeWriterState::DictArrayItemCompleted) currentState_ = TreeWriterState::ArrayItemCompleted;
        else if (currentState_ == TreeWriterState::ValueArrayItemCompleted) currentState_ = TreeWriterState::ArrayItemCompleted;
        else
            throw dot::exception(
                "A call to WriteEndArrayItem(...) does not follow a matching WriteStartArrayItem(...) at the same indent level.");

        // Nothing to write here
    }

    void JsonWriterImpl::WriteStartValue()
    {
        // Check state transition matrix
        if (currentState_ == TreeWriterState::ElementStarted) currentState_ = TreeWriterState::ValueStarted;
        else if (currentState_ == TreeWriterState::ArrayItemStarted) currentState_ = TreeWriterState::ValueArrayItemStarted;
        else
            throw dot::exception(
                "A call to WriteStartValue() must follow WriteStartElement(...) or WriteStartArrayItem().");

        // Nothing to write here
    }

    void JsonWriterImpl::WriteEndValue()
    {
        // Check state transition matrix
        if (currentState_ == TreeWriterState::ValueWritten) currentState_ = TreeWriterState::ValueCompleted;
        else if (currentState_ == TreeWriterState::ValueArrayItemWritten) currentState_ = TreeWriterState::ValueArrayItemCompleted;
        else
            throw dot::exception(
                "A call to WriteEndValue(...) does not follow a matching WriteValue(...) at the same indent level.");

        // Nothing to write here
    }

    void JsonWriterImpl::WriteValue(dot::object value)
    {
        // Check state transition matrix
        if (currentState_ == TreeWriterState::ValueStarted) currentState_ = TreeWriterState::ValueWritten;
        else if (currentState_ == TreeWriterState::ValueArrayItemStarted) currentState_ = TreeWriterState::ValueArrayItemWritten;
        else
            throw dot::exception(
                "A call to WriteEndValue(...) does not follow a matching WriteValue(...) at the same indent level.");

        if (value.IsEmpty())
        {
            // Null or empty value is serialized as null JSON value.
            // We should only get her for an array as for dictionaries
            // null values should be skipped
            jsonWriter_.Null();
            return;
        }

        // Serialize based on value type
        dot::type_t valueType = value->GetType();

        if (valueType->Equals(dot::typeof<dot::string>()))
            jsonWriter_.String(*(dot::string)value);
        else
        if (valueType->Equals(dot::typeof<double>())) // ? TODO check dot::typeof<Double>() dot::typeof<NullableDouble>()
            jsonWriter_.Double((double)value);
        else
        if (valueType->Equals(dot::typeof<bool>()))
            jsonWriter_.Bool((bool)value);
        else
        if (valueType->Equals(dot::typeof<int>()))
            jsonWriter_.Int((int)value);
        else
        if (valueType->Equals(dot::typeof<int64_t>()))
            jsonWriter_.Int64((int64_t)value);
        else
        if (valueType->Equals(dot::typeof<dot::local_date>()))
            jsonWriter_.Int(LocalDateHelper::ToIsoInt((dot::local_date)value));
        else
        if (valueType->Equals(dot::typeof<dot::local_time>()))
            jsonWriter_.Int(LocalTimeHelper::ToIsoInt((dot::local_time)value));
        else
        if (valueType->Equals(dot::typeof<dot::local_minute>()))
            jsonWriter_.Int(LocalMinuteHelper::ToIsoInt((dot::local_minute) value));
        else
        if (valueType->Equals(dot::typeof<dot::local_date_time>()))
            jsonWriter_.Int64(LocalDateTimeHelper::ToIsoLong((dot::local_date_time)value));
        else
        if (valueType->Equals(dot::typeof<ObjectId>()))
            jsonWriter_.String(*value->ToString());
        else
        if (valueType->IsEnum)
            jsonWriter_.String(*value->ToString());
        //else
        //if (valueType.is<Object>()) // TODO KeyType
        //    jsonWriter_.String(*valueType->ToString()); // TODO semicolonDelimitedKeyString = keyElement.AsString();
        else
            throw dot::exception(dot::string::Format("Element type {0} is not supported for JSON serialization.", valueType));
    }

    dot::string JsonWriterImpl::ToString()
    {
        return buffer_.GetString();
    }
}
