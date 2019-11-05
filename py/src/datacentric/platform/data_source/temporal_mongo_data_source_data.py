import datetime as dt
from typing import Dict, Union, Optional
from bson import ObjectId
from pymongo.collection import Collection

from datacentric.types.record import Record, TypedKey, DeletedRecord
from datacentric.platform.data_source import MongoDataSourceData
from datacentric.platform.reflection import ClassInfo
from datacentric.platform.serialization.serializer import serialize, deserialize


class TemporalMongoDataSourceData(MongoDataSourceData):
    saved_by_time: dt.datetime
    saved_by_id: ObjectId
    freeze_imports: bool

    def __init__(self, mongo_uri, db_name):
        super().__init__(mongo_uri, db_name)
        self._collection_dict = dict()  # type: Dict[type, Collection]

        self.saved_by_time = None
        self.saved_by_id = None
        self.freeze_imports = True

    def is_readonly(self) -> bool:
        return self.readonly or self.saved_by_id is not None or self.saved_by_time is not None

    def load_or_null(self, id_: ObjectId, type_: type) -> Union[Record, None]:
        saved_by = self._get_saved_by()
        if saved_by is not None and id_ > saved_by:
            return
        pipeline = [
            {'$match': {'$id': {'$eq': id_}}},
            {'$limit': 1}
        ]
        collection = self._get_or_create_collection(type_)
        cursor = collection.aggregate(pipeline)
        if cursor.alive:
            cursor_next = cursor.next()
            result = deserialize(cursor_next)

            if result is not None and isinstance(result, DeletedRecord):
                if not isinstance(result, type_):
                    raise Exception(f'Stored type {type(result).__name__} for ObjectId={id_} and '
                                    f'Key={result.key} is not an instance of the requested type {type_.__name__}.')
                result.init(self.context)
                return result

    def delete(self, key: TypedKey[Record], delete_in: ObjectId) -> None:
        raise NotImplemented

    def get_query(self, load_from: ObjectId):
        raise NotImplemented

    def reload_or_null(self, key: TypedKey, load_from: ObjectId) -> Optional[Record]:
        lookup_list = self.get_data_set_lookup_list(load_from)
        key_value = key.value
        pipeline = [
            {"$match": {"_key": key_value}},
            {"$match": {"_dataset": {"$in": lookup_list}}},
            {"$sort": {"_dataset": -1}},
            {"$sort": {"_id": -1}},
            {'$limit': 1}
        ]
        collection = self._get_or_create_collection(type(key))
        cursor = collection.aggregate(pipeline)
        if cursor.alive:
            cursor_next = cursor.next()
            return deserialize(cursor_next)
        else:
            return None

    def save(self, record: Record, save_to: ObjectId) -> None:
        self.check_not_readonly()
        object_id = self.create_ordered_object_id()
        if object_id <= save_to:
            raise Exception(f'Attempting to save a record with ObjectId={object_id} that is later '
                            f'than ObjectId={save_to} of the dataset where it is being saved.')
        record.id_ = object_id
        record.data_set = save_to
        record.init(self.context)

        serialized_record = serialize(record)

        collection = self._get_or_create_collection(type(record))
        collection.insert_one(serialized_record)

    def _get_or_create_collection(self, type_: type) -> Collection:
        if type_ in self._collection_dict:
            return self._collection_dict[type_]
        root_type = ClassInfo.get_root_type(type_)
        mapped_class_name = ClassInfo.get_mapped_class_name(root_type)
        collection = self.db.get_collection(mapped_class_name)
        self._collection_dict[type_] = collection
        return collection

    def _get_saved_by(self):
        if self.saved_by_time is None and self.saved_by_id is None:
            return None
        elif self.saved_by_time is not None and self.saved_by_id is None:
            return ObjectId.from_datetime(self.saved_by_time)
        elif self.saved_by_time is None and self.saved_by_id is not None:
            return self.saved_by_id
        raise Exception('Elements SavedByTime and SavedById are alternates; '
                        'they cannot be specified at the same time.')
