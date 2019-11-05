import unittest

from datacentric.platform.data_source import TemporalMongoDataSourceData


class TestTemporalDataSource(unittest.TestCase):
    def test_instantiation(self):
        source = TemporalMongoDataSourceData(mongo_uri='', db_name='Test')



    # def test_abstract_fail(self):
    #     with self.assertRaises(TypeError):
    #         Record()


if __name__ == "__main__":
    unittest.main()
