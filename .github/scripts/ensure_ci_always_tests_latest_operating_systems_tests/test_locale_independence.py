from __future__ import annotations

import ast
import json
import locale
import os
import re
import unittest
from typing import Any, Dict, List

from ensure_ci_always_tests_latest_operating_systems import (
    android_levels, pull_request_text, required_xcode, runner_labels, token_owner)
from ensure_ci_always_tests_latest_operating_systems.markdown_text import markdown_code
from ensure_ci_always_tests_latest_operating_systems.pinned_versions import read_pinned_versions_from_texts
from ensure_ci_always_tests_latest_operating_systems.version_numbers import feature_band, parse_android_level, parse_utc

from .helpers import FakeDownloader, fixture
from .test_change_plan import Night, android_with_sixteen_kb_37_3
from .test_required_xcode import downloads_for_band_11, pinned_with
from .test_runner_labels import pinned_versions as pinned_versions_with_labels
from .test_xcode_changes import repo_text

PACKAGE_FOLDER = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))),
                              'ensure_ci_always_tests_latest_operating_systems')
TURKISH_LOCALE = 'tr_TR.UTF-8'
RUNNING_IN_CI = os.environ.get('CI') == 'true'
STRFTIME_DIRECTIVES_THAT_NAME_THINGS = re.compile(r'%[aAbBcpxX]')
LOCALE_DEPENDENT_NUMBER_FORMAT = re.compile(r':n\}')
SUBPROCESS_FUNCTIONS = {'run', 'Popen', 'check_output', 'check_call', 'call'}
TEXT_MODE_PARAMETERS = {'text', 'universal_newlines'}


def keyword_names(call: ast.Call) -> List[str]:
    return [keyword.arg for keyword in call.keywords if keyword.arg is not None]


def string_constant(node: ast.AST) -> str:
    return node.value if isinstance(node, ast.Constant) and isinstance(node.value, str) else ''


def open_is_binary(call: ast.Call) -> bool:
    mode_arguments = call.args[1:2] + [keyword.value for keyword in call.keywords if keyword.arg == 'mode']
    return any('b' in string_constant(mode) for mode in mode_arguments)


def find_locale_dependent_code(source: str, name: str) -> List[str]:
    """Each problem names the file and line; an empty list means the source reads no locale."""
    problems: List[str] = []
    tree = ast.parse(source, filename=name)
    for node in ast.walk(tree):
        if isinstance(node, ast.Import) and any(alias.name.split('.')[0] == 'locale' for alias in node.names):
            problems.append('%s:%d imports locale' % (name, node.lineno))
        if isinstance(node, ast.ImportFrom) and (node.module or '').split('.')[0] == 'locale':
            problems.append('%s:%d imports from locale' % (name, node.lineno))
        if isinstance(node, ast.Constant) and isinstance(node.value, str):
            if LOCALE_DEPENDENT_NUMBER_FORMAT.search(node.value):
                problems.append('%s:%d formats a number with :n' % (name, node.lineno))
        if isinstance(node, ast.FormattedValue) and isinstance(node.format_spec, ast.JoinedStr):
            spec = ''.join(string_constant(part) for part in node.format_spec.values)
            if spec.endswith('n'):
                problems.append('%s:%d formats a number with :n' % (name, node.lineno))
        if not isinstance(node, ast.Call):
            continue
        function = node.func
        function_name = function.id if isinstance(function, ast.Name) else getattr(function, 'attr', '')
        if function_name == 'strftime':
            for argument in node.args:
                directives = STRFTIME_DIRECTIVES_THAT_NAME_THINGS.findall(string_constant(argument).replace('%%', ''))
                if directives:
                    problems.append('%s:%d strftime uses %s' % (name, node.lineno, ', '.join(directives)))
        is_plain_open = isinstance(function, ast.Name) and function.id == 'open'
        is_io_open = isinstance(function, ast.Attribute) and function.attr == 'open' \
            and isinstance(function.value, ast.Name) and function.value.id == 'io'
        if (is_plain_open or is_io_open) and 'encoding' not in keyword_names(node) and not open_is_binary(node):
            problems.append('%s:%d opens a file in text mode without encoding=' % (name, node.lineno))
        if function_name in ('read_text', 'write_text') and 'encoding' not in keyword_names(node):
            problems.append('%s:%d %s without encoding=' % (name, node.lineno, function_name))
        is_subprocess = isinstance(function, ast.Attribute) and function.attr in SUBPROCESS_FUNCTIONS \
            and isinstance(function.value, ast.Name) and function.value.id == 'subprocess'
        if is_subprocess:
            names = keyword_names(node)
            if 'encoding' not in names and TEXT_MODE_PARAMETERS.intersection(names):
                problems.append('%s:%d runs a text-mode subprocess without encoding=' % (name, node.lineno))
    return problems


