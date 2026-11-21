from __future__ import annotations

import difflib
import os
import shutil
import subprocess
import tempfile
import unittest
from typing import Dict, List

from ensure_ci_always_tests_latest_operating_systems import line_editor as editor
from ensure_ci_always_tests_latest_operating_systems.data_types import SafeChange
from ensure_ci_always_tests_latest_operating_systems.markers import PIN_FILES
from ensure_ci_always_tests_latest_operating_systems.pinned_versions import read_pinned_versions_from_texts
from ensure_ci_always_tests_latest_operating_systems.version_numbers import parse_android_level

from .helpers import fixture

SHARED_ENV, DEVICE_TESTS, DOTNET_BUILD = PIN_FILES


def repo_texts() -> Dict[str, str]:
    return {path: fixture('repo/' + path.rsplit('/', 1)[1]).decode('utf-8') for path in PIN_FILES}


def pinned():
    texts = repo_texts()
    return read_pinned_versions_from_texts(texts[DEVICE_TESTS], texts[DOTNET_BUILD], texts[SHARED_ENV])


def change(kind: str, file: str, line: int, old=None, new=None, inserted=None, pin='x') -> SafeChange:
    return SafeChange(family='test', kind=kind, file=file, line=line, old=old, new=new,
                      inserted_lines=list(inserted or []), pin=pin, description='', source='test')


def xcode_change(key: str, line: int, new: str = '27.3') -> SafeChange:
    return change('set-xcode-version', SHARED_ENV, line, '27.2', new, pin='env:' + key)


def api_level_change(old: str, new: str, line: int) -> SafeChange:
    return change('replace-api-level', DEVICE_TESTS, line, old, new, pin='android-leg:google_apis:36')


def insert_android_leg(level: str, target: str, after_line: int) -> SafeChange:
    return change('insert-android-leg', DEVICE_TESTS, after_line, inserted=editor.android_leg_lines(
        pinned().android_dash_prefix, level, target), pin='android-leg:%s:%s' % (target, level.split('.')[0]))


def label_changes(new: str) -> List[SafeChange]:
    return [change('replace-runner-label', file, line, 'xcode-27', new, pin='apple-label:%s:%s' % (file, job))
            for file, line, job in pinned().apple_label.occurrences]


def changed_line_numbers(before: str, after: str) -> List[int]:
    """1-based numbers, in the before text, of every line the edit replaced or deleted; an insert reports the line before it."""
    matcher = difflib.SequenceMatcher(None, before.split('\n'), after.split('\n'), autojunk=False)
    numbers: List[int] = []
    for tag, start, end, _, _ in matcher.get_opcodes():
        if tag == 'insert':
            numbers.append(start)
        elif tag != 'equal':
            numbers.extend(range(start + 1, end + 1))
    return numbers


def git_is_available() -> bool:
    if shutil.which('git') is not None:
        return True
    if os.environ.get('CI'):
        raise AssertionError('git must exist on a CI runner')
    return False


def run_git(folder: str, *arguments: str, check: bool = True) -> subprocess.CompletedProcess:
    environment = dict(os.environ, GIT_CONFIG_GLOBAL=os.devnull, GIT_CONFIG_SYSTEM=os.devnull, GIT_CONFIG_NOSYSTEM='1')
    result = subprocess.run(['git', '-c', 'user.name=Test', '-c', 'user.email=test@example.com', '-c', 'commit.gpgsign=false',
                             '-c', 'core.autocrlf=false'] + list(arguments), cwd=folder, env=environment,
                            stdout=subprocess.PIPE, stderr=subprocess.STDOUT, encoding='utf-8')
    if check and result.returncode != 0:
        raise AssertionError('git %s failed: %s' % (' '.join(arguments), result.stdout))
    return result


