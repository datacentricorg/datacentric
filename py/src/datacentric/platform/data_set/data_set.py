from datacentric.platform.context import Context
from datacentric.types.record import Key
from datacentric.types.record.record import Record
from bson.objectid import ObjectId
from typing import List


class DataSetData(Record):
    def __init__(self):
        Record.__init__(self)
        self.data_set_id = None  # type: str
        self.imports = None  # type: List[ObjectId]

    def init(self, context: Context):
        Record.init(self, context)


class DataSetKey(Key):
    def __init__(self, id_: str = None):
        Key.__init__(self)
        if id_ is None:
            self.data_set_id = None
        else:
            self.data_set_id = id_
