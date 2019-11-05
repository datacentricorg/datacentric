from abc import ABC, abstractmethod

from bson.objectid import ObjectId
from typing import List, Set, Dict, Iterable, Union
from datacentric.platform.data_set import DataSetData
from datacentric.platform.data_set.data_set import DataSetKey
from datacentric.types.record import RootRecord, Record, Key


class DataSourceData(RootRecord, ABC):
    empty_id = ObjectId('000000000000000000000000')
    common_id = 'Common'

    def __init__(self):
        RootRecord.__init__(self)
        self._data_set_dict = dict()  # type: Dict[str, ObjectId]
        self._import_dict = dict()  # type: Dict[ObjectId, Set[ObjectId]]

        self.data_source_id = None  # type: str

        self.db_name = None  # type: str
        self.data_store = None  # type: str
        self.readonly = None  # type: bool

    @abstractmethod
    def create_ordered_object_id(self) -> ObjectId:
        pass

    @abstractmethod
    def is_readonly(self) -> bool:
        pass

    def check_not_readonly(self):
        if self.is_readonly():
            raise Exception(f'Attempting write operation for readonly data source {self.data_source_id}. '
                            f'A data source is readonly if either (a) its ReadOnly flag is set, or (b) '
                            f'one of SavedByTime or SavedById is set.')

    @abstractmethod
    def load_or_null(self, id_: ObjectId) -> Record:
        pass

    @abstractmethod
    def reload_or_null(self, key: Key, load_from: ObjectId) -> Record:
        pass

    @abstractmethod
    def get_query(self, load_from: ObjectId):
        pass

    @abstractmethod
    def save(self, record: Record, save_to: ObjectId) -> None:
        pass

    @abstractmethod
    def delete(self, key: Key, delete_in: ObjectId) -> None:
        pass

    @abstractmethod
    def delete_db(self) -> None:
        pass

    def get_data_set_or_empty(self, data_set_id: str, load_from: ObjectId) -> ObjectId:
        if data_set_id in self._data_set_dict:
            return self._data_set_dict[data_set_id]
        else:
            return self.__load_data_set_or_empty(data_set_id, load_from)

    def save_data_set(self, data_set_data: DataSetData, save_to: ObjectId) -> None:
        self.save(data_set_data, save_to)
        self._data_set_dict[data_set_data.key_] = data_set_data.id_
        lookup_list = self.__build_data_set_lookup_list(data_set_data)
        self._import_dict[data_set_data.id_] = lookup_list

    def get_data_set_lookup_list(self, load_from: ObjectId) -> Iterable[ObjectId]:
        if load_from == self.empty_id:
            return [self.empty_id]
        if load_from in self._import_dict:
            return self._import_dict[load_from]
        else:
            data_set_data = self.load_or_null(load_from)  # type: DataSetData
            if data_set_data is None:
                raise Exception(f'Dataset with ObjectId={load_from} is not found.')
            if data_set_data.data_set != self.empty_id:
                raise Exception(f'Dataset with ObjectId={load_from} is not stored in root dataset.')
            result = self.__build_data_set_lookup_list(data_set_data)
            self._import_dict[load_from] = result
            return result

    @abstractmethod
    def _get_saved_by(self) -> ObjectId:
        pass

    def __load_data_set_or_empty(self, data_set_id: str, load_from: ObjectId) -> ObjectId:
        data_set_key = DataSetKey(data_set_id)
        data_set_data = self.load_or_null_by_key(data_set_key, load_from)  # type: DataSetData

        if data_set_data is None:
            return self.empty_id
        self._data_set_dict[data_set_id] = data_set_data.id_
        if data_set_data.id_ not in self._import_dict:
            import_set = self.__build_data_set_lookup_list(data_set_data)
            self._import_dict[data_set_data.id_] = import_set
        return data_set_data.id_

    def __build_data_set_lookup_list(self, data_set_data: DataSetData) -> Union[Set[ObjectId], None]:
        if data_set_data is None:
            return

        if not ObjectId.is_valid(data_set_data.id_):
            raise Exception('Required ObjectId value is not set.')
        if data_set_data.key_ is None or data_set_data.key_ == '':
            raise Exception('Required string value is not set.')

        saved_by = self._get_saved_by()
        if saved_by is not None and data_set_data.id_ > saved_by:
            return

        result = set()
        result.add(data_set_data.id_)
        if data_set_data.imports is not None:
            for data_set_id in data_set_data.imports:
                if data_set_data.id_ == data_set_id:
                    raise Exception(f'Dataset {data_set_data.key_} with ObjectId={data_set_data.id_} '
                                    f'includes itself in the list of its imports.')
                if data_set_id not in result:
                    result.add(data_set_id)
                    cached_import_list = self.get_data_set_lookup_list(data_set_id)
                    for import_id in cached_import_list:
                        result.add(import_id)
                else:
                    result.add(data_set_id)

    # From extensions
    def load(self, id_: ObjectId) -> Record:
        result = self.load_or_null(id_)
        if result is None:
            raise Exception(f'Record with ObjectId={id_} is not found in data store {self.data_source_id}.')
        return result

    # renamed
    def load_by_key(self, key_: Key, load_from: ObjectId):
        return key_.load(self.context, load_from)

    # renamed
    def load_or_null_by_key(self, key_: Key, load_from: ObjectId):
        return key_.load_or_null(self.context, load_from)

    def get_data_set(self, data_set_id: str, load_from: ObjectId):
        result = self.get_data_set_or_empty(data_set_id, load_from)
        if result is None:
            raise Exception(f'Dataset {data_set_id} is not found in data store {self.data_source_id}.')

    def get_common(self):
        return self.get_data_set(self.common_id, self.empty_id)

    def create_common(self) -> ObjectId:
        result = DataSetData()
        result.data_set_id = self.common_id
        self.save_data_set(result, self.empty_id)
        return result.id_

    def create_data_set(self, data_set_id: str, save_to: ObjectId, import_data_sets: List[ObjectId]) -> ObjectId:
        result = DataSetData()
        result.data_set_id = data_set_id

        if import_data_sets is not None:
            result.imports = [x for x in import_data_sets]

        self.save_data_set(result, save_to)
        return result.id_
