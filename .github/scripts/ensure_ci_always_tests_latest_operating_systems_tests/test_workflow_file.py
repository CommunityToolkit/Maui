from __future__ import annotations

import json
import os
import re
import unittest
from typing import Any, Dict, List, NamedTuple, Optional, Tuple

from ensure_ci_always_tests_latest_operating_systems import markers
from ensure_ci_always_tests_latest_operating_systems_tests import github_expressions

WORKFLOW_PATH = os.path.join(
    os.path.dirname(os.path.abspath(__file__)), "..", "..", "workflows", "ensure-ci-always-tests-latest-operating-systems.yml"
)
WORKFLOW_PATH = os.environ.get("WORKFLOW_FILE_UNDER_TEST", WORKFLOW_PATH)

TOKEN_NAME = "ENSURE_CI_ALWAYS_TESTS_LATEST_OPERATING_SYSTEMS_TOKEN"
MAUI = "CommunityToolkit/Maui"
MAUI_BOT_BRANCH = "automation/ensure-ci-always-tests-latest-operating-systems"
PINNED_ACTIONS = {
    "actions/checkout@3d3c42e5aac5ba805825da76410c181273ba90b1",
    "actions/upload-artifact@cf430e030ddbb5b0abf93d22962f4752f3646cd9",
    "actions/download-artifact@9000827ccba6bdab643e8b6fd33ac0654aef8333",
    "peter-evans/create-pull-request@3ffccacd46053766916f087ce41d627c0b8cf2d1",
}
JOB_ORDER = [
    "run-unit-tests",
    "read-latest-versions",
    "inspect-macos-runner-image",
    "plan-changes",
    "open-or-update-pull-request",
    "update-issue-for-a-person",
]


def read_workflow_lines() -> List[str]:
    with open(WORKFLOW_PATH, encoding="utf-8") as workflow_file:
        return workflow_file.read().split("\n")


def without_comment(line: str) -> str:
    """Drops a whole-line comment; the workflow has no trailing comment that holds an expression, only the SHA version notes."""
    return "" if line.lstrip().startswith("#") else line


def indentation_of(line: str) -> int:
    return len(line) - len(line.lstrip(" "))


def code_lines() -> List[Tuple[int, str]]:
    return [(number, line) for number, line in enumerate(read_workflow_lines(), start=1) if without_comment(line).strip()]


def run_values(lines: Optional[List[str]] = None) -> List[Tuple[int, str]]:
    """Reads every deeper line whatever the first line holds, so no block style hides an expression."""
    lines = read_workflow_lines() if lines is None else lines
    found: List[Tuple[int, str]] = []
    for index, line in enumerate(lines):
        match = re.match(r"^(\s*)(?:-\s+)?run:\s*(.*)$", line)
        if match is None or line.lstrip().startswith("#"):
            continue
        key_indent = indentation_of(line) + (4 if line.lstrip().startswith("-") else 0)
        text = match.group(2)
        following = index + 1
        block: List[str] = []
        while following < len(lines) and (lines[following].strip() == "" or indentation_of(lines[following]) > key_indent):
            block.append(lines[following])
            following += 1
        first_line_is_only_a_block_indicator = re.fullmatch(r"[|>][+-]?[1-9]?[+-]?", text) is not None
        text = "\n".join(block if first_line_is_only_a_block_indicator else [text] + block)
        found.append((index + 1, text))
    return found


def read_jobs() -> Dict[str, Dict[str, Any]]:
    lines = read_workflow_lines()
    jobs_start = lines.index("jobs:")
    jobs: Dict[str, Dict[str, Any]] = {}
    current: Optional[str] = None
    for index in range(jobs_start + 1, len(lines)):
        line = lines[index]
        if not line.strip() or line.lstrip().startswith("#"):
            continue
        indent = indentation_of(line)
        if indent == 4 and line.rstrip().endswith(":"):
            current = line.strip()[:-1]
            jobs[current] = {"first_line": index + 1, "last_line": index + 1}
            continue
        if current is None:
            continue
        jobs[current]["last_line"] = index + 1
        if indent == 8:
            field = re.match(r"^(\S[^:]*):\s*(.*)$", line.strip())
            if field is not None:
                jobs[current].setdefault(field.group(1), field.group(2))
    return jobs


