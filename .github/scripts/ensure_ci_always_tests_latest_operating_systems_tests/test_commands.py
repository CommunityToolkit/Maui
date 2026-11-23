from __future__ import annotations

import contextlib
import io
import json
import os
import re
import shutil
import tempfile
import unittest
from typing import Any, Dict, List, Optional, Tuple
from unittest import mock

from ensure_ci_always_tests_latest_operating_systems import commands, markers, runner_labels
from ensure_ci_always_tests_latest_operating_systems.android_levels import PLATFORMS_URL, SYSTEM_IMAGES_URL
from ensure_ci_always_tests_latest_operating_systems.change_plan import Plan
from ensure_ci_always_tests_latest_operating_systems.commands import Services, format_step_output, main, write_step_outputs
from ensure_ci_always_tests_latest_operating_systems.errors import NotFoundError
from ensure_ci_always_tests_latest_operating_systems.data_types import ThingToCheck
from ensure_ci_always_tests_latest_operating_systems.required_xcode import DOTNET_RELEASES_INDEX
from ensure_ci_always_tests_latest_operating_systems.runner_labels import README_URL, ROLLOUT_STATE_URL
from ensure_ci_always_tests_latest_operating_systems.token_owner import TokenStatus

from .helpers import FakeDownloader, FakeGitHub, fixture
from .test_change_plan import (BASE, BASE_SHA, BOT_BRANCH, NO_PULL_REQUEST, RUN_URL, Night, android_with_sixteen_kb_37_3,
                               read_file)
from .test_required_xcode import downloads_for_band_11

REPO = 'CommunityToolkit/Maui'
MAINTAINER_TOKEN = 'maintainer-secret'
WORKFLOW_TOKEN = 'workflow-secret'
RULES_PATH = '/repos/%s/rules/branches/%s' % (REPO, BASE)
DELIMITER_LINE = re.compile(r'^(?P<name>[\w-]+)<<(?P<delimiter>EOF_[0-9a-f]{32})$')


def todays_downloads() -> Dict[str, Any]:
    mapping: Dict[str, Any] = {
        SYSTEM_IMAGES_URL: fixture('android/google_apis-sys-img2-4.xml'),
        PLATFORMS_URL: fixture('android/repository2-3.xml'),
        DOTNET_RELEASES_INDEX: fixture('xcode/releases-index.json'),
        README_URL: fixture('runners/README-0e63d4f9d.md'),
    }
    mapping.update(downloads_for_band_11())
    for row in runner_labels.parse_runner_images_readme(fixture('runners/README-0e63d4f9d.md').decode('utf-8')):
        if row.rollout_state_key:
            url = ROLLOUT_STATE_URL.format(key=row.rollout_state_key)
            mapping[url] = NotFoundError(url)
    for name in ('macos-15-arm64', 'macos-26-arm64', 'xcode-27-arm64'):
        mapping[ROLLOUT_STATE_URL.format(key=name)] = fixture('runners/rollout-state-%s.json' % name)
    return mapping


class RecordingServices(Services):
    """Hands out fakes and remembers which token each client was built for."""

    def __init__(self, downloader: Optional[FakeDownloader] = None,
                 github_by_token: Optional[Dict[Optional[str], FakeGitHub]] = None) -> None:
        self.downloader = downloader if downloader is not None else FakeDownloader(todays_downloads())
        self.github_by_token = github_by_token or {}
        self.downloader_save_dirs: List[Optional[str]] = []
        self.clients_built_for: List[Tuple[Optional[str], str]] = []
        super().__init__(make_downloader=self._make_downloader, make_github_client=self._make_github_client)

    def _make_downloader(self, save_dir: Optional[str]) -> Any:
        self.downloader_save_dirs.append(save_dir)
        return self.downloader

    def _make_github_client(self, token: Optional[str], repo: str) -> Any:
        self.clients_built_for.append((token, repo))
        client = self.github_by_token[token]
        client.repo = repo
        return client


def rules_github() -> FakeGitHub:
    return FakeGitHub({'GET ' + RULES_PATH: json.loads(fixture('github/rules-branches-main.json'))})


class CommandTestCase(unittest.TestCase):
    def setUp(self) -> None:
        self.folder = tempfile.mkdtemp()
        self.addCleanup(shutil.rmtree, self.folder, True)
        self.github_output = os.path.join(self.folder, 'github-output.txt')
        self.environ: Dict[str, str] = {'GITHUB_OUTPUT': self.github_output, 'GITHUB_REPOSITORY': REPO}

    def path(self, *parts: str) -> str:
        return os.path.join(self.folder, *parts)

    def run_main(self, argv: List[str], services: Optional[Services] = None, **environ: str) -> Tuple[int, str, str]:
        out, err = io.StringIO(), io.StringIO()
        with contextlib.redirect_stdout(out), contextlib.redirect_stderr(err):
            code = main(argv, dict(self.environ, **environ), services or RecordingServices())
        return code, out.getvalue(), err.getvalue()

    def read_json(self, *parts: str) -> Dict[str, Any]:
        with open(self.path(*parts), 'r', encoding='utf-8') as file:
            return json.load(file)

    def written_outputs(self) -> str:
        return read_file(self.github_output) if os.path.exists(self.github_output) else ''

    def repo_root_with_the_pin_files(self) -> str:
        root = self.path('repo')
        for name in ('device-tests.yml', 'dotnet-build.yml', 'shared.env'):
            target = os.path.join(root, '.github', 'workflows', name)
            os.makedirs(os.path.dirname(target), exist_ok=True)
            with open(target, 'wb') as file:
                file.write(fixture('repo/' + name))
        return root


