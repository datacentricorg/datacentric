from abc import ABC
from typing import TypeVar
from bson import ObjectId

from datacentric.platform.context import Context
from datacentric.types.record import TypedRecord

TKey = TypeVar('TKey')


class RootRecord(TypedRecord[TKey], ABC):
    __slots__ = []

    def __init__(self):
        TypedRecord.__init__(self)
        self.data_set = ObjectId('000000000000000000000000')

    def init(self, context: Context) -> None:
        TypedRecord.init(self, context)
