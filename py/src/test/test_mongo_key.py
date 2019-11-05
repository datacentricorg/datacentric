import unittest

from datacentric.types.record import TypedRecord, TypedKey


class BaseSampleData(TypedRecord['BaseSampleKey']):
    pass


class BaseSampleKey(TypedKey[BaseSampleData]):
    __slots__ = ['record_id', 'record_index']

    record_id: str
    record_index: int

    def __init__(self):
        super().__init__()
        self.record_id = None
        self.record_index = None


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


if __name__ == "__main__":
    unittest.main()
