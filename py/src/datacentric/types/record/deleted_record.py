from datacentric.types.record import Record


class DeletedRecord(Record):
    """When returned by the data source, this record has the same
    effect as if no record was found. It is used to indicate
    a deleted record when audit log must be preserved."""
    __slots__ = []

    def __init__(self):
        super().__init__()
        self._key = None

    @property
    def key(self) -> str:
        """String key consists of semicolon delimited primary key elements
        """
        return self._key

    @key.setter
    def key(self, value) -> None:
        self._key = value
