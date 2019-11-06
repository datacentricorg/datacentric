from io import StringIO
from os import linesep

from datacentric.platform.logging.log import Log
from datacentric.platform.logging.log_entry import LogEntry
from datacentric.platform.logging.log_entry_type import LogEntryType


class InMemoryLog(Log):
    def __init__(self):
        super().__init__()
        self.__str_io: StringIO = StringIO()

    def __str__(self):
        """Return multi-line log text as string."""
        return self.__str_io.getvalue()

    def close(self) -> None:
        pass

    def flush(self) -> None:
        """Flush log contents to permanent storage."""
        self.__str_io.flush()

    def append(self, entry_type: LogEntryType, entry_sub_type: str, message: str, *message_params: object) -> None:
        if self.verbosity == LogEntryType.Empty or entry_type <= self.verbosity:
            log_entry = LogEntry(entry_type, entry_sub_type, message, *message_params)
            self.__str_io.write(str(log_entry))
            self.__str_io.write(linesep)
