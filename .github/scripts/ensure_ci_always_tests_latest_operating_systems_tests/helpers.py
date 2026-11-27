"""Fakes and builders shared by the tests; no test touches the network."""
from __future__ import annotations

import io
import os
import re
import urllib.parse
import zipfile
from typing import Any, Dict, List, Mapping, Optional, Union

from ensure_ci_always_tests_latest_operating_systems.errors import DownloadError
from ensure_ci_always_tests_latest_operating_systems.github_client import CaseInsensitiveHeaders

FIXTURES_FOLDER = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'fixtures')
_GRAPHQL_NAME = re.compile(r'(?:query|mutation)\s+(\w+)|\{\s*(\w+)', re.ASCII)


class InOrder(list):
    """Wrap answers in this to have them given one per call; a plain list is itself the answer (a paginated reply)."""


class UnscriptedCall(AssertionError):
    """A test reached something it didn't script; the message says what."""


def fixture(relpath: str) -> bytes:
    with open(os.path.join(FIXTURES_FOLDER, *relpath.split('/')), 'rb') as file:
        return file.read()


def nupkg(members: Mapping[str, Union[bytes, str]]) -> bytes:
    """A deterministic zip in memory, so tests don't depend on the clock."""
    buffer = io.BytesIO()
    with zipfile.ZipFile(buffer, 'w', zipfile.ZIP_DEFLATED) as archive:
        for name, content in members.items():
            info = zipfile.ZipInfo(name, date_time=(1980, 1, 1, 0, 0, 0))
            info.compress_type = zipfile.ZIP_DEFLATED
            archive.writestr(info, content.encode('utf-8') if isinstance(content, str) else content)
    return buffer.getvalue()


def system_image_entry(path: str, api_level: str, codename: Optional[str] = None, obsolete: bool = False) -> str:
    """Clones the real android-37.2 16 KB entry, so a synthetic level has every element the parser sees in production."""
    text = fixture('android/google_apis-sys-img2-4.xml').decode('utf-8')
    original = re.search(
        r'  <remotePackage path="system-images;android-37\.2;google_apis_ps16k;x86_64">.*?</remotePackage>\n', text, re.S)
    if original is None:
        raise AssertionError('the fixture has no android-37.2 16 KB x86_64 entry')
    entry = original.group(0)
    entry = entry.replace('path="system-images;android-37.2;google_apis_ps16k;x86_64"', 'path="%s"' % path, 1)
    entry = entry.replace('<api-level>37.2</api-level>', '<api-level>%s</api-level>' % api_level, 1)
    if codename is not None:
        entry = entry.replace('<api-level>%s</api-level>\n' % api_level,
                              '<api-level>%s</api-level>\n      <codename>%s</codename>\n' % (api_level, codename), 1)
    if obsolete:
        entry = entry.replace('<remotePackage path=', '<remotePackage obsolete="true" path=', 1)
    return entry


class FakeDownloader:
    """mapping: url -> bytes, an exception to raise, a callable(url) or an InOrder list."""

    def __init__(self, mapping: Mapping[str, Any]) -> None:
        self.mapping = dict(mapping)
        self.calls: List[Dict[str, Any]] = []
        self.save_dir = None

    def get(self, url: str, *, max_bytes: int = 20_000_000, headers: Optional[Dict[str, str]] = None) -> bytes:
        self.calls.append({'url': url, 'max_bytes': max_bytes, 'headers': dict(headers or {})})
        if url not in self.mapping:
            raise UnscriptedCall('FakeDownloader has no answer for %s' % url)
        answer = self.mapping[url]
        if isinstance(answer, InOrder):
            if not answer:
                raise UnscriptedCall('FakeDownloader answered every call for %s' % url)
            answer = answer.pop(0)
        if callable(answer):
            answer = answer(url)
        if isinstance(answer, BaseException):
            raise answer
        if isinstance(answer, str):
            answer = answer.encode('utf-8')
        if len(answer) > max_bytes:
            raise DownloadError(url, None, '%s is larger than %d bytes' % (url, max_bytes))
        return answer

    def urls_requested(self) -> List[str]:
        return [call['url'] for call in self.calls]


