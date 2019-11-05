from bson.objectid import ObjectId

# from datacentric.platform.data_source import DataSourceData


class Context:
    data_source = None  # type_: DataSourceData
    data_set = None  # type: ObjectId

    def __init__(self):
        pass