class ArgumentErrorTests(CommandTestCase):
    def test_no_command_exits_with_2(self) -> None:
        self.assertEqual(self.run_main([])[0], 2)

    def test_an_unknown_command_exits_with_2(self) -> None:
        code, _, error = self.run_main(['merge-everything'])
        self.assertEqual(code, 2)
        self.assertIn('invalid choice', error)

    def test_a_missing_argument_exits_with_2_and_names_it(self) -> None:
        code, _, error = self.run_main(['plan-changes', '--repo-root', '.'])
        self.assertEqual(code, 2)
        self.assertIn('--base-sha', error)

    def test_a_reopen_rejected_value_other_than_true_or_false_exits_with_2(self) -> None:
        argv = ['plan-changes', '--repo-root', '.', '--base', BASE, '--base-sha', BASE_SHA, '--bot-branch', BOT_BRANCH,
                '--latest-versions', 'a', '--macos-runner-images', 'b', '--reopen-rejected', 'maybe', '--out', 'c']
        self.assertEqual(self.run_main(argv)[0], 2)

    def test_a_pull_request_number_that_is_not_a_number_exits_with_2(self) -> None:
        argv = ['approve-and-turn-on-auto-merge', '--plan', 'a', '--decision', 'b', '--base', BASE, '--base-sha', BASE_SHA,
                '--bot-branch', BOT_BRANCH, '--pull-request', 'five', '--head-sha', 'f' * 40, '--operation', 'created',
                '--out', 'c']
        self.assertEqual(self.run_main(argv)[0], 2)

    def test_help_exits_with_0(self) -> None:
        code, out, _ = self.run_main(['--help'])
        self.assertEqual(code, 0)
        for command in commands._COMMANDS:
            self.assertIn(command, out)

    def test_every_command_of_the_workflow_exists(self) -> None:
        self.assertEqual(sorted(commands._COMMANDS), sorted([
            'read-latest-versions', 'inspect-macos-runner-image', 'plan-changes', 'decide-pull-request-update',
            'approve-and-turn-on-auto-merge', 'record-step-outcomes', 'update-issue-for-a-person']))

    def test_the_repository_defaults_to_the_environment_and_then_to_maui(self) -> None:
        argv = ['record-step-outcomes', '--out', 'x']
        self.assertEqual(commands._build_parser({'GITHUB_REPOSITORY': 'a/b'}).parse_args(argv).repo, 'a/b')
        self.assertEqual(commands._build_parser({}).parse_args(argv).repo, REPO)


class StepOutputTests(CommandTestCase):
    def test_a_single_line_value_is_written_as_name_equals_value(self) -> None:
        self.assertEqual(format_step_output('open-or-update', 'true'), 'open-or-update=true\n')

    def test_a_multi_line_value_uses_a_random_delimiter(self) -> None:
        text = format_step_output('commit-message', 'Title\n\nGenerated-by: x')
        first, *middle, last = text.splitlines()
        match = DELIMITER_LINE.match(first)
        self.assertIsNotNone(match)
        self.assertEqual(middle, ['Title', '', 'Generated-by: x'])
        self.assertEqual(last, match.group('delimiter'))

    def test_the_delimiter_differs_on_every_call(self) -> None:
        delimiters = {DELIMITER_LINE.match(format_step_output('a', 'x\ny').splitlines()[0]).group('delimiter')
                      for _ in range(20)}
        self.assertEqual(len(delimiters), 20)

    def test_a_carriage_return_also_counts_as_multi_line(self) -> None:
        self.assertTrue(DELIMITER_LINE.match(format_step_output('a', 'x\ry').splitlines()[0]))

    def test_a_value_that_forges_an_output_line_stays_inside_the_value(self) -> None:
        text = format_step_output('title', 'harmless\nopen-or-update=true\nEOF')
        lines = text.splitlines()
        self.assertTrue(DELIMITER_LINE.match(lines[0]))
        delimiter = DELIMITER_LINE.match(lines[0]).group('delimiter')
        self.assertEqual(lines[1:-1], ['harmless', 'open-or-update=true', 'EOF'])
        self.assertEqual(lines.count(delimiter), 1)
        self.assertEqual(lines[-1], delimiter)

    def test_values_are_appended_to_github_output_in_utf8(self) -> None:
        write_step_outputs({'a': '1', 'b': 'café\nbar'}, self.environ)
        write_step_outputs({'c': '3'}, self.environ)
        with open(self.github_output, 'rb') as file:
            data = file.read().decode('utf-8')
        self.assertTrue(data.startswith('a=1\nb<<EOF_'))
        self.assertIn('café\nbar\n', data)
        self.assertTrue(data.endswith('c=3\n'))

    def test_without_github_output_nothing_is_written(self) -> None:
        write_step_outputs({'a': '1'}, {})
        self.assertFalse(os.path.exists(self.github_output))


