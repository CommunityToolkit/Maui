from __future__ import annotations

import unittest
from datetime import datetime, timezone

from ensure_ci_always_tests_latest_operating_systems import version_numbers as v


class AndroidLevelTests(unittest.TestCase):
    def test_parses_whole_and_minor_levels(self) -> None:
        for text, major, minor in [('37', 37, 0), ('37.0', 37, 0), ('36.1', 36, 1), ('100.0', 100, 0)]:
            with self.subTest(text):
                level = v.parse_android_level(text)
                self.assertEqual((level.major, level.minor, level.text), (major, minor, text))

    def test_rejects_malformed_levels(self) -> None:
        for text in ['36x', ' 37', '37.0.1', '', '37.', '.5', '37\n', '\u0663\u0667', None]:
            with self.subTest(repr(text)):
                with self.assertRaises(ValueError):
                    v.parse_android_level(text)  # type: ignore[arg-type]

    def test_orders_by_major_then_minor(self) -> None:
        parse = v.parse_android_level
        self.assertLess(parse('37.0'), parse('37.1'))
        self.assertLess(parse('37.1'), parse('38'))
        self.assertGreater(parse('100.0'), parse('37.2'))
        self.assertEqual(parse('37'), parse('37.0'))
        self.assertNotEqual(parse('37'), parse('37.1'))
        self.assertEqual(hash(parse('37')), hash(parse('37.0')))
        self.assertEqual(max([parse('36.1'), parse('37.2'), parse('37')]).text, '37.2')

    def test_keeps_the_text_the_file_used(self) -> None:
        self.assertEqual(v.parse_android_level('37').text, '37')


class DottedNumberTests(unittest.TestCase):
    def test_numeric_key_orders_numerically_not_alphabetically(self) -> None:
        self.assertLess(v.numeric_key('10.108.0'), v.numeric_key('10.108.1'))
        self.assertLess(v.numeric_key('10.108.1'), v.numeric_key('10.115.0'))
        self.assertLess(v.numeric_key('9.9'), v.numeric_key('10.0'))

    def test_numeric_key_rejects_anything_but_digits(self) -> None:
        for text in ['10.x', '10.108.0-rc.1', '', '1..2', ' 1.2']:
            with self.subTest(text):
                with self.assertRaises(ValueError):
                    v.numeric_key(text)

    def test_feature_band_rounds_the_patch_down_to_hundreds(self) -> None:
        self.assertEqual(v.feature_band('11.0.203'), '11.0.200')
        self.assertEqual(v.feature_band('10.0.404'), '10.0.400')
        self.assertEqual(v.feature_band('11.0.100'), '11.0.100')
        self.assertEqual(v.feature_band('10.0.99'), '10.0.0')

    def test_feature_band_rejects_a_short_version(self) -> None:
        with self.assertRaises(ValueError):
            v.feature_band('11.0')

    def test_channel_of(self) -> None:
        self.assertEqual(v.channel_of('11.0.x'), '11.0')
        self.assertEqual(v.channel_of('10.0.x'), '10.0')
        self.assertIsNone(v.channel_of('11.0.100'))
        self.assertIsNone(v.channel_of('11.0'))

    def test_workload_set_version(self) -> None:
        self.assertEqual(v.workload_set_version('11.100.1'), '11.0.100.1')
        self.assertEqual(v.workload_set_version('11.100.0'), '11.0.100')
        self.assertEqual(v.workload_set_version('10.108.3'), '10.0.108.3')
        self.assertEqual(v.workload_set_version('10.204.1'), '10.0.204.1')

    def test_workload_set_version_keeps_the_prerelease_label(self) -> None:
        self.assertEqual(v.workload_set_version('11.100.0-rc.3.26524.4'), '11.0.100-rc.3.26524.4')
        self.assertEqual(v.workload_set_version('8.201.1-preview'), '8.0.201.1-preview')

    def test_workload_set_version_rejects_a_workload_set_version(self) -> None:
        with self.assertRaises(ValueError):
            v.workload_set_version('11.0.100.1')

    def test_major_minor_and_major(self) -> None:
        self.assertEqual(v.major_minor('27'), '27.0')
        self.assertEqual(v.major_minor('26.4.1'), '26.4')
        self.assertEqual(v.major_minor('27.2'), '27.2')
        self.assertEqual(v.major('27.2'), 27)
        self.assertEqual(v.major('9'), 9)


class ParseUtcTests(unittest.TestCase):
    def test_seven_fraction_digits(self) -> None:
        moment = v.parse_utc('2026-11-10T18:30:44.7287108+00:00')
        self.assertEqual(moment, datetime(2026, 11, 10, 18, 30, 44, 728710, tzinfo=timezone.utc))

    def test_z_and_zero_offset_agree(self) -> None:
        self.assertEqual(v.parse_utc('2026-11-10T18:30:44Z'), v.parse_utc('2026-11-10T18:30:44+00:00'))

    def test_offsets_are_converted_to_utc(self) -> None:
        self.assertEqual(v.parse_utc('2026-11-10T10:30:44-08:00'), datetime(2026, 11, 10, 18, 30, 44, tzinfo=timezone.utc))
        self.assertEqual(v.parse_utc('2026-11-10T20:30:44+02:00'), datetime(2026, 11, 10, 18, 30, 44, tzinfo=timezone.utc))

    def test_result_is_aware_utc(self) -> None:
        self.assertEqual(v.parse_utc('2026-11-10T18:30:44.5Z').utcoffset().total_seconds(), 0)

    def test_fraction_with_one_digit(self) -> None:
        self.assertEqual(v.parse_utc('2026-11-10T18:30:44.5Z').microsecond, 500000)

    def test_rejects_garbage(self) -> None:
        for text in ['yesterday', '2026-11-10', '2026-11-10T18:30', '2026-11-10T18:30:44.12345678Z']:
            with self.subTest(text):
                with self.assertRaises(ValueError):
                    v.parse_utc(text)


if __name__ == '__main__':
    unittest.main()
