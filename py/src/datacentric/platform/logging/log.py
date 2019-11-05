from abc import ABC, abstractmethod
from typing import Optional

from datacentric.platform.logging.log_entry_type import LogEntryType


class Log(ABC):
    verbosity: LogEntryType
    """Log verbosity is the highest log entry type displayed.
    Verbosity can be modified at runtime to provide different levels of
    verbosity for different code segments.
    """

    def __init__(self):
        self.verbosity = LogEntryType.Empty

    @abstractmethod
    def append(self, entry_type: LogEntryType, entry_sub_type: Optional[str], message: str,
               *message_params: object) -> None:
        """Append new entry to the log if entry type is the same or lower than log verbosity.
        Entry subtype is an optional tag in dot delimited format (specify null if no subtype).
        """
        pass

    @abstractmethod
    def flush(self) -> None:
        """Flush log contents to permanent storage."""
        pass

    @abstractmethod
    def close(self) -> None:
        """Close log and release handle to permanent storage."""
        pass

    def exception(self, message: str, *message_params: object) -> Exception:
        """Record an error message and return exception with the same message.
        The caller is expected to raise the exception: raise Log.exception(message, messageParams)."""
        self.append(LogEntryType.Error, None, message, *message_params)
        e = Exception(message.format(message_params))
        return e

    def error(self, message: str, *message_params: object) -> None:
        """Record an error message and throw exception return by Log.exception(...)."""
        raise self.exception(message, *message_params)

    def warning(self, message: str, *message_params: object):
        """Record a warning."""
        self.append(LogEntryType.Warning, None, message, *message_params)

    def status(self, message: str, *message_params: object):
        """Record a status message."""
        self.append(LogEntryType.Status, None, message, *message_params)
