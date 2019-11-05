from bson.objectid import ObjectId


# To avoid circular reference
# from datacentric.platform.data_source import DataSourceData
from datacentric.platform.logging.log import Log


class Context:
    data_source: 'DataSourceData'
    data_set: ObjectId
    log: Log
    
    def __init__(self):
        self.data_source = None
        self.data_set = None
        self.log = None
