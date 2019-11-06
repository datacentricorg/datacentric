from bson.objectid import ObjectId

from datacentric.platform.logging.log import Log


class Context:
    """Context defines dataset and provides access to data,
    logging, and other supporting functionality.
    """

    data_source: 'DataSource'
    data_set: ObjectId
    log: Log

    def __init__(self):
        self.data_source = None
        """Default data source of the context."""
        self.data_set = None
        """Default dataset of the context."""
        self.log = None
        """Logging interface."""