def read_bot_branch_expression() -> str:
    for line in read_workflow_lines():
        if line.startswith("    BOT_BRANCH:"):
            return line.split(":", 1)[1].strip()
    raise AssertionError("BOT_BRANCH is not set under the top-level env")


def step_blocks() -> List[Tuple[int, List[str]]]:
    lines = read_workflow_lines()
    blocks: List[Tuple[int, List[str]]] = []
    current: Optional[List[str]] = None
    start = 0
    for index, line in enumerate(lines):
        starts_step = re.match(r"^            -   ", line) is not None
        ends_step = current is not None and line.strip() and not line.lstrip().startswith("#") and indentation_of(line) <= 8
        if starts_step or ends_step:
            if current is not None:
                blocks.append((start, current))
                current = None
            if starts_step:
                current = [line]
                start = index + 1
                continue
        if current is not None:
            current.append(line)
    if current is not None:
        blocks.append((start, current))
    return blocks


def step_with_id(step_id: str) -> List[str]:
    for _, block in step_blocks():
        if any(re.match(r"^\s+id:\s*%s\s*$" % re.escape(step_id), line) for line in block):
            return block
    raise AssertionError("no step with id %s" % step_id)


def contexts_for(event_name: str, repository: str, ref: str, inputs: Dict[str, Any], needs: Dict[str, Any], status: Dict[str, Any]) -> Dict[str, Any]:
    ref_name = ref.split("/", 2)[2]
    return {
        "github": {"event_name": event_name, "repository": repository, "ref": ref, "ref_name": ref_name},
        "inputs": inputs,
        "needs": needs,
        "job_status_functions": status,
    }


def if_expression_of(job: Dict[str, Any]) -> str:
    return job["if"]


def simulate_run(event_name: str, repository: str, ref: str, dry_run: Optional[bool]) -> Dict[str, bool]:
    """Decides which jobs run: a job's `if` must hold and, unless it uses a status function, every job it needs must have succeeded."""
    jobs = read_jobs()
    inputs: Dict[str, Any] = {} if dry_run is None else {"dry-run": dry_run, "reopen-rejected": False}
    results: Dict[str, str] = {}
    ran: Dict[str, bool] = {}
    macos_labels_output = '["xcode-27","macos-26","macos-15"]'
    for name in JOB_ORDER:
        needs_text = jobs[name].get("needs", "")
        needed = [item for item in re.findall(r"[a-z][a-z-]*", needs_text)]
        needs_context = {
            needed_name: {"result": results.get(needed_name, "skipped"), "outputs": {"macos-labels-to-inspect": macos_labels_output}}
            for needed_name in needed
        }
        needs_results = [needs_context[needed_name]["result"] for needed_name in needed]
        status = github_expressions.status_functions(needs_results=needs_results)
        contexts = contexts_for(event_name, repository, ref, inputs, needs_context, status)
        condition = if_expression_of(jobs[name]) if "if" in jobs[name] else "${{ success() }}"
        uses_status_function = re.search(r"\b(always|cancelled|failure|success)\(", condition) is not None
        condition_holds = github_expressions.is_truthy(github_expressions.evaluate_template(condition, contexts))
        implicit_success_holds = uses_status_function or all(result == "success" for result in needs_results)
        ran[name] = condition_holds and implicit_success_holds
        results[name] = "success" if ran[name] else "skipped"
    return ran


def bot_branch_for(event_name: str, repository: str, ref: str) -> str:
    contexts = contexts_for(event_name, repository, ref, {}, {}, github_expressions.status_functions())
    return github_expressions.evaluate_template(read_bot_branch_expression(), contexts)


