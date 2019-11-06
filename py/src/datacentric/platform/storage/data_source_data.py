from abc import ABC, abstractmethod
from bson.objectid import ObjectId
from typing import List, Set, Dict, Iterable, Optional, TypeVar

from datacentric.platform.storage.db_name import DbNameKey
from datacentric.types.record import RootRecord, TypedKey, Record
from datacentric.platform.data_set import DataSetData, DataSetKey

TRecord = TypeVar('TRecord', bound=Record)


class DataSourceKey(TypedKey['DataSourceData']):
    """Key class for DataSourceData.
    Record associated with this key is stored in root dataset.
    """
    __slots__ = ['data_source_name']

    data_source_name: str
    cache: str = 'Cache'
    master: str = 'Master'

    def __init__(self, value: str = None):
        super().__init__()
        self.data_source_name = None
        if value is not None:
            self.data_source_name = value


class DataSourceData(RootRecord[DataSourceKey], ABC):
    """Data source is a logical concept similar to database
    that can be implemented for a document DB, relational DB,
    key-value store, or filesystem.

    Data source API provides the ability to:

    (a) store and query datasets;
    (b) store records in a specific dataset; and
    (c) query record across a group of datasets.

    This record is stored in root dataset.
    """

    __slots__ = ('data_source_name', 'db_name', 'non_temporal', 'readonly', '_data_set_dict', '_import_dict')

    _empty_id = ObjectId('000000000000000000000000')
    common_id: str = 'Common'

    _data_set_dict: Dict[str, ObjectId]
    _import_dict: Dict[ObjectId, Set[ObjectId]]

    data_source_name: str
    """Unique data source name."""

    db_name: DbNameKey
    """Database name."""

    non_temporal: bool
    """For the data stored in data sources where non_temporal == false,
    the data source keeps permanent history of changes to each
    record (except where dataset or record are marked as NonTemporal),
    and provides the ability to access the record as of the specified
    TemporalId, where TemporalId serves as a timeline (records created
    later have greater TemporalId than records created earlier).
    
    For the data stored in data source where NonTemporal == true,
    the data source keeps only the latest version of the record. All
    datasets created by a NonTemporal data source must also be non-temporal.
    
    In a non-temporal data source, this flag is ignored as all
    datasets in such data source are non-temporal.
    """

    readonly: bool
    """Use this flag to mark data source as readonly."""

    def __init__(self):
        super().__init__()
        self._data_set_dict = dict()
        self._import_dict = dict()

        self.data_source_name = None
        self.db_name = None
        self.non_temporal = None
        self.readonly = None

    @abstractmethod
    def create_ordered_object_id(self) -> ObjectId:
        """The returned ObjectIds have the following order guarantees:

        * For this data source instance, to arbitrary resolution; and
        * Across all processes and machines, to one second resolution

        One second resolution means that two ObjectIds created within
        the same second by different instances of the data source
        class may not be ordered chronologically unless they are at
        least one second apart."""
        pass

    @abstractmethod
    def is_readonly(self) -> bool:
        pass

    def check_not_readonly(self):
        if self.is_readonly():
            raise Exception(f'Attempting write operation for readonly data source {self.data_source_name}. '
                            f'A data source is readonly if either (a) its ReadOnly flag is set, or (b) '
                            f'one of SavedByTime or SavedById is set.')

    @abstractmethod
    def load_or_null(self, id_: ObjectId, type_: type) -> Optional[TRecord]:
        """Load record by its ObjectId.

        Return None if there is no record for the specified ObjectId;
        however an exception will be thrown if the record exists but
        is not derived from type_.
        """
        pass

    @abstractmethod
    def load_or_null_by_key(self, key_: TypedKey, load_from: ObjectId) -> Optional[TRecord]:
        """Load record by string key from the specified dataset or
        its list of imports. The lookup occurs first in descending
        order of dataset ObjectIds, and then in the descending
        order of record ObjectIds within the first dataset that
        has at least one record. Both dataset and record ObjectIds
        are ordered chronologically to one second resolution,
        and are unique within the database server or cluster.

        The root dataset has empty ObjectId value that is less
        than any other ObjectId value. Accordingly, the root
        dataset is the last one in the lookup order of datasets.

        The first record in this lookup order is returned, or null
        if no records are found or if DeletedRecord is the first
        record.

        Return None if there is no record for the specified ObjectId;
        however an exception will be thrown if the record exists but
        is not derived from TRecord.
        """
        pass

    @abstractmethod
    def get_query(self, load_from: ObjectId, type_: type):
        pass

    @abstractmethod
    def save(self, record: TRecord, save_to: ObjectId) -> None:
        pass

    @abstractmethod
    def delete(self, key: TypedKey[TRecord], delete_in: ObjectId) -> None:
        pass

    @abstractmethod
    def delete_db(self) -> None:
        pass

    def get_data_set_or_none(self, data_set_id: str, load_from: ObjectId) -> Optional[ObjectId]:
        """Get ObjectId of the dataset with the specified name.

        All of the previously requested data_set_ids are cached by
        the data source.

        Returns null if not found.
        """
        if data_set_id in self._data_set_dict:
            return self._data_set_dict[data_set_id]
        else:
            data_set_key = DataSetKey(data_set_id)

            data_set_data: DataSetData = self.load_or_null_by_key(data_set_key, load_from)

            if data_set_data is None:
                return None

            # If found cache in dictionary
            self._data_set_dict[data_set_id] = data_set_data.id_
            if data_set_data.id_ not in self._import_dict:
                import_set = self.__build_data_set_lookup_list(data_set_data)
                self._import_dict[data_set_data.id_] = import_set
            return data_set_data.id_

    def save_data_set(self, data_set_data: DataSetData, save_to: ObjectId) -> None:
        """Save new version of the dataset and update in-memory cache to the saved dataset."""
        self.save(data_set_data, save_to)
        self._data_set_dict[data_set_data.key] = data_set_data.id_
        lookup_list = self.__build_data_set_lookup_list(data_set_data)
        self._import_dict[data_set_data.id_] = lookup_list

    def get_data_set_lookup_list(self, load_from: ObjectId) -> Iterable[ObjectId]:
        """Returns enumeration of import datasets for specified dataset data,
        including imports of imports to unlimited depth with cyclic
        references and duplicates removed.
        """
        if load_from == DataSourceData._empty_id:
            return [DataSourceData._empty_id]
        if load_from in self._import_dict:
            return list(self._import_dict[load_from])
        else:
            # noinspection PyTypeChecker
            data_set_data: DataSetData = self.load_or_null(load_from, DataSetData)
            if data_set_data is None:
                raise Exception(f'Dataset with ObjectId={load_from} is not found.')
            if data_set_data.data_set != DataSourceData._empty_id:
                raise Exception(f'Dataset with ObjectId={load_from} is not stored in root dataset.')
            result = self.__build_data_set_lookup_list(data_set_data)
            self._import_dict[load_from] = result
            return list(result)

    @abstractmethod
    def _get_saved_by(self) -> Optional[ObjectId]:
        """Records where id_ is greater than the returned value will be
        ignored by the data source.
        """
        pass

    def __build_data_set_lookup_list(self, data_set_data: DataSetData) -> Optional[Set[ObjectId]]:
        if data_set_data is None:
            return

        if not ObjectId.is_valid(data_set_data.id_):
            raise Exception('Required ObjectId value is not set.')
        if data_set_data.key == '':
            raise Exception('Required string value is not set.')

        saved_by = self._get_saved_by()
        if saved_by is not None and data_set_data.id_ > saved_by:
            return

        result = set()
        result.add(data_set_data.id_)
        if data_set_data.imports is not None:
            for data_set_id in data_set_data.imports:
                if data_set_data.id_ == data_set_id:
                    raise Exception(f'Dataset {data_set_data.key} with ObjectId={data_set_data.id_} '
                                    f'includes itself in the list of its imports.')
                if data_set_id not in result:
                    result.add(data_set_id)
                    cached_import_list = self.get_data_set_lookup_list(data_set_id)
                    for import_id in cached_import_list:
                        result.add(import_id)
                else:
                    result.add(data_set_id)
        return result

    def get_data_set(self, data_set_id: str, load_from: ObjectId) -> ObjectId:
        result = self.get_data_set_or_none(data_set_id, load_from)
        if result is None:
            raise Exception(f'Dataset {data_set_id} is not found in data store {self.data_source_name}.')
        return result

    def get_common(self) -> ObjectId:
        return self.get_data_set(self.common_id, DataSourceData._empty_id)

    def create_common(self) -> ObjectId:
        result = DataSetData()
        result.data_set_name = self.common_id
        self.save_data_set(result, DataSourceData._empty_id)
        return result.id_

    def create_data_set(self, data_set_id: str, save_to: ObjectId, import_data_sets: List[ObjectId] = None) -> ObjectId:
        result = DataSetData()
        result.data_set_name = data_set_id

        if import_data_sets is not None:
            result.imports = [x for x in import_data_sets]

        self.save_data_set(result, save_to)
        return result.id_
