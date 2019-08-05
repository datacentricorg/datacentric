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
#include <dc/platform/data_source/DataSourceData.hpp>
#include <dc/platform/context/IContext.hpp>
#include <dot/system/Array1D.hpp>

namespace dc
{
    bool data_source_data_impl::is_read_only()
    {
        return read_only == true || revised_before != nullptr || revised_before_id != nullptr;
    }

    void data_source_data_impl::check_not_read_only()
    {
        if (is_read_only())
            throw dot::exception(dot::string::format(
                "Attempting write operation for readonly data source {0}. "
                "A data source is readonly if either (a) its ReadOnly flag is set, or (b) "
                "one of RevisedBefore or RevisedBeforeId is set.", data_source_id));
    }

    ObjectId data_source_data_impl::get_data_set_or_empty(dot::string data_set_id, ObjectId load_from)
    {
        ObjectId result;

        if (data_set_dict_->try_get_value(data_set_id, result))
        {
            // Check if already cached, return if found
            return result;
        }
        else
        {
            // Otherwise load from storage (this also updates the dictionaries)
            return load_data_set_or_empty(data_set_id, load_from);
        }
    }

    void data_source_data_impl::save_data_set(DataSetData data_set_data, ObjectId save_to)
    {
        // Save dataset to storage. This updates its ID
        // to the new ObjectId created during save
        save(data_set_data, save_to);

        // Update dataset dictionary with the new ID
        data_set_dict_[data_set_data->getKey()] = data_set_data->ID;

        // Update lookup list dictionary
        dot::HashSet<ObjectId> lookupList = build_data_set_lookup_list(data_set_data);
        data_set_parent_dict_->add(data_set_data->ID, lookupList);
    }

    dot::HashSet<ObjectId> data_source_data_impl::get_data_set_lookup_list(ObjectId load_from)
    {
        // Root dataset has no parents, return list containing
        // root dataset identifier only (ObjectId.Empty) and exit
        if (load_from == ObjectId::Empty)
        {
            dot::HashSet<ObjectId> res = dot::new_HashSet<ObjectId>();
            res->Add(ObjectId::Empty);
            return res;
        }

        dot::HashSet<ObjectId> result;
        if (data_set_parent_dict_->try_get_value(load_from, result))
        {
            // Check if the lookup list is already cached, return if yes
            return result;
        }
        else
        {
            // Otherwise load from storage (returns null if not found)
            DataSetData data_set_data = load_or_null<DataSetData>(load_from).template as<DataSetData>();

            if (data_set_data == nullptr)
                throw dot::exception(dot::string::format("Dataset with ObjectId={0} is not found.", load_from.ToString()));
            if ((ObjectId) data_set_data->DataSet != ObjectId::Empty)
                throw dot::exception(dot::string::format("Dataset with ObjectId={0} is not stored in root dataset.", load_from.ToString()));

            // Build the lookup list
            result = build_data_set_lookup_list(data_set_data);

            // Add to dictionary and return
            data_set_parent_dict_->add(load_from, result);
            return result;
        }
    }

    dot::nullable<ObjectId> data_source_data_impl::get_revision_time_constraint()
    {
        // Set revisionTimeConstraint_ based on either RevisedBefore or RevisedBeforeId element
        if (revised_before == nullptr && revised_before_id == nullptr)
        {
            // Clear the revision time constraint.
            //
            // This is only required when  running Init(...) again
            // on an object that has been initialized before.
            return nullptr;
        }
        else if (revised_before != nullptr && revised_before_id == nullptr)
        {
            // We already know that RevisedBefore is not null,
            // but we need to check separately that it is not empty
            //RevisedBefore.CheckHasValue(); // TODO uncomment

            // Convert to the least value of ObjectId with the specified timestamp
            dot::local_date_time date = ((dot::nullable<dot::local_date_time>) revised_before).value();
            return ObjectId(date);
        }
        else if (revised_before == nullptr && revised_before_id != nullptr)
        {
            // We already know that RevisedBeforeId is not null,
            // but we need to check separately that it is not empty
            //RevisedBeforeId.Value.CheckHasValue(); // TODO uncomment

            // Set the revision time constraint
            return revised_before_id;
        }
        else
        {
            throw dot::exception(
                "Elements RevisedBefore and RevisedBeforeId are alternates; "
                "they cannot be specified at the same time.");
        }
    }