def package_sources() -> Dict[str, str]:
    sources: Dict[str, str] = {}
    for file_name in sorted(os.listdir(PACKAGE_FOLDER)):
        if file_name.endswith('.py'):
            with open(os.path.join(PACKAGE_FOLDER, file_name), 'r', encoding='utf-8') as file:
                sources[file_name] = file.read()
    return sources


class StaticChecksOfThePackageTests(unittest.TestCase):
    def test_the_package_has_the_modules_to_check(self) -> None:
        self.assertGreater(len(package_sources()), 20)
        self.assertIn('commands.py', package_sources())

    def test_no_module_of_the_package_depends_on_the_locale_or_the_default_encoding(self) -> None:
        problems: List[str] = []
        for name, source in package_sources().items():
            problems.extend(find_locale_dependent_code(source, name))
        self.assertEqual(problems, [])


class ForbiddenWordTests(unittest.TestCase):
    # The maintainers' plain-words rule: a prefix match, so every inflection of the word is caught.
    FORBIDDEN_WORD = re.compile(r'\bfl' + r'ag', re.IGNORECASE)

    def test_the_bot_text_never_uses_the_word_the_maintainers_replaced_with_plain_words(self) -> None:
        scripts_folder = os.path.dirname(PACKAGE_FOLDER)
        files = [os.path.join(scripts_folder, 'ensure-ci-always-tests-latest-operating-systems.py'),
                 os.path.join(scripts_folder, '..', 'workflows', 'ensure-ci-always-tests-latest-operating-systems.yml')]
        for folder in (PACKAGE_FOLDER, os.path.dirname(os.path.abspath(__file__))):
            files.extend(os.path.join(folder, name) for name in sorted(os.listdir(folder)) if name.endswith('.py'))
        offending = []
        for path in files:
            with open(path, encoding='utf-8') as source:
                for number, line in enumerate(source, start=1):
                    if self.FORBIDDEN_WORD.search(line):
                        offending.append('%s:%d' % (os.path.basename(path), number))
        self.assertEqual([], offending)

    def test_the_pattern_catches_every_inflection(self) -> None:
        for word in ('fl' + 'ag', 'fl' + 'ags', 'fl' + 'agged', 'fl' + 'agging', 'Fl' + 'AG'):
            with self.subTest(word=word):
                self.assertIsNotNone(self.FORBIDDEN_WORD.search('a %s here' % word))


class StaticCheckDetectsEachProblemTests(unittest.TestCase):
    def problems_of(self, code: str) -> List[str]:
        return find_locale_dependent_code(code, 'example.py')

    def test_import_locale(self) -> None:
        self.assertEqual(len(self.problems_of('import locale\n')), 1)
        self.assertEqual(len(self.problems_of('from locale import getlocale\n')), 1)

    def test_strftime_with_a_name_of_a_day_or_month(self) -> None:
        for directive in ('%a', '%A', '%b', '%B', '%c', '%p', '%x', '%X'):
            self.assertEqual(len(self.problems_of("when.strftime('%s')\n" % directive)), 1, directive)

    def test_strftime_with_numbers_only_is_fine(self) -> None:
        self.assertEqual(self.problems_of("when.strftime('%Y-%m-%d %H:%M')\nwhen.strftime('100%%')\n"), [])

    def test_the_n_number_format(self) -> None:
        self.assertEqual(len(self.problems_of("'{:n}'.format(5)\n")), 1)
        self.assertEqual(len(self.problems_of("f'{count:n}'\n")), 1)

    def test_open_without_encoding(self) -> None:
        self.assertEqual(len(self.problems_of("open('a')\nopen('a', 'w')\nio.open('a')\n")), 3)

    def test_open_with_encoding_or_in_binary_mode_is_fine(self) -> None:
        self.assertEqual(self.problems_of("open('a', encoding='utf-8')\nopen('a', 'rb')\nopen('a', mode='wb')\n"), [])

    def test_a_method_named_open_on_another_object_is_not_the_builtin(self) -> None:
        self.assertEqual(self.problems_of('opener.open(request)\narchive.open(name)\n'), [])

    def test_path_read_text_and_write_text_without_encoding(self) -> None:
        self.assertEqual(len(self.problems_of("path.read_text()\npath.write_text('x')\n")), 2)
        self.assertEqual(self.problems_of("path.read_text(encoding='utf-8')\n"), [])

    def test_a_text_mode_subprocess_without_encoding(self) -> None:
        self.assertEqual(len(self.problems_of("subprocess.run(['x'], text=True)\n")), 1)
        self.assertEqual(len(self.problems_of("subprocess.check_output(['x'], universal_newlines=True)\n")), 1)

    def test_a_subprocess_with_encoding_or_in_binary_mode_is_fine(self) -> None:
        self.assertEqual(self.problems_of("subprocess.run(['x'], encoding='utf-8')\nsubprocess.run(['x'])\n"), [])


