import unittest

from datacentric.platform.reflection import ClassInfo
from datacentric.types.record import TypedRecord, TypedKey, Data


class BaseKey(TypedKey['BaseRecord']):
    pass


class BaseRecord(TypedRecord[BaseKey]):
    pass


class DerivedRecord(BaseRecord):
    pass


class ElementData(Data):
    pass


class TestClassInfo(unittest.TestCase):
    def test_root_type(self):
        with self.assertRaises(Exception):
            ClassInfo.get_root_type(TypedKey[BaseRecord])
        with self.assertRaises(Exception):
            ClassInfo.get_root_type(ClassInfo)
        self.assertTrue(ClassInfo.get_root_type(BaseKey) == BaseKey)
        self.assertTrue(ClassInfo.get_root_type(BaseRecord) == BaseRecord)
        self.assertTrue(ClassInfo.get_root_type(DerivedRecord) == BaseRecord)
        self.assertTrue(ClassInfo.get_root_type(ElementData) == ElementData)

    def test_key_type(self):
        self.assertEqual(ClassInfo.get_key_from_record(BaseRecord), BaseKey)
        self.assertEqual(ClassInfo.get_key_from_record(DerivedRecord), BaseKey)


if __name__ == "__main__":
    unittest.main()
