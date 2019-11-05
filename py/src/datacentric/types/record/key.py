from abc import ABC

from datacentric.types.record import Data


class Key(Data, ABC):
    def __init__(self):
        Data.__init__(self)

    def value(self) -> str:
        raise NotImplemented

    def get_key_token(self, obj: object, element_info) -> str:
        raise NotImplemented

    def assign_string(self, value: str) -> None:
        raise NotImplemented
