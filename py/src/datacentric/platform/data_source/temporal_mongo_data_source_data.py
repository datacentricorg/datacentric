import datetime as dt
from typing import Dict
from bson import ObjectId

from datacentric.platform.data_source import MongoDataSourceData
from datacentric.platform.data_source.data_source_data import TRecord


class TemporalMongoDataSourceData(MongoDataSourceData):
    def __init__(self, mongo_uri, db_name):
        super().__init__(mongo_uri, db_name)
        self._collection_dict = dict()  # type: Dict[type, object]
        self.saved_by_time = None  # type: dt.datetime
        self.saved_by_id = None  # type: ObjectId
        self.freeze_imports = True  # type: bool

    def is_readonly(self) -> bool:
        return self.readonly or self.saved_by_id is not None or self.saved_by_time is not None

    def load_or_null(self, id_: ObjectId) -> TRecord:
        saved_by = self._get_saved_by()
        if saved_by is not None and id_ > saved_by:
            return
        



