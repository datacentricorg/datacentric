import datetime as dt
from abc import ABC
from bson import ObjectId

from datacentric.types.record import Data


class Key(Data, ABC):
    __slots__ = []

    def __init__(self):
        Data.__init__(self)

    @property
    def value(self) -> str:
        raise NotImplemented

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
        # TODO: introduce type?
        # elif attr_type == minute:
        elif attr_type == dt.datetime:
            raise NotImplemented
        elif attr_type in [bool, int, ObjectId]:
            raise NotImplemented
        # TODO:
        # elif attr_type in [Key, Enum]:
        else:
            raise ValueError(f'Key element {slot} of type {type(obj).__name__} has type {attr_type.__name__} '
                             f'that is not one of the supported key element types. Available key element types are '
                             f'string, double, bool, int, long, LocalDate, LocalTime, LocalMinute, LocalDateTime, '
                             f'LocalMinute, ObjectId, or Enum.')

    def assign_string(self, value: str) -> None:
        raise NotImplemented
