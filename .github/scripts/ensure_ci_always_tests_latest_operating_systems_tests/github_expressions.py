"""Evaluates the subset of GitHub Actions expressions that the Ensure CI Always Tests Latest Operating Systems workflow uses."""
from __future__ import annotations

import json
import math
import re
from typing import Any, Callable, Dict, List, Optional, Tuple

_TOKEN_PATTERN = re.compile(
    r"""\s*(?:
        (?P<string>'(?:[^']|'')*')
        |(?P<number>\d+(?:\.\d+)?)
        |(?P<name>[A-Za-z_][A-Za-z0-9_-]*)
        |(?P<operator>&&|\|\||==|!=|!|\(|\)|,|\.)
    )""",
    re.VERBOSE,
)

_EXPRESSION_IN_TEMPLATE = re.compile(r"\$\{\{(.*?)\}\}", re.DOTALL)

SUPPORTED_FUNCTIONS = ("format", "contains", "fromJSON", "toJSON", "always", "cancelled", "failure", "success")


class UnsupportedExpressionError(Exception):
    """Raised for anything outside the subset, so a new construct in the workflow can't be silently judged wrong."""


def is_truthy(value: Any) -> bool:
    if value is None or value is False or value == "":
        return False
    if isinstance(value, (int, float)) and not isinstance(value, bool):
        return value != 0 and not math.isnan(value)
    return True


def _as_number(value: Any) -> float:
    if value is None:
        return 0.0
    if isinstance(value, bool):
        return 1.0 if value else 0.0
    if isinstance(value, (int, float)):
        return float(value)
    if isinstance(value, str):
        if value.strip() == "":
            return 0.0
        try:
            return float(value)
        except ValueError:
            return math.nan
    return math.nan


def loosely_equal(left: Any, right: Any) -> bool:
    if isinstance(left, str) and isinstance(right, str):
        # GitHub compares strings without regard to case; ASCII-only fold keeps this independent of the locale.
        return left.encode("utf-8").lower() == right.encode("utf-8").lower()
    if left is None and right is None:
        return True
    if isinstance(left, (dict, list)) or isinstance(right, (dict, list)):
        return left is right
    return _as_number(left) == _as_number(right)


def _look_up(container: Any, name: str) -> Any:
    if isinstance(container, dict):
        if name in container:
            return container[name]
        for key, value in container.items():
            if isinstance(key, str) and key.encode("utf-8").lower() == name.encode("utf-8").lower():
                return value
    return None


def _stringify(value: Any) -> str:
    if value is None:
        return ""
    if value is True:
        return "true"
    if value is False:
        return "false"
    if isinstance(value, float) and value.is_integer():
        return str(int(value))
    return str(value)


def _tokenize(text: str) -> List[Tuple[str, str]]:
    tokens: List[Tuple[str, str]] = []
    position = 0
    while position < len(text):
        if text[position:].strip() == "":
            break
        match = _TOKEN_PATTERN.match(text, position)
        if match is None:
            raise UnsupportedExpressionError("cannot read %r at offset %d of %r" % (text[position:position + 10], position, text))
        kind = match.lastgroup or ""
        tokens.append((kind, match.group(kind)))
        position = match.end()
    return tokens


