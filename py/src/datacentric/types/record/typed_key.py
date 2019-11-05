from abc import ABC
from typing import TypeVar, Generic
from bson import ObjectId

from datacentric.platform.context import Context
from datacentric.platform.reflection import ClassInfo
from datacentric.types.record import Key, DeletedRecord

TRecord = TypeVar('TRecord')


class TypedKey(Generic[TRecord], Key, ABC):
    """Base class of a foreign key.
    Generic parameter TRecord make it possible to bound key type to its record.
    Any elements of defined in the class derived from this one
    become key tokens.
    """
    def __init__(self):
        super().__init__()

    def load(self, context: Context, load_from: ObjectId = None) -> TRecord:
        """Load record from context.data_source
        Error message if the record is not found or is a DeletedRecord.
        """
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
        if result is not None and self.value != result.key:
            if isinstance(result, DeletedRecord):
                raise Exception(f'Delete marker with Type={type(result).__name__} stored for '
                                f'Key={self.value} has a non-matching Key={result.key_}.')
            else:
                raise Exception(f'Delete marker with Type={type(result).__name__} stored for '
                                f'Key={self.value} has a non-matching Key={result.key_}.')
        return result

    def delete(self, context: Context, delete_in: ObjectId = None) -> None:
        context.data_source.delete(self, delete_in)

    def populate_from(self, record: TRecord) -> None:
        root_type_name = ClassInfo.get_root_type(type(self))
        record_elements = type(record).__slots__
        key_elements = type(self).__slots__

        if len(record_elements) < len(key_elements):
            raise Exception(f'Root data type {root_type_name} has fewer elements than key type {type(self).__name__}.')

        for key_element in key_elements:
            value = record.__getattribute__(key_element)
            self.__setattr__(key_element, value)

