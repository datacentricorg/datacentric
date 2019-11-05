from typing import Dict, List
import typing_inspect

from datacentric.platform.reflection import ClassMapSettings


class ClassInfo:
    """Contains reflection based helper static methods.
    """
    __is_initialized: bool = False
    __data_types_map: Dict[str, type] = dict()

    @staticmethod
    def get_type(name: str) -> type:
        """Returns data derived type given its name."""
        if not ClassInfo.__is_initialized:
            from datacentric.types.record import TypedRecord, TypedKey, Data
            children = ClassInfo.__get_runtime_imported_data(Data, [])
            for child in children:
                if child not in ClassInfo.__data_types_map:
                    ClassInfo.__data_types_map[child.__name__] = child
            ClassInfo.__is_initialized = True

        if name not in ClassInfo.__data_types_map:
            raise KeyError

        return ClassInfo.__data_types_map[name]

    @staticmethod
    def get_key_from_record(type_: type) -> type:
        """Extracts associated key from RootRecord and TypedRecord derived types."""
        if not typing_inspect.is_generic_type(type_):
            raise Exception(f'Cannot get associated key from not generic type {type_.__name__}')

        from datacentric.types.record import TypedKey, TypedRecord, RootRecord
        from typing import ForwardRef

        generic_base = typing_inspect.get_generic_bases(type_)[0]

        generic_origin = typing_inspect.get_origin(generic_base)
        if generic_origin is not RootRecord and generic_origin is not TypedRecord:
            raise Exception(f'Wrong generic origin: {generic_origin.__name__}. Expected TypeRecord || RootRecord')

        generic_arg = typing_inspect.get_args(generic_base)[0]  # Arg

        # Generic parameter is forward ref
        if type(generic_arg) is ForwardRef:
            return ClassInfo.get_type(generic_arg.__forward_arg__)
        # Generic parameter is type
        elif issubclass(generic_arg, TypedKey):
            return generic_arg
        else:
            raise Exception(f'Cannot deduce key from type {type_.__name__}')

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
        """Returns type of the class at the root of the inheritance chain, one
        before Data, TypedKey[TRecord], TypedRecord[TKey] or RootRecord[TKey].
        """
        from datacentric.types.record import TypedKey, TypedRecord, RootRecord, Data
        root_types = [TypedKey, TypedRecord, RootRecord, Data]

        if type_.mro()[0] in root_types:
            raise Exception(f'Cannot get root type from root type.')
        type_mro = type_.mro()
        for root_type in root_types:
            if root_type in type_mro:
                index = type_mro.index(root_type)
                return type_mro[index - 1]
        raise Exception(f'Type is not derived from Data.')

    @staticmethod
    def __get_runtime_imported_data(type_: type, children: List[type]) -> List[type]:
        """For the given type recursively adds its children."""
        current_children = type_.__subclasses__()
        for t in current_children:
            ClassInfo.__get_runtime_imported_data(t, children)
        children.extend(current_children)
        return children
