import unittest

from datacentric.platform.reflection import ClassInfo
from datacentric.types.record import TypedRecord, TypedKey


class BaseKey(TypedKey['BaseData']):
    pass


class BaseData(TypedRecord[BaseKey]):
    pass


class DerivedData(BaseData):
    pass


class TestClassInfo(unittest.TestCase):
    def test_root_type(self):
        self.assertTrue(ClassInfo.get_root_type(BaseKey) == BaseKey)
        self.assertTrue(ClassInfo.get_root_type(BaseData) == BaseData)
        self.assertTrue(ClassInfo.get_root_type(DerivedData) == BaseData)

    def test_mapped_name(self):
        self.assertTrue(ClassInfo.get_mapped_class_name(BaseKey) == 'Base')
        self.assertTrue(ClassInfo.get_mapped_class_name(BaseData) == 'Base')
        self.assertTrue(ClassInfo.get_mapped_class_name(DerivedData) == 'Derived')


if __name__ == "__main__":
    unittest.main()
