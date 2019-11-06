from bson import ObjectId

from datacentric.types.record import TypedKey, TypedRecord


class DataSetDetailKey(TypedKey['DataSetDetail']):
    __slots__ = ['data_set_id']
    data_set_id: ObjectId

    def __init__(self):
        super().__init__()
        self.data_set_id = None


class DataSetDetail(TypedRecord[DataSetDetailKey]):
    __slots__ = ['data_set_id', 'read_only', 'cutoff_time', 'imports_cutoff_time']
    data_set_id: ObjectId
    read_only: bool
    cutoff_time: ObjectId
    imports_cutoff_time: ObjectId

    def __init__(self):
        super().__init__()
        self.data_set_id = None
        self.read_only = None
        self.cutoff_time = None
        self.imports_cutoff_time = None