class _Parser:
    def __init__(self, tokens: List[Tuple[str, str]], contexts: Dict[str, Any], source: str) -> None:
        self.tokens = tokens
        self.index = 0
        self.contexts = contexts
        self.source = source

    def peek(self) -> Optional[Tuple[str, str]]:
        return self.tokens[self.index] if self.index < len(self.tokens) else None

    def take(self) -> Tuple[str, str]:
        token = self.peek()
        if token is None:
            raise UnsupportedExpressionError("unexpected end of %r" % self.source)
        self.index += 1
        return token

    def expect(self, text: str) -> None:
        token = self.take()
        if token[1] != text:
            raise UnsupportedExpressionError("expected %r but found %r in %r" % (text, token[1], self.source))

    def at_operator(self, text: str) -> bool:
        token = self.peek()
        return token is not None and token[0] == "operator" and token[1] == text

    def parse_whole(self) -> Any:
        value = self.parse_or()
        if self.peek() is not None:
            raise UnsupportedExpressionError("unexpected %r in %r" % (self.peek(), self.source))
        return value

    def parse_or(self) -> Any:
        value = self.parse_and()
        while self.at_operator("||"):
            self.take()
            right = self.parse_and()
            value = value if is_truthy(value) else right
        return value

    def parse_and(self) -> Any:
        value = self.parse_equality()
        while self.at_operator("&&"):
            self.take()
            right = self.parse_equality()
            value = right if is_truthy(value) else value
        return value

    def parse_equality(self) -> Any:
        value = self.parse_not()
        while self.at_operator("==") or self.at_operator("!="):
            operator = self.take()[1]
            right = self.parse_not()
            equal = loosely_equal(value, right)
            value = equal if operator == "==" else not equal
        return value

    def parse_not(self) -> Any:
        if self.at_operator("!"):
            self.take()
            return not is_truthy(self.parse_not())
        return self.parse_primary()

    def parse_primary(self) -> Any:
        kind, text = self.take()
        if kind == "string":
            return text[1:-1].replace("''", "'")
        if kind == "number":
            return float(text) if "." in text else int(text)
        if kind == "operator" and text == "(":
            value = self.parse_or()
            self.expect(")")
            return value
        if kind == "name":
            if text == "true":
                return True
            if text == "false":
                return False
            if text == "null":
                return None
            if self.at_operator("("):
                return self.parse_call(text)
            return self.parse_property_chain(text)
        raise UnsupportedExpressionError("unexpected %r in %r" % (text, self.source))

    def parse_property_chain(self, first_name: str) -> Any:
        value = _look_up(self.contexts, first_name)
        if first_name.encode("utf-8").lower() not in (b"github", b"inputs", b"needs", b"env", b"steps", b"matrix", b"runner", b"secrets", b"job"):
            raise UnsupportedExpressionError("unknown context %r in %r" % (first_name, self.source))
        while self.at_operator("."):
            self.take()
            kind, name = self.take()
            if kind != "name":
                raise UnsupportedExpressionError("bad property after '.' in %r" % self.source)
            value = _look_up(value, name)
        return value

    def parse_call(self, name: str) -> Any:
        self.expect("(")
        arguments: List[Any] = []
        if not self.at_operator(")"):
            arguments.append(self.parse_or())
            while self.at_operator(","):
                self.take()
                arguments.append(self.parse_or())
        self.expect(")")
        return self.call_function(name, arguments)

    def call_function(self, name: str, arguments: List[Any]) -> Any:
        if name not in SUPPORTED_FUNCTIONS:
            raise UnsupportedExpressionError("function %r is not supported (in %r)" % (name, self.source))
        job_status = self.contexts.get("job_status_functions", {})
        if name == "format":
            template = _stringify(arguments[0])
            return re.sub(r"\{(\d+)\}", lambda match: _stringify(arguments[int(match.group(1)) + 1]), template)
        if name == "contains":
            haystack, needle = arguments
            if isinstance(haystack, list):
                return any(loosely_equal(item, needle) for item in haystack)
            return _stringify(needle).encode("utf-8").lower() in _stringify(haystack).encode("utf-8").lower()
        if name == "fromJSON":
            return json.loads(_stringify(arguments[0]))
        if name == "toJSON":
            return json.dumps(arguments[0], indent=2)
        return job_status[name]()


def evaluate(expression: str, contexts: Dict[str, Any]) -> Any:
    """The status functions read `contexts['job_status_functions']`, a dict of `always`, `cancelled`, `failure` and `success` callables."""
    return _Parser(_tokenize(expression), contexts, expression).parse_whole()


def evaluate_template(text: str, contexts: Dict[str, Any]) -> Any:
    stripped = text.strip()
    matches = list(_EXPRESSION_IN_TEMPLATE.finditer(stripped))
    if not matches:
        return stripped
    if len(matches) == 1 and matches[0].start() == 0 and matches[0].end() == len(stripped):
        return evaluate(matches[0].group(1), contexts)
    return _EXPRESSION_IN_TEMPLATE.sub(lambda match: _stringify(evaluate(match.group(1), contexts)), stripped)


def status_functions(always_result: bool = True, cancelled: bool = False, needs_results: Optional[List[str]] = None) -> Dict[str, Callable[[], bool]]:
    results = needs_results or []
    return {
        "always": lambda: always_result,
        "cancelled": lambda: cancelled,
        "failure": lambda: any(result == "failure" for result in results),
        "success": lambda: all(result == "success" for result in results),
    }
