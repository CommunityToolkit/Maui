"""The seven commands that ensure-ci-always-tests-latest-operating-systems.yml runs, one per job step."""
from __future__ import annotations

import argparse
import json
import os
import secrets
import sys
from dataclasses import dataclass
from datetime import datetime, timezone
from typing import Any, Callable, Dict, List, Mapping, Optional, Sequence

from . import markers
from .android_levels import read_android_index
from .approve_and_merge import approve_and_turn_on_auto_merge
from .bump_pull_request import read_bump_pull_request_state
from .change_plan import build_plan, write_plan_files
from .data_types import RunnerImageRow
from .downloads import Downloader
from .errors import GitHubApiError, UnreadableSourceError
from .github_client import GitHubClient
from .issue_for_a_person import PATCH_FILE_OF_UNOPENED_BUMP, collect_items, read_push_error_from_job_log, sync_issue
from .macos_runner_image import inspect_macos_runner_image
from .pinned_versions import read_pinned_versions
from .pull_request_decision import decide_pull_request_update, step_outputs
from .required_checks import read_branch_rules
from .required_xcode import read_required_xcode
from .runner_labels import macos_labels_to_inspect, read_runner_images
from .token_owner import check_token
from .version_numbers import major

DEFAULT_REPOSITORY = 'CommunityToolkit/Maui'
GITHUB_TOKEN_NAME = 'GITHUB_TOKEN'
MAINTAINER_TOKEN_NAME = markers.TOKEN_SECRET_NAME
STEP_OUTCOME_NAMES = ('decide', 'apply', 'open_or_update', 'approve')

SOURCE_PINNED_VERSIONS = 'pinned-versions'
SOURCE_ANDROID = 'android-system-images'
SOURCE_WORKLOAD_SETS = 'workload-sets'
SOURCE_RUNNER_IMAGES_README = 'runner-images-readme'
SOURCE_BRANCH_RULES = 'branch-rules'


@dataclass
class Services:
    """Tests replace these two to run every command without a network."""

    make_downloader: Callable[[Optional[str]], Any] = Downloader
    make_github_client: Callable[[Optional[str], str], Any] = GitHubClient


class _ArgumentError(Exception):
    pass


class _ArgumentParser(argparse.ArgumentParser):
    """argparse would exit the process; main returns the exit code instead, so a test can read it."""

    def error(self, message: str) -> Any:
        self.print_usage(sys.stderr)
        raise _ArgumentError(message)


def _true_or_false(text: str) -> bool:
    lowered = text.lower()
    if lowered not in ('true', 'false'):
        raise argparse.ArgumentTypeError("expected 'true' or 'false', got %r" % text)
    return lowered == 'true'


def _build_parser(environ: Mapping[str, str]) -> argparse.ArgumentParser:
    default_repository = environ.get('GITHUB_REPOSITORY') or DEFAULT_REPOSITORY
    parser = _ArgumentParser(prog='ensure-ci-always-tests-latest-operating-systems.py')
    commands = parser.add_subparsers(dest='command', required=True, parser_class=_ArgumentParser)

    def add_command(name: str, summary: str) -> argparse.ArgumentParser:
        command = commands.add_parser(name, help=summary, description=summary)
        command.add_argument('--repo', default=default_repository,
                             help='owner/name; default: $GITHUB_REPOSITORY, else %s' % DEFAULT_REPOSITORY)
        return command

    read = add_command('read-latest-versions', 'Read the Android index, workload sets, runner images and branch rules.')
    read.add_argument('--repo-root', required=True)
    read.add_argument('--base', required=True)
    read.add_argument('--out', required=True, help='folder for the JSON files')

    inspect = add_command('inspect-macos-runner-image', "List the Xcode apps, simulator runtimes and .NET SDKs of this Mac.")
    inspect.add_argument('--label', required=True)
    inspect.add_argument('--out', required=True, help='the JSON file to write')

    plan = add_command('plan-changes', 'Plan the safe changes, the changes a person must make and the things to check.')
    plan.add_argument('--repo-root', required=True)
    plan.add_argument('--base', required=True)
    plan.add_argument('--base-sha', required=True)
    plan.add_argument('--bot-branch', required=True)
    plan.add_argument('--latest-versions', required=True)
    plan.add_argument('--macos-runner-images', required=True)
    plan.add_argument('--reopen-rejected', type=_true_or_false, default=False)
    plan.add_argument('--run-url', default='')
    plan.add_argument('--out', required=True, help='folder for the plan files')

    decide = add_command('decide-pull-request-update', "Check the maintainer's token and decide whether the pull request may be pushed.")
    decide.add_argument('--plan', required=True, help='the plan folder')
    decide.add_argument('--base', required=True)
    decide.add_argument('--base-sha', required=True)
    decide.add_argument('--bot-branch', required=True)
    decide.add_argument('--out', required=True, help='folder for decision.json')

    approve = add_command('approve-and-turn-on-auto-merge', 'Check the bump pull request, then turn on auto-merge and approve it.')
    approve.add_argument('--plan', required=True, help='the plan folder')
    approve.add_argument('--decision', required=True, help='the decision.json file')
    approve.add_argument('--base', required=True)
    approve.add_argument('--base-sha', required=True)
    approve.add_argument('--bot-branch', required=True)
    approve.add_argument('--pull-request', required=True, type=int)
    approve.add_argument('--head-sha', required=True)
    approve.add_argument('--operation', required=True)
    approve.add_argument('--out', required=True, help='folder for approval.json')

    outcomes = add_command('record-step-outcomes', 'Write the outcomes of the publishing steps from the environment.')
    outcomes.add_argument('--out', required=True, help='folder for step-outcomes.json')

    issue = add_command('update-issue-for-a-person', 'Open, update or close the one issue that a person needs to act on.')
    issue.add_argument('--plan', required=True, help='the plan folder')
    issue.add_argument('--pull-request', required=True, help='the folder of the pull request results')
    issue.add_argument('--run-url', required=True)
    return parser


