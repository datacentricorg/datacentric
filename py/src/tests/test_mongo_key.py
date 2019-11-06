import unittest
import datetime as dt

from bson import ObjectId

from datacentric.types.record import TypedRecord, TypedKey
from tests.data_sample import BaseSampleKey


class CompositeKeySampleKey(TypedKey['CompositeKeySampleData']):
    __slots__ = ['key_element1', 'key_element2', 'key_element3']

    key_element1: str
    key_element2: BaseSampleKey
    key_element3: str

    def __init__(self):
        super().__init__()
        self.key_element1 = None
        self.key_element2 = None
        self.key_element3 = None


class CompositeKeySampleData(TypedRecord[CompositeKeySampleKey]):
    __slots__ = ['key_element1', 'key_element2', 'key_element3']

    key_element1: str
    key_element2: BaseSampleKey
    key_element3: str

    def __init__(self):
        super().__init__()
        self.key_element1 = None
        self.key_element2 = None
        self.key_element3 = None


class SingletonSampleKey(TypedKey['SingletonSampleData']):
    __slots__ = []

    def __init__(self):
        super().__init__()


class SingletonSampleData(TypedRecord[SingletonSampleKey]):
    __slots__ = ['string_element']
    string_element: str

    def __init__(self):
        super().__init__()
        self.string_element = None


class IdBasedKeySampleKey(TypedKey['IdBasedKeySampleData']):
    __slots__ = ['id_']
    id_: ObjectId

    def __init__(self):
        super().__init__()
        self.id_ = None


class IdBasedKeySampleData(TypedRecord[IdBasedKeySampleKey]):
    __slots__ = ['string_element']
    string_element: str

    def __init__(self):
        super().__init__()
        self.string_element = ''


class TestMongoKey(unittest.TestCase):
    def test_composite_key(self):
        rec = CompositeKeySampleData()
        rec.key_element1 = 'abc'
        rec.key_element2 = BaseSampleKey()
        rec.key_element2.record_id = 'def'
        rec.key_element2.record_index = 123
        rec.key_element3 = 'xyz'
        key_value = rec.to_key().value

        key = CompositeKeySampleKey()
        key.populate_from_string(key_value)
        self.assertEqual(key.key_element1, rec.key_element1)
        self.assertEqual(key.key_element2.record_id, rec.key_element2.record_id)
        self.assertEqual(key.key_element2.record_index, rec.key_element2.record_index)
        self.assertEqual(key.key_element3, rec.key_element3)

    def test_singleton_key(self):
        rec = SingletonSampleData()
        rec.string_element = 'abc'

        key_value = rec.to_key().value
        self.assertEqual(key_value, '')

        key = SingletonSampleKey()
        key.populate_from_string(key_value)
        self.assertEqual(key.value, key_value)

    def test_id_based_key(self):
        rec = IdBasedKeySampleData()
        rec.id_ = ObjectId.from_datetime(dt.datetime.fromtimestamp(123456789))
        rec.string_element = 'abc'

        key_value = rec.to_key().value
        key = IdBasedKeySampleKey()
        key.populate_from_string(key_value)
        self.assertEqual(key_value, str(key))


if __name__ == "__main__":
    unittest.main()