def parse_every_machine_text() -> Dict[str, Any]:
    """Everything the bot parses or formats from outside text, in one comparable value."""
    results: Dict[str, Any] = {}
    readme = fixture('runners/README-0e63d4f9d.md').decode('utf-8')
    rows = runner_labels.parse_runner_images_readme(readme)
    results['readme'] = [row.to_json() for row in rows]
    results['labels-today'] = runner_labels.check_runner_labels(pinned_versions_with_labels(), rows).to_json()
    for key in ('macos-26-arm64', 'xcode-27-arm64', 'ubuntu26'):
        results['rollout-' + key] = runner_labels.parse_rollout_state(
            fixture('runners/rollout-state-%s.json' % key)).to_json()
    images = android_levels.parse_system_images(fixture('android/google_apis-sys-img2-4.xml'))
    results['images'] = [image.to_json() for image in images]
    results['platforms'] = sorted(android_levels.parse_platforms(fixture('android/repository2-3.xml')))
    pinned = read_pinned_versions_from_texts(
        repo_text('device-tests.yml'), repo_text('dotnet-build.yml'), repo_text('shared.env'))
    results['pinned'] = pinned.to_json()
    downloader = FakeDownloader(dict(downloads_for_band_11(), **{
        required_xcode.DOTNET_RELEASES_INDEX: fixture('xcode/releases-index.json')}))
    results['required-xcode'] = required_xcode.read_required_xcode(downloader, pinned_with('11.0.100', '11.0.x'))
    results['versions'] = [
        list(parse_android_level('37.2')), feature_band('11.0.103'), parse_utc('2026-11-21T02:17:05.1234567Z').isoformat(),
        parse_utc('2026-11-21T04:17:05+02:00').isoformat(),
        token_owner.parse_expiration('2026-12-01 10:00:00 UTC').isoformat(),
    ]
    results['markdown'] = [markdown_code(text) for text in (
        'TITLE title Iİıi', 'a|b', 'x`y', 'FILE.txt', 'İstanbul')]
    return results


def plan_and_text_of_an_android_change(test: unittest.TestCase) -> Dict[str, Any]:
    plan = Night(test, android=android_with_sixteen_kb_37_3()).plan()
    return {'plan': plan.to_json(), 'title': pull_request_text.render_title(plan),
            'body': pull_request_text.render_pull_request_body(plan)}


def snapshot(test: unittest.TestCase) -> str:
    combined = {'machine-texts': parse_every_machine_text(), 'android-change': plan_and_text_of_an_android_change(test),
                'night-today': Night(test).plan().to_json()}
    return json.dumps(combined, sort_keys=True, indent=1, default=str)


class SameResultsInEveryLocaleTests(unittest.TestCase):
    """Compares results under the C locale with results under another locale in the same process."""

    def setUp(self) -> None:
        self.original_locale = locale.setlocale(locale.LC_ALL)
        self.addCleanup(locale.setlocale, locale.LC_ALL, self.original_locale)

    def switch_to(self, name: str) -> None:
        try:
            locale.setlocale(locale.LC_ALL, name)
        except locale.Error as error:
            message = 'the locale %r is not installed here: %s' % (name or '(the environment)', error)
            if RUNNING_IN_CI:
                self.fail(message)
            self.skipTest(message)

    def assert_same_as_under_c(self, other_locale: str) -> None:
        self.switch_to('C')
        under_c = snapshot(self)
        self.switch_to(other_locale)
        under_other = snapshot(self)
        self.assertEqual(under_c, under_other)

    def test_results_under_the_locale_of_the_environment_equal_those_under_c(self) -> None:
        self.assert_same_as_under_c('')

    def test_results_under_turkish_equal_those_under_c(self) -> None:
        self.assert_same_as_under_c(TURKISH_LOCALE)

    def test_the_snapshot_covers_every_parser_and_the_planner(self) -> None:
        text = snapshot(self)
        for expected in ('"readme"', '"images"', '"platforms"', '"pinned"', '"required-xcode"', '"night-today"',
                         '"android-change"', 'ubuntu-label:ubuntu-26.04'):
            self.assertIn(expected, text)

    def test_the_turkish_dotted_capital_i_stays_inside_the_code_span_unchanged(self) -> None:
        self.assertEqual(markdown_code('İstanbul TITLE'), '`İstanbul TITLE`')

    def test_ascii_keywords_keep_their_case_mapping_under_python_whatever_the_locale(self) -> None:
        self.switch_to(TURKISH_LOCALE)
        self.assertEqual('TITLE'.lower(), 'title')
        self.assertEqual('internal'.upper(), 'INTERNAL')
        self.assertTrue('FILE'.lower() == 'file')


if __name__ == '__main__':
    unittest.main()
