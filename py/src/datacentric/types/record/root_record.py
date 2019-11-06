from abc import ABC
from typing import TypeVar
from bson import ObjectId

from datacentric.platform.context import Context
from datacentric.types.record import TypedRecord

TKey = TypeVar('TKey')


class RootRecord(TypedRecord[TKey], ABC):
    """Base class of records stored in root dataset of the data store.
    This class overrides DataSet property to always return ObjectId('000000000000000000000000').
    """
    __slots__ = []

    def __init__(self):
        super().__init__()
        self.data_set = ObjectId('000000000000000000000000')

    def init(self, context: Context) -> None:
        super().init(context)
