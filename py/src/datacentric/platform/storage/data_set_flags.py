from enum import IntFlag


class DataSetFlags(IntFlag):
    """Binary flags for the dataset create options."""

    Default = 0
    """Specifies that no flags are defined."""

    NonTemporal = 1
    """By default, a dataset will hold temporal data if the data source
    has temporal capability. Specify this flag to create a dataset that
    holds non-temporal data in a temporal data source.
    
    The reason to specify this flag is to avoid accumulation of historical
    data for records that are frequently changed and do not require an
    audit log, such as user interface preferences, customizations, etc.
    
    Note that the non-temporal flag applies to the data held in the dataset,
    but not to the dataset record itself. In a temporal data source, all
    dataset records are themselves temporal, even those dataset that hold
    non-temporal data.
    
    In a non-temporal data source, this flag is ignored as all
    datasets in such data source are non-temporal.
    """
