from abc import ABC
from typing import TypeVar, Generic, List
from bson.objectid import ObjectId

from datacentric.platform.context import Context
from datacentric.types.record import Record, Key
from datacentric.platform.reflection.class_info import ClassInfo

TKey = TypeVar('TKey')


class TypedRecord(Generic[TKey], Record, ABC):
    __slots__ = []

    def __init__(self):
        Record.__init__(self)

    @property
    def key(self) -> str:
        key_type = ClassInfo.get_key_from_record(type(self))
        key_slots = key_type.__slots__
        data_slots = self.__slots__

        if len(key_slots) > len(data_slots):
            raise Exception(f'Key type {key_type.__name__} has more elements than {self.__name__}.')

        tokens: List[str] = []
        for slot in key_slots:
            token = Key.get_key_token(self, slot)
            tokens.append(token)

        return ';'.join(tokens)

    @key.setter
    def key(self, value: str):
        pass

    def to_key(self) -> TKey:
        key = TKey()
        key.assign_key_elements(self)
        return key

    # def r_save(self, save_to: ObjectId = None) -> None:
    #     if save_to is None:
    #         self.context.data_source.save(self, self.context.data_set)
    #     else:
    #         self.context.data_source.save(self, self.context.data_set, save_to)
    #
    # def r_delete(self, context: Context, delete_in: ObjectId = None) -> None:
    #     context.data_source.delete(self, self.key, delete_in)
