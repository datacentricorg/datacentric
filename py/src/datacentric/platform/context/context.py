from bson.objectid import ObjectId


# To avoid circular reference
# from datacentric.platform.data_source import DataSourceData


class Context:
    def __init__(self):
        self.data_source = None  # type_: 'DataSourceData'
        self.data_set = None  # type: ObjectId
        self.log = None