class WorkflowStaticChecks(unittest.TestCase):
    def test_workflow_reads_no_event_payload_outside_comments(self) -> None:
        offending = [(number, line.strip()) for number, line in code_lines() if "github.event." in line]
        self.assertEqual([], offending, "a schedule has no payload: use github.event_name, github.ref and github.repository")

    def test_workflow_never_reads_event_repository_or_event_inputs_even_in_comments(self) -> None:
        text = "\n".join(read_workflow_lines())
        self.assertNotIn("github.event.repository", text)
        self.assertNotIn("github.event.inputs", text)

    def test_no_expression_inside_any_run_value(self) -> None:
        values = run_values()
        self.assertGreaterEqual(len(values), 6, "the reader found too few run values to be trusted")
        offending = [(number, text) for number, text in values if "${{" in text]
        self.assertEqual([], offending)

    def test_run_value_reader_sees_inline_folded_and_literal_blocks(self) -> None:
        by_command = [text for _, text in run_values()]
        self.assertTrue(any(text.startswith("sudo locale-gen") for text in by_command))
        self.assertTrue(any("run_unit_tests -v" in text for text in by_command))
        self.assertTrue(any("decide-pull-request-update" in text and "--out" in text for text in by_command))

    def test_token_appears_only_in_decide_env_and_open_or_update_token(self) -> None:
        decide_block = "\n".join(step_with_id("decide"))
        open_block = "\n".join(step_with_id("open-or-update"))
        mentions = [(number, line.strip()) for number, line in code_lines() if TOKEN_NAME in line]
        in_decide = [line for line in decide_block.split("\n") if TOKEN_NAME in line and not line.lstrip().startswith("#")]
        in_open = [line for line in open_block.split("\n") if TOKEN_NAME in line and not line.lstrip().startswith("#")]
        self.assertEqual(len(mentions), len(in_decide) + len(in_open), "the token appears outside decide and open-or-update: %s" % mentions)
        self.assertEqual(1, len(in_decide))
        self.assertRegex(in_decide[0], r"^\s+%s: \$\{\{ secrets\.%s \}\}$" % (TOKEN_NAME, TOKEN_NAME))
        env_line_index = [line.strip() for line in decide_block.split("\n")].index("env:")
        self.assertLess(env_line_index, decide_block.split("\n").index(in_decide[0]), "the token must sit under env:")
        self.assertEqual(1, len(in_open))
        self.assertRegex(in_open[0], r"^\s+token: \$\{\{ secrets\.%s \}\}$" % TOKEN_NAME)

    def test_run_value_reader_follows_every_block_style_and_plain_multi_line_values(self) -> None:
        samples = ["run: |+\n    echo ${{ github.ref_name }}\n", "run: >+\n    echo ${{ github.ref_name }}\n",
                   "run: |2\n      echo ${{ github.ref_name }}\n", "run: echo hi\n    ${{ github.ref_name }}\n"]
        for sample in samples:
            with self.subTest(sample=sample):
                lines = ("jobs:\n    a:\n        steps:\n            -   name: x\n                "
                         + sample.replace("\n    ", "\n                    ")).split("\n")
                values = run_values(lines)
                self.assertEqual(1, len(values))
                self.assertIn("${{", values[0][1])

    def test_only_the_two_allowed_lines_mention_secrets(self) -> None:
        allowed = [r"^\s+%s: \$\{\{ secrets\.%s \}\}$" % (TOKEN_NAME, TOKEN_NAME), r"^\s+token: \$\{\{ secrets\.%s \}\}$" % TOKEN_NAME]
        mentions = [(number, line) for number, line in code_lines() if "secrets" in line.lower()]
        self.assertEqual(2, len(mentions), mentions)
        for number, line in mentions:
            self.assertTrue(any(re.match(pattern, line) for pattern in allowed), "line %d: %s" % (number, line.strip()))

    def test_token_is_never_inside_a_run_value(self) -> None:
        self.assertEqual([], [number for number, text in run_values() if TOKEN_NAME in text])

    def test_every_uses_is_one_of_the_four_pinned_actions(self) -> None:
        used = set()
        for number, line in code_lines():
            match = re.match(r"^\s*(?:-\s+)?uses:\s*(\S+)", line)
            if match is not None:
                used.add(match.group(1))
        self.assertTrue(used, "no uses: lines found")
        self.assertEqual(set(), used - PINNED_ACTIONS)
        self.assertEqual(PINNED_ACTIONS, used)

    def test_top_level_permissions_are_empty(self) -> None:
        self.assertIn("permissions: {}", read_workflow_lines())

    def test_every_job_declares_its_own_permissions_and_timeout(self) -> None:
        for name, job in read_jobs().items():
            with self.subTest(job=name):
                self.assertIn("permissions", job)
                self.assertIn("timeout-minutes", job)

    def read_permissions(self, job_name: str) -> Dict[str, str]:
        job = read_jobs()[job_name]
        lines = read_workflow_lines()
        permissions: Dict[str, str] = {}
        inside = False
        for line in lines[job["first_line"]:job["last_line"]]:
            if not line.strip() or line.lstrip().startswith("#"):
                continue
            if indentation_of(line) == 8:
                inside = line.strip() == "permissions:"
                continue
            if inside:
                key, value = line.strip().split(":", 1)
                permissions[key] = value.strip()
        return permissions

    def test_each_job_holds_exactly_the_permissions_of_the_design(self) -> None:
        expected = {
            "run-unit-tests": {"contents": "read"},
            "read-latest-versions": {"contents": "read"},
            "inspect-macos-runner-image": {"contents": "read"},
            "plan-changes": {"contents": "read", "pull-requests": "read", "checks": "read", "statuses": "read"},
            "open-or-update-pull-request": {"contents": "write", "pull-requests": "write", "checks": "read", "statuses": "read",
                                            "issues": "read"},
            "update-issue-for-a-person": {"contents": "read", "issues": "write", "actions": "read"},
        }
        for name, permissions in expected.items():
            with self.subTest(job=name):
                self.assertEqual(permissions, self.read_permissions(name))

    def test_only_the_publish_job_may_write_contents_or_pull_requests_and_only_the_issue_job_may_write_issues(self) -> None:
        for name in JOB_ORDER:
            permissions = self.read_permissions(name)
            writes = {key for key, value in permissions.items() if value == "write"}
            allowed = {"open-or-update-pull-request": {"contents", "pull-requests"}, "update-issue-for-a-person": {"issues"}}.get(name, set())
            self.assertEqual(set(), writes - allowed, name)

    def test_the_pull_request_labels_are_the_labels_the_issue_and_the_bot_look_for(self) -> None:
        from ensure_ci_always_tests_latest_operating_systems import markers

        block = step_with_id("open-or-update")
        start = next(index for index, line in enumerate(block) if line.strip() == "labels: |")
        labels = []
        for line in block[start + 1:]:
            if indentation_of(line) <= indentation_of(block[start]):
                break
            labels.append(line.strip())
        self.assertEqual(list(markers.LABELS), labels)

    def test_environment_is_declared_only_on_the_open_or_update_job(self) -> None:
        with_environment = [name for name, job in read_jobs().items() if "environment" in job]
        self.assertEqual(["open-or-update-pull-request"], with_environment)
        self.assertEqual("ensure-ci-always-tests-latest-operating-systems", read_jobs()["open-or-update-pull-request"]["environment"])
        self.assertEqual(1, len([line for _, line in code_lines() if re.match(r"^\s*environment:", line)]))

    def test_workflow_never_merges_with_the_command_line_or_admin_rights(self) -> None:
        text = "\n".join(line for _, line in code_lines())
        self.assertNotIn("gh pr merge", text)
        self.assertNotIn("--admin", text)

    def test_every_checkout_keeps_no_credentials(self) -> None:
        checkouts = [block for _, block in step_blocks() if any("actions/checkout@" in line for line in block)]
        self.assertGreaterEqual(len(checkouts), 5)
        for block in checkouts:
            self.assertIn("persist-credentials: false", "\n".join(block))

    def test_every_artifact_upload_keeps_fourteen_days(self) -> None:
        uploads = [block for _, block in step_blocks() if any("actions/upload-artifact@" in line for line in block)]
        self.assertGreaterEqual(len(uploads), 4)
        for block in uploads:
            self.assertIn("retention-days: 14", "\n".join(block))

    def test_job_names_follow_the_design_and_the_issue_can_find_the_pull_request_job(self) -> None:
        self.assertEqual(JOB_ORDER, list(read_jobs()))
        from ensure_ci_always_tests_latest_operating_systems import markers

        pull_request_job_name = read_jobs()["open-or-update-pull-request"]["name"]
        self.assertEqual(markers.PULL_REQUEST_JOB_NAME, pull_request_job_name)

    def test_the_job_and_step_names_the_issue_quotes_are_the_ones_in_the_workflow(self) -> None:
        from ensure_ci_always_tests_latest_operating_systems import issue_for_a_person

        jobs = read_jobs()
        for job_id, name in issue_for_a_person.JOB_NAMES.items():
            with self.subTest(job=job_id):
                self.assertEqual(name, jobs[job_id]["name"].replace(" ${{ matrix.label }}", ""))
        for outcome_name, name in issue_for_a_person.STEP_NAMES.items():
            with self.subTest(step=outcome_name):
                block = step_with_id(outcome_name.replace("_", "-"))
                self.assertEqual("            -   name: " + name, block[0].rstrip())

    def test_workflow_name_is_the_display_name_of_the_actions_tab(self) -> None:
        self.assertEqual("name: Ensure CI Always Tests Latest Operating Systems", read_workflow_lines()[0])


