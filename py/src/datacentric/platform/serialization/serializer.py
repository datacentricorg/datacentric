import datetime as dt
import numpy as np
import re
import inspect
import itertools
from bson import ObjectId
from enum import Enum
from typing import Dict, Any, get_type_hints
from typing_inspect import get_origin, get_args

import datacentric.types.date_ext as date_ext
from datacentric.platform.reflection.class_info import ClassInfo
from datacentric.types.local_minute import LocalMinute
from datacentric.types.record import Record, Key, Data


# Serialization: object -> dict

def serialize(obj: Record):
    dict_ = _serialize_class(obj)
    dict_['_t'] = obj.__class__.__name__
    dict_['_dataset'] = obj.data_set
    dict_['_key'] = obj.key

    return dict_


def _serialize_class(obj):
    dict_ = dict()
    dict_['_t'] = obj.__class__.__name__
    mro = inspect.getmro(type(obj))
    from datacentric.types.record import Record, Data

    if Record in mro:
        idx = mro.index(Record)
        chain = mro[:idx]
    elif Data in mro:
        idx = mro.index(Data)
        chain = mro[:idx]
    else:
        raise Exception(f'Cannot serialize class {obj.__class__.__name__} not derived from Record or Data.')

    slot_lists = [x.__slots__ for x in chain]
    slots = list(itertools.chain.from_iterable(slot_lists))

    for slot in slots:
        value = obj.__getattribute__(slot)
        if value is None:
            continue

        value_type = type(value)
        if issubclass(value_type, Key):
            serialized_value = value.value
        elif issubclass(value_type, Data):
            serialized_value = _serialize_class(value)
        elif issubclass(value_type, Enum):
            serialized_value = value.name
        elif value_type is list:
            serialized_value = _serialize_list(value)
        else:
            serialized_value = _serialize_primitive(value)

        dict_[_to_pascal_case(slot)] = serialized_value
    return dict_


def _serialize_list(list_):
    result = []
    for value in list_:
        value_type = type(value)
        if issubclass(value_type, Key):
            result.append(value.value)
        elif issubclass(value_type, Data):
            result.append(_serialize_class(value))
        elif issubclass(value_type, Enum):
            result.append(value.name)
        elif value_type is list:
            raise Exception(f'List of lists are prohibited.')
        else:
            result.append(_serialize_primitive(value))
    return result


def _serialize_primitive(value):
    value_type = type(value)
    if value_type == LocalMinute:
        return date_ext.minute_to_iso_int(value)
    elif value_type == dt.date:
        return date_ext.date_to_iso_int(value)
    elif value_type == dt.time:
        return date_ext.time_to_iso_int(value)
    elif value_type == dt.datetime:
        return date_ext.date_time_to_iso_int(value)
    elif value_type == str:
        return value
    elif value_type == bool:
        return value
    elif value_type == int:
        return value
    elif value_type == float:
        return value
    elif value_type == ObjectId:
        return value
    # TODO: check for pymongo.binary.Binary to speed-up
    elif value_type == np.ndarray:
        return value.tolist()
    else:
        raise Exception(f'Cannot serialize type {value_type.__name__}')


def _to_pascal_case(name: str):
    return ''.join(x for x in name.title() if not x == '_')


# Deserialization: dict -> object
__first_cap_re = re.compile('(.)([A-Z][a-z]+)')
__all_cap_re = re.compile('([a-z0-9])([A-Z])')


def to_snake_case(name):
    s1 = __first_cap_re.sub(r'\1_\2', name)
    return __all_cap_re.sub(r'\1_\2', s1).lower()


def deserialize(dict_: Dict) -> Record:
    data_set = dict_.pop('_dataset')
    _key = dict_.pop('_key')
    id_ = dict_.pop('_id')

    new_obj = _deserialize_class(dict_)

    new_obj.__setattr__('data_set', data_set)
    new_obj.__setattr__('_key', _key)
    new_obj.__setattr__('id_', id_)

    return new_obj


def _deserialize_class(dict_: Dict[str, Any]):
    type_name: str = dict_.pop('_t')

    type_info = ClassInfo.get_type(type_name)
    new_obj = type_info()

    for dict_key, dict_value in dict_.items():
        slot = to_snake_case(dict_key)
        hints = get_type_hints(type_info)
        member_type = hints[slot]
        if get_origin(member_type) is not None and get_origin(member_type) is list:
            deserialized_value = _deserialize_list(member_type, dict_value)
        elif issubclass(member_type, Key):
            deserialized_value = member_type()
            deserialized_value.populate_from_string(dict_value)
        elif issubclass(member_type, Data):
            deserialized_value = _deserialize_class(dict_value)
        elif issubclass(member_type, Enum):
            deserialized_value = member_type[dict_value]
        else:
            deserialized_value = _deserialize_primitive(member_type, dict_value)

        new_obj.__setattr__(slot, deserialized_value)
    return new_obj


def _deserialize_list(type_, list_):
    expected_item_type = get_args(type_)[0]
    if issubclass(expected_item_type, Key):
        result = []
        for item in list_:
            deserialized_key = expected_item_type()
            deserialized_key.populate_from_string(item)
            result.append(deserialized_key)
        return result
    elif issubclass(expected_item_type, Data):
        return [_deserialize_class(x) for x in list_]
    elif issubclass(expected_item_type, Enum):
        return [expected_item_type[x] for x in list_]
    elif expected_item_type is list:
        raise Exception(f'List of lists are prohibited.')
    else:
        return [_deserialize_primitive(expected_item_type, x) for x in list_]


def _deserialize_primitive(expected_type, value):
    if expected_type == str:
        return value
    elif expected_type == np.ndarray:
        return np.array(value)
    elif expected_type == bool:
        return value
    elif expected_type == LocalMinute:
        return date_ext.iso_int_to_local_minute(value)
    elif expected_type == dt.datetime:
        return date_ext.iso_int_to_date_time(value)
    elif expected_type == dt.date:
        return date_ext.iso_int_to_date(value)
    elif expected_type == dt.time:
        return date_ext.iso_int_to_time(value)
    elif expected_type == int:
        return value
    elif expected_type == float:
        return value
    else:
        raise TypeError(f'Cannot deduce type {expected_type}')