class ReadLatestVersionsTests(CommandTestCase):
    def argv(self, root: Optional[str] = None) -> List[str]:
        return ['read-latest-versions', '--repo-root', root or self.repo_root_with_the_pin_files(), '--base', BASE,
                '--out', self.path('latest-versions')]

    def services(self, downloader: Optional[FakeDownloader] = None) -> RecordingServices:
        return RecordingServices(downloader, {WORKFLOW_TOKEN: rules_github(), None: rules_github()})

    def test_every_source_is_written_with_status_ok_and_the_labels_go_to_the_step_output(self) -> None:
        code, out, _ = self.run_main(self.argv(), self.services(), GITHUB_TOKEN=WORKFLOW_TOKEN)
        self.assertEqual(code, 0)
        self.assertEqual(out, '')
        for name in ('pinned-versions', 'android', 'required-xcode', 'runner-images', 'branch-rules',
                     'macos-labels-to-inspect'):
            self.assertEqual(self.read_json('latest-versions', name + '.json')['status'], 'ok', name)
        self.assertEqual(self.read_json('latest-versions', 'macos-labels-to-inspect.json')['labels'],
                         ['xcode-27', 'macos-26', 'macos-15'])
        self.assertEqual(self.written_outputs(), 'macos-labels-to-inspect=["xcode-27", "macos-26", "macos-15"]\n')

    def test_the_branch_rules_are_read_with_the_workflow_token_and_the_downloads_are_saved_beside_the_files(self) -> None:
        services = self.services()
        self.run_main(self.argv(), services, GITHUB_TOKEN=WORKFLOW_TOKEN)
        self.assertEqual(services.clients_built_for, [(WORKFLOW_TOKEN, REPO)])
        self.assertEqual(services.downloader_save_dirs, [self.path('latest-versions', 'downloads')])

    def test_without_a_token_the_branch_rules_are_read_unauthenticated(self) -> None:
        services = self.services()
        code, _, _ = self.run_main(self.argv(), services)
        self.assertEqual(code, 0)
        self.assertEqual(services.clients_built_for, [(None, REPO)])

    def test_the_maintainer_token_is_never_used_to_read_sources(self) -> None:
        services = self.services()
        self.run_main(self.argv(), services, GITHUB_TOKEN=WORKFLOW_TOKEN,
                      **{markers.TOKEN_SECRET_NAME: MAINTAINER_TOKEN})
        self.assertNotIn(MAINTAINER_TOKEN, [token for token, _ in services.clients_built_for])

    def test_one_source_that_raises_still_leaves_the_others_written_and_exits_with_0(self) -> None:
        downloads = todays_downloads()
        del downloads[SYSTEM_IMAGES_URL]
        code, out, _ = self.run_main(self.argv(), self.services(FakeDownloader(downloads)))
        self.assertEqual(code, 0)
        android = self.read_json('latest-versions', 'android.json')
        self.assertEqual(android['status'], 'error')
        self.assertIn('no answer for ' + SYSTEM_IMAGES_URL, android['error'])
        for name in ('pinned-versions', 'required-xcode', 'runner-images', 'branch-rules'):
            self.assertEqual(self.read_json('latest-versions', name + '.json')['status'], 'ok', name)
        self.assertEqual(out.splitlines()[0][:len('::error::android-system-images: ')], '::error::android-system-images: ')
        self.assertEqual(len(out.splitlines()), 1)
        self.assertIn('macos-labels-to-inspect=["xcode-27", "macos-26", "macos-15"]', self.written_outputs())

    def test_a_readme_that_cannot_be_read_leaves_only_the_repos_own_labels(self) -> None:
        downloads = todays_downloads()
        downloads[README_URL] = NotFoundError(README_URL)
        code, out, _ = self.run_main(self.argv(), self.services(FakeDownloader(downloads)))
        self.assertEqual(code, 0)
        self.assertEqual(self.read_json('latest-versions', 'runner-images.json')['status'], 'error')
        self.assertEqual(self.read_json('latest-versions', 'macos-labels-to-inspect.json')['labels'],
                         ['xcode-27', 'macos-26', 'macos-15'])
        self.assertIn('::error::runner-images-readme: ', out)

    def test_unreadable_pinned_versions_make_the_sources_that_need_them_errors_and_list_no_labels(self) -> None:
        empty_root = self.path('empty-repo')
        os.makedirs(empty_root)
        code, out, _ = self.run_main(self.argv(empty_root), self.services())
        self.assertEqual(code, 0)
        self.assertEqual(self.read_json('latest-versions', 'pinned-versions.json')['status'], 'error')
        self.assertEqual(self.read_json('latest-versions', 'required-xcode.json')['status'], 'error')
        self.assertEqual(self.read_json('latest-versions', 'android.json')['status'], 'ok')
        self.assertEqual(self.read_json('latest-versions', 'macos-labels-to-inspect.json')['labels'], [])
        self.assertEqual(self.written_outputs(), 'macos-labels-to-inspect=[]\n')
        self.assertIn('::error::pinned-versions: ', out)

    def test_the_branch_rules_failing_is_reported_as_that_source(self) -> None:
        from ensure_ci_always_tests_latest_operating_systems.errors import GitHubApiError
        broken = FakeGitHub({'GET ' + RULES_PATH: GitHubApiError('GET', RULES_PATH, 403, 'forbidden')})
        services = RecordingServices(None, {None: broken})
        code, out, _ = self.run_main(self.argv(), services)
        self.assertEqual(code, 0)
        self.assertEqual(self.read_json('latest-versions', 'branch-rules.json')['status'], 'error')
        self.assertIn('::error::branch-rules: ', out)

    def test_an_error_message_with_line_breaks_cannot_start_another_workflow_command(self) -> None:
        downloads = todays_downloads()
        downloads[SYSTEM_IMAGES_URL] = ValueError('first line\n::set-output name=x::y\r100%')
        _, out, _ = self.run_main(self.argv(), self.services(FakeDownloader(downloads)))
        self.assertEqual(len(out.splitlines()), 1)
        self.assertIn('first line%0A::set-output name=x::y%0D100%25', out)