class PublishStepConditions(unittest.TestCase):
    """The PAT is used only by create-pull-request, so its `if:` and the steps around it carry the safety argument of DESIGN 3.2."""

    def step_condition_holds(self, step_id: str, decide_open_or_update: Optional[str], created_outcome: str = "success",
                             operation: str = "created", number: str = "12", earlier_steps: str = "success", cancelled: bool = False) -> bool:
        condition = next(line.split("if:", 1)[1] for line in step_with_id(step_id) if line.strip().startswith("if:"))
        decide_outputs = {} if decide_open_or_update is None else {"open-or-update": decide_open_or_update}
        contexts = {"steps": {
            "decide": {"outputs": decide_outputs, "outcome": "success"},
            "open-or-update": {"outcome": created_outcome, "outputs": {"pull-request-number": number, "pull-request-operation": operation}},
        }, "job_status_functions": github_expressions.status_functions(cancelled=cancelled, needs_results=[earlier_steps])}
        holds = github_expressions.is_truthy(github_expressions.evaluate_template(condition.strip(), contexts))
        names_a_status_function = re.search(r"\b(always|cancelled|failure|success)\(", condition) is not None
        # GitHub adds success() to a step condition that names no status function.
        implicitly_successful = names_a_status_function or (earlier_steps == "success" and not cancelled)
        return holds and implicitly_successful

    def test_apply_and_open_or_update_run_only_when_decide_says_true(self) -> None:
        for step_id in ("apply", "open-or-update"):
            for decided, runs in (("true", True), ("false", False), ("", False), (None, False)):
                with self.subTest(step=step_id, decided=decided):
                    self.assertEqual(runs, self.step_condition_holds(step_id, decided))

    def test_apply_and_open_or_update_never_run_after_a_failed_step_or_a_cancelled_run(self) -> None:
        for step_id in ("apply", "open-or-update"):
            for earlier_steps, cancelled in (("failure", False), ("success", True), ("failure", True)):
                with self.subTest(step=step_id, earlier_steps=earlier_steps, cancelled=cancelled):
                    self.assertFalse(self.step_condition_holds(step_id, "true", earlier_steps=earlier_steps, cancelled=cancelled))

    def test_the_workflow_starts_only_on_schedule_dispatch_and_pull_request(self) -> None:
        lines = read_workflow_lines()
        events_in_on = [line.strip()[:-1] for line in lines[lines.index("on:") + 1:lines.index("jobs:")]
                        if re.match(r"^    [a-z_]+:\s*$", line)]
        self.assertEqual({"schedule", "workflow_dispatch", "pull_request"}, set(events_in_on))

    def test_create_pull_request_may_stage_only_the_three_pin_files(self) -> None:
        block = step_with_id("open-or-update")
        start = next(index for index, line in enumerate(block) if line.strip() == "add-paths: |")
        paths = set()
        for line in block[start + 1:]:
            if not line.strip() or indentation_of(line) <= indentation_of(block[start]):
                break
            paths.add(line.strip())
        self.assertEqual(set(markers.PIN_FILES), paths)

    def test_approve_runs_only_for_a_pull_request_that_was_created_updated_or_left_alone(self) -> None:
        for operation in ("created", "updated", "none"):
            with self.subTest(operation=operation):
                self.assertTrue(self.step_condition_holds("approve", "true", operation=operation))
        for outcome, operation, number in (("success", "closed", "12"), ("failure", "created", "12"), ("skipped", "created", "12"),
                                           ("success", "created", ""), ("cancelled", "updated", "12")):
            with self.subTest(outcome=outcome, operation=operation, number=number):
                self.assertFalse(self.step_condition_holds("approve", "true", outcome, operation, number))


