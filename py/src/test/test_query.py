import unittest
import datetime as dt

from datacentric.platform.context import Context
from datacentric.platform.data_source import TemporalMongoDataSourceData
from datacentric.platform.logging.in_memory_log import InMemoryLog
from datacentric.types.local_minute import LocalMinute
from test.data_sample import NullableElementsSampleData, SampleEnum


class TestQuery(unittest.TestCase):
    def test_nullable_elements(self):
        context = Context()
        source = TemporalMongoDataSourceData(mongo_uri='localhost:27017', db_name='TestQuery')
        source.init(context)

        context.data_source = source
        context.data_set = context.data_source.create_common()
        context.log = InMemoryLog()

        for record_index in range(8):
            record_index_mod2 = record_index % 2
            record_index_mod4 = record_index % 4
            record = NullableElementsSampleData()
            record.record_index = record_index
            record.data_set = context.data_set
            record.string_token = 'A' + str(record_index_mod4)
            record.bool_token = record_index_mod2 == 0
            record.int_token = record_index_mod4
            record.local_date_token = dt.date(2003, 5, 1) + dt.timedelta(days=record_index_mod4)
            record.local_time_token = dt.time(10, 15, 30 + record_index_mod4)
            record.local_minute_token = LocalMinute(10, record_index_mod4)
            record.local_date_time_token = dt.datetime(2003, 5, 1, 10, 15) + dt.timedelta(days=record_index_mod4)
            record.enum_token = SampleEnum(record_index_mod2 + 1)

            context.data_source.save(record, context.data_set)

        query = context.data_source.get_query(context.data_set, NullableElementsSampleData)

        # Unconstrained query
        unconstrained_results = []
        for obj in query.as_iterable():  # type: NullableElementsSampleData
            unconstrained_results.append((obj.key, obj.record_index))

        self.assertTrue(('A0;true;0;20030501;101530000;1000;20030501101500000;EnumValue1', 4) in unconstrained_results)
        self.assertTrue(('A1;false;1;20030502;101531000;1001;20030502101500000;EnumValue2', 5) in unconstrained_results)
        self.assertTrue(('A2;true;2;20030503;101532000;1002;20030503101500000;EnumValue1', 6) in unconstrained_results)
        self.assertTrue(('A3;false;3;20030504;101533000;1003;20030504101500000;EnumValue2', 7) in unconstrained_results)

        # Query with constraints
        query = context.data_source.get_query(context.data_set, NullableElementsSampleData) \
            .where({'string_token': 'A1'}).where({'bool_token': False}).where({'int_token': 1}) \
            .where({'local_date_token': dt.date(2003, 5, 1) + dt.timedelta(days=1)}) \
            .where({'local_time_token': dt.time(10, 15, 30 + 1)}) \
            .where({'local_minute_token': LocalMinute(10, 1)}) \
            .where({'local_date_time_token': dt.datetime(2003, 5, 1, 10, 15) + dt.timedelta(days=1)}) \
            .where({'enum_token': SampleEnum.EnumValue2})

        constrained_results = []
        for obj in query.as_iterable():
            constrained_results.append((obj.key, obj.record_index))

        self.assertTrue(('A1;false;1;20030502;101531000;1001;20030502101500000;EnumValue2', 5) in constrained_results)


if __name__ == "__main__":
    unittest.main()
