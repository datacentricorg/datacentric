import pkgutil
import importlib
import inspect
import os
from typing import Dict

from datacentric.types.record import Record


class ClassInfo:
    __is_initialized = False
    __data_types_map = dict()  # type: Dict[str, type]

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
                        ClassInfo.__add_type(element)

            qualified_name = module_name + "." + sub_module.name
            ClassInfo.__explore_package(qualified_name)

    @staticmethod
    def __add_type(type_):
        ClassInfo.__data_types_map[type_.__name__] = type_
        return None

    @staticmethod
    def get_type(name: str) -> type:
        if not ClassInfo.__is_initialized:
            ClassInfo.__initialize_typ_map()
        if name not in ClassInfo.__data_types_map:
            raise KeyError
        return ClassInfo.__data_types_map[name]
