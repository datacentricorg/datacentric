from abc import ABC
from typing import TypeVar, Generic
from bson import ObjectId

from datacentric.platform.context import Context
from datacentric.types.record import Key, DeletedRecord

TRecord = TypeVar('TRecord')


class TypedKey(Generic[TRecord], Key, ABC):
    def __init__(self):
        super().__init__()

    def load(self, context: Context, load_from: ObjectId = None) -> TRecord:
        if load_from is None:
            result = self.load_or_null(context, context.data_set)
        else:
            result = self.load_or_null(context, load_from)
        if result is None:
            raise KeyError(f'Record with key {self.value} is not found in dataset with ObjectId={load_from}.')

        return result

    def load_or_null(self, context: Context, load_from: ObjectId) -> TRecord:
        if load_from is None:
            result = context.data_source.reload_or_null(context, context.data_set)
        else:
            result = context.data_source.reload_or_null(context, load_from)
        if result is not None and self.value() != result.key:
            if isinstance(result, DeletedRecord):
                raise Exception(f'Delete marker with Type={type(result).__name__} stored for '
                                f'Key={self.value()} has a non-matching Key={result.key_}.')
            else:
                raise Exception(f'Delete marker with Type={type(result).__name__} stored for '
                                f'Key={self.value()} has a non-matching Key={result.key_}.')
        return result

    def delete(self, context: Context, delete_in: ObjectId = None) -> None:
        context.data_source.delete(self, delete_in)

    def assign_key_elements(self, record) -> None:
        raise NotImplemented