def _now() -> datetime:
    return datetime.now(timezone.utc)


def _write_text(path: str, text: str) -> None:
    folder = os.path.dirname(path)
    if folder:
        os.makedirs(folder, exist_ok=True)
    with open(path, 'w', encoding='utf-8', newline='') as file:
        file.write(text)


def _write_json(path: str, data: Any) -> None:
    _write_text(path, json.dumps(data, indent=2, sort_keys=True) + '\n')


def _read_json_if_present(path: str) -> Optional[Dict[str, Any]]:
    try:
        with open(path, 'r', encoding='utf-8-sig') as file:
            data = json.load(file)
    except (OSError, ValueError):
        return None
    return data if isinstance(data, dict) else None


def format_step_output(name: str, value: str) -> str:
    """The random delimiter keeps a value that contains the delimiter text, or a forged 'name=value' line, from ending the output early."""
    if '\n' not in value and '\r' not in value:
        return '%s=%s\n' % (name, value)
    delimiter = 'EOF_' + secrets.token_hex(16)
    return '%s<<%s\n%s\n%s\n' % (name, delimiter, value, delimiter)


def write_step_outputs(values: Mapping[str, str], environ: Mapping[str, str]) -> None:
    """Without GITHUB_OUTPUT (a local run) there is no step to hand the values to."""
    output_path = environ.get('GITHUB_OUTPUT')
    if not output_path:
        return
    with open(output_path, 'a', encoding='utf-8', newline='') as file:
        for name, value in values.items():
            file.write(format_step_output(name, value))


def _escape_workflow_command_text(text: str) -> str:
    return text.replace('%', '%25').replace('\r', '%0D').replace('\n', '%0A')


def _report_error(source: str, message: str) -> None:
    print('::error::%s: %s' % (source, _escape_workflow_command_text(message)))


def _token_from(environ: Mapping[str, str], name: str) -> Optional[str]:
    return environ.get(name) or None


def _read_one_source(source: str, out_file: str, read: Callable[[], Dict[str, Any]]) -> Dict[str, Any]:
    """One broken source must not stop the others: any exception becomes that source's error JSON, and the run goes on."""
    try:
        result = read()
    except Exception as error:  # noqa: BLE001
        result = {'status': 'error', 'error': '%s: %s' % (type(error).__name__, error)}
    _write_json(out_file, result)
    if result.get('status') == 'error':
        _report_error(source, str(result.get('error', 'unreadable')))
    return result


def _required_xcode_majors(required_xcode: Mapping[str, Any]) -> List[int]:
    majors: List[int] = []
    if required_xcode.get('status') != 'ok':
        return majors
    for band_result in (required_xcode.get('bands') or {}).values():
        if band_result.get('status') != 'ok':
            continue
        for platform in ('ios', 'maccatalyst'):
            xcode = (band_result.get(platform) or {}).get('xcode')
            if xcode:
                majors.append(major(xcode))
    return majors


