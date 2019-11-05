from abc import ABC, abstractmethod

from datacentric.platform.context import Context
from datacentric.types.record import Data
from bson.objectid import ObjectId


class Record(Data, ABC):

    def __init__(self):
        Data.__init__(self)
        self.context = None  # type: Context
        self.id_ = None  # type: ObjectId
        self.data_set = None  # type: ObjectId
        self.key_ = None  # type: str

    def init(self, context: Context) -> None:
        if context is None:
            raise Exception(f'Null context is passed to the Init(...) method for {type(self).__name__}."')
        self.context = context

    @property
    def key(self) -> str:
        raise NotImplemented

    @abstractmethod
    def to_key(self) -> 'Key':
        pass

    # def save(self, save_to: ObjectId = None) -> None:
    #     raise NotImplemented
    #
    # def delete(self, context: Context, load_from: ObjectId = None) -> None:
    #     raise NotImplemented
