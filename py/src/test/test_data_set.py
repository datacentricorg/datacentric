import unittest

from datacentric.platform.data_set import DataSetKey, DataSetData
from datacentric.types.record import Record


class TestDataSet(unittest.TestCase):
    def test_key_instantiation(self):
        null_key = DataSetKey()
        key = DataSetKey('key_id')
        self.assertTrue(null_key.data_set_id is None)
        self.assertTrue(key.data_set_id == 'key_id')

    def test_abstract_fail(self):
        with self.assertRaises(TypeError):
            Record()


if __name__ == "__main__":
    unittest.main()
