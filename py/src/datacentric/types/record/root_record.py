from abc import ABC
from bson import ObjectId
from typing import TypeVar, Generic

from datacentric.platform.context import Context
from datacentric.types.record import TypedRecord

TKey = TypeVar('TKey')


class RootRecord(TypedRecord[TKey], ABC):
    __slots__ = []

    def __init__(self):
        TypedRecord.__init__(self)

    def init(self, context: Context) -> None:
        TypedRecord.init(self, context)
        self.data_set = ObjectId('000000000000000000000000')