def read_latest_versions(args: argparse.Namespace, environ: Mapping[str, str], services: Services) -> int:
    out = args.out
    downloader = services.make_downloader(os.path.join(out, 'downloads'))
    github = services.make_github_client(_token_from(environ, GITHUB_TOKEN_NAME), args.repo)
    pinned_holder: List[Any] = []

    def read_pinned() -> Dict[str, Any]:
        pinned = read_pinned_versions(args.repo_root)
        pinned_holder.append(pinned)
        return dict(pinned.to_json(), status='ok')

    def need_pinned_versions() -> Any:
        if not pinned_holder:
            raise UnreadableSourceError('the pinned versions could not be read')
        return pinned_holder[0]

    _read_one_source(SOURCE_PINNED_VERSIONS, os.path.join(out, 'pinned-versions.json'), read_pinned)
    _read_one_source(SOURCE_ANDROID, os.path.join(out, 'android.json'), lambda: read_android_index(downloader))
    required_xcode = _read_one_source(
        SOURCE_WORKLOAD_SETS, os.path.join(out, 'required-xcode.json'),
        lambda: read_required_xcode(downloader, need_pinned_versions()))

    required_majors = _required_xcode_majors(required_xcode)
    runner_images = _read_one_source(
        SOURCE_RUNNER_IMAGES_README, os.path.join(out, 'runner-images.json'),
        lambda: read_runner_images(downloader, _labels_the_repo_might_inspect(pinned_holder, required_majors)))
    _read_one_source(SOURCE_BRANCH_RULES, os.path.join(out, 'branch-rules.json'),
                     lambda: read_branch_rules(github, args.base))

    labels = _macos_labels_to_inspect(pinned_holder, runner_images, required_majors)
    _write_json(os.path.join(out, 'macos-labels-to-inspect.json'), {'status': 'ok', 'labels': labels})
    write_step_outputs({'macos-labels-to-inspect': json.dumps(labels)}, environ)
    return 0


def _labels_the_repo_might_inspect(pinned_holder: List[Any], required_majors: List[int]) -> List[str]:
    """The README is read once, so the rollout states are fetched for every label the repo could list."""
    if not pinned_holder:
        return []
    pinned = pinned_holder[0]
    labels = [pinned.apple_label.label] + [leg.runner for leg in pinned.ios_legs]
    labels.extend('xcode-%d' % number for number in sorted(set(required_majors)))
    return labels


def _macos_labels_to_inspect(pinned_holder: List[Any], runner_images: Mapping[str, Any],
                             required_majors: List[int]) -> List[str]:
    if not pinned_holder:
        return []
    rows: Optional[List[RunnerImageRow]] = None
    if runner_images.get('status') == 'ok':
        rows = [RunnerImageRow.from_json(row) for row in runner_images.get('rows') or []]
    return macos_labels_to_inspect(pinned_holder[0], rows, required_majors)


def inspect_macos_runner_image_command(args: argparse.Namespace, environ: Mapping[str, str], services: Services) -> int:
    listing = inspect_macos_runner_image(args.label)
    _write_json(args.out, listing)
    if listing.get('status') == 'error':
        _report_error('macos-runner-image:%s' % args.label, str(listing.get('error', 'unreadable')))
    return 0


def plan_changes(args: argparse.Namespace, environ: Mapping[str, str], services: Services) -> int:
    github = services.make_github_client(_token_from(environ, GITHUB_TOKEN_NAME), args.repo)
    bump_state = read_bump_pull_request_state(github, args.base, args.bot_branch)
    plan = build_plan(args.latest_versions, args.macos_runner_images, args.repo_root, bump_state, args.base,
                      args.base_sha, args.bot_branch, args.reopen_rejected, args.run_url, _now())
    outputs = write_plan_files(plan, args.repo_root, args.out)
    write_step_outputs(outputs, environ)
    return 0


def _read_plan(plan_folder: str) -> Dict[str, Any]:
    plan = _read_json_if_present(os.path.join(plan_folder, 'plan.json'))
    if plan is None:
        raise UnreadableSourceError('%s has no readable plan.json' % plan_folder)
    return plan


def decide_pull_request_update_command(args: argparse.Namespace, environ: Mapping[str, str], services: Services) -> int:
    plan = _read_plan(args.plan)
    github = services.make_github_client(_token_from(environ, GITHUB_TOKEN_NAME), args.repo)

    def make_maintainer_client(token: str) -> Any:
        return services.make_github_client(token, args.repo)

    token_status = check_token(_token_from(environ, MAINTAINER_TOKEN_NAME), make_maintainer_client, _now())
    decision = decide_pull_request_update(plan, token_status, github, args.base, args.base_sha, _now())
    _write_json(os.path.join(args.out, 'decision.json'), decision.to_json())
    write_step_outputs(step_outputs(decision, plan), environ)
    return 0