class EachKindChangesOnlyTheIntendedLinesTests(unittest.TestCase):
    def test_xcode_versions(self) -> None:
        texts = repo_texts()
        after = editor.apply_changes(texts[SHARED_ENV], [
            xcode_change('CommunityToolkitLibrary_Xcode_Version', 18), xcode_change('CommunityToolkitSample_Xcode_Version', 21)])
        self.assertEqual(changed_line_numbers(texts[SHARED_ENV], after), [18, 21])
        self.assertIn('CommunityToolkitLibrary_Xcode_Version=27.3\n', after)
        self.assertIn('CommunityToolkitSample_Xcode_Version=27.3\n', after)

    def test_api_level_keeps_everything_else_on_the_line(self) -> None:
        texts = repo_texts()
        after = editor.apply_changes(texts[DEVICE_TESTS], [api_level_change('36.1', '36.2', 406)])
        self.assertEqual(changed_line_numbers(texts[DEVICE_TESTS], after), [406])
        self.assertEqual(after.split('\n')[405], " " * 20 + "-   api-level: '36.2'")

    def test_insert_a_four_kb_leg_after_an_entry(self) -> None:
        texts = repo_texts()
        after = editor.apply_changes(texts[DEVICE_TESTS], [insert_android_leg('38.0', 'google_apis', 417)])
        self.assertEqual(changed_line_numbers(texts[DEVICE_TESTS], after), [417])
        self.assertEqual(after.split('\n')[417], " " * 20 + "-   api-level: '38.0'")
        self.assertEqual(len(after.split('\n')), len(texts[DEVICE_TESTS].split('\n')) + 1)

    def test_insert_a_sixteen_kb_leg_adds_an_aligned_target_line(self) -> None:
        texts = repo_texts()
        after = editor.apply_changes(texts[DEVICE_TESTS], [insert_android_leg('38.1', 'google_apis_ps16k', 417)])
        lines = after.split('\n')
        self.assertEqual(lines[417:419], [" " * 20 + "-   api-level: '38.1'", ' ' * 24 + 'target: google_apis_ps16k'])
        read_pinned_versions_from_texts(after, texts[DOTNET_BUILD], texts[SHARED_ENV])

    def test_insert_an_ios_leg_after_the_include_line(self) -> None:
        texts = repo_texts()
        lines = editor.ios_leg_lines(pinned().ios_dash_prefix, '27.3', 'xcode-27', '27.3')
        after = editor.apply_changes(texts[DEVICE_TESTS], [change('insert-ios-leg', DEVICE_TESTS, 776, inserted=lines)])
        self.assertEqual(after.split('\n')[776:779], [" " * 20 + "-   ios-version: '27.3'", ' ' * 24 + 'runner: xcode-27',
                                                      ' ' * 24 + "xcode: '27.3'"])
        legs = read_pinned_versions_from_texts(after, texts[DOTNET_BUILD], texts[SHARED_ENV]).ios_legs
        self.assertEqual([leg.ios_version for leg in legs], ['27.3', '26.5', '18.6', '17.5', '16.4', '15.5'])

    def test_runner_label_on_all_five_apple_lines_leaves_windows_alone(self) -> None:
        texts = repo_texts()
        after = editor.apply_changes_to_texts(texts, label_changes('xcode-28'))
        self.assertEqual(changed_line_numbers(texts[DEVICE_TESTS], after[DEVICE_TESTS]), [551, 673, 905])
        self.assertEqual(changed_line_numbers(texts[DOTNET_BUILD], after[DOTNET_BUILD]), [57, 101])
        self.assertIn('os: [ windows-latest, xcode-28 ]', after[DOTNET_BUILD])
        self.assertEqual(after[DEVICE_TESTS].count('windows-2025'), texts[DEVICE_TESTS].count('windows-2025'))
        self.assertEqual(after[SHARED_ENV], texts[SHARED_ENV])

    def test_label_that_only_contains_the_old_one_is_not_replaced(self) -> None:
        text = 'x: [ a, xcode-270, xcode-27.1, xcode-27 ]\n'
        after = editor.apply_changes(text, [change('replace-runner-label', 'f', 1, 'xcode-27', 'xcode-28')])
        self.assertEqual(after, 'x: [ a, xcode-270, xcode-27.1, xcode-28 ]\n')


