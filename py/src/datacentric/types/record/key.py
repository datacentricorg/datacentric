import datetime as dt
from abc import ABC
from enum import Enum
from typing import List
from bson import ObjectId

from datacentric.types.local_minute import LocalMinute
from datacentric.types.record import Data
import datacentric.types.date_ext as date_ext


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
            return str(date_ext.date_to_iso_int(attr_value))
        elif attr_type == dt.time:
            return str(date_ext.time_to_iso_int(attr_value))
        elif attr_type == LocalMinute:
            return str(date_ext.minute_to_iso_int(attr_value))
        elif attr_type == dt.datetime:
            return str(date_ext.date_time_to_iso_int(attr_value))
        elif attr_type == bool:
            if attr_value:
                return 'true'
            else:
                return 'false'
        elif attr_type == int:
            return str(attr_value)
        elif attr_type == ObjectId:
            return str(attr_value)
        elif issubclass(attr_type, Key):
            return str(attr_value)
        elif issubclass(attr_type, Enum):
            return attr_value.name
        else:
            raise ValueError(f'Key element {slot} of type {type(obj).__name__} has type {attr_type.__name__} '
                             f'that is not one of the supported key element types. Available key element types are '
                             f'string, double, bool, int, long, LocalDate, LocalTime, LocalMinute, LocalDateTime, '
                             f'LocalMinute, ObjectId, or Enum.')

    def populate_from_string(self, value: str) -> None:
        tokens = value.split(';')
        token_index = self.__populate_from_string(tokens, 0)

        if len(tokens) != token_index:
            raise Exception(f'Key with type {type(self).__name__} requires {token_index} tokens including '
                            f'any composite key elements, while key value {value} contains {len(tokens)} tokens.')

    def __populate_from_string(self, tokens: List[str], token_index: int) -> int:
        slots = self.__slots__

        # Singleton key case
        if len(slots) == 0:
            if len(tokens) != 1 or tokens[0] != '':
                raise Exception(f'Type {type(self).__name__} has key {";".join(tokens)} while '
                                f'for a singleton the key must be an empty string. '
                                f'Singleton key is a key that has no key elements.')
        if len(tokens) - token_index < len(slots):
            raise Exception(f'Key of type {type(self).__name__} requires at least {len(slots)} elements '
                            f'{";".join(slots)} while there are only {len(tokens) - token_index} remaining key tokens:'
                            f'{";".join(tokens)}')

        """
        Iterate over element info elements, advancing tokenIndex by the required
        number of tokens for each element. In case of embedded keys, the value of
        tokenIndex is advanced by the recursive call to InitFromTokens method
        of the embedded key.
        """
        for slot in slots:
            a = slot

        raise NotImplemented