def approve_and_turn_on_auto_merge_command(args: argparse.Namespace, environ: Mapping[str, str], services: Services) -> int:
    plan = _read_plan(args.plan)
    decision = _read_json_if_present(args.decision)
    if decision is None:
        raise UnreadableSourceError('%s is missing or unreadable' % args.decision)
    github = services.make_github_client(_token_from(environ, GITHUB_TOKEN_NAME), args.repo)
    approval = approve_and_turn_on_auto_merge(
        github, plan, decision, args.pull_request, args.head_sha, args.operation, args.base, args.base_sha,
        args.bot_branch, run_url=str(plan.get('run_url', '')))
    _write_json(os.path.join(args.out, 'approval.json'), approval)
    return 0


def record_step_outcomes(args: argparse.Namespace, environ: Mapping[str, str], services: Services) -> int:
    outcomes = {name: environ.get('%s_OUTCOME' % name.upper(), '') for name in STEP_OUTCOME_NAMES}
    _write_json(os.path.join(args.out, 'step-outcomes.json'), outcomes)
    return 0


def _read_job_results(environ: Mapping[str, str]) -> Optional[Dict[str, Any]]:
    try:
        results = json.loads(environ.get('JOB_RESULTS') or '')
    except ValueError:
        return None
    return results if isinstance(results, dict) else None


def _read_patches(plan_folder: str, plan: Optional[Mapping[str, Any]]) -> Dict[str, str]:
    """Keyed by the file's path inside the plan folder, as the plan names each change's patch_file."""
    names = [PATCH_FILE_OF_UNOPENED_BUMP]
    names.extend(str(change.get('patch_file', '')) for change in (plan or {}).get('changes_for_a_person') or [])
    patches: Dict[str, str] = {}
    for name in names:
        if not name:
            continue
        try:
            with open(os.path.join(plan_folder, *name.split('/')), 'r', encoding='utf-8') as file:
                patches[name] = file.read()
        except OSError:
            continue
    return patches


def update_issue_for_a_person(args: argparse.Namespace, environ: Mapping[str, str], services: Services) -> int:
    plan = _read_json_if_present(os.path.join(args.plan, 'plan.json'))
    decision = _read_json_if_present(os.path.join(args.pull_request, 'decision.json'))
    approval = _read_json_if_present(os.path.join(args.pull_request, 'approval.json'))
    step_outcomes = _read_json_if_present(os.path.join(args.pull_request, 'step-outcomes.json'))
    github = services.make_github_client(_token_from(environ, GITHUB_TOKEN_NAME), args.repo)

    push_error: Optional[str] = None
    if (step_outcomes or {}).get('open_or_update') == 'failure':
        push_error = read_push_error_from_job_log(
            github, environ.get('GITHUB_RUN_ID', ''), environ.get('GITHUB_RUN_ATTEMPT', '1'),
            markers.PULL_REQUEST_JOB_NAME)

    items = collect_items(plan, decision, approval, step_outcomes, _read_job_results(environ), push_error,
                          _read_patches(args.plan, plan))
    result = sync_issue(github, items, args.run_url, _now())
    print('Issue %s%s.' % (result['action'], ' #%s' % result['number'] if result.get('number') else ''))
    return int(result['exit_code'])


_COMMANDS: Dict[str, Callable[[argparse.Namespace, Mapping[str, str], Services], int]] = {
    'read-latest-versions': read_latest_versions,
    'inspect-macos-runner-image': inspect_macos_runner_image_command,
    'plan-changes': plan_changes,
    'decide-pull-request-update': decide_pull_request_update_command,
    'approve-and-turn-on-auto-merge': approve_and_turn_on_auto_merge_command,
    'record-step-outcomes': record_step_outcomes,
    'update-issue-for-a-person': update_issue_for_a_person,
}


def main(argv: Sequence[str], environ: Optional[Mapping[str, str]] = None, services: Optional[Services] = None) -> int:
    environ = os.environ if environ is None else environ
    services = services if services is not None else Services()
    parser = _build_parser(environ)
    try:
        args = parser.parse_args(list(argv))
    except _ArgumentError as error:
        print('error: %s' % error, file=sys.stderr)
        return 2
    except SystemExit as stop:
        return int(stop.code or 0)
    try:
        return _COMMANDS[args.command](args, environ, services)
    except (UnreadableSourceError, GitHubApiError) as error:
        _report_error(args.command, str(error))
        return 1
