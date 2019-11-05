from datacentric.types.record import Record


class DeletedRecord(Record):
    def __init__(self):
        self.key_ = None
        Record.__init__(self)

    @property
    def key(self) -> str:
        return self.key_

    @key.setter
    def key(self, value) -> None:
        self.key_ = value
