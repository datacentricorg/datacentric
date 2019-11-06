from abc import ABC

from bson import ObjectId
from pymongo import MongoClient
from pymongo.database import Database

from datacentric.platform.context import Context
from datacentric.platform.storage import DataSource
from datacentric.platform.storage.instance_type import InstanceType


class MongoDataSource(DataSource, ABC):
    __slots__ = ['mongo_server', '__instance_type', '__client', '__db', '__db_name', '__prev_object_id']

    # Class attributes
    __prohibited_symbols = '/\\. "$*<>:|?'
    __max_db_name_length = 64

    # Instance attributes
    mongo_server: str

    #
    __instance_type: InstanceType
    __db: Database
    __db_name: str
    __client: MongoClient
    __prev_object_id: ObjectId

    def __init__(self):
        super().__init__()
        # Data part
        self.mongo_server = None

        # Private part
        self.__instance_type = None
        self.__client = None
        self.__db = None
        self.__db_name = None
        self.__prev_object_id = DataSource._empty_id

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

        self.__db_name = self.db_name.value
        self.__instance_type = self.db_name.instance_type
        if any(x in self.__db_name for x in MongoDataSource.__prohibited_symbols):
            raise Exception(f'MongoDB database name {self.__db_name} contains a space or another '
                            f'prohibited character from the following list: /\\.\"$*<>:|?')

        if len(self.__db_name) > MongoDataSource.__max_db_name_length:
            raise Exception(f'MongoDB database name {self.__db_name} exceeds the maximum length of 64 characters.')

        self.__client = MongoClient(self.mongo_server)
        self.__db = self.__client.get_database(self.__db_name)

    @property
    def db(self):
        return self.__db

    def create_ordered_object_id(self) -> ObjectId:
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

    def delete_db(self) -> None:
        if self.readonly is not None and self.readonly:
            raise Exception(f'Attempting to drop (delete) database for the data source {self.data_source_name} '
                            f'where ReadOnly flag is set.')
        if self.__client is not None and self.__db is not None:
            if self.__instance_type in [InstanceType.DEV, InstanceType.USER, InstanceType.TEST]:
                self.__client.drop_database(self.__db)
            else:
                raise Exception(f'As an extra safety measure, database {self.__db_name} cannot be '
                                f'dropped because this operation is not permitted for database '
                                f'instance type {self.__instance_type.name}.')
