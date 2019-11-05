from enum import Enum


class InstanceType(Enum):
    """Specifies instance type.
    Some API functions are restricted based on the instance type.
    """
    Empty = 0,
    """Empty"""

    PROD = 1,
    """This instance type is used for live production data
    and has the most restrictions. For example, it
    does not allow a database to be deleted (dropped)
    through the API call.
    """

    UAT = 2,
    """This instance type has some of the restrictions
    of the PROD instance type, including the restriction
    on deleting (dropping) the database through an API
    call.
    """

    DEV = 3,
    """Shared development instance type.."""

    USER = 4,
    """Personal instance type of a specific user."""

    TEST = 5
    """Instance type is used for testing."""
