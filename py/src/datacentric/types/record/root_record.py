from abc import ABC

from datacentric.platform.context import Context
from datacentric.types.record import Record
from bson.objectid import ObjectId


class RootRecord(Record, ABC):
    def __init__(self):
        Record.__init__(self)

    def init(self, context: Context) -> None:
        Record.init(self, context)
        self.data_set = ObjectId('000000000000000000000000')
