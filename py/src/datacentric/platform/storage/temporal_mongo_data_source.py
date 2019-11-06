import datetime as dt
from typing import Dict, Optional, TypeVar, Set, Iterable
from bson import ObjectId
from pymongo.collection import Collection

from datacentric.platform.storage.data_set_detail import DataSetDetail, DataSetDetailKey
from datacentric.platform.storage.temporal_mongo_query import TemporalMongoQuery
from datacentric.types.record import Record, TypedKey, DeletedRecord
from datacentric.platform.storage import MongoDataSource, DataSet, DataSource, DataSetKey
from datacentric.platform.reflection import ClassInfo
from datacentric.platform.serialization.serializer import serialize, deserialize

TRecord = TypeVar('TRecord', bound=Record)


class TemporalMongoDataSource(MongoDataSource):
    __slots__ = ('cutoff_time', '__collection_dict', '__data_set_dict', '__data_set_parent_dict',
                 '__data_set_detail_dict', '__import_dict')

    cutoff_time: Optional[ObjectId]

    __collection_dict: Dict[type, Collection]
    __data_set_dict: Dict[str, ObjectId]
    __data_set_parent_dict: Dict[ObjectId, ObjectId]
    __data_set_detail_dict: Dict[ObjectId, DataSetDetail]
    __import_dict: Dict[ObjectId, Set[ObjectId]]

    def __init__(self):
        super().__init__()
        self.__collection_dict = dict()
        self.__data_set_dict = dict()
        self.__data_set_parent_dict = dict()
        self.__data_set_detail_dict = dict()
        self.__import_dict = dict()

        self.cutoff_time = None

    def load_or_null(self, id_: ObjectId, type_: type) -> Optional[TRecord]:
        if self.cutoff_time is not None:
            if id_ >= self.cutoff_time:
                return None

        pipeline = [
            {'$match': {'_id': {'$eq': id_}}},
            {'$limit': 1}
        ]
        collection = self._get_or_create_collection(type_)
        cursor = collection.aggregate(pipeline)
        if cursor.alive:
            cursor_next = cursor.next()
            result = deserialize(cursor_next)

            if result is not None and not isinstance(result, DeletedRecord):

                cutoff_time = self.get_cutoff_time(result.data_set)
                if cutoff_time is not None:
                    if id_ >= cutoff_time:
                        return None

                is_requested_instance = isinstance(result, type_)
                if not is_requested_instance:
                    raise Exception(f'Stored type {type(result).__name__} for ObjectId={id_} and '
                                    f'Key={result.key} is not an instance of the requested type {type_.__name__}.')
                result.init(self.context)
                return result

    def load_or_null_by_key(self, key_: TypedKey[Record], load_from: ObjectId) -> Optional[TRecord]:
        key_value = key_.value

        base_pipe = [{"$match": {"_key": key_value}}]
        pipe_with_constraints = self.apply_final_constraints(base_pipe, load_from)
        ordered_pipe = pipe_with_constraints
        ordered_pipe.extend(
            [
                {"$sort": {"_dataset": -1}},
                {"$sort": {"_id": -1}},
                {'$limit': 1}
            ]
        )

        record_type = ClassInfo.get_record_from_key(type(key_))
        collection = self._get_or_create_collection(record_type)

        cursor = collection.aggregate(ordered_pipe)
        if cursor.alive:
            cursor_next = cursor.next()
            result = deserialize(cursor_next)

            if result is not None and not isinstance(result, DeletedRecord):

                is_proper_record = isinstance(result, record_type)
                if not is_proper_record:
                    raise Exception(f'Stored type {type(result).__name__} for Key={key_value} in '
                                    f'data_set={load_from} is not an instance of '
                                    f'the requested type {record_type.__name__}.')
                result.init(self.context)
                return result

    def get_query(self, load_from: ObjectId, type_: type) -> TemporalMongoQuery:
        collection = self._get_or_create_collection(type_)
        return TemporalMongoQuery(self, type_, collection, load_from)

    def save_many(self, record_type: type, records: Iterable[TRecord], save_to: ObjectId):
        self._check_not_readonly(save_to)
        collection = self._get_or_create_collection(record_type)
        if records is None:
            return None

        for record in records:
            record_id = self.create_ordered_object_id()
            if record_id <= save_to:
                raise Exception(f'TemporalId={record_id} of a record must be greater than '
                                f'TemporalId={save_to} of the dataset where it is being saved.')
            record.id_ = record_id
            record.data_set = save_to
            record.init(self.context)
        if self.is_non_temporal(record_type, save_to):
            collection.insert_many([serialize(x) for x in records])  # TODO: replace by upsert
        else:
            collection.insert_many([serialize(x) for x in records])

    def delete(self, key: TypedKey[Record], delete_in: ObjectId) -> None:
        self._check_not_readonly(delete_in)
        record = DeletedRecord()
        record.key = key.value

        record.id_ = self.create_ordered_object_id()
        record.data_set = delete_in

        record_type = ClassInfo.get_record_from_key(type(key))
        collection = self._get_or_create_collection(record_type)

        collection.insert_one(record)

    def apply_final_constraints(self, pipeline, load_from: ObjectId):
        data_set_lookup_list = self.get_data_set_lookup_list(load_from)
        pipeline.append({'$match': {"_dataset": {"$in": data_set_lookup_list}}})

        cutoff_time = self.get_cutoff_time(load_from)
        if cutoff_time is not None:
            pipeline.append({'$match': {'_id': {'$lte': cutoff_time}}})

        return pipeline

    def get_data_set_or_none(self, data_set_name: str, load_from: ObjectId) -> Optional[ObjectId]:
        if data_set_name in self.__data_set_dict:
            return self.__data_set_dict[data_set_name]
        data_set_key = DataSetKey()
        data_set_key.data_set_name = data_set_name

        data_set_record = self.load_or_null_by_key(data_set_key, load_from)

        if data_set_record is None:
            return None

        self.__data_set_dict[data_set_name] = data_set_record.id_
        self.__data_set_parent_dict[data_set_record.id_] = data_set_record.data_set

        if data_set_record.id_ not in self.__import_dict:
            import_set = self._build_data_set_lookup_list(data_set_record)
            self.__import_dict[data_set_record.id_] = import_set

        return data_set_record.id_

    def save_data_set(self, data_set: DataSet, save_to: ObjectId) -> None:
        self.save_one(DataSet, data_set, save_to)
        self.__data_set_dict[data_set.key] = data_set.id_
        self.__data_set_parent_dict[data_set.id_] = data_set.data_set

        lookup_list = self._build_data_set_lookup_list(data_set)
        self.__import_dict[data_set.id_] = lookup_list

    def get_data_set_lookup_list(self, load_from: ObjectId) -> Iterable[ObjectId]:
        """Returns enumeration of import datasets for specified dataset data,
        including imports of imports to unlimited depth with cyclic
        references and duplicates removed.
        """
        if load_from == DataSource._empty_id:
            return [DataSource._empty_id]

        if load_from in self.__import_dict:
            return self.__import_dict[load_from]

        else:
            data_set_data: DataSet = self.load_or_null(load_from, DataSet)
            if data_set_data is None:
                raise Exception(f'Dataset with ObjectId={load_from} is not found.')
            if data_set_data.data_set != DataSource._empty_id:
                raise Exception(f'Dataset with ObjectId={load_from} is not stored in root dataset.')
            result = self._build_data_set_lookup_list(data_set_data)
            self.__import_dict[load_from] = result
            return result

    def get_data_set_detail_or_none(self, detail_for: ObjectId) -> Optional[DataSetDetail]:
        if detail_for == DataSource._empty_id:
            return None
        if detail_for in self.__data_set_detail_dict:
            return self.__data_set_detail_dict[detail_for]

        parent_id = self.__data_set_parent_dict[detail_for]
        data_set_detail_key = DataSetDetailKey()
        data_set_detail_key.data_set_id = detail_for

        result = self.load_or_null_by_key(data_set_detail_key, parent_id)
        self.__data_set_detail_dict[detail_for] = result
        return result

    def is_non_temporal(self, record_type: type, data_set_id: ObjectId) -> bool:
        if self.non_temporal is not None and self.non_temporal:
            return True
        if hasattr(record_type, 'non_temporal') and record_type.non_temporal:
            return True
        if data_set_id == DataSource._empty_id:
            return False
        data_set_detail: DataSet = self.load_or_null(data_set_id, DataSet)
        if data_set_detail is not None and data_set_detail.non_temporal:
            return True
        else:
            return False

    def get_cutoff_time(self, data_set_id: ObjectId) -> Optional[ObjectId]:
        data_set_detail = self.get_data_set_detail_or_none(data_set_id)
        if data_set_detail is not None:
            data_set_cutoff_time = data_set_detail.cutoff_time
        else:
            data_set_cutoff_time = None

        # Min of (self.cutoff_time, data_set_cutoff_time)
        if self.cutoff_time is not None and data_set_cutoff_time is not None:
            if self.cutoff_time < data_set_cutoff_time:
                return self.cutoff_time
            else:
                return data_set_cutoff_time

        if self.cutoff_time is None:
            # Covers the case if both are None
            return data_set_cutoff_time

        return self.cutoff_time

    def get_imports_cutoff_time(self, data_set_id: ObjectId) -> Optional[ObjectId]:
        data_set_detail = self.get_data_set_detail_or_none(data_set_id)
        if data_set_detail is not None:
            return data_set_detail.imports_cutoff_time

    def _get_or_create_collection(self, type_: type) -> Collection:
        if type_ in self.__collection_dict:
            return self.__collection_dict[type_]
        root_type = ClassInfo.get_root_type(type_)
        collection_name = root_type.__name__
        collection = self.db.get_collection(collection_name)
        self.__collection_dict[type_] = collection
        return collection

    def _build_data_set_lookup_list(self, data_set_record: DataSet):
        result = set()
        self._fill_data_set_lookup_set(data_set_record, result)

        return result

    def _fill_data_set_lookup_set(self, data_set_record: DataSet, result: Set[ObjectId]) -> None:
        if data_set_record is None:
            return

        if not ObjectId.is_valid(data_set_record.id_):
            raise Exception('Required ObjectId value is not set.')
        if data_set_record.key == '':
            raise Exception('Required string value is not set.')

        cutoff_time = self.get_cutoff_time(data_set_record.data_set)

        if cutoff_time is not None and data_set_record.id_ >= cutoff_time:
            return

        result.add(data_set_record.id_)

        if data_set_record.imports is not None:
            for data_set_id in data_set_record.imports:
                if data_set_record.id_ == data_set_id:
                    raise Exception(f'Dataset {data_set_record.key} with ObjectId={data_set_record.id_} '
                                    f'includes itself in the list of its imports.')
                if data_set_id not in result:
                    result.add(data_set_id)
                    cached_import_list = self.get_data_set_lookup_list(data_set_id)
                    for import_id in cached_import_list:
                        result.add(import_id)

    def _check_not_readonly(self, data_set_id: ObjectId):
        if self.readonly:
            raise Exception(f'Attempting write operation for data source {self.data_source_name} '
                            f'where ReadOnly flag is set.')
        data_set_detail = self.get_data_set_detail_or_none(data_set_id)

        if data_set_detail is not None and data_set_detail.read_only:
            raise Exception(f'Attempting write operation for dataset {data_set_id} where ReadOnly flag is set.')

        if self.cutoff_time is not None:
            raise Exception(f'Attempting write operation for data source {self.data_source_name} where '
                            f'CutoffTime is set. Historical view of the data cannot be written to.')

        if data_set_detail is not None and data_set_detail.cutoff_time is not None:
            raise Exception(f'Attempting write operation for the dataset {data_set_id} where '
                            f'CutoffTime is set. Historical view of the data cannot be written to.')
