import datetime as dt
from abc import ABC
from enum import Enum
from typing import List
from bson import ObjectId

from datacentric.types.time.local_minute import LocalMinute
from datacentric.types.record import Data
import datacentric.types.time.date_ext as date_ext


class Key(Data, ABC):
    """Base class of a foreign key. Any slots defined in
    type specific become key tokens. Property value and str(self)
    consists of key tokens with semicolon delimiter.
    """
    __slots__ = []

    def __init__(self):
        super().__init__()

    def __str__(self):
        return self.value

    @property
    def value(self) -> str:
        """String key consists of semicolon delimited primary key elements:

        key_element1;key_element2

        To avoid serialization format uncertainty, key elements
        can have any atomic type except float.
        """
        tokens = []
        element_array = self.__slots__
        if type(self.__slots__) is str:
            element_array = [element_array]

        for element in element_array:
            tokens.append(self.get_key_token(self, element))
        return ';'.join(tokens)

    @staticmethod
    def get_key_token(obj: object, slot: str) -> str:
        """Convert key element to string key token.
        """
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
        """Populate key attributes from semicolon delimited string.
        Attributes that are themselves keys may use more than
        one token.

        If key AKey has two elements, B and C, where

        * B has type BKey which has two string elements, and
        * C has type string,

        the semicolon delimited key has the following format:

        BToken1;BToken2;CToken
        """
        tokens = value.split(';')
        token_index = self.__populate_from_string(tokens, 0)

        if len(tokens) != token_index:
            raise Exception(f'Key with type {type(self).__name__} requires {token_index} tokens including '
                            f'any composite key elements, while key value {value} contains {len(tokens)} tokens.')

    def __populate_from_string(self, tokens: List[str], token_index: int) -> int:
        """Populate key elements from an array of tokens starting
        at the specified token index.
        """
        slots = self.__slots__
        if type(slots) is str:
            slots = [slots]

        # Singleton key case
        if len(slots) == 0:
            if len(tokens) != 1 or tokens[0] != '':
                raise Exception(f'Type {type(self).__name__} has key {";".join(tokens)} while '
                                f'for a singleton the key must be an empty string. '
                                f'Singleton key is a key that has no key elements.')
            return 1
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
        annotations = type(self).__annotations__
        for slot in slots:
            member_type = annotations[slot]
            if member_type == type(float):
                raise Exception(f'Key element {slot} has type Double. Elements of this type '
                                f'cannot be part of key due to serialization format uncertainty.')
            if issubclass(member_type, Key):
                key_element = member_type()
                token_index = key_element.__populate_from_string(tokens, token_index)
                self.__setattr__(slot, key_element)
                continue

            # Check that token is not empty
            token = tokens[token_index]
            if token == '':
                raise Exception(f'Key {";".join(tokens)} for key type {type(self).__name__} contains an empty token.')

            if member_type == str:
                value = token
            elif member_type == bool:
                value = bool(token.capitalize())
            elif member_type == int:
                value = int(token)
            elif member_type == dt.date:
                value = date_ext.iso_int_to_date(int(token))
            elif member_type == dt.time:
                value = date_ext.iso_int_to_time(int(token))
            elif member_type == LocalMinute:
                value = date_ext.iso_int_to_local_minute(int(token))
            elif member_type == dt.datetime:
                value = date_ext.iso_int_to_date_time(int(token))
            elif member_type == ObjectId:
                value = ObjectId(token)
            elif issubclass(member_type, Enum):
                value = member_type[token]
            else:
                raise Exception(f'Unexpected type {member_type.__name__} in key tokens.')

            self.__setattr__(slot, value)
            token_index += 1

        return token_index
