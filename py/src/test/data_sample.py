import datetime as dt
import numpy as np
from enum import Enum
from typing import List

from datacentric.types.local_minute import LocalMinute
from datacentric.types.record import TypedRecord, TypedKey, Data


class ElementSampleData(Data):
    __slots__ = ['double_element3', 'string_element3']
    double_element3: float
    string_element3: str

    def __init__(self):
        super().__init__()
        self.double_element3 = None
        self.string_element3 = None


class SampleEnum(Enum):
    Empty = 0,
    EnumValue1 = 1,
    EnumValue2 = 2


class BaseSampleData(TypedRecord['BaseSampleKey']):
    __slots__ = ['record_id',
                 'record_index',
                 'double_element',
                 'local_date_element',
                 'local_time_element',
                 'local_minute_element',
                 'local_date_time_element',
                 'enum_value',
                 'version']
    record_id: str
    record_index: int
    double_element: float
    local_date_element: dt.date
    local_time_element: dt.time
    local_minute_element: LocalMinute
    local_date_time_element: dt.datetime
    enum_value: SampleEnum
    version: int

    def __init__(self):
        super().__init__()
        self.record_id = None
        self.record_index = None
        self.double_element = None
        self.local_date_element = None
        self.local_time_element = None
        self.local_minute_element = None
        self.local_date_time_element = None
        self.enum_value = None
        self.version = None


class BaseSampleKey(TypedKey[BaseSampleData]):
    __slots__ = ['record_id', 'record_index']

    record_id: str
    record_index: int

    def __init__(self):
        super().__init__()
        self.record_id = None
        self.record_index = None


class DerivedSampleData(BaseSampleData):
    __slots__ = ['double_element2', 'string_element2', 'list_of_string', 'list_of_double', 'list_of_nullable_double',
                 'data_element', 'data_element_list', 'key_element', 'key_element_list']
    double_element2: float
    string_element2: str
    list_of_string: List[str]
    list_of_double: np.ndarray
    list_of_nullable_double: np.ndarray
    data_element: ElementSampleData
    data_element_list: List[ElementSampleData]
    key_element: BaseSampleKey
    key_element_list: List[BaseSampleKey]

    def __init__(self):
        super().__init__()
        self.double_element2 = None
        self.string_element2 = None
        self.list_of_string = None
        self.list_of_double = None
        self.list_of_nullable_double = None
        self.data_element = None
        self.data_element_list = None
        self.key_element = None
        self.key_element_list = None
