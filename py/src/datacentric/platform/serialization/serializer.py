import datetime as dt
import inspect
from enum import Enum
from typing import Dict, List
import numpy as np
import inflection

import datacentric.types.date_ext as date_ext
from datacentric.platform.reflection.class_info import ClassInfo
from datacentric.types.local_minute import LocalMinute
from datacentric.types.record import Record


# Serialization: object -> dict

def serialize(obj: Record):
    dict_ = dict()
    dict_['_t'] = obj.__class__.__name__
    dict_['_key'] = obj.key
    dict_['_dataset'] = obj.data_set
    for slot in obj.__slots__:
        value = obj.__getattribute__(slot)
        value_type = type(value)
        if value is None:
            continue
        elif inspect.isclass(value):
            serialized_value = _serialize_class(value)
        elif value_type == LocalMinute:
            serialized_value = date_ext.minute_to_iso_int(value)
        elif value_type == dt.date:
            serialized_value = date_ext.date_to_iso_int(value)
        elif value_type == dt.time:
            serialized_value = date_ext.time_to_iso_int(value)
        elif value_type == dt.datetime:
            serialized_value = date_ext.date_time_to_iso_int(value)
        elif issubclass(value_type, Enum):
            serialized_value = value.name
        else:
            serialized_value = value

        if value is not None:
            dict_[_to_pascal_case(slot)] = serialized_value
    return dict_


def _serialize_class(obj):
    dict_ = dict()
    dict_['_t'] = obj.__class__.__name__
    for slot in obj.__slots__:
        value = obj.__getattribute__(slot)
        value_type = type(value)
        if inspect.isclass(value):
            serialized_value = _serialize_class(value)
        elif value_type == LocalMinute:
            serialized_value = date_ext.minute_to_iso_int(value)
        elif value_type == dt.date:
            serialized_value = date_ext.date_to_iso_int(value)
        elif value_type == dt.time:
            serialized_value = date_ext.time_to_iso_int(value)
        elif value_type == dt.datetime:
            serialized_value = date_ext.date_time_to_iso_int(value)
        else:
            serialized_value = value

        if value is not None:
            dict_[_to_pascal_case(slot)] = serialized_value
    return dict_


def _to_pascal_case(name: str):
    return ''.join(x for x in name.title() if not x == '_')


# Deserialization: dict -> object

def _deserialize_list(member_type, param):
    arg = member_type.__args__[0]
    return param


def deserialize(dict_: Dict):
    type_name = dict_['_t']  # type: str

    type_info = ClassInfo.get_type(type_name)
    new_obj = type_info()
    new_obj.__setattr__('_t', type_name)
    new_obj.__setattr__('_dataset', dict_['_dataset'])
    new_obj.__setattr__('_key', dict_['_key'])
    new_obj.__setattr__('id', dict_['_id'])
    annotations = type_info.__annotations__
    slots__ = type_info.__slots__
    mro = inspect.getmro(type_info)
    a = [x for x in mro if issubclass(x, Record) and x is not Record]

    for slot in slots__:
        member_type = annotations[slot]
        camel_case_slot = inflection.camelize(slot)
        slot_value = dict_[camel_case_slot]
        if member_type.__module__ == 'typing':
            value = _deserialize_list(member_type, slot_value)
        elif member_type == str:
            value = slot_value
        elif member_type == np.array:
            value = np.array(slot_value)
        elif member_type == bool:
            value = slot_value
        elif member_type == LocalMinute:
            value = date_ext.iso_int_to_local_minute(slot_value)
        elif member_type == dt.datetime:
            value = date_ext.iso_int_to_date_time(slot_value)
        elif member_type == dt.date:
            value = date_ext.iso_int_to_date(slot_value)
        elif member_type == dt.time:
            value = date_ext.iso_int_to_time(slot_value)
        elif member_type == int:
            value = slot_value
        elif member_type == float:
            value = slot_value
        else:
            raise TypeError('Cannot deduce type')

        new_obj.__setattr__(slot, value)
    return new_obj