class ExpressionEvaluatorChecks(unittest.TestCase):
    def evaluate(self, expression: str, **contexts: Any) -> Any:
        contexts.setdefault("job_status_functions", github_expressions.status_functions())
        return github_expressions.evaluate(expression, contexts)

    def test_and_or_return_an_operand_like_github(self) -> None:
        self.assertEqual("b", self.evaluate("'' || 'b'"))
        self.assertEqual("", self.evaluate("'' && 'b'"))
        self.assertEqual("b", self.evaluate("'a' && 'b'"))
        self.assertEqual("a", self.evaluate("'a' || 'b'"))

    def test_falsy_values_are_false_zero_empty_string_and_null(self) -> None:
        for expression in ("false", "0", "''", "null"):
            self.assertFalse(github_expressions.is_truthy(self.evaluate(expression)), expression)
        self.assertTrue(github_expressions.is_truthy(self.evaluate("'false'")))

    def test_string_equality_ignores_case(self) -> None:
        self.assertTrue(self.evaluate("'CommunityToolkit/Maui' == 'communitytoolkit/maui'"))

    def test_turkish_dotted_and_dotless_i_do_not_change_equality(self) -> None:
        self.assertTrue(self.evaluate("'TITLE' == 'title'"))
        self.assertFalse(self.evaluate("'TITLE' == 't\u0131tle'"))

    def test_missing_property_is_null_and_null_equals_empty_string(self) -> None:
        self.assertIsNone(self.evaluate("inputs.dry-run", inputs={}))
        self.assertTrue(self.evaluate("inputs.dry-run == ''", inputs={}))
        self.assertTrue(self.evaluate("!inputs.dry-run", inputs={}))

    def test_format_replaces_numbered_placeholders(self) -> None:
        self.assertEqual("main-x", self.evaluate("format('{0}-x', github.ref_name)", github={"ref_name": "main"}))

    def test_contains_from_json_and_status_functions(self) -> None:
        self.assertTrue(self.evaluate("contains(fromJSON('[\"a\",\"b\"]'), 'b')"))
        self.assertTrue(self.evaluate("always()"))
        self.assertFalse(self.evaluate("cancelled()"))

    def test_unsupported_constructs_raise(self) -> None:
        for expression in ("1 < 2", "github['ref']", "startsWith('a', 'a')", "hashFiles('x')", "github.*"):
            with self.subTest(expression=expression), self.assertRaises(github_expressions.UnsupportedExpressionError):
                self.evaluate(expression, github={"ref": "x"})

    def test_template_with_text_and_expressions_becomes_a_string(self) -> None:
        contexts = {"github": {"server_url": "https://github.com", "repository": "a/b", "run_id": 7}, "job_status_functions": {}}
        self.assertEqual(
            "https://github.com/a/b/actions/runs/7",
            github_expressions.evaluate_template("${{ github.server_url }}/${{ github.repository }}/actions/runs/${{ github.run_id }}", contexts),
        )


