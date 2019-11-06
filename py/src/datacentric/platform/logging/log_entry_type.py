from enum import Enum


class LogEntryType(Enum):
    Empty = 0,
    """Empty"""

    Error = 1,
    """Error message (recorded when exception is thrown)"""

    Warning = 2,
    """Warning message."""

    Status = 3,
    """Status message."""

    Progress = 4,
    """Progress ratio or message."""

    Verify = 5
    """Approval test verification record."""
