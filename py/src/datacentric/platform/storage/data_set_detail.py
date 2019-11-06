from bson import ObjectId

from datacentric.types.record import TypedKey, TypedRecord


class DataSetDetailKey(TypedKey['DataSetDetail']):
    """Key for DataSetDetail."""
    __slots__ = ['data_set_id']
    data_set_id: ObjectId

    def __init__(self):
        super().__init__()

        self.data_set_id = None
        """TemporalId of the referenced dataset."""


class DataSetDetail(TypedRecord[DataSetDetailKey]):
    """Provides the ability to change data associated with the dataset
    without changing the dataset record, which is immutable in a
    temporal data source.

    The reason dataset record is immutable is that any change to the
    the dataset record in a temporal data source results in creation
    of a record with new TemporalId, which is treated as a new dataset.

    The DataSetDetail record uses TemporalId of the referenced dataset
    as its primary key. It is located in the parent of the dataset
    record to which it applies, rather than inside that record, so it
    is not affected by its own settings.
    """
    __slots__ = ['data_set_id', 'read_only', 'cutoff_time', 'imports_cutoff_time']
    data_set_id: ObjectId
    read_only: bool
    cutoff_time: ObjectId
    imports_cutoff_time: ObjectId

    def __init__(self):
        super().__init__()
        self.data_set_id = None
        """TemporalId of the referenced dataset."""

        self.read_only = None
        """If specified, write operations to the referenced dataset
        will result in an error.
        """

        self.cutoff_time = None
        """Records with ObjectId that is greater than or equal to cutoff_time
        will be ignored by load methods and queries, and the latest available
        record where ObjectId is less than cutoff_time will be returned instead.
        
        cutoff_time applies to both the records stored in the dataset itself,
        and the reports loaded through the imports list.
        
        cutoff_time may be set in data source globally, or for a specific dataset
        in its details record. If cutoff_time is set for both, the earlier of the
        two values will be used.
        """

        self.imports_cutoff_time = None
        """Imported records (records loaded through the imports list)
        where ObjectId is greater than or equal to cutoff_time
        will be ignored by load methods and queries, and the latest
        available record where ObjectId is less than cutoff_time will
        be returned instead.
        
        This setting only affects records loaded through the imports
        list. It does not affect records stored in the dataset itself.
        
        If imports_cutoff_time is set for both data source and dataset,
        the earlier of the two values will be used.
        """