class InspectMacosRunnerImageTests(CommandTestCase):
    def test_the_listing_is_written_to_the_file_and_a_missing_image_file_is_reported_without_failing(self) -> None:
        target = self.path('macos-runner-images', 'local.json')
        listing = {'status': 'error', 'label': 'local', 'error': 'no imagedata.json'}
        with mock.patch.object(commands, 'inspect_macos_runner_image', return_value=listing) as inspect:
            code, out, _ = self.run_main(['inspect-macos-runner-image', '--label', 'local', '--out', target])
        self.assertEqual(code, 0)
        inspect.assert_called_once_with('local')
        self.assertEqual(self.read_json('macos-runner-images', 'local.json'), listing)
        self.assertEqual(out, '::error::macos-runner-image:local: no imagedata.json\n')

    def test_a_good_listing_prints_nothing(self) -> None:
        target = self.path('listing.json')
        with mock.patch.object(commands, 'inspect_macos_runner_image',
                               return_value={'status': 'ok', 'label': 'xcode-27'}):
            code, out, _ = self.run_main(['inspect-macos-runner-image', '--label', 'xcode-27', '--out', target])
        self.assertEqual((code, out), (0, ''))


class PlanChangesTests(CommandTestCase):
    def argv(self, night: Night, reopen_rejected: str = 'false') -> List[str]:
        return ['plan-changes', '--repo-root', night.repo, '--base', BASE, '--base-sha', BASE_SHA,
                '--bot-branch', BOT_BRANCH, '--latest-versions', night.latest, '--macos-runner-images', night.software,
                '--reopen-rejected', reopen_rejected, '--run-url', RUN_URL, '--out', night.out]

    def run_plan(self, night: Night, reopen_rejected: str = 'false') -> Tuple[int, Any]:
        services = RecordingServices(None, {WORKFLOW_TOKEN: FakeGitHub({})})
        with mock.patch.object(commands, 'read_bump_pull_request_state', return_value=NO_PULL_REQUEST) as read_state:
            code, _, _ = self.run_main(self.argv(night, reopen_rejected), services, GITHUB_TOKEN=WORKFLOW_TOKEN)
        return code, read_state

    def test_today_writes_the_plan_files_and_reports_no_source_problem(self) -> None:
        night = Night(self)
        code, read_state = self.run_plan(night)
        self.assertEqual(code, 0)
        for name in ('plan.json', 'changes.patch', 'pull-request-body.md', 'commit-message.txt', 'title.txt', 'summary.md'):
            self.assertTrue(os.path.exists(os.path.join(night.out, name)), name)
        outputs = self.written_outputs()
        self.assertIn('has-source-problems=false\n', outputs)
        self.assertIn('pull-request-action=nothing\n', outputs)
        self.assertEqual(read_state.call_args[0][1:], (BASE, BOT_BRANCH))

    def test_a_planned_change_gives_the_action_and_the_change_list_key(self) -> None:
        night = Night(self, android=android_with_sixteen_kb_37_3())
        self.run_plan(night)
        plan = Plan.from_json(json.loads(read_file(os.path.join(night.out, 'plan.json'))))
        self.assertEqual(plan.pull_request_action, 'open-or-update')
        self.assertRegex(plan.change_list_key, r'^[0-9a-f]{12}$')
        self.assertIn('pull-request-action=open-or-update\n', self.written_outputs())
        self.assertIn('change-list-key=%s\n' % plan.change_list_key, self.written_outputs())
        self.assertEqual(plan.run_url, RUN_URL)

    def test_a_source_problem_is_reported_in_the_step_output(self) -> None:
        night = Night(self)
        shutil.rmtree(night.software)
        self.run_plan(night)
        self.assertIn('has-source-problems=true\n', self.written_outputs())

    def test_reopen_rejected_reaches_the_plan(self) -> None:
        night = Night(self)
        services = RecordingServices(None, {None: FakeGitHub({})})
        with mock.patch.object(commands, 'read_bump_pull_request_state', return_value=NO_PULL_REQUEST), \
                mock.patch.object(commands, 'build_plan', wraps=commands.build_plan) as build_plan:
            self.run_main(self.argv(night, 'TRUE'), services)
        self.assertIs(build_plan.call_args[0][7], True)


