from abc import ABC
from bson import ObjectId

from datacentric.platform.context import Context
from datacentric.types.record import Record


class RootRecord(Record, ABC):
    def __init__(self):
        Record.__init__(self)

    def init(self, context: Context) -> None:
        Record.init(self, context)
        self.data_set = ObjectId('000000000000000000000000')
