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

#include <dc/implement.hpp>
#include <dc/serialization/TupleWriter.hpp>
#include <dc/types/local_time/LocalTime.hpp>
#include <dc/types/local_minute/LocalMinute.hpp>
#include <dc/types/local_date/LocalDate.hpp>
#include <dc/types/local_date_time/LocalDateTime.hpp>
#include <dc/types/record/KeyType.hpp>
#include <dot/system/Enum.hpp>
#include <dot/system/reflection/Activator.hpp>
#include <dot/noda_time/LocalTime.hpp>
#include <dot/noda_time/LocalMinute.hpp>
#include <dot/noda_time/LocalDate.hpp>
#include <dot/noda_time/LocalDateTime.hpp>
#include <dc/serialization/DataWriter.hpp>

namespace dc
{
    void TupleWriterImpl::WriteStartDocument(dot::string rootElementName)
    {

    }

    void TupleWriterImpl::WriteEndDocument(dot::string rootElementName)
    {

    }

    void TupleWriterImpl::WriteStartElement(dot::string elementName)
    {
        if (dataWriter_ != nullptr)
        {
            dataWriter_->WriteStartElement(elementName);
        }
        else
        {
            for (int i = 0; i < props_->count(); ++i)
            {
                if (elementName == "_key" || elementName == "_id")
                {
                    indexOfCurrent_ = -1;
                    return;
                }

                if (props_[i]->name == elementName)
                {
                    indexOfCurrent_ = i;
                    if (props_[i]->field_type->name->EndsWith("Data")) //! TODO change EndsWith
                    {
                        Data result = (Data)dot::Activator::CreateInstance(props_[i]->field_type);
                        dataWriter_ = new_DataWriter(result);
                        dataWriter_->WriteStartDocument(props_[i]->field_type->name);

                        tuple_->type()->GetMethod("SetItem")->Invoke(tuple_, dot::new_Array1D<dot::object>({ tuple_, indexOfCurrent_, result }));

                        //dataWriter_->WriteStartElement(elementName);
                        //DeserializeDocument(doc, writer);
                        //writer->WriteEndDocument(typeName);
                    }
                    if (! props_[i]->field_type->GetInterface("IObjectEnumerable").IsEmpty())
                    {
                        dataWriter_ = new_DataWriter(nullptr);
                        dataWriter_->currentElementInfo_ = props_[i];
                        dataWriter_->currentElementName_ = props_[i]->name;
                        dataWriter_->currentState_ = TreeWriterState::ElementStarted;

                        dataWriter_->currentArray_ = dot::new_List<dot::IObjectCollection>();

                        //dataWriter_->WriteStartElement(elementName);
                        //DeserializeDocument(doc, writer);
                        //writer->WriteEndDocument(typeName);
                    }

                    return;
                }
            }
            throw dot::exception(dot::string::format("Unknown element {0} in tuple writer.", elementName));
        }
    }

    void TupleWriterImpl::WriteEndElement(dot::string elementName)
    {
        if (dataWriter_ != nullptr)
        {
            dataWriter_->WriteEndElement(elementName);
        }
    }

    void TupleWriterImpl::WriteStartDict()
    {
        if (dataWriter_ != nullptr)
        {
            dataWriter_->WriteStartDict();
        }

    }

    void TupleWriterImpl::WriteEndDict()
    {
        if (dataWriter_ != nullptr)
        {
            dataWriter_->WriteEndDict();
            if (dataWriter_->currentState_ == TreeWriterState::DocumentStarted)
                dataWriter_ = nullptr;
        }
    }

    void TupleWriterImpl::WriteStartArray()
    {
        if (dataWriter_ != nullptr)
        {
            dataWriter_->WriteStartArray();
        }

    }

    void TupleWriterImpl::WriteEndArray()
    {
        if (dataWriter_ != nullptr)
        {
            tuple_->type()->GetMethod("SetItem")->Invoke(tuple_, dot::new_Array1D<dot::object>({ tuple_, indexOfCurrent_, dataWriter_->currentArray_ }));
            dataWriter_->WriteEndArray();
            dataWriter_ = nullptr;
        }

    }

    void TupleWriterImpl::WriteStartArrayItem()
    {
        if (dataWriter_ != nullptr)
        {
            dataWriter_->WriteStartArrayItem();
        }

    }

    void TupleWriterImpl::WriteEndArrayItem()
    {
        if (dataWriter_ != nullptr)
        {
            dataWriter_->WriteEndArrayItem();
        }

    }

    void TupleWriterImpl::WriteStartValue()
    {
        if (dataWriter_ != nullptr)
        {
            dataWriter_->WriteStartValue();
        }

    }

    void TupleWriterImpl::WriteEndValue()
    {
        if (dataWriter_ != nullptr)
        {
            dataWriter_->WriteEndValue();
        }

    }

