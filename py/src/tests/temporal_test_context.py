from datacentric.platform.context import Context
from datacentric.platform.logging.in_memory_log import InMemoryLog
from datacentric.platform.storage import TemporalMongoDataSource
from datacentric.platform.storage.db_name import DbNameKey
from datacentric.platform.storage.instance_type import InstanceType


class TemporalTestContext:
    def __init__(self, test):
        self.test = test

    def __enter__(self):
        context = Context()

        source = TemporalMongoDataSource()
        db_name = DbNameKey()
        db_name.instance_type = InstanceType.TEST
        db_name.instance_name = self.test.id().split('.')[-2]
        db_name.env_name = self.test.id().split('.')[-1]
        source.db_name = db_name
        source.init(context)

        context.data_source = source
        context.data_set = context.data_source.create_common()
        context.log = InMemoryLog()

        return context

    def __exit__(self, exc_type, exc_val, exc_tb):
        print('Exit tests context')
