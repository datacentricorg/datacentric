import importlib
import inspect
import os
import pkgutil
from typing import Dict, List

from datacentric.platform.reflection import ClassMapSettings
from datacentric.types.record import Record


class ClassInfo:
    __is_initialized = False
    __data_types_map = dict()  # type: Dict[str, type]

    @staticmethod
    def get_type(name: str) -> type:
        if not ClassInfo.__is_initialized:
            ClassInfo.__initialize_typ_map()

            from datacentric.types.record import TypedRecord, TypedKey
            children = ClassInfo.__get_imported_records(TypedRecord, [])
            children = ClassInfo.__get_imported_records(TypedKey, children)
            for child in children:
                if child not in ClassInfo.__data_types_map:
                    ClassInfo.__data_types_map[child.__name__] = child
        if name not in ClassInfo.__data_types_map:
            raise KeyError
        return ClassInfo.__data_types_map[name]

    @staticmethod
    def __get_imported_records(type_: type, children: List[type]):
        # children.append(type_)
        current_children = type_.__subclasses__()
        for t in current_children:
            ClassInfo.__get_imported_records(t, children)
        children.extend(current_children)
        return children

    @staticmethod
    def get_key_from_record(type_: type) -> type:
        key_type = type_.__orig_bases__[0].__args__[0]
        if '__forward_arg__' in dir(key_type):
            forward_arg = type_.__orig_bases__[0].__args__[0].__forward_arg__
            key_type = ClassInfo.get_type(forward_arg)
        return key_type

    @staticmethod
    def get_mapped_class_name(type_: type) -> str:
        mapped_class_name = type_.__name__

        for prefix in ClassMapSettings.ignored_class_name_prefixes():
            if mapped_class_name.startswith(prefix):
                mapped_class_name = mapped_class_name[len(prefix):]
                break

        for suffix in ClassMapSettings.ignored_class_name_suffixes():
            if mapped_class_name.endswith(suffix):
                mapped_class_name = mapped_class_name[:len(mapped_class_name) - len(suffix)]
                break

        return mapped_class_name

    @staticmethod
    def get_root_type(type_: type) -> type:
        record_base_name = 'TypedRecord'
        key_base_name = 'TypedKey'

        if type_.__name__ == record_base_name or type_.__name__ == key_base_name:
            raise TypeError(f'{record_base_name} and {key_base_name} are not allowed.')

        mro = inspect.getmro(type_)
        base_names = [x.__name__ for x in mro]

        if record_base_name in base_names:
            rec_idx = base_names.index(record_base_name)
            return mro[rec_idx - 1]
        elif key_base_name in base_names:
            key_idx = base_names.index(key_base_name)
            return mro[key_idx - 1]
        else:
            raise TypeError(f'Cannot find root type for {type_.__name__}. '
                            f'{record_base_name} and {key_base_name} are not found in mro.')

    @staticmethod
    def __initialize_typ_map():
        ClassInfo.__explore_package('datacentric')
        ClassInfo.__is_initialized = True

    @staticmethod
    def __explore_package(module_name):
        loader = pkgutil.get_loader(module_name)
        if os.path.basename(loader.path) == '__init__.py':
            package_path = os.path.dirname(loader.path)
        else:
            package_path = loader.path

        packages = pkgutil.walk_packages([package_path])
        for sub_module in packages:
            if not sub_module.ispkg:
                inner_module = importlib.import_module('.' + sub_module.name, module_name)
                module_attrs = dir(inner_module)

                for element_name in module_attrs:
                    element = getattr(inner_module, element_name)
                    if inspect.isclass(element) and issubclass(element, Record):
                        ClassInfo.__data_types_map[element.__name__] = element

            qualified_name = module_name + "." + sub_module.name
            ClassInfo.__explore_package(qualified_name)
