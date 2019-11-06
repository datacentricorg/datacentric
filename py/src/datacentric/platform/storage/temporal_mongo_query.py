from __future__ import annotations

from enum import Enum
from typing import Iterable, Dict, Any, List, TypeVar
from bson import ObjectId
from pymongo.collection import Collection
from pymongo.command_cursor import CommandCursor
import datetime as dt
import numpy as np

import datacentric.extensions.str as str_ext
from datacentric.types import date_ext
from datacentric.types.local_minute import LocalMinute
from datacentric.types.record import Record
from datacentric.platform.serialization.serializer import deserialize

TRecord = TypeVar('TRecord', bound=Record)


class TemporalMongoQuery:
    def __init__(self, data_source: 'TemporalMongoDataSource', type_: type, collection: Collection,
                 load_from: ObjectId):
        from datacentric.platform.storage import TemporalMongoDataSource
        self._data_source: TemporalMongoDataSource = data_source
        self._type = type_
        self._collection = collection
        self._load_from = load_from
        self._pipeline: List[Dict[str, Dict[Any]]] = [{'$match': {'_t': self._type.__name__}}]

    def __has_sort(self) -> bool:
        stage_names = [stage_name
                       for stage in self._pipeline
                       for stage_name in stage.keys()]
        return '$sort' in stage_names

    def where(self, predicate: Dict[str, Any]) -> TemporalMongoQuery:
        if not self.__has_sort():
            renamed_keys = dict()
            for k, v in predicate.items():
                new_key = str_ext.to_pascal_case(k)
                renamed_keys[new_key] = v

            TemporalMongoQuery.__fix_predicate_query(renamed_keys)

            query = TemporalMongoQuery(self._data_source, self._type, self._collection, self._load_from)
            query._pipeline = self._pipeline.copy()
            query._pipeline.append({'$match': renamed_keys})
            return query
        else:
            raise Exception(f'All where(...) clauses of the query must precede'
                            f'sort_by(...) or sort_by_descending(...) clauses of the same query.')

    @staticmethod
    def __fix_predicate_query(dict_: Dict[str, Any]):
        """Updated and convert user defined query to bson friendly format."""
        for k, value in dict_.items():
            if type(value) is dict:
                updated_value = TemporalMongoQuery.__process_dict(value)
            elif type(value) is list:
                updated_value = TemporalMongoQuery.__process_list(value)
            else:
                updated_value = TemporalMongoQuery.__process_element(value)
            dict_[k] = updated_value

    @staticmethod
    def __process_dict(dict_: Dict[str, Any]) -> Dict[str, Any]:
        """Process dictionary values."""
        for k, value in dict_.items():
            if type(value) is dict:
                updated_value = TemporalMongoQuery.__process_dict(value)
            elif type(value) is list:
                updated_value = TemporalMongoQuery.__process_list(value)
            else:
                updated_value = TemporalMongoQuery.__process_element(value)

            dict_[k] = updated_value
        return dict_

    @staticmethod
    def __process_list(list_: List[Any]) -> List[Any]:
        """Process list elements."""
        updated_list = []
        for value in list_:
            if type(value) is dict:
                updated_value = TemporalMongoQuery.__process_dict(value)
            elif type(value) is list:
                updated_value = TemporalMongoQuery.__process_list(value)
            else:
                updated_value = TemporalMongoQuery.__process_element(value)
            updated_list.append(updated_value)
        return updated_list

    @staticmethod
    def __process_element(value) -> Any:
        """Serializes elements to bson valid objects."""
        value_type = type(value)
        if value_type == LocalMinute:
            return date_ext.minute_to_iso_int(value)
        elif value_type == dt.date:
            return date_ext.date_to_iso_int(value)
        elif value_type == dt.time:
            return date_ext.time_to_iso_int(value)
        elif value_type == dt.datetime:
            return value
        elif value_type == np.ndarray:
            return value.tolist()
        elif issubclass(value_type, Enum):
            return value.name
        else:
            return value

    def sort_by(self, attr: str) -> TemporalMongoQuery:
        # Adding sort argument since sort stage is already present.
        if self.__has_sort():
            query = TemporalMongoQuery(self._data_source, self._type, self._collection, self._load_from)
            query._pipeline = self._pipeline.copy()
            sorts = next(stage['$sort'] for stage in query._pipeline
                         if '$sort' in stage)
            sorts[str_ext.to_pascal_case(attr)] = 1
            return query
        # append sort stage
        else:
            query = TemporalMongoQuery(self._data_source, self._type, self._collection, self._load_from)
            query._pipeline = self._pipeline.copy()
            query._pipeline.append({'$sort': {str_ext.to_pascal_case(attr): 1}})
            return query

    def sort_by_descending(self, attr) -> TemporalMongoQuery:
        # Adding sort argument since sort stage is already present.
        if self.__has_sort():
            query = TemporalMongoQuery(self._data_source, self._type, self._collection, self._load_from)
            query._pipeline = self._pipeline.copy()
            sorts = next(stage['$sort'] for stage in query._pipeline
                         if '$sort' in stage)
            sorts[str_ext.to_pascal_case(attr)] = -1
            return query
        # append sort stage
        else:
            query = TemporalMongoQuery(self._data_source, self._type, self._collection, self._load_from)
            query._pipeline = self._pipeline.copy()
            query._pipeline.append({'$sort': {str_ext.to_pascal_case(attr): -1}})
            return query

    def as_iterable(self) -> Iterable[TRecord]:

        if not self.__has_sort():
            batch_queryable = self._data_source.apply_final_constraints(self._pipeline, self._load_from)
        else:
            batch_queryable = self._pipeline

        projected_batch_queryable = batch_queryable
        projected_batch_queryable.append({'$project': {'Id': '$_id', 'Key': '$_key', '_id': 0}})

        with self._collection.aggregate(projected_batch_queryable) as cursor:  # type: CommandCursor
            batch_size = 1000
            continue_query = True

            while continue_query:
                batch_index = 0
                batch_keys_hash_set = set()
                batch_ids_hash_set = set()
                batch_ids_list = []

                while True:
                    continue_query = cursor.alive
                    if continue_query:
                        record_info = cursor.next()
                        batch_key = record_info['Key']
                        batch_id = record_info['Id']
                        if batch_key not in batch_keys_hash_set:
                            batch_keys_hash_set.add(batch_key)
                            batch_index += 1
                        batch_ids_hash_set.add(batch_id)
                        batch_ids_list.append(batch_id)
                        if batch_index == batch_size:
                            break
                    else:
                        break
                if not continue_query and batch_index == 0:
                    break

                id_queryable = [{'$match': {'_key': {'$in': list(batch_keys_hash_set)}}}]
                id_queryable = self._data_source.apply_final_constraints(id_queryable, self._load_from)
                id_queryable.append({'$sort': {'_key': 1, '_dataset': -1, '_id': -1}})

                projected_id_queryable = id_queryable
                projected_id_queryable.append(
                    {'$project': {'Id': '$_id', 'DataSet': '$_dataset', 'Key': '$_key', '_id': 0}})

                descending_lookup_list = None
                if self._data_source.freeze_imports:
                    data_set_lookup_enumerable = self._data_source.get_data_set_lookup_list(self._load_from)
                    descending_lookup_list = sorted(data_set_lookup_enumerable, reverse=True)

                record_ids = []
                current_key = None
                for obj in self._collection.aggregate(projected_id_queryable):
                    obj_key = obj['Key']
                    if current_key == obj_key:
                        pass
                        # self._data_source.context.log.warning(obj_key)
                    else:
                        if self._data_source.freeze_imports:
                            record_id = obj['Id']
                            record_data_set = obj['DataSet']
                            for data_set_id in descending_lookup_list:
                                if data_set_id == record_data_set:
                                    current_key = obj_key
                                    if record_id in batch_ids_hash_set:
                                        record_ids.append(record_id)
                                if data_set_id < record_id:
                                    break
                        else:
                            current_key = obj_key
                            record_id = obj['Id']
                            if record_id in batch_ids_hash_set:
                                record_ids.append(record_id)
                if len(record_ids) == 0:
                    break

                record_queryable = [{'$match': {'_id': {'$in': record_ids}}}]
                record_dict = dict()
                for record in self._collection.aggregate(record_queryable):
                    rec = deserialize(record)
                    record_dict[rec.id_] = rec

                for batch_id in batch_ids_list:
                    if batch_id in record_dict:
                        yield record_dict[batch_id]