def decision_plan(action: str, commit_message: str = 'Update the device tests\n\nGenerated-by: x') -> Dict[str, Any]:
    plan = Plan(status='ok', run_url=RUN_URL, base=BASE, base_sha=BASE_SHA, bot_branch=BOT_BRANCH,
                pull_request_action=action, title='A title', commit_message=commit_message)
    return plan.to_json()


def token_github(scopes: str = 'public_repo, workflow') -> FakeGitHub:
    return FakeGitHub({
        'GET /user': (200, {'X-OAuth-Scopes': scopes}, {'login': 'maintainer', 'id': 7}),
        'GET /repos/' + REPO: {'permissions': {'push': True}},
    }, token_name='MAINTAINER')


def workflow_github(newest_commit: str = BASE_SHA) -> FakeGitHub:
    return FakeGitHub({'GET /repos/%s/git/ref/heads/%s' % (REPO, BASE): {'object': {'sha': newest_commit}}})


class DecidePullRequestUpdateTests(CommandTestCase):
    def argv(self) -> List[str]:
        return ['decide-pull-request-update', '--plan', self.path('plan'), '--base', BASE, '--base-sha', BASE_SHA,
                '--bot-branch', BOT_BRANCH, '--out', self.path('pull-request')]

    def write_plan(self, plan: Dict[str, Any]) -> None:
        os.makedirs(self.path('plan'))
        with open(self.path('plan', 'plan.json'), 'w', encoding='utf-8') as file:
            json.dump(plan, file)

    def test_a_usable_token_lets_the_pull_request_be_pushed_and_the_outputs_feed_create_pull_request(self) -> None:
        self.write_plan(decision_plan('open-or-update'))
        maintainer, workflow = token_github(), workflow_github()
        services = RecordingServices(None, {MAINTAINER_TOKEN: maintainer, WORKFLOW_TOKEN: workflow})
        code, _, _ = self.run_main(self.argv(), services, GITHUB_TOKEN=WORKFLOW_TOKEN,
                                   **{markers.TOKEN_SECRET_NAME: MAINTAINER_TOKEN})
        self.assertEqual(code, 0)
        decision = self.read_json('pull-request', 'decision.json')
        self.assertTrue(decision['open_or_update'])
        self.assertEqual(decision['token']['login'], 'maintainer')
        self.assertEqual([(call.method, call.path) for call in maintainer.calls],
                         [('GET', '/user'), ('GET', '/repos/' + REPO)])
        self.assertEqual([call.path for call in workflow.calls], ['/repos/%s/git/ref/heads/%s' % (REPO, BASE)])
        outputs = self.written_outputs()
        self.assertIn('open-or-update=true\n', outputs)
        self.assertIn('identity=maintainer <7+maintainer@users.noreply.github.com>\n', outputs)
        self.assertIn('title=A title\n', outputs)
        self.assertRegex(outputs, r'commit-message<<EOF_[0-9a-f]{32}\nUpdate the device tests\n\nGenerated-by: x\nEOF_')

    def test_a_secret_pasted_with_a_trailing_line_break_is_trimmed_before_the_clients_are_built(self) -> None:
        self.write_plan(decision_plan('open-or-update'))
        services = RecordingServices(None, {MAINTAINER_TOKEN: token_github(), WORKFLOW_TOKEN: workflow_github()})
        code, _, _ = self.run_main(self.argv(), services, GITHUB_TOKEN=WORKFLOW_TOKEN + '\n',
                                   **{markers.TOKEN_SECRET_NAME: MAINTAINER_TOKEN + '\r\n'})
        self.assertEqual(code, 0)
        self.assertEqual(sorted(services.clients_built_for),
                         sorted([(WORKFLOW_TOKEN, REPO), (MAINTAINER_TOKEN, REPO)]))
        self.assertTrue(self.read_json('pull-request', 'decision.json')['open_or_update'])

    def test_a_missing_token_stops_the_push_and_builds_no_maintainer_client(self) -> None:
        self.write_plan(decision_plan('open-or-update'))
        services = RecordingServices(None, {WORKFLOW_TOKEN: workflow_github()})
        code, _, _ = self.run_main(self.argv(), services, GITHUB_TOKEN=WORKFLOW_TOKEN)
        self.assertEqual(code, 0)
        decision = self.read_json('pull-request', 'decision.json')
        self.assertEqual((decision['open_or_update'], decision['token']['reason']), (False, 'missing'))
        self.assertIn('open-or-update=false\n', self.written_outputs())
        self.assertEqual(services.clients_built_for, [(WORKFLOW_TOKEN, REPO)])

    def run_decide(self, services: Services, **environ: str) -> Tuple[int, str, str]:
        summary = self.path('summary.md')
        code, out, _ = self.run_main(self.argv(), services, GITHUB_TOKEN=WORKFLOW_TOKEN, GITHUB_STEP_SUMMARY=summary,
                                     **environ)
        return code, out, read_file(summary)

    def test_a_usable_token_is_named_in_the_summary_and_raises_no_error(self) -> None:
        self.write_plan(decision_plan('nothing'))
        services = RecordingServices(None, {MAINTAINER_TOKEN: token_github(), WORKFLOW_TOKEN: workflow_github()})
        _, out, summary = self.run_decide(services, **{markers.TOKEN_SECRET_NAME: MAINTAINER_TOKEN})
        self.assertEqual(summary, 'Token: usable, it belongs to `maintainer`.\n\n')
        self.assertNotIn('::error::', out)

    def test_an_unusable_token_is_explained_in_the_summary_and_raises_an_error_annotation(self) -> None:
        self.write_plan(decision_plan('nothing'))
        services = RecordingServices(None, {WORKFLOW_TOKEN: workflow_github()})
        _, out, summary = self.run_decide(services)
        self.assertIn('Token: unusable', summary)
        self.assertIn('the secret holding the maintainer token is empty or not set', summary)
        self.assertIn('gh secret set %s -R %s --env %s' % (markers.TOKEN_SECRET_NAME, REPO, markers.TOKEN_ENVIRONMENT),
                      summary)
        self.assertTrue(out.startswith('::error::decide-pull-request-update: Token: unusable'))
        self.assertEqual(out.count('\n'), 1)

    def test_the_error_annotation_escapes_percent_signs_and_line_breaks(self) -> None:
        status = TokenStatus(usable=False, reason='x', things_to_check=[ThingToCheck('token-unusable:x', '100%\nbad')])
        out = io.StringIO()
        with contextlib.redirect_stdout(out):
            commands._report_token_status(status, REPO, {})
        self.assertIn('100%25%0Abad', out.getvalue())
        self.assertEqual(out.getvalue().count('\n'), 1)

    def test_the_maintainer_token_never_reaches_the_workflow_client(self) -> None:
        self.write_plan(decision_plan('nothing'))
        services = RecordingServices(None, {MAINTAINER_TOKEN: token_github(), WORKFLOW_TOKEN: workflow_github()})
        self.run_main(self.argv(), services, GITHUB_TOKEN=WORKFLOW_TOKEN, **{markers.TOKEN_SECRET_NAME: MAINTAINER_TOKEN})
        self.assertEqual(sorted(token for token, _ in services.clients_built_for), sorted([MAINTAINER_TOKEN, WORKFLOW_TOKEN]))
        self.assertEqual(services.clients_built_for[0], (WORKFLOW_TOKEN, REPO))

    def test_main_that_moved_stops_the_push(self) -> None:
        self.write_plan(decision_plan('open-or-update'))
        services = RecordingServices(None, {MAINTAINER_TOKEN: token_github(), WORKFLOW_TOKEN: workflow_github('b' * 40)})
        self.run_main(self.argv(), services, GITHUB_TOKEN=WORKFLOW_TOKEN, **{markers.TOKEN_SECRET_NAME: MAINTAINER_TOKEN})
        decision = self.read_json('pull-request', 'decision.json')
        self.assertTrue(decision['main_moved'])
        self.assertFalse(decision['open_or_update'])

    def run_with_a_moved_main(self, **environ: str) -> Tuple[int, str, str]:
        self.write_plan(decision_plan('open-or-update'))
        services = RecordingServices(None, {MAINTAINER_TOKEN: token_github(), WORKFLOW_TOKEN: workflow_github('b' * 40)})
        return self.run_main(self.argv(), services, GITHUB_TOKEN=WORKFLOW_TOKEN,
                             **{markers.TOKEN_SECRET_NAME: MAINTAINER_TOKEN}, **environ)

    def test_a_decision_note_reaches_the_run_summary_and_the_job_log(self) -> None:
        summary = self.path('summary.md')
        _, out, _ = self.run_with_a_moved_main(GITHUB_STEP_SUMMARY=summary)
        note = self.read_json('pull-request', 'decision.json')['notes'][0]
        self.assertIn('- Note: %s\n' % note, read_file(summary))
        self.assertIn('::notice title=Pull request decision::%s\n' % note, out)

    def test_a_decision_note_is_escaped_for_the_workflow_command(self) -> None:
        self.assertEqual(commands._escape_workflow_command_data('100%\r\nnext'), '100%25%0D%0Anext')

    def test_a_decision_note_still_prints_when_there_is_no_run_summary_file(self) -> None:
        code, out, _ = self.run_with_a_moved_main()
        self.assertEqual(code, 0)
        self.assertIn('::notice title=Pull request decision::', out)

    def test_a_missing_plan_fails_the_step_with_a_workflow_error(self) -> None:
        code, out, _ = self.run_main(self.argv(), RecordingServices(None, {WORKFLOW_TOKEN: workflow_github()}),
                                     GITHUB_TOKEN=WORKFLOW_TOKEN)
        self.assertEqual(code, 1)
        self.assertTrue(out.startswith('::error::decide-pull-request-update: '))
        self.assertFalse(os.path.exists(self.path('pull-request', 'decision.json')))


