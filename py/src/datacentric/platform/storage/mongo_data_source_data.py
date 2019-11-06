from abc import ABC

from bson import ObjectId
from pymongo import MongoClient
from pymongo.database import Database

from datacentric.platform.context import Context
from datacentric.platform.storage import DataSourceData
from datacentric.platform.storage.instance_type import InstanceType


class MongoDataSourceData(DataSourceData, ABC):
    __slots__ = ['mongo_server', '_instance_type', '_client', '_db', '_db_name', '__prev_object_id']
    __prohibited_symbols = '/\\. "$*<>:|?'
    __max_db_name_length = 64

    _instance_type: InstanceType
    _client: MongoClient
    _db: Database
    _db_name: str

    mongo_server: str

    def __init__(self):
        super().__init__()
        # Data part
        self.mongo_server = None

        # Non-data part
        self._instance_type = None
        self._client = None
        self._db = None
        self._db_name = None
        self.__prev_object_id = DataSourceData._empty_id

    def init(self, context: Context) -> None:
        super().init(context)

        # perform database name validation
        if self.db_name is None:
            raise Exception('DB key is null or empty.')
        if self.db_name.instance_type == InstanceType.Empty:
            raise Exception('DB instance type is not specified.')
        if not self.db_name.instance_name:
            raise Exception('DB instance name is not specified.')
        if not self.db_name.env_name:
            raise Exception('DB environment name is not specified.')

        self._db_name = self.db_name.value
        self._instance_type = self.db_name.instance_type
        if any(x in self._db_name for x in MongoDataSourceData.__prohibited_symbols):
            raise Exception(f'MongoDB database name {self._db_name} contains a space or another '
                            f'prohibited character from the following list: /\\.\"$*<>:|?')

        if len(self._db_name) > MongoDataSourceData.__max_db_name_length:
            raise Exception(f'MongoDB database name {self._db_name} exceeds the maximum length of 64 characters.')

        self._client = MongoClient(self.mongo_server)
        self._db = self._client.get_database(self._db_name)

    @property
    def db(self):
        return self._db

    def create_ordered_object_id(self) -> ObjectId:
        self.check_not_readonly()
        result = ObjectId()
        retry_count = 0
        while result <= self.__prev_object_id:
            retry_count = retry_count + 1
            if retry_count == 0:
                self.context.log.warning('MongoDB generated ObjectId not in increasing order, retrying.')
                result = ObjectId()
        if retry_count != 0:
            self.context.log.warning(f'Generated ObjectId in increasing order after {retry_count} retries.')

        self.__prev_object_id = result
        return result

    def apply_final_constraints(self, pipeline, load_from: ObjectId):
        data_set_lookup_list = self.get_data_set_lookup_list(load_from)
        pipeline.append({'$match': {"_dataset": {"$in": data_set_lookup_list}}})
        saved_by = self._get_saved_by()
        if saved_by is not None:
            pipeline.append({'$match': {'_id': {'$lte': saved_by}}})
        return pipeline

    def delete_db(self) -> None:
        if self.readonly is not None and self.readonly:
            raise Exception(f'Attempting to drop (delete) database for the data source {self.data_source_name} '
                            f'where ReadOnly flag is set.')
        if self._client is not None and self._db is not None:
            if self._instance_type in [InstanceType.DEV, InstanceType.USER, InstanceType.TEST]:
                self._client.drop_database(self._db)
            else:
                raise Exception(f'As an extra safety measure, database {self._db_name} cannot be '
                                f'dropped because this operation is not permitted for database '
                                f'instance type {self._instance_type.name}.')