    void TupleWriterImpl::WriteValue(dot::object value)
    {
        if (dataWriter_ != nullptr)
        {
            dataWriter_->WriteValue(value);
            return;
        }

        if (indexOfCurrent_ == -1)
            return;

        // Check that we are either inside dictionary or array
        dot::type_t elementType = tuple_->type()->GetGenericArguments()[indexOfCurrent_];

        if (value.IsEmpty())  // TODO IsEmpty method should be implemented according to c# extension
        {
            // Do not record null or empty value into dictionary, but add it to an array
            // Add to dictionary or array, depending on what we are inside of
            return;
        }

        // Write based on element type
        dot::type_t valueType = value->type();
        if (elementType->Equals(dot::typeof<dot::string>()) ||
            elementType->Equals(dot::typeof<double>()) || elementType->Equals(dot::typeof<dot::Nullable<double>>()) ||
            elementType->Equals(dot::typeof<bool>()) || elementType->Equals(dot::typeof<dot::Nullable<bool>>()) ||
            elementType->Equals(dot::typeof<int>()) || elementType->Equals(dot::typeof<dot::Nullable<int>>()) ||
            elementType->Equals(dot::typeof<int64_t>()) || elementType->Equals(dot::typeof<dot::Nullable<int64_t>>()) ||
            elementType->Equals(dot::typeof<ObjectId>())
            )
        {
            // Check type match
            //if (!elementType->Equals(valueType)) // TODO change to !elementType->IsAssignableFrom(valueType)
            //    throw dot::exception(
            //        dot::string::format("Attempting to deserialize value of type {0} ", valueType->name) +
            //        dot::string::format("into element of type {0}.", elementType->name));

            dot::object convertedValue = value;
            if (elementType->Equals(dot::typeof<double>()))
            {
                if (valueType->Equals(dot::typeof<int>())) convertedValue = static_cast<double>((int) value);
                if (valueType->Equals(dot::typeof<int64_t>())) convertedValue = static_cast<double>((int64_t) value);
            }
            else if (elementType->Equals(dot::typeof<int64_t>()) && valueType->Equals(dot::typeof<int>()))
            {
                convertedValue = static_cast<int64_t>((int) value);
            }
            else if (elementType->Equals(dot::typeof<int>()) && valueType->Equals(dot::typeof<int64_t>()))
            {
                convertedValue = static_cast<int>((int64_t) value);
            }
            else if (elementType->Equals(dot::typeof<ObjectId>()) && valueType->Equals(dot::typeof<dot::string>()))
            {
                convertedValue = ObjectId((dot::string) value);
            }

            // Add to array or dictionary, depending on what we are inside of
            tuple_->type()->GetMethod("SetItem")->Invoke(tuple_, dot::new_Array1D<dot::object>({ tuple_, indexOfCurrent_, convertedValue }));
        }
        else if (elementType->Equals(dot::typeof<dot::local_date>()) || elementType->Equals(dot::typeof<dot::Nullable<dot::local_date>>()))
        {
            dot::local_date dateValue;

            // Check type match
            if (valueType->Equals(dot::typeof<int>()))
            {
                // Deserialize LocalDate as ISO int in yyyymmdd format
                dateValue = LocalDateHelper::ParseIsoInt((int)value);
            }
            else if (valueType->Equals(dot::typeof<int64_t>()))
            {
                // Deserialize LocalDate as ISO int in yyyymmdd format
                dateValue = LocalDateHelper::ParseIsoInt((int64_t)value);
            }
            else throw dot::exception(
                    dot::string::format("Attempting to deserialize value of type {0} ", valueType->name) +
                    "into LocalDate; type should be int32.");

            tuple_->type()->GetMethod("SetItem")->Invoke(tuple_, dot::new_Array1D<dot::object>({ tuple_, indexOfCurrent_, dateValue }));
        }
        else if (elementType->Equals(dot::typeof<dot::local_time>()) || elementType->Equals(dot::typeof<dot::Nullable<dot::local_time>>()))
        {
            dot::local_time timeValue;

            // Check type match
            if (valueType->Equals(dot::typeof<int>()))
            {
                // Deserialize LocalTime as ISO int in hhmmssfff format
                timeValue = LocalTimeHelper::ParseIsoInt((int)value);
            }
            else if (valueType->Equals(dot::typeof<int64_t>()))
            {
                // Deserialize LocalTime as ISO int in hhmmssfff format
                timeValue = LocalTimeHelper::ParseIsoInt((int64_t)value);
            }
            else throw dot::exception(
                    dot::string::format("Attempting to deserialize value of type {0} ", valueType->name) +
                    "into LocalTime; type should be int32.");

            tuple_->type()->GetMethod("SetItem")->Invoke(tuple_, dot::new_Array1D<dot::object>({ tuple_, indexOfCurrent_, timeValue }));
        }
        else if (elementType->Equals(dot::typeof<dot::local_minute>()) || elementType->Equals(dot::typeof<dot::Nullable<dot::local_minute>>()))
        {
            dot::local_minute minuteValue;

            // Check type match
            if (valueType->Equals(dot::typeof<int>()))
            {
                // Deserialize LocalMinute as ISO int in hhmmssfff format
                minuteValue = LocalMinuteHelper::ParseIsoInt((int)value);
            }
            else if (valueType->Equals(dot::typeof<int64_t>()))
            {
                // Deserialize LocalMinute as ISO int in hhmmssfff format
                minuteValue = LocalMinuteHelper::ParseIsoInt((int64_t)value);
            }
            else throw dot::exception(
                dot::string::format("Attempting to deserialize value of type {0} ", valueType->name) +
                "into LocalMinute; type should be int32.");

            tuple_->type()->GetMethod("SetItem")->Invoke(tuple_, dot::new_Array1D<dot::object>({ tuple_, indexOfCurrent_, minuteValue }));
        }
        else if (elementType->Equals(dot::typeof<dot::local_date_time>()) || elementType->Equals(dot::typeof<dot::Nullable<dot::local_date_time>>()))
        {
        dot::local_date_time dateTimeValue;

            // Check type match
            if (valueType->Equals(dot::typeof<dot::local_date_time>()))
            {
                dateTimeValue = (dot::local_date_time)value;
            }
            else if (valueType->Equals(dot::typeof<int64_t>()))
            {
                // Deserialize LocalDateTime as ISO long in yyyymmddhhmmssfff format
                dateTimeValue = LocalDateTimeHelper::ParseIsoLong((int64_t)value);
            }
            else if (valueType->Equals(dot::typeof<int>()))
            {
                // Deserialize LocalDateTime as ISO long in yyyymmddhhmmssfff format
                dateTimeValue = LocalDateTimeHelper::ParseIsoLong((int)value);
            }
            else if (valueType->Equals(dot::typeof<dot::string>()))
            {
                // Deserialize LocalDateTime as ISO string
                dateTimeValue = LocalDateTimeHelper::Parse((dot::string)value);
            }
            else throw dot::exception(
                    dot::string::format("Attempting to deserialize value of type {0} ", valueType->name) +
                    "into LocalDateTime; type should be LocalDateTime.");

            tuple_->type()->GetMethod("SetItem")->Invoke(tuple_, dot::new_Array1D<dot::object>({ tuple_, indexOfCurrent_, dateTimeValue }));
        }
        else if (elementType->IsEnum)
        {
            // Check type match
            if (!valueType->Equals(dot::typeof<dot::string>()))
                throw dot::exception(
                    dot::string::format("Attempting to deserialize value of type {0} ", valueType->name) +
                    dot::string::format("into enum {0}; type should be string.", elementType->name));

            // Deserialize enum as string
            dot::string enumString = (dot::string) value;
            dot::object enumValue = dot::enum_base::Parse(elementType, enumString);

            tuple_->type()->GetMethod("SetItem")->Invoke(tuple_, dot::new_Array1D<dot::object>({ tuple_, indexOfCurrent_, enumValue }));
        }
        else
        {
            // We run out of value types at this point, now we can create
            // a reference type and check that it implements KeyType
            dot::object keyObj = (KeyType)dot::Activator::CreateInstance(elementType);
            if (keyObj.is<KeyType>())
            {
                KeyType key = (KeyType)keyObj;

                // Check type match
                if (!valueType->Equals(dot::typeof<dot::string>()) && !valueType->Equals(elementType))
                    throw dot::exception(
                        dot::string::format("Attempting to deserialize value of type {0} ", valueType->name) +
                        dot::string::format("into key type {0}; keys should be serialized into semicolon delimited string.", elementType->name));

                // Populate from semicolon delimited string
                dot::string stringValue = value->ToString();
                key->AssignString(stringValue);

                // Add to array or dictionary, depending on what we are inside of
                tuple_->type()->GetMethod("SetItem")->Invoke(tuple_, dot::new_Array1D<dot::object>({ tuple_, indexOfCurrent_, key }));

            }
            else
            {
                // Argument type is unsupported, error message
                throw dot::exception(dot::string::format("Element type {0} is not supported for serialization.", value->type()));
            }
        }
    }

    dot::string TupleWriterImpl::ToString()
    {
        return tuple_->ToString();
    }


    TupleWriterImpl::TupleWriterImpl(dot::object tuple, dot::List<dot::field_info> props)
        : tuple_(tuple)
        , props_(props)
    {

    }


}