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

        # token0 = NullableElementsSampleData.int_token == 1
        # # token1 = NullableElementsSampleData.int_token > False
        # token2 = NullableElementsSampleData.int_token is None
        # token3 = NullableElementsSampleData.int_token in []

        query = context.data_source.get_query(context.data_set, NullableElementsSampleData)

        for obj in query.as_iterable():
            context.log.status(f'$"    {obj.key} (record index {obj.record_index})."')

        res = str(context.log)


if __name__ == "__main__":
    unittest.main()
