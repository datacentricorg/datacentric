from enum import Enum


class IsoDayOfWeek(Enum):
    """Equates the days of the week with their numerical value according to
    ISO-8601.
    """
    None_ = 0,
    """Value indicating no day of the week; this will never be returned
    by any IsoDayOfWeek property, and is not valid as an argument to
    any method.
    """

    Monday = 1,
    """Value representing Monday (1)."""

    Tuesday = 2,
    """Value representing Tuesday (2)."""

    Wednesday = 3,
    """Value representing Wednesday (3)."""

    Thursday = 4,
    """Value representing Thursday (4)."""

    Friday = 5,
    """Value representing Friday (5)."""

    Saturday = 6,
    """Value representing Saturday (6)."""

    Sunday = 7,
    """Value representing Sunday (7)."""