class SeveralChangesTests(unittest.TestCase):
    def test_two_inserts_after_one_line_end_up_in_ascending_order(self) -> None:
        texts = repo_texts()
        higher = insert_android_leg('39.0', 'google_apis', 417)
        lower = insert_android_leg('38.0', 'google_apis', 417)
        for order in ([lower, higher], [higher, lower]):
            with self.subTest([c.inserted_lines[0] for c in order]):
                after = editor.apply_changes(texts[DEVICE_TESTS], order)
                levels = [leg.level.text for leg in read_pinned_versions_from_texts(
                    after, texts[DOTNET_BUILD], texts[SHARED_ENV]).android_legs]
                self.assertEqual(levels[-3:], ['37.2', '38.0', '39.0'])

    def test_a_replacement_and_inserts_together(self) -> None:
        texts = repo_texts()
        after = editor.apply_changes(texts[DEVICE_TESTS], [
            insert_android_leg('38.0', 'google_apis', 417), api_level_change('36.1', '36.2', 406),
            insert_android_leg('38.1', 'google_apis_ps16k', 417)])
        legs = read_pinned_versions_from_texts(after, texts[DOTNET_BUILD], texts[SHARED_ENV]).android_legs
        self.assertEqual([leg.level.text for leg in legs][-5:], ['36.2', '37.0', '37.2', '38.0', '38.1'])

    def test_two_replacements_of_one_line_are_a_bug(self) -> None:
        texts = repo_texts()
        with self.assertRaises(ValueError):
            editor.apply_changes(texts[DEVICE_TESTS], [api_level_change('36.1', '36.2', 406), api_level_change('36.1', '36.3', 406)])

    def test_a_line_that_is_not_what_the_planner_saw_is_refused(self) -> None:
        texts = repo_texts()
        for bad in [api_level_change('36.0', '36.2', 406), api_level_change('36.1', '36.2', 405),
                    api_level_change('36.1', '36.2', 99999), xcode_change('TOOLKIT_NET_VERSION', 9)]:
            with self.subTest(bad.pin + str(bad.line)):
                with self.assertRaises(ValueError):
                    editor.apply_changes(texts[DEVICE_TESTS] if bad.kind == 'replace-api-level' else texts[SHARED_ENV], [bad])

    def test_unknown_kind_and_missing_file(self) -> None:
        with self.assertRaises(ValueError):
            editor.apply_changes('a\n', [change('rename-file', 'f', 1)])
        with self.assertRaises(ValueError):
            editor.apply_changes_to_texts({'a': 'x\n'}, [change('set-xcode-version', 'b', 1)])


class BytesStayTests(unittest.TestCase):
    def test_a_missing_final_newline_stays_missing(self) -> None:
        text = 'CommunityToolkitSample_Xcode_Version=27.2'
        after = editor.apply_changes(text, [change('set-xcode-version', SHARED_ENV, 1, '27.2', '27.3',
                                                   pin='env:CommunityToolkitSample_Xcode_Version')])
        self.assertEqual(after, 'CommunityToolkitSample_Xcode_Version=27.3')

    def test_carriage_returns_are_kept_on_changed_and_inserted_lines(self) -> None:
        text = '# note\r\nCommunityToolkitSample_Xcode_Version=27.2\r\nz\r\n'
        after = editor.apply_changes(text, [
            change('set-xcode-version', SHARED_ENV, 2, '27.2', '27.3', pin='env:CommunityToolkitSample_Xcode_Version'),
            change('insert-ios-leg', SHARED_ENV, 3, inserted=['new'])])
        self.assertEqual(after, '# note\r\nCommunityToolkitSample_Xcode_Version=27.3\r\nz\r\nnew\r\n')

    def test_an_insert_after_a_last_line_without_a_newline(self) -> None:
        after = editor.apply_changes('a', [change('insert-android-leg', 'f', 1, inserted=['b'])])
        self.assertEqual(after, 'a\nb\n')

    def test_non_ascii_comments_survive(self) -> None:
        text = '# café ☃   ı\nCommunityToolkitSample_Xcode_Version=27.2\n'
        after = editor.apply_changes(text, [change('set-xcode-version', SHARED_ENV, 2, '27.2', '27.3',
                                                   pin='env:CommunityToolkitSample_Xcode_Version')])
        self.assertEqual(after.split('\n')[0], '# café ☃   ı')

    def test_lines_helpers(self) -> None:
        self.assertEqual(editor.android_leg_lines('    -   ', parse_android_level('37.0'), 'google_apis'), ["    -   api-level: '37.0'"])
        self.assertEqual(editor.android_leg_lines('    -   ', '37.2', 'google_apis_ps16k'),
                         ["    -   api-level: '37.2'", '        target: google_apis_ps16k'])


