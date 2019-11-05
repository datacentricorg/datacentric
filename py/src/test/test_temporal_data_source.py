import unittest
import datetime as dt

from bson import ObjectId

from datacentric.platform.context import Context
from datacentric.platform.data_source import TemporalMongoDataSourceData
from datacentric.types.local_minute import LocalMinute
from test.data_sample import BaseSampleData, SampleEnum


def save_base_record(context: Context, data_set_id, record_id, record_index) -> ObjectId:
    rec = BaseSampleData()
    rec.record_id = record_id
    rec.record_index = record_index
    rec.double_element = 100.0
    rec.local_date_element = dt.date(2003, 5, 1)
    rec.local_time_element = dt.time(10, 15, 30)  # 10:15:30
    rec.local_minute_element = LocalMinute(10, 15)  # 10:15
    rec.local_date_time_element = dt.datetime(2003, 5, 1, 10, 15)  # 2003-05-01T10:15:00
    rec.enum_value = SampleEnum.EnumValue2

    data_set = context.data_source.get_data_set(data_set_id, context.data_set)
    context.data_source.save(rec, data_set)
    return rec.id_


def save_basic_data(context: Context):
    data_set0 = context.data_source.create_data_set('DataSet0', context.data_set)
    save_base_record(context, "DataSet0", "A", 0)
    pass


class TestTemporalDataSource(unittest.TestCase):
    def test_instantiation(self):
        context = Context()
        source = TemporalMongoDataSourceData(mongo_uri='localhost:27017', db_name='Test')
        source.init(context)
        context.data_source = source

    def test_create_common(self):
        context = Context()
        source = TemporalMongoDataSourceData(mongo_uri='localhost:27017', db_name='Test')
        source.init(context)
        context.data_source = source
        id_ = context.data_source.create_common()
        self.assertTrue(isinstance(id_, ObjectId))

    def test_smoke(self):
        context = Context()
        source = TemporalMongoDataSourceData(mongo_uri='localhost:27017', db_name='Test')
        source.init(context)

        context.data_source = source
        context.data_set = context.data_source.create_common()

        save_basic_data(context)


if __name__ == "__main__":
    unittest.main()