class Row(NamedTuple):
    event: str
    repository: str
    ref: str
    dry_run: Optional[bool]
    unit_tests_run: bool
    reading_and_planning_run: bool
    publish_runs: bool
    issue_runs: bool
    bot_branch: Optional[str]


ROWS = [
    Row("schedule", MAUI, "refs/heads/main", None, True, True, True, True, MAUI_BOT_BRANCH),
    Row("workflow_dispatch", MAUI, "refs/heads/main", False, True, True, True, True, MAUI_BOT_BRANCH),
    Row("workflow_dispatch", MAUI, "refs/heads/main", True, True, True, False, False, MAUI_BOT_BRANCH),
    Row("workflow_dispatch", MAUI, "refs/heads/some-branch", False, True, True, False, False, "some-branch-ensure-ci-always-tests-latest-operating-systems"),
    Row("workflow_dispatch", MAUI, "refs/heads/some-branch", True, True, True, False, False, "some-branch-ensure-ci-always-tests-latest-operating-systems"),
    Row("workflow_dispatch", "TheCodeTraveler/CTM", "refs/heads/e2e", False, True, True, True, True, "e2e-ensure-ci-always-tests-latest-operating-systems"),
    Row("workflow_dispatch", "TheCodeTraveler/CTM", "refs/heads/e2e", True, True, True, False, False, "e2e-ensure-ci-always-tests-latest-operating-systems"),
    Row("workflow_dispatch", "TheCodeTraveler/CTM", "refs/heads/main", False, True, True, True, True, "main-ensure-ci-always-tests-latest-operating-systems"),
    Row("schedule", "TheCodeTraveler/CTM", "refs/heads/main", None, False, False, False, False, None),
    Row("pull_request", MAUI, "refs/pull/1/merge", None, True, False, False, False, None),
    Row("pull_request", "TheCodeTraveler/CTM", "refs/pull/1/merge", None, True, False, False, False, None),
]


