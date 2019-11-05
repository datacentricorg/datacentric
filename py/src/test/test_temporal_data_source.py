import unittest

from bson import ObjectId

from datacentric.platform.data_set import DataSetData
from datacentric.platform.logging.in_memory_log import InMemoryLog
from test.data_sample import *
from datacentric.platform.context import Context
from datacentric.platform.data_source import TemporalMongoDataSourceData


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


def verify_load(context, data_set_id, key):
    data_set = context.data_source.get_data_set(data_set_id, context.data_set)
    record = context.data_source.load_or_null_by_key(key, data_set)
    if record is None:
        return 'Not found'
    else:
        if record.key != str(key):
            return 'Found. Wrong key'
        else:
            return f'Found. Type = {type(record).__name__}'


def save_minimal_record(context, data_set_id, record_id, record_index, version):
    rec = BaseSampleData()
    rec.record_id = record_id
    rec.record_index = record_index
    rec.version = version

    data_set = context.data_source.get_data_set(data_set_id, context.data_set)
    context.data_source.save(rec, data_set)

    return rec.id_


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

        key_a0 = BaseSampleKey()
        key_a0.record_id = 'A'
        key_a0.record_index = 0

        key_b0 = BaseSampleKey()
        key_b0.record_id = 'B'
        key_b0.record_index = 0

        self.assertEqual(verify_load(context, 'DataSet0', key_a0), 'Found. Type = BaseSampleData')
        self.assertEqual(verify_load(context, 'DataSet1', key_a0), 'Found. Type = BaseSampleData')
        self.assertEqual(verify_load(context, 'DataSet0', key_b0), 'Not found')
        self.assertEqual(verify_load(context, 'DataSet1', key_b0), 'Found. Type = DerivedSampleData')

    def test_multiple_data_set_query(self):
        context = Context()
        source = TemporalMongoDataSourceData(mongo_uri='localhost:27017', db_name='Test')
        source.init(context)

        context.data_source = source
        context.data_set = context.data_source.create_common()

        # Begin from DataSet0
        data_set0 = context.data_source.create_data_set('DataSet0', context.data_set)

        # Create initial version of the records
        save_minimal_record(context, 'DataSet0', 'A', 0, 0)
        save_minimal_record(context, 'DataSet0', 'B', 1, 0)
        save_minimal_record(context, 'DataSet0', 'A', 2, 0)
        save_minimal_record(context, 'DataSet0', 'B', 3, 0)

        # Create second version of some records
        save_minimal_record(context, 'DataSet0', 'A', 0, 1)
        save_minimal_record(context, 'DataSet0', 'B', 1, 1)
        save_minimal_record(context, 'DataSet0', 'A', 2, 1)
        save_minimal_record(context, 'DataSet0', 'B', 3, 1)

        # Create third version of even fewer records
        save_minimal_record(context, 'DataSet0', 'A', 0, 2)
        save_minimal_record(context, 'DataSet0', 'B', 1, 2)
        save_minimal_record(context, 'DataSet0', 'A', 2, 2)
        save_minimal_record(context, 'DataSet0', 'B', 3, 2)

        # Same in DataSet1
        data_set1 = context.data_source.create_data_set("DataSet1", context.data_set, [data_set0])

        # Create initial version of the records
        save_minimal_record(context, "DataSet1", "A", 4, 0)
        save_minimal_record(context, "DataSet1", "B", 5, 0)
        save_minimal_record(context, "DataSet1", "A", 6, 0)
        save_minimal_record(context, "DataSet1", "B", 7, 0)

        # Create second version of some records
        save_minimal_record(context, "DataSet1", "A", 4, 1)
        save_minimal_record(context, "DataSet1", "B", 5, 1)
        save_minimal_record(context, "DataSet1", "A", 6, 1)
        save_minimal_record(context, "DataSet1", "B", 7, 1)

        # Next in DataSet2
        data_set2 = context.data_source.create_data_set("DataSet2", context.data_set, [data_set0])
        save_minimal_record(context, "DataSet2", "A", 8, 0)
        save_minimal_record(context, "DataSet2", "B", 9, 0)

        # Next in DataSet3
        data_set3 = context.data_source.create_data_set("DataSet3", context.data_set, [data_set0, data_set1, data_set2])
        save_minimal_record(context, "DataSet3", "A", 10, 0)
        save_minimal_record(context, "DataSet3", "B", 11, 0)

        query = context.data_source.get_query(data_set3, BaseSampleData) \
            .where({'record_id': 'B'}) \
            .sort_by('record_id') \
            .sort_by('record_index')

        query_result = []
        for obj in query.as_iterable():  # type: BaseSampleData
            data_set: DataSetData = context.data_source.load_or_null(obj.data_set, DataSetData)
            data_set_name = data_set.data_set_name
            query_result.append((obj.key, data_set_name, obj.version))

        self.assertEqual(query_result[0], ('B;1', 'DataSet0', 2))
        self.assertEqual(query_result[1], ('B;3', 'DataSet0', 2))
        self.assertEqual(query_result[2], ('B;5', 'DataSet1', 1))
        self.assertEqual(query_result[3], ('B;7', 'DataSet1', 1))
        self.assertEqual(query_result[4], ('B;9', 'DataSet2', 0))
        self.assertEqual(query_result[5], ('B;11', 'DataSet3', 0))

    def test_create_ordered_id(self):
        """Stress test to check ObjectIds are created in increasing order."""
        context = Context()
        source = TemporalMongoDataSourceData(mongo_uri='localhost:27017', db_name='Test')
        source.init(context)

        context.data_source = source
        context.data_set = context.data_source.create_common()
        context.log = InMemoryLog()

        for i in range(10_000):
            context.data_source.create_ordered_object_id()

        # Log should not contain warnings.
        self.assertTrue(str(context.log) == '')


if __name__ == "__main__":
    unittest.main()
