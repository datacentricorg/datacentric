from bson import ObjectId
from typing import List

from datacentric.platform.context import Context
from datacentric.types.record import TypedRecord, TypedKey


class DataSetKey(TypedKey['DataSetData']):
    __slots__ = ['data_set_id']
    data_set_id: str

    def __init__(self, id_: str = None):
        TypedKey.__init__(self)
        self.data_set_id = None
        if id_ is not None:
            self.data_set_id = id_


class DataSetData(TypedRecord[DataSetKey]):
    __slots__ = ['data_set_id', 'imports']

    data_set_id: str
    imports: List[ObjectId]

    def __init__(self):
        TypedRecord.__init__(self)
        self.data_set_id = None
        self.imports = None

    def init(self, context: Context):
        TypedRecord.init(self, context)
