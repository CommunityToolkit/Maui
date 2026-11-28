"""Reads which checks the base branch requires, so a version change that would rename one is routed to a person."""
from __future__ import annotations

import re
import urllib.parse
from typing import Any, Dict, Iterable, List, Pattern, Tuple

from .data_types import SafeChange
from .errors import GitHubApiError


def read_branch_rules(github: Any, base: str) -> Dict[str, Any]:
    path = '/repos/%s/rules/branches/%s' % (github.repo, urllib.parse.quote(base, safe=''))
    try:
        rules = github.paginate(path)
    except GitHubApiError as error:
        return {'status': 'error', 'error': str(error), 'base': base, 'required_checks': [],
                'require_last_push_approval': False, 'required_approving_review_count': 0}
    required_checks: List[str] = []
    require_last_push_approval = False
    required_approving_review_count = 0
    for rule in rules:
        parameters = rule.get('parameters') or {}
        if rule.get('type') == 'required_status_checks':
            for check in parameters.get('required_status_checks') or []:
                context = check.get('context')
                if context and context not in required_checks:
                    required_checks.append(context)
        elif rule.get('type') == 'pull_request':
            require_last_push_approval = require_last_push_approval or bool(parameters.get('require_last_push_approval'))
            required_approving_review_count = max(required_approving_review_count,
                                                  int(parameters.get('required_approving_review_count') or 0))
    return {'status': 'ok', 'base': base, 'required_checks': required_checks,
            'require_last_push_approval': require_last_push_approval,
            'required_approving_review_count': required_approving_review_count}


def _compile_standing_alone_pattern(value: str) -> Pattern[str]:
    return re.compile(r'(?<![\w.])' + re.escape(value) + r'(?![\w.])', re.ASCII)


def is_value_standing_alone_in(text: str, value: str) -> bool:
    return bool(value) and _compile_standing_alone_pattern(value).search(text) is not None


def renamed_required_checks(change: SafeChange, required_checks: Iterable[str]) -> List[Tuple[str, str]]:
    """The old value must stand alone, so 27.2 never matches inside 27.20 or 127.2."""
    if not change.old or not change.new or change.old == change.new:
        return []
    standing_alone = _compile_standing_alone_pattern(change.old)
    renamed: List[Tuple[str, str]] = []
    for context in required_checks:
        if standing_alone.search(context) is None:
            continue
        pair = (context, standing_alone.sub(lambda _: change.new, context))
        if pair not in renamed:
            renamed.append(pair)
    return renamed
