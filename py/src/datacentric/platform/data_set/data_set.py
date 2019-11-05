from datacentric.platform.context import Context
from datacentric.types.record import TypedRecord, TypedKey
from bson import ObjectId
from typing import List


class DataSetData(TypedRecord['DataSetKey']):
    __slots__ = ['imports']

    def __init__(self):
        TypedRecord.__init__(self)
        self.data_set_id = None  # type: str
        self.imports = None  # type: List[ObjectId]

    def init(self, context: Context):
        TypedRecord.init(self, context)


class DataSetKey(TypedKey[DataSetData]):
    __slots__ = ['data_set_id']

    def __init__(self, id_: str = None):
        TypedKey.__init__(self)
        if id_ is None:
            self.data_set_id = None
        else:
            self.data_set_id = id_
