import unittest
import datetime as dt
import numpy as np

from bson import ObjectId

from datacentric.platform.context import Context
from datacentric.platform.data_source import TemporalMongoDataSourceData
from datacentric.types.local_minute import LocalMinute
from test.data_sample import *


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


def save_derived_record(context, data_set_id, record_id, record_index) -> ObjectId:
    rec = DerivedSampleData()
    rec.record_id = record_id
    rec.record_index = record_index
    rec.double_element = 300.
    rec.local_date_element = dt.date(2003, 5, 1)
    rec.local_time_element = dt.time(10, 15, 30)  # 10:15:30
    rec.local_minute_element = LocalMinute(10, 15)  # 10:15
    rec.local_date_time_element = dt.datetime(2003, 5, 1, 10, 15)  # 2003-05-01T10:15:00
    rec.string_element2 = ''
    rec.double_element = 200.
    rec.list_of_string = ['A', 'B', 'C']

    rec.list_of_double = np.array([1.0, 2.0, 3.0])
    rec.list_of_nullable_double = np.array([10.0, None, 30.0])

    # Data element
    rec.data_element = ElementSampleData()
    rec.data_element.double_element3 = 1.0
    rec.data_element.string_element3 = 'AA'

    # Data element list

    element_list0 = ElementSampleData()
    element_list0.double_element3 = 1.0
    element_list0.string_element3 = "A0"
    element_list1 = ElementSampleData()
    element_list1.double_element3 = 2.0
    element_list1.string_element3 = "A1"
    rec.data_element_list = [element_list0, element_list1]

    # Key element
    rec.key_element = BaseSampleKey()
    rec.key_element.record_id = 'BB'
    rec.key_element.record_index = 2

    # Key element list
    key_list0 = BaseSampleKey()
    key_list0.record_id = "B0"
    key_list0.record_index = 3
    key_list1 = BaseSampleKey()
    key_list1.record_id = "B1"
    key_list1.record_index = 4
    rec.key_element_list = [key_list0, key_list1]

    data_set = context.data_source.get_data_set(data_set_id, context.data_set)
    context.data_source.save(rec, data_set)
    return rec.id_


def save_basic_data(context: Context):
    data_set0 = context.data_source.create_data_set('DataSet0', context.data_set)
    save_base_record(context, 'DataSet0', 'A', 0)
    data_set1 = context.data_source.create_data_set('DataSet1', context.data_set, [data_set0])
    save_derived_record(context, 'DataSet1', 'B', 0)
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
