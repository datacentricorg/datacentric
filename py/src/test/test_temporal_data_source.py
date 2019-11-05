import unittest

from datacentric.platform.context import Context
from datacentric.platform.data_source import TemporalMongoDataSourceData


class TestTemporalDataSource(unittest.TestCase):
    def test_instantiation(self):
        context = Context()
        source = TemporalMongoDataSourceData(mongo_uri='localhost:27017', db_name='Test')
        source.init(context)
        context.data_source = source
        print('Initialized context')
        # source.create_common()

    def test_create_common(self):
        context = Context()
        source = TemporalMongoDataSourceData(mongo_uri='localhost:27017', db_name='Test')
        source.init(context)
        context.data_source = source
        context.data_source.create_common()


if __name__ == "__main__":
    unittest.main()
