from abc import ABC, abstractmethod
from bson import ObjectId

from datacentric.platform.context import Context
from datacentric.types.record import Data


class Record(Data, ABC):
    """Base class of records stored in data source.
    """
    __slots__ = ['context', 'id_', 'data_set', '_key']

    context: Context
    id_: ObjectId
    data_set: ObjectId
    _key: str

    def __init__(self):
        super().__init__()

        self.context = None
        """Execution context"""

        self.id_ = None
        """ObjectId of the record is specific to its version.
        For the record's history to be captured correctly, all
        update operations must assign a new ObjectId with the
        timestamp that matches update time.
        """

        self.data_set = None
        """ObjectId of the dataset where the record is stored.
        For records stored in root dataset, the value of
        data_set element should be ObjectId('000000000000000000000000').
        """

        self._key = None
        """Backing attribute for key() property."""

    def init(self, context: Context) -> None:
        if context is None:
            raise Exception(f'Null context is passed to the Init(...) method for {type(self).__name__}."')
        self.context = context

    @property
    @abstractmethod
    def key(self) -> str:
        """String key consists of semicolon delimited primary key elements:

        key_element1;key_element2

        To avoid serialization format uncertainty, key elements
        can have any atomic type except float.
        """
        pass
