import unittest

from datacentric.platform.reflection import ClassInfo
from datacentric.types.record import TypedRecord, TypedKey, Data


class BaseKey(TypedKey['BaseData']):
    pass


class BaseData(TypedRecord[BaseKey]):
    pass


class DerivedData(BaseData):
    pass


class SampleData(TypedRecord['SampleKey']):
    pass


class SampleKey(TypedKey[SampleData]):
    pass


class SimpleData(Data):
    pass


class TestClassInfo(unittest.TestCase):
    def test_root_type(self):
        with self.assertRaises(Exception):
            ClassInfo.get_root_type(TypedKey[BaseData])
        with self.assertRaises(Exception):
            ClassInfo.get_root_type(ClassInfo)
        self.assertTrue(ClassInfo.get_root_type(BaseKey) == BaseKey)
        self.assertTrue(ClassInfo.get_root_type(BaseData) == BaseData)
        self.assertTrue(ClassInfo.get_root_type(DerivedData) == BaseData)
        self.assertTrue(ClassInfo.get_root_type(SimpleData) == SimpleData)

    def test_mapped_name(self):
        self.assertTrue(ClassInfo.get_mapped_class_name(BaseKey) == 'Base')
        self.assertTrue(ClassInfo.get_mapped_class_name(BaseData) == 'Base')
        self.assertTrue(ClassInfo.get_mapped_class_name(DerivedData) == 'Derived')

    def test_key_type(self):
        self.assertEqual(ClassInfo.get_key_from_record(BaseData), BaseKey)
        self.assertEqual(ClassInfo.get_key_from_record(DerivedData), BaseKey)
        self.assertEqual(ClassInfo.get_key_from_record(SampleData), SampleKey)


if __name__ == "__main__":
    unittest.main()
