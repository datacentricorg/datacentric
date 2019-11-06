from abc import ABC, abstractmethod
from bson.objectid import ObjectId
from typing import List, Set, Dict, Iterable, Optional, TypeVar

from datacentric.platform.storage.data_set_flags import DataSetFlags
from datacentric.platform.storage.db_name import DbNameKey
from datacentric.types.record import RootRecord, TypedKey, Record
from datacentric.platform.storage import DataSet, DataSetKey

TRecord = TypeVar('TRecord', bound=Record)


class DataSourceKey(TypedKey['DataSource']):
    """Key class for DataSource.
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


class DataSource(RootRecord[DataSourceKey], ABC):
    """Data source is a logical concept similar to database
    that can be implemented for a document DB, relational DB,
    key-value store, or filesystem.

    Data source API provides the ability to:

    (a) store and query datasets;
    (b) store records in a specific dataset; and
    (c) query record across a group of datasets.

    This record is stored in root dataset.
    """

    __slots__ = ('data_source_name', 'db_name', 'non_temporal', 'readonly')

    _empty_id = ObjectId('000000000000000000000000')
    common_id: str = 'Common'

    data_source_name: str
    db_name: DbNameKey
    non_temporal: bool
    readonly: bool

    def __init__(self):
        super().__init__()

        self.data_source_name = None
        """Unique data source name."""

        self.db_name = None
        """Database name."""

        self.non_temporal = None
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

        self.readonly = None
        """Use this flag to mark data source as readonly."""

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
    def load_or_null(self, record_type: type, id_: ObjectId) -> Optional[TRecord]:
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
    def get_query(self, record_type: type, load_from: ObjectId):
        """Get query for the specified type.

        After applying query parameters, the lookup occurs first in
        descending order of dataset ObjectIds, and then in the descending
        order of record ObjectIds within the first dataset that
        has at least one record. Both dataset and record ObjectIds
        are ordered chronologically to one second resolution,
        and are unique within the database server or cluster.

        The root dataset has empty ObjectId value that is less
        than any other ObjectId value. Accordingly, the root
        dataset is the last one in the lookup order of datasets.
        """
        pass

    @abstractmethod
    def save_many(self, record_type: type, records: Iterable[TRecord], save_to: ObjectId) -> None:
        """Save multiple records to the specified dataset. After the method exits,
        for each record the property record.data_set will be set to the value of
        the save_to parameter.

        All save methods ignore the value of record.data_set before the
        save method is called. When dataset is not specified explicitly,
        the value of dataset from the context, not from the record, is used.
        The reason for this behavior is that the record may be stored from
        a different dataset than the one where it is used.

        This method guarantees that ObjectIds of the saved records will be in
        strictly increasing order.
        """
        pass

    @abstractmethod
    def delete(self, key: TypedKey[TRecord], delete_in: ObjectId) -> None:
        """Write a DeletedRecord in delete_in dataset for the specified key
        instead of actually deleting the record. This ensures that
        a record in another dataset does not become visible during
        lookup in a sequence of datasets.

        To avoid an additional roundtrip to the data store, the delete
        marker is written even when the record does not exist.
        """
        pass

    @abstractmethod
    def delete_db(self) -> None:
        """Permanently deletes (drops) the database with all records
        in it without the possibility to recover them later.

        This method should only be used to free storage. For
        all other purposes, methods that preserve history should
        be used.

        ATTENTION - THIS METHOD WILL DELETE ALL DATA WITHOUT
        THE POSSIBILITY OF RECOVERY. USE WITH CAUTION.
        """
        pass

    @abstractmethod
    def get_data_set_or_none(self, data_set_name: str, load_from: ObjectId) -> Optional[ObjectId]:
        """Get ObjectId of the dataset with the specified name.
        Returns null if not found.
        """
        pass

    @abstractmethod
    def save_data_set(self, data_set: DataSet, save_to: ObjectId) -> None:
        """Save new version of the dataset and update in-memory cache to the saved dataset."""
        pass

    # From extensions:
    def load(self, record_type: type, id_: ObjectId) -> TRecord:
        """Load record by its ObjectId.

        Error message if there is no record for the specified ObjectId,
        or if the record exists but is not derived from record_type.
        """
        raise NotImplemented

    def load_by_key(self, key_: TypedKey, load_from: ObjectId) -> TRecord:
        """Load record from context.data_source, overriding the dataset
        specified in the context with the value specified as the
        second parameter. The lookup occurs in the specified dataset
        and its imports, expanded to arbitrary depth with repetitions
        and cyclic references removed.

        IMPORTANT - this overload of the method loads from load_from
        dataset, not from context.data_set.

        Error message if the record is not found or is a DeletedRecord.
        """
        raise NotImplemented

    def save_one(self, record_type: type, record: TRecord, save_to: ObjectId):
        """Save one record to the specified dataset. After the method exits,
        record.data_set will be set to the value of the data_set parameter.

        All save methods ignore the value of record.data_set before the
        Save method is called. When dataset is not specified explicitly,
        the value of dataset from the context, not from the record, is used.
        The reason for this behavior is that the record may be stored from
        a different dataset than the one where it is used.

        This method guarantees that ObjectIds of the saved records will be in
        strictly increasing order.
        """
        self.save_many(record_type, [record], save_to)

    def get_common(self) -> ObjectId:
        """Return ObjectId of the latest Common dataset.
        Common dataset is always stored in root dataset.
        """
        return self.get_data_set(DataSource.common_id, DataSource._empty_id)

    def get_data_set(self, data_set_name: str, load_from: ObjectId) -> ObjectId:
        """Get ObjectId of the dataset with the specified name.
        Error message if not found.
        """
        result = self.get_data_set_or_none(data_set_name, load_from)
        if result is None:
            raise Exception(f'Dataset {data_set_name} is not found in data store {self.data_source_name}.')
        return result

    def create_common(self, flags: DataSetFlags = None) -> ObjectId:
        """Create Common dataset with the specified flags.

        The flags may be used, among other things, to specify
        that the created dataset will be NonTemporal even if the
        data source is itself temporal. This setting is typically
        used to prevent the accumulation of data where history is
        not needed.

        By convention, the Common dataset contains reference and
        configuration data and is included as import in all other
        datasets.

        The Common dataset is always stored in root dataset.

        This method updates in-memory dataset cache to include
        the created dataset.
        """
        if flags is None:
            flags = DataSetFlags.Default
        return self.create_data_set(DataSource.common_id, DataSource._empty_id, flags=flags)

    def create_data_set(self, data_set_name: str, parent_data_set: ObjectId, imports: List[ObjectId] = None,
                        flags: DataSetFlags = None) -> ObjectId:
        """Create dataset with the specified data_set_name, parent_data_set, imports,
        and flags.

        This method updates in-memory dataset cache to include
        the created dataset.
        """
        if flags is None:
            flags = DataSetFlags.Default
        if imports is None:
            imports = [parent_data_set]

        result = DataSet()
        result.data_set_name = data_set_name
        if imports is not None:
            result.imports = [x for x in imports]

        if (self.non_temporal is not None and self.non_temporal) or \
                flags & DataSetFlags.NonTemporal == DataSetFlags.NonTemporal:
            result.non_temporal = True

        self.save_data_set(result, parent_data_set)

        return result.id_
