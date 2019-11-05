from bson.objectid import ObjectId

from datacentric.platform.logging.log import Log


class Context:
    data_source: 'DataSourceData'
    data_set: ObjectId
    log: Log

    def __init__(self):
        self.data_source = None
        self.data_set = None
        self.log = None
