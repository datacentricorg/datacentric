from abc import ABC

from bson import ObjectId
from pymongo import MongoClient

from datacentric.platform.data_source import DataSourceData


class MongoDataSourceData(DataSourceData, ABC):

    __prohibited_symbols = '/\\. "$*<>:|?'
    __max_db_name_length = 64

    def __init__(self, mongo_uri, db_name):
        DataSourceData.__init__(self)

        if any(x in db_name for x in MongoDataSourceData.__prohibited_symbols):
            raise Exception(f'MongoDB database name {db_name} contains a space or another '
                            f'prohibited character from the following list: /\\.\"$*<>:|?')

        if len(db_name) > MongoDataSourceData.__max_db_name_length:
            raise Exception(f'MongoDB database name {db_name} exceeds the maximum length of 64 characters.')

        self._client = MongoClient(mongo_uri)
        self._db = self._client.get_database(db_name)
        self.__prev_object_id = MongoDataSourceData.__empty_id

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

    def apply_final_constraints(self):
        raise NotImplemented

    def delete_db(self) -> None:
        raise NotImplemented