class UnifiedDiffTests(unittest.TestCase):
    def setUp(self) -> None:
        if not git_is_available():
            self.skipTest('git is not installed')
        self.folder = tempfile.TemporaryDirectory()
        self.addCleanup(self.folder.cleanup)
        self.texts = repo_texts()

    def write_repo_copy(self, folder: str) -> None:
        for path, text in self.texts.items():
            full = os.path.join(folder, *path.split('/'))
            os.makedirs(os.path.dirname(full), exist_ok=True)
            with open(full, 'w', encoding='utf-8', newline='') as file:
                file.write(text)

    def many_changes_patch(self) -> str:
        changes = [xcode_change('CommunityToolkitLibrary_Xcode_Version', 18), xcode_change('CommunityToolkitSample_Xcode_Version', 21),
                   api_level_change('36.1', '36.2', 406), insert_android_leg('38.0', 'google_apis', 417)] + label_changes('xcode-28')
        after = editor.apply_changes_to_texts(self.texts, changes)
        return ''.join(editor.unified_diff(path, self.texts[path], after[path]) for path in PIN_FILES)

    def test_an_unchanged_file_has_an_empty_patch(self) -> None:
        self.assertEqual(editor.unified_diff(SHARED_ENV, 'a\n', 'a\n'), '')

    def test_patch_passes_git_apply_check_and_reproduces_the_edit(self) -> None:
        patch = self.many_changes_patch()
        self.write_repo_copy(self.folder.name)
        patch_path = os.path.join(self.folder.name, 'changes.patch')
        with open(patch_path, 'w', encoding='utf-8', newline='') as file:
            file.write(patch)
        run_git(self.folder.name, 'apply', '--check', patch_path)
        run_git(self.folder.name, 'apply', patch_path)
        for path in PIN_FILES:
            with open(os.path.join(self.folder.name, *path.split('/')), encoding='utf-8', newline='') as file:
                self.assertEqual(file.read(), editor.apply_changes_to_texts(self.texts, [
                    xcode_change('CommunityToolkitLibrary_Xcode_Version', 18),
                    xcode_change('CommunityToolkitSample_Xcode_Version', 21), api_level_change('36.1', '36.2', 406),
                    insert_android_leg('38.0', 'google_apis', 417)] + label_changes('xcode-28'))[path])

    def test_index_blob_ids_equal_git_hash_object(self) -> None:
        before = '# café\nA=1\n'
        after = '# café\nA=2\n'
        patch = editor.unified_diff(SHARED_ENV, before, after)
        index_line = [line for line in patch.split('\n') if line.startswith('index ')][0]
        for text, blob in zip((before, after), index_line[len('index '):].split(' ')[0].split('..')):
            path = os.path.join(self.folder.name, 'blob.txt')
            with open(path, 'w', encoding='utf-8', newline='') as file:
                file.write(text)
            self.assertEqual(run_git(self.folder.name, 'hash-object', 'blob.txt').stdout.strip(), blob)

    def test_patch_of_a_file_without_a_final_newline(self) -> None:
        patch = editor.unified_diff(SHARED_ENV, 'a\nb', 'a\nc')
        self.assertIn('\\ No newline at end of file', patch)
        path = os.path.join(self.folder.name, *SHARED_ENV.split('/'))
        os.makedirs(os.path.dirname(path))
        with open(path, 'w', encoding='utf-8', newline='') as file:
            file.write('a\nb')
        patch_path = os.path.join(self.folder.name, 'p.patch')
        with open(patch_path, 'w', encoding='utf-8', newline='') as file:
            file.write(patch)
        run_git(self.folder.name, 'apply', '--check', patch_path)

    def test_three_way_apply_works_after_a_context_line_moved(self) -> None:
        folder = self.folder.name
        run_git(folder, 'init', '-q')
        self.write_repo_copy(folder)
        run_git(folder, 'add', '-A')
        run_git(folder, 'commit', '-q', '-m', 'base')
        after = editor.apply_changes(self.texts[SHARED_ENV], [xcode_change('CommunityToolkitSample_Xcode_Version', 21)])
        patch_folder = tempfile.TemporaryDirectory()
        self.addCleanup(patch_folder.cleanup)
        patch_path = os.path.join(patch_folder.name, 'sample-line.patch')
        with open(patch_path, 'w', encoding='utf-8', newline='') as file:
            file.write(editor.unified_diff(SHARED_ENV, self.texts[SHARED_ENV], after))

        env_path = os.path.join(folder, *SHARED_ENV.split('/'))
        moved = self.texts[SHARED_ENV].replace('CommunityToolkitLibrary_Xcode_Version=27.2',
                                               'CommunityToolkitLibrary_Xcode_Version=27.3')
        with open(env_path, 'w', encoding='utf-8', newline='') as file:
            file.write(moved)
        run_git(folder, 'commit', '-q', '-am', 'library moved on')

        self.assertNotEqual(run_git(folder, 'apply', '--check', patch_path, check=False).returncode, 0)
        run_git(folder, 'apply', '-3', patch_path)
        with open(env_path, encoding='utf-8', newline='') as file:
            result = file.read()
        self.assertIn('CommunityToolkitLibrary_Xcode_Version=27.3', result)
        self.assertIn('CommunityToolkitSample_Xcode_Version=27.3', result)


if __name__ == '__main__':
    unittest.main()
