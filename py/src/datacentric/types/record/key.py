from abc import ABC

from datacentric.platform.context import Context
from datacentric.types.record import Data
from datacentric.types.record import Record
from bson.objectid import ObjectId


class Key(Data):
    def __init__(self):
        Data.__init__(self)

    def value(self) -> str:
        raise NotImplemented

    @staticmethod
    def get_key_token(obj, element) -> str:
        raise NotImplemented

    def assign_string(self):
        raise NotImplemented

    def load(self, context: Context, load_from: ObjectId = None) -> Record:
        raise NotImplemented

    def load_or_null(self, context: Context, load_from: ObjectId) -> Record:
        raise NotImplemented

    def delete(self, context: Context, load_from: ObjectId = None) -> None:
        raise NotImplemented

    def assign_key_elements(self, record) -> None:
        raise NotImplemented
