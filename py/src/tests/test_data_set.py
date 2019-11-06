import unittest

from datacentric.platform.storage import DataSetKey
from datacentric.types.record import Record


class TestDataSet(unittest.TestCase):
    def test_key_instantiation(self):
        null_key = DataSetKey()
        key = DataSetKey('key_id')
        self.assertTrue(null_key.data_set_name is None)
        self.assertTrue(key.data_set_name == 'key_id')

    def test_abstract_fail(self):
        with self.assertRaises(TypeError):
            Record()


if __name__ == "__main__":
    unittest.main()
