import datetime as dt
import time
import inspect
from typing import Dict, List
import numpy as np
import inflection

from datacentric.platform.reflection.class_info import ClassInfo
from datacentric.types.record import Record


# Serialization: object -> dict

def serialize(obj: Record):
    dict_ = dict()
    dict_['_t'] = obj.__class__.__name__
    dict_['_key'] = obj.key
    dict_['_dataset'] = obj.data_set
    for slot in obj.__slots__:
        val = obj.__getattribute__(slot)
        if inspect.isclass(val):
            serialized_value = _serialize_class(val)
        elif type(val) == dt.date:
            serialized_value = _date_to_iso_int(val)
        elif type(val) == dt.time:
            serialized_value = _time_to_iso_int(val)
        elif type(val) == dt.datetime:
            serialized_value = _date_time_to_int(val)
        else:
            serialized_value = val

        if val is not None:
            dict_[_to_pascal_case(slot)] = serialized_value
    return dict_


def _serialize_class(obj):
    dict_ = dict()
    dict_['_t'] = obj.__class__.__name__
    for slot in obj.__slots__:
        val = obj.__getattribute__(slot)
        if inspect.isclass(val):
            serialized_value = _serialize_class(val)
        elif type(val) == dt.date:
            serialized_value = _date_to_iso_int(val)
        elif type(val) == dt.time:
            serialized_value = _time_to_iso_int(val)
        elif type(val) == dt.datetime:
            serialized_value = _date_time_to_int(val)
        else:
            serialized_value = val

        if val is not None:
            dict_[_to_pascal_case(slot)] = serialized_value
    return dict_


def _to_pascal_case(name: str):
    return ''.join(x for x in name.title() if not x == '_')


def _date_to_iso_int(date: dt.date):
    return date.year * 10_000 + date.month * 100 + date.day


def _time_to_iso_int(time_: dt.time):
    # todo: microseconds rounding
    return time_.hour * 100_00_000 + time_.minute * 100_000 + time_.second * 1000 + time_.microsecond * 1000


def _date_time_to_int(datetime: dt.datetime):
    milliseconds = datetime.microsecond / 1000
    return time.mktime(datetime.utctimetuple()) * 1000 + milliseconds


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
        if member_type.__module__ == 'typing':
            value = _deserialize_list(member_type, dict_[camel_case_slot])
        elif member_type == str:
            value = dict_[camel_case_slot]
        elif member_type == dt.datetime:
            value = dict_[camel_case_slot]
        elif member_type == np.array:
            value = np.array(dict_[camel_case_slot])
        elif member_type == bool:
            value = dict_[camel_case_slot]
        elif member_type == dt.datetime:
            value = dict_[camel_case_slot]
        elif member_type == dt.date:
            value = _parse_int_to_date(dict_[camel_case_slot])
        elif member_type == dt.time:
            value = dict_[camel_case_slot]
        elif member_type == int:
            value = dict_[camel_case_slot]
        elif member_type == float:
            value = dict_[camel_case_slot]
        else:
            raise TypeError('Cannot deduce type')

        new_obj.__setattr__(slot, value)
    return new_obj


def _parse_int_to_date(iso_int: int):
    year = int(iso_int / 100_00)
    iso_int -= year * 100_00
    month = int(iso_int / 100)
    iso_int -= month * 100
    day = iso_int
    return dt.date(year, month, day)
