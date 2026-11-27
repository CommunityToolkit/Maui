from __future__ import annotations

import unittest

from ensure_ci_always_tests_latest_operating_systems.markdown_text import markdown_code


class MarkdownCodeTests(unittest.TestCase):
    def test_wraps_plain_text_in_a_code_span(self) -> None:
        self.assertEqual(markdown_code('27.2'), '`27.2`')

    def test_replaces_backticks_so_the_span_cannot_end_early(self) -> None:
        self.assertEqual(markdown_code('a`b``c'), "`a'b''c`")

    def test_mentions_and_html_stay_inside_the_span(self) -> None:
        self.assertEqual(markdown_code('@octocat'), '`@octocat`')
        self.assertEqual(markdown_code('<script>alert(1)</script>'), '`<script>alert(1)</script>`')

    def test_removes_control_characters_and_turns_line_breaks_into_spaces(self) -> None:
        self.assertEqual(markdown_code('a\x00b\x1b[31mc\nd\r\ne\tf'), '`ab[31mc d  e f`')

    def test_removes_invisible_bidi_overrides(self) -> None:
        self.assertEqual(markdown_code('ab\u202ecd\u200bef'), '`abcdef`')

    def test_cuts_long_text_to_200_characters(self) -> None:
        result = markdown_code('x' * 500)
        self.assertEqual(len(result), 202)
        self.assertTrue(result.endswith('\u2026`'))
        self.assertEqual(markdown_code('x' * 200), '`' + 'x' * 200 + '`')

    def test_escapes_pipes_only_inside_tables(self) -> None:
        self.assertEqual(markdown_code('a|b'), '`a|b`')
        self.assertEqual(markdown_code('a|b', in_table=True), '`a\\|b`')

    def test_none_and_empty_still_give_a_valid_span(self) -> None:
        self.assertEqual(markdown_code(None), '` `')
        self.assertEqual(markdown_code('   '), '` `')

    def test_non_text_values_are_converted(self) -> None:
        self.assertEqual(markdown_code(37), '`37`')

    def test_turkish_letters_are_left_alone(self) -> None:
        self.assertEqual(markdown_code('T\u0130TLE i\u0131 I'), '`T\u0130TLE i\u0131 I`')


if __name__ == '__main__':
    unittest.main()