class RecordedCall:
    def __init__(self, token_name: str, method: str, path: str, params: Any, body: Any, operation: Optional[str],
                 is_write: bool) -> None:
        self.token_name = token_name
        self.method = method
        self.path = path
        self.params = params
        self.body = body
        self.operation = operation
        self.is_write = is_write

    def __repr__(self) -> str:
        return 'RecordedCall(%s %s %s %s)' % (self.token_name, self.method, self.operation or self.path, self.is_write)


class FakeGitHub:
    """Answers from a script and records every call with the token that made it.

    responses is keyed by 'METHOD path' (for example 'GET /user'), or by a GraphQL operation name with the
    key 'graphql NAME'. A value is an answer, an exception to raise, a callable(body) or an InOrder list
    answered one per call. REST answers are (status, headers, json) tuples or a bare json value (status 200); GraphQL answers
    are the data object. Give two fakes one journal to assert the order of write calls across tokens.
    """

    def __init__(self, responses: Mapping[str, Any], token_name: str = 'GITHUB_TOKEN',
                 journal: Optional[List[RecordedCall]] = None) -> None:
        self.responses = dict(responses)
        self.token_name = token_name
        self.journal = journal if journal is not None else []
        self.calls: List[RecordedCall] = []

    def get(self, path: str, params: Optional[Dict[str, Any]] = None) -> Any:
        return self._rest('GET', path, params, None, False)

    def paginate(self, path: str, params: Optional[Dict[str, Any]] = None) -> List[Any]:
        _, _, data = self._rest('GET', path, params, None, False)
        return data

    def post(self, path: str, body: Optional[Any] = None) -> Any:
        return self._rest('POST', path, None, body, True)

    def patch(self, path: str, body: Optional[Any] = None) -> Any:
        return self._rest('PATCH', path, None, body, True)

    def put(self, path: str, body: Optional[Any] = None) -> Any:
        return self._rest('PUT', path, None, body, True)

    def graphql(self, query: str, variables: Optional[Dict[str, Any]] = None) -> Dict[str, Any]:
        match = _GRAPHQL_NAME.search(query)
        name = (match.group(1) or match.group(2)) if match else 'graphql'
        is_write = query.lstrip().startswith('mutation')
        self._record('POST', '/graphql', None, {'query': query, 'variables': variables or {}}, name, is_write)
        return self._answer('graphql ' + name, {'query': query, 'variables': variables or {}})

    def get_text_following_redirect(self, path: str) -> str:
        self._record('GET', path, None, None, None, False)
        answer = self._answer('GET ' + path, None)
        return answer if isinstance(answer, str) else str(answer)

    def write_calls(self) -> List[RecordedCall]:
        return [call for call in self.calls if call.is_write]

    def calls_to(self, method: str, path: str) -> List[RecordedCall]:
        return [call for call in self.calls if call.method == method and call.path == path]

    def _rest(self, method: str, path: str, params: Optional[Dict[str, Any]], body: Any, is_write: bool) -> Any:
        self._record(method, path, params, body, None, is_write)
        key = '%s %s' % (method, path)
        if params and key not in self.responses:
            key = '%s %s?%s' % (method, path, urllib.parse.urlencode(params))
        answer = self._answer(key, body)
        if isinstance(answer, tuple) and len(answer) == 3:
            return answer[0], CaseInsensitiveHeaders(answer[1]), answer[2]
        return 200, CaseInsensitiveHeaders({}), answer

    def _record(self, method: str, path: str, params: Any, body: Any, operation: Optional[str], is_write: bool) -> None:
        call = RecordedCall(self.token_name, method, path, params, body, operation, is_write)
        self.calls.append(call)
        self.journal.append(call)

    def _answer(self, key: str, body: Any) -> Any:
        if key not in self.responses:
            raise UnscriptedCall('FakeGitHub (%s) has no answer for %s' % (self.token_name, key))
        answer = self.responses[key]
        if isinstance(answer, InOrder):
            if not answer:
                raise UnscriptedCall('FakeGitHub (%s) answered every call for %s' % (self.token_name, key))
            answer = answer.pop(0)
        if callable(answer) and not isinstance(answer, BaseException):
            answer = answer(body)
        if isinstance(answer, BaseException):
            raise answer
        return answer
