from __future__ import annotations
from typing import Iterable
from bson import ObjectId
from pymongo.collection import Collection
from pymongo.command_cursor import CommandCursor

from datacentric.types.record import Record
from datacentric.platform.serialization.serializer import deserialize


class TemporalMongoQuery:
    def __init__(self, data_source: 'TemporalMongoDataSourceData', type_: type, collection: Collection,
                 load_from: ObjectId):
        from datacentric.platform.data_source import TemporalMongoDataSourceData
        self._data_source: TemporalMongoDataSourceData = data_source
        self._type = type_
        self._collection = collection
        self._load_from = load_from
        self._queryable = [{"$match": {"_t": self._type.__name__}}]
        self._ordered_queryable = None

    def where(self, predicate) -> TemporalMongoQuery:
        if self._queryable is not None and self._ordered_queryable is None:
            query = TemporalMongoQuery(self._data_source, self._type, self._collection, self._load_from)
            query._queryable = self._queryable
            query._queryable.append({'match': predicate})
            return query
        elif self._queryable is None and self._ordered_queryable is not None:
            raise Exception(f'All where(...) clauses of the query must precede'
                            f'sort_by(...) or sort_by_descending(...) clauses of the same query.')
        else:
            raise Exception(f'Strictly one of _queryable or _ordered_queryable can'
                            f'have value, not both and not neither.')

    def sort_by(self) -> TemporalMongoQuery:
        raise NotImplemented

    def sort_by_descending(self) -> TemporalMongoQuery:
        raise NotImplemented

    def as_iterable(self) -> Iterable[Record]:
        if self._queryable is not None and self._ordered_queryable is None:
            batch_queryable = self._data_source.apply_final_constraints(self._queryable, self._load_from)
        elif self._queryable is None and self._ordered_queryable is not None:
            batch_queryable = self._ordered_queryable
        else:
            raise Exception(f'Strictly one of _queryable or _ordered_queryable can'
                            f'have value, not both and not neither.')
        projected_batch_queryable = batch_queryable
        projected_batch_queryable.append({"$project": {"Id": "$_id", "Key": "$_key", "_id": 0}})
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
                id_queryable = [{"$match": {"_key": {"$in": list(batch_keys_hash_set)}}}]
                id_queryable = self._data_source.apply_final_constraints(id_queryable, self._load_from)
                id_queryable.append({"$sort": {"_key": 1, "_dataset": -1, "_id": -1}})
                projected_id_queryable = id_queryable
                projected_id_queryable.append(
                    {"$project": {"Id": "$_id", "DataSet": "$_dataset", "Key": "$_key", "_id": 0}})

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
                record_queryable = [{"$match": {"_id": {"$in": record_ids}}}]
                record_dict = dict()
                for record in self._collection.aggregate(record_queryable):
                    rec = deserialize(record)
                    record_dict[rec.id_] = rec

                for batch_id in batch_ids_list:
                    if batch_id in record_dict:
                        yield record_dict[batch_id]