    ObjectId data_source_data_impl::load_data_set_or_empty(dot::string data_set_id, ObjectId load_from)
    {
        // Always load even if present in cache
        DataSetKey data_set_key = new_DataSetKey();
        data_set_key->DataSetID = data_set_id;
        DataSetData data_set_data = (DataSetData) reload_or_null(data_set_key, load_from);

        // If not found, return ObjectId.Empty
        if (data_set_data == nullptr) return ObjectId::Empty;

        // If found, cache result in ObjectId dictionary
        data_set_dict_[data_set_id] = data_set_data->ID;

        // Build and cache dataset lookup list if not found
        dot::HashSet<ObjectId> parent_set;
        if (!data_set_parent_dict_->try_get_value(data_set_data->ID, parent_set))
        {
            parent_set = build_data_set_lookup_list(data_set_data);
            data_set_parent_dict_->add(data_set_data->ID, parent_set);
        }

        return data_set_data->ID;
    }

    dot::HashSet<ObjectId> data_source_data_impl::build_data_set_lookup_list(DataSetData data_set_data)
    {
        // Delegate to the second overload
        dot::HashSet<ObjectId> result = dot::new_HashSet<ObjectId>();
        build_data_set_lookup_list(data_set_data, result);
        return result;
    }

    void data_source_data_impl::build_data_set_lookup_list(DataSetData data_set_data, dot::HashSet<ObjectId> result)
    {
        // Return if the dataset is null or has no parents
        if (data_set_data == nullptr) return;

        // Error message if dataset has no ID or Key
        //dataSetData->ID->CheckHasValue();
        //dataSetData->getKey()->CheckHasValue();
        //! TODO uncomment

        // Add self to the result
        result->Add(data_set_data->ID);

        // Add parents to the result
        if (!((dot::list<ObjectId>)data_set_data->Parents).IsEmpty())
        {
            for(ObjectId data_set_id : data_set_data->Parents)
            {
                // Dataset cannot include itself as parent
                if (data_set_data->ID == data_set_id)
                    throw dot::exception(
                        dot::string::format("Dataset {0} with ObjectId={1} includes itself in the list of parents.", (dot::string)data_set_data->getKey(), ObjectId(data_set_data->ID).ToString()));

                // The Add method returns true if the argument is not yet present in the list
                if (!result->contains(data_set_id))
                {
                    result->Add(data_set_id);
                    // Add recursively if not already present in the hashset
                    dot::HashSet<ObjectId> cached_parent_set = get_data_set_lookup_list(data_set_id);
                    for (ObjectId cached_parent_id : cached_parent_set)
                    {
                        result->Add(cached_parent_id);
                    }
                }
            }
        }
    }

    ObjectId data_source_data_impl::get_common()
    {
        return get_data_set(DataSetKeyImpl::Common->DataSetID, ObjectId::Empty);
    }

    ObjectId data_source_data_impl::get_data_set(dot::string data_set_id, ObjectId load_from)
    {
        auto result = get_data_set_or_empty(data_set_id, load_from);
        if (result == ObjectId::Empty) throw dot::exception(
            dot::string::format("Dataset {0} is not found in data store {1}.", data_set_id, data_source_id));
        return result;
    }

    ObjectId data_source_data_impl::create_data_set(dot::string dataSetID, ObjectId saveTo)
    {
        // Delegate to the overload taking IEnumerable as second parameter
        return create_data_set(dataSetID, nullptr, saveTo);
    }

    ObjectId data_source_data_impl::create_data_set(dot::string data_set_id, dot::list<ObjectId> parent_data_sets, ObjectId save_to)
    {
        // Create dataset record
        auto result = new_DataSetData();
        result->DataSetID = data_set_id;

        if (parent_data_sets != nullptr)
        {
            // Add parents if second argument is not null
            result->Parents = dot::make_list<ObjectId>();
            for (auto parent_data_set : parent_data_sets)
            {
                result->Parents->add(parent_data_set);
            }
        }

        // Save the record (this also updates the dictionaries)
        save_data_set(result, save_to);

        // Return ObjectId that was assigned to the
        // record inside the SaveDataSet method
        return result->ID;
    }

    ObjectId data_source_data_impl::create_common()
    {
        auto result = new_DataSetData();
        result->DataSetID = DataSetKeyImpl::Common->DataSetID;

        // Save in root dataset
        save_data_set(result, ObjectId::Empty);
        return result->ID;
    }
}
