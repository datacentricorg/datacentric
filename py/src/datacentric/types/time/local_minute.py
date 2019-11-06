from __future__ import annotations
import datetime as dt


class LocalMinute:
    def __init__(self, hour: int, minute: int):
        hour: int
        minute: int

        if hour < 0 or hour > 23:
            raise AttributeError(f'Hour {hour} is not between 0 and 23.')
        if minute < 0 or minute > 59:
            raise AttributeError(f'Minute {minute} is not between 0 and 59.')
        self.hour = hour
        self.minute = minute

    def to_time(self) -> dt.time:
        return dt.time(self.hour, self.minute)

    @property
    def minute_of_day(self) -> int:
        return self.hour * 60 + self.minute

    def __eq__(self, other: LocalMinute):
        return self.minute_of_day == other.minute_of_day

    def __ne__(self, other: LocalMinute):
        return self.minute_of_day != other.minute_of_day

    def __lt__(self, other: LocalMinute):
        return self.minute_of_day < other.minute_of_day

    def __le__(self, other: LocalMinute):
        return self.minute_of_day <= other.minute_of_day

    def __gt__(self, other: LocalMinute):
        return self.minute_of_day > other.minute_of_day

    def __ge__(self, other: LocalMinute):
        return self.minute_of_day >= other.minute_of_day

    def __str__(self):
        return f'{self.hour:02}:{self.minute:02}'
