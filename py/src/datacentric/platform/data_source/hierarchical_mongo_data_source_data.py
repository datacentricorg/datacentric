import datetime as dt
from typing import Dict
from bson import ObjectId

from datacentric.platform.data_source import MongoDataSourceData
from datacentric.platform.data_source.data_source_data import TRecord


class HierarchicalMongoDataSourceData(MongoDataSourceData):
    def __init__(self, mongo_uri, db_name):
        super().__init__(mongo_uri, db_name)
        self._collection_dict = dict()  # type: Dict[type, object]
