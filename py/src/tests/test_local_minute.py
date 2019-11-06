import unittest
import datetime as dt
from datacentric.types.time import LocalMinute


class TestLocalMinute(unittest.TestCase):
    def test_properties(self):
        t = LocalMinute(12, 10)
        self.assertEqual(t.hour, 12)
        self.assertEqual(t.minute, 10)
        self.assertEqual(t.minute_of_day, 730)

    def test_methods(self):
        t1 = LocalMinute(12, 10)
        t2 = LocalMinute(1, 15)
        self.assertEqual(str(t1), '12:10')
        self.assertEqual(str(t2), '01:15')
        self.assertEqual(t1.to_time(), dt.time(12, 10))
        self.assertEqual(t2.to_time(), dt.time(1, 15))

    def test_operators(self):
        t = LocalMinute(12, 0)
        t1 = LocalMinute(12, 0)
        t2 = LocalMinute(13, 1)
        t3 = LocalMinute(14, 2)

        self.assertTrue(t1 == t)
        self.assertTrue(t1 != t2)
        self.assertTrue(t2 != t3)

        self.assertTrue(t1 <= t1)
        self.assertTrue(t1 <= t2)
        self.assertTrue(t2 <= t3)

        self.assertTrue(t1 < t2)
        self.assertTrue(t1 < t3)
        self.assertTrue(t2 < t3)

        self.assertTrue(t1 >= t1)
        self.assertTrue(t2 >= t1)
        self.assertTrue(t3 >= t2)

        self.assertTrue(t2 > t1)
        self.assertTrue(t3 > t1)
        self.assertTrue(t3 > t2)


if __name__ == "__main__":
    unittest.main()
    c = dt.date.toordinal()
