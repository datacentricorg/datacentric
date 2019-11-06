from bson import ObjectId
from typing import List

from datacentric.platform.context import Context
from datacentric.types.record import TypedRecord, TypedKey


class DataSetKey(TypedKey['DataSetData']):
    __slots__ = ['data_set_name']
    data_set_name: str

    def __init__(self, id_: str = None):
        super().__init__()
        self.data_set_name = None
        if id_ is not None:
            self.data_set_name = id_


class DataSet(TypedRecord[DataSetKey]):
    """Dataset is a concept similar to a folder, applied to data in any
    data source including relational or document databases, OData
    endpoints, etc.
    Datasets can be stored in other datasets. The dataset where dataset
    record is stored is called parent dataset.
    Dataset has an Imports array which provides the list of ObjectIds of
    datasets where records are looked up if they are not found in the
    current dataset. The specific lookup rules are specific to the data
    source type and described in detail in the data source documentation.
    Some data source types do not support Imports. If such data
    source is used with a dataset where Imports array is not empty,
    an error will be raised.
    The root dataset uses ObjectId('000000000000000000000000')
    and does not have versions or its own DataSetData record. It is always last in the dataset
    lookup sequence. The root dataset cannot have Imports.
    """
    __slots__ = ['data_set_name', 'non_temporal', 'imports']

    data_set_name: str
    """Unique dataset name."""

    non_temporal: bool
    """Flag indicating that the dataset is non-temporal even if the
    data source supports temporal data.
    For the data stored in datasets where non_temporal == False, a
    temporal data source keeps permanent history of changes to each
    record within the dataset, and provides the ability to access
    the record as of the specified ObjectId, where ObjectId serves
    as a timeline (records created later have greater ObjectId than
    records created earlier).
    For the data stored in datasets where non_temporal == True, the
    data source keeps only the latest version of the record. All
    child datasets of a non-temporal dataset must also be non-temporal.
    In a non-temporal data source, this flag is ignored as all
    datasets in such data source are non-temporal."""

    imports: List[ObjectId]
    """List of datasets where records are looked up if they are
    not found in the current dataset.
    The specific lookup rules are specific to the data source
    type and described in detail in the data source documentation.
    
    The parent dataset is not included in the list of imports by
    default and must be included in the list of imports explicitly.
    """

    def __init__(self):
        super().__init__()
        self.data_set_name = None
        self.non_temporal = None
        self.imports = None

    def init(self, context: Context):
        super().init(context)