class ApproveAndTurnOnAutoMergeTests(CommandTestCase):
    def argv(self) -> List[str]:
        return ['approve-and-turn-on-auto-merge', '--plan', self.path('plan'), '--decision', self.path('pull-request', 'decision.json'),
                '--base', BASE, '--base-sha', BASE_SHA, '--bot-branch', BOT_BRANCH, '--pull-request', '5',
                '--head-sha', 'f' * 40, '--operation', 'created', '--out', self.path('pull-request')]

    def write(self, *parts: str, data: Any) -> None:
        os.makedirs(os.path.dirname(self.path(*parts)), exist_ok=True)
        with open(self.path(*parts), 'w', encoding='utf-8') as file:
            json.dump(data, file)

    def test_the_approval_uses_only_the_workflow_token_and_is_written_next_to_the_decision(self) -> None:
        self.write('plan', 'plan.json', data=decision_plan('open-or-update'))
        self.write('pull-request', 'decision.json', data={'token': {'login': 'maintainer'}})
        approval = {'status': 'ok', 'refusals': [], 'approved': 'new'}
        services = RecordingServices(None, {WORKFLOW_TOKEN: FakeGitHub({})})
        with mock.patch.object(commands, 'approve_and_turn_on_auto_merge', return_value=approval) as approve:
            code, _, _ = self.run_main(self.argv(), services, GITHUB_TOKEN=WORKFLOW_TOKEN,
                                       **{markers.TOKEN_SECRET_NAME: MAINTAINER_TOKEN})
        self.assertEqual(code, 0)
        self.assertEqual(services.clients_built_for, [(WORKFLOW_TOKEN, REPO)])
        self.assertEqual(self.read_json('pull-request', 'approval.json'), approval)
        positional = approve.call_args[0]
        self.assertEqual(positional[2], {'token': {'login': 'maintainer'}})
        self.assertEqual(positional[3:], (5, 'f' * 40, 'created', BASE, BASE_SHA, BOT_BRANCH))
        self.assertEqual(approve.call_args[1], {'run_url': RUN_URL})

    def test_a_missing_decision_fails_the_step_and_approves_nothing(self) -> None:
        self.write('plan', 'plan.json', data=decision_plan('open-or-update'))
        with mock.patch.object(commands, 'approve_and_turn_on_auto_merge') as approve:
            code, out, _ = self.run_main(self.argv(), RecordingServices(None, {WORKFLOW_TOKEN: FakeGitHub({})}),
                                         GITHUB_TOKEN=WORKFLOW_TOKEN)
        self.assertEqual(code, 1)
        approve.assert_not_called()
        self.assertIn('decision.json', out)