class ConditionTable(unittest.TestCase):
    """Every row of DESIGN 3.3; dry-run rows follow 3.5."""

    def test_each_row_runs_the_expected_jobs_and_names_the_expected_branch(self) -> None:
        for event, repository, ref, dry_run, unit_tests, reading_and_planning, publish, issue, branch in ROWS:
            label = "%s %s %s dry_run=%s" % (event, repository, ref, dry_run)
            with self.subTest(row=label):
                ran = simulate_run(event, repository, ref, dry_run)
                self.assertEqual(unit_tests, ran["run-unit-tests"], "run-unit-tests")
                for name in ("read-latest-versions", "inspect-macos-runner-image", "plan-changes"):
                    self.assertEqual(reading_and_planning, ran[name], name)
                self.assertEqual(publish, ran["open-or-update-pull-request"], "open-or-update-pull-request")
                self.assertEqual(issue, ran["update-issue-for-a-person"], "update-issue-for-a-person")
                if branch is not None:
                    self.assertEqual(branch, bot_branch_for(event, repository, ref))

    def test_a_scheduled_run_with_no_inputs_counts_as_not_a_dry_run(self) -> None:
        ran = simulate_run("schedule", MAUI, "refs/heads/main", None)
        self.assertTrue(ran["open-or-update-pull-request"])

    def test_issue_job_runs_after_a_failed_planning_job(self) -> None:
        jobs = read_jobs()
        needs = {name: {"result": "success", "outputs": {}} for name in JOB_ORDER[:-1]}
        needs["plan-changes"]["result"] = "failure"
        status = github_expressions.status_functions(needs_results=[item["result"] for item in needs.values()])
        contexts = contexts_for("schedule", MAUI, "refs/heads/main", {}, needs, status)
        self.assertTrue(github_expressions.is_truthy(github_expressions.evaluate_template(jobs["update-issue-for-a-person"]["if"], contexts)))
        self.assertFalse(github_expressions.is_truthy(github_expressions.evaluate_template(jobs["open-or-update-pull-request"]["if"], {
            **contexts, "needs": {"plan-changes": {"result": "failure"}}})))

    def test_inspect_job_is_skipped_when_no_label_is_listed(self) -> None:
        jobs = read_jobs()
        for listed in ("", "[]"):
            with self.subTest(listed=listed):
                needs = {"read-latest-versions": {"result": "success", "outputs": {"macos-labels-to-inspect": listed}}}
                contexts = contexts_for("schedule", MAUI, "refs/heads/main", {}, needs, github_expressions.status_functions(needs_results=["success"]))
                self.assertFalse(github_expressions.is_truthy(github_expressions.evaluate_template(jobs["inspect-macos-runner-image"]["if"], contexts)))

    def test_plan_job_still_runs_when_a_runner_list_job_failed(self) -> None:
        jobs = read_jobs()
        needs = {"read-latest-versions": {"result": "success"}, "inspect-macos-runner-image": {"result": "failure"}}
        contexts = contexts_for("schedule", MAUI, "refs/heads/main", {}, needs, github_expressions.status_functions(needs_results=["success", "failure"]))
        self.assertTrue(github_expressions.is_truthy(github_expressions.evaluate_template(jobs["plan-changes"]["if"], contexts)))

    def test_plan_job_does_not_run_when_reading_failed(self) -> None:
        jobs = read_jobs()
        needs = {"read-latest-versions": {"result": "failure"}, "inspect-macos-runner-image": {"result": "skipped"}}
        contexts = contexts_for("schedule", MAUI, "refs/heads/main", {}, needs, github_expressions.status_functions(needs_results=["failure", "skipped"]))
        self.assertFalse(github_expressions.is_truthy(github_expressions.evaluate_template(jobs["plan-changes"]["if"], contexts)))

    def test_matrix_labels_come_from_the_json_output(self) -> None:
        contexts = {"needs": {"read-latest-versions": {"outputs": {"macos-labels-to-inspect": json.dumps(["xcode-27", "macos-26"])}}}}
        self.assertEqual(
            ["xcode-27", "macos-26"],
            github_expressions.evaluate("fromJSON(needs.read-latest-versions.outputs.macos-labels-to-inspect)", contexts),
        )


if __name__ == "__main__":
    unittest.main()
