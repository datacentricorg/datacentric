import datetime as dt
from abc import ABC
from enum import Enum

from bson import ObjectId

from datacentric.types.local_minute import LocalMinute
from datacentric.types.record import Data


class Key(Data, ABC):
    __slots__ = []

    def __init__(self):
        super().__init__()

    def __str__(self):
        return self.value

    @property
    def value(self) -> str:
        tokens = []
        element_array = self.__slots__
        for element in element_array:
            tokens.append(self.get_key_token(self, element))
        return ';'.join(tokens)

    @staticmethod
    def get_key_token(obj: object, slot: str) -> str:
        attr_value = obj.__getattribute__(slot)
        attr_type = type(attr_value)
        if attr_value is None:
            raise ValueError(f'Key element {slot} of type {type(obj).__name__} is null. '
                             f'Null elements are not permitted in key.')
        elif attr_type == str:
            if attr_value == '':
                raise ValueError(f'String key element {slot} is empty. Empty elements are not permitted in key.')
            if ';' in attr_value:
                raise ValueError(f'Key element {slot} of type {type(obj).__name__} includes semicolon delimiter. '
                                 f'The use of this delimiter is reserved for separating key tokens.')
            return attr_value
        elif attr_type == float:
            raise ValueError(f'Key element {slot} of type {type(obj).__name__} has type float. '
                             f'Elements of this type cannot be part of key due to serialization format uncertainty.')
        elif attr_type == dt.date:
            raise NotImplemented
        elif attr_type == dt.time:
            raise NotImplemented
        elif attr_type == LocalMinute:
            return attr_value.to_iso_int()
        elif attr_type == dt.datetime:
            raise NotImplemented
        elif attr_type in [bool, int, ObjectId]:
            raise NotImplemented
        elif issubclass(attr_type, Key):
            return str(attr_type)
        elif issubclass(attr_type, Enum):
            return attr_value.name
        else:
            raise ValueError(f'Key element {slot} of type {type(obj).__name__} has type {attr_type.__name__} '
                             f'that is not one of the supported key element types. Available key element types are '
                             f'string, double, bool, int, long, LocalDate, LocalTime, LocalMinute, LocalDateTime, '
                             f'LocalMinute, ObjectId, or Enum.')

    def assign_string(self, value: str) -> None:
        raise NotImplemented
