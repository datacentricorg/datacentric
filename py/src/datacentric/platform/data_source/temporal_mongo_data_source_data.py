import datetime as dt
from typing import Dict, Union
from bson import ObjectId

from datacentric.types.record import Record, TypedKey
from datacentric.platform.data_source import MongoDataSourceData


class TemporalMongoDataSourceData(MongoDataSourceData):
    def __init__(self, mongo_uri, db_name):
        super().__init__(mongo_uri, db_name)
        self._collection_dict = dict()  # type: Dict[type, object]
        self.saved_by_time = None  # type: dt.datetime
        self.saved_by_id = None  # type: ObjectId
        self.freeze_imports = True  # type: bool

    def is_readonly(self) -> bool:
        return self.readonly or self.saved_by_id is not None or self.saved_by_time is not None

    def load_or_null(self, id_: ObjectId) -> Union[Record, None]:
        saved_by = self._get_saved_by()
        if saved_by is not None and id_ > saved_by:
            return
        raise NotImplemented

    def _get_saved_by(self) -> ObjectId:
        raise NotImplemented

    def delete(self, key: TypedKey[Record], delete_in: ObjectId) -> None:
        raise NotImplemented

    def get_query(self, load_from: ObjectId):
        raise NotImplemented

    def reload_or_null(self, key: TypedKey, load_from: ObjectId) -> Record:
        raise NotImplemented

    def save(self, record: Record, save_to: ObjectId) -> None:
        self.check_not_readonly()
        object_id = self.create_ordered_object_id()
        if object_id <= save_to:
            raise Exception(f'Attempting to save a record with ObjectId={object_id} that is later '
                            f'than ObjectId={save_to} of the dataset where it is being saved.')
        record.id_ = object_id
        record.data_set = save_to
        record.init(self.context)
        collection = self.db.get_collection('Type')
        collection.insert_one(record)

        raise NotImplemented