class RecordStepOutcomesTests(CommandTestCase):
    def test_the_outcomes_of_the_four_steps_are_written(self) -> None:
        code, _, _ = self.run_main(['record-step-outcomes', '--out', self.path('pull-request')], DECIDE_OUTCOME='success',
                                   APPLY_OUTCOME='skipped', OPEN_OR_UPDATE_OUTCOME='failure', APPROVE_OUTCOME='cancelled')
        self.assertEqual(code, 0)
        self.assertEqual(self.read_json('pull-request', 'step-outcomes.json'), {
            'decide': 'success', 'apply': 'skipped', 'open_or_update': 'failure', 'approve': 'cancelled'})

    def test_a_step_that_never_ran_has_an_empty_outcome(self) -> None:
        self.run_main(['record-step-outcomes', '--out', self.path('pull-request')], DECIDE_OUTCOME='failure')
        self.assertEqual(self.read_json('pull-request', 'step-outcomes.json'), {
            'decide': 'failure', 'apply': '', 'open_or_update': '', 'approve': ''})


ISSUES_PATH = '/repos/%s/issues' % REPO


def issue_github(open_issues: Optional[List[Dict[str, Any]]] = None) -> FakeGitHub:
    return FakeGitHub({
        'GET %s?state=all&labels=requirements+updated&creator=github-actions%%5Bbot%%5D&per_page=100' % ISSUES_PATH:
            open_issues or [],
        'POST ' + ISSUES_PATH: (201, {}, {'number': 99}),
    })


