from abc import ABC
from typing import TypeVar, Generic
from bson.objectid import ObjectId

from datacentric.platform.context import Context
from datacentric.types.record import Record

TKey = TypeVar('TKey')


class TypedRecord(Generic[TKey], Record, ABC):
    __slots__ = []

    def __init__(self):
        Record.__init__(self)

    @property
    def key(self) -> str:
        raise NotImplemented

    @key.setter
    def key(self, value: str) -> str:
        pass

    def to_key(self) -> TKey:
        key = TKey()
        key.assign_key_elements(self)
        return key

    def r_save(self, save_to: ObjectId = None) -> None:
        if save_to is None:
            self.context.data_source.save(self, self.context.data_set)
        else:
            self.context.data_source.save(self, self.context.data_set, save_to)

    def r_delete(self, context: Context, delete_in: ObjectId = None) -> None:
        context.data_source.delete(self, self.key, delete_in)