class UpdateIssueForAPersonTests(CommandTestCase):
    def argv(self) -> List[str]:
        return ['update-issue-for-a-person', '--plan', self.path('plan'), '--pull-request', self.path('pull-request'),
                '--run-url', RUN_URL]

    def write_plan(self, plan: Dict[str, Any]) -> None:
        os.makedirs(self.path('plan'), exist_ok=True)
        with open(self.path('plan', 'plan.json'), 'w', encoding='utf-8') as file:
            json.dump(plan, file)

    def run_issue(self, github: FakeGitHub, **environ: str) -> Tuple[int, str]:
        services = RecordingServices(None, {WORKFLOW_TOKEN: github})
        code, out, _ = self.run_main(self.argv(), services, GITHUB_TOKEN=WORKFLOW_TOKEN, **environ)
        return code, out

    def test_a_failed_job_creates_the_issue_and_exits_with_1(self) -> None:
        github = issue_github()
        code, out = self.run_issue(github, JOB_RESULTS=json.dumps({'run-unit-tests': {'result': 'failure'}}))
        self.assertEqual(code, 1)
        self.assertEqual(out, 'Issue created #99.\n')
        body = github.calls_to('POST', ISSUES_PATH)[0].body['body']
        self.assertIn('failed-job:run-unit-tests', body)

    def test_a_clean_run_with_nothing_for_a_person_exits_with_0_and_opens_no_issue(self) -> None:
        self.write_plan(decision_plan('nothing'))
        github = issue_github()
        code, out = self.run_issue(github, JOB_RESULTS=json.dumps({'plan-changes': {'result': 'success'}}))
        self.assertEqual((code, out), (0, 'Issue none.\n'))
        self.assertEqual(github.write_calls(), [])

    def test_a_source_problem_in_the_plan_exits_with_1(self) -> None:
        plan = decision_plan('nothing')
        plan['source_problems'] = [{'key': 'source:android-system-images', 'source': 'android-system-images',
                                    'message': 'broken', 'family': 'android'}]
        self.write_plan(plan)
        code, _ = self.run_issue(issue_github(), JOB_RESULTS='{}')
        self.assertEqual(code, 1)

    def test_an_unusable_token_in_the_decision_turns_the_run_red(self) -> None:
        self.write_plan(decision_plan('nothing'))
        os.makedirs(self.path('pull-request'))
        decision = {'token': {'usable': False, 'reason': 'malformed'}, 'things_to_check': [
            {'key': 'token-unusable:malformed', 'text': 'The maintainer token can not be used.'}]}
        with open(self.path('pull-request', 'decision.json'), 'w', encoding='utf-8') as file:
            json.dump(decision, file)
        github = issue_github()
        code, out = self.run_issue(github, JOB_RESULTS='{}')
        self.assertEqual((code, out), (1, 'Issue created #99.\n'))
        self.assertIn('token-unusable:malformed', github.calls_to('POST', ISSUES_PATH)[0].body['body'])

    def test_a_job_results_value_that_is_not_json_is_ignored(self) -> None:
        self.write_plan(decision_plan('nothing'))
        code, _ = self.run_issue(issue_github(), JOB_RESULTS='not json')
        self.assertEqual(code, 0)

    def test_a_failed_push_quotes_the_error_line_of_this_runs_job_log(self) -> None:
        plan = decision_plan('open-or-update')
        plan['safe_changes'] = [{
            'family': 'android', 'kind': 'replace-api-level', 'file': '.github/workflows/device-tests.yml', 'line': 415,
            'old': '37.2', 'new': '37.3', 'inserted_lines': [], 'pin': 'android-leg:google_apis_ps16k:37',
            'description': 'x', 'source': 'x'}]
        plan['change_list_key'] = 'abcdef012345'
        self.write_plan(plan)
        with open(self.path('plan', 'changes.patch'), 'w', encoding='utf-8') as file:
            file.write('diff --git a/x b/x\n-37.2\n+37.3\n')
        os.makedirs(self.path('pull-request'))
        with open(self.path('pull-request', 'step-outcomes.json'), 'w', encoding='utf-8') as file:
            json.dump({'decide': 'success', 'apply': 'success', 'open_or_update': 'failure', 'approve': ''}, file)
        log = '2026-11-21T02:20:00.0000000Z ##[group]Run peter-evans/create-pull-request@x\n' \
              '2026-11-21T02:20:01.0000000Z remote: refusing to allow a Personal Access Token to create or update workflow\n'
        github = issue_github()
        github.responses['GET /repos/%s/actions/runs/42/attempts/2/jobs' % REPO] = [
            {'id': 777, 'name': markers.PULL_REQUEST_JOB_NAME}]
        github.responses['GET /repos/%s/actions/jobs/777/logs' % REPO] = log
        code, _ = self.run_issue(github, JOB_RESULTS='{}', GITHUB_RUN_ID='42', GITHUB_RUN_ATTEMPT='2')
        self.assertEqual(code, 1)
        body = github.calls_to('POST', ISSUES_PATH)[0].body['body']
        self.assertIn('refusing to allow a Personal Access Token', body)
        self.assertIn('bump-not-pushed:abcdef012345', body)
        self.assertIn('diff --git', body)

    def test_the_patches_of_the_plan_folder_are_read_by_their_path_in_the_folder(self) -> None:
        plan = {'changes_for_a_person': [{'patch_file': 'patches/a_b.patch'}]}
        self.assertEqual(commands._read_patches(self.path('plan'), plan), {})
        os.makedirs(self.path('plan', 'patches'))
        with open(self.path('plan', 'patches', 'a_b.patch'), 'w', encoding='utf-8') as file:
            file.write('diff text\n')
        with open(self.path('plan', 'changes.patch'), 'w', encoding='utf-8') as file:
            file.write('all changes\n')
        self.assertEqual(commands._read_patches(self.path('plan'), plan),
                         {'changes.patch': 'all changes\n', 'patches/a_b.patch': 'diff text\n'})


class ModuleSurfaceTests(unittest.TestCase):
    def test_the_entry_script_runs_main_and_exits_with_its_code(self) -> None:
        import subprocess
        import sys
        script = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))),
                              'ensure-ci-always-tests-latest-operating-systems.py')
        completed = subprocess.run([sys.executable, '-I', script, 'plan-changes'], stdout=subprocess.PIPE,
                                   stderr=subprocess.PIPE, encoding='utf-8')
        self.assertEqual(completed.returncode, 2)
        self.assertIn('--base-sha', completed.stderr)
        completed = subprocess.run([sys.executable, '-I', script, '--help'], stdout=subprocess.PIPE,
                                   stderr=subprocess.PIPE, encoding='utf-8')
        self.assertEqual(completed.returncode, 0)
        self.assertIn('update-issue-for-a-person', completed.stdout)


if __name__ == '__main__':
    unittest.main()
