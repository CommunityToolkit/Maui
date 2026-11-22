"""The only code that talks to the GitHub API; the token travels in a header that redirects never copy."""
from __future__ import annotations

import json
import re
import socket
import time
import urllib.error
import urllib.parse
import urllib.request
from typing import Any, Callable, Dict, List, Optional, Tuple

from .downloads import ATTEMPTS, TIMEOUT_SECONDS, WAITS_BETWEEN_ATTEMPTS_SECONDS, is_transient_status
from .errors import GitHubApiError

API_VERSION = '2022-11-28'
USER_AGENT = 'CommunityToolkit-Maui-ensure-ci-always-tests-latest-operating-systems'
_NEXT_LINK = re.compile(r'<([^>]+)>\s*;\s*rel="next"', re.ASCII)
_GRAPHQL_OPERATION = re.compile(r'\b(?:query|mutation)\s+(\w+)', re.ASCII)


class CaseInsensitiveHeaders(dict):
    """Response headers keyed by lower-case name, so callers read 'x-oauth-scopes' whatever GitHub sends."""

    def __init__(self, items: Optional[Any] = None) -> None:
        super().__init__()
        for name, value in dict(items or {}).items():
            self[name] = value

    def __setitem__(self, name: str, value: Any) -> None:
        super().__setitem__(name.lower(), value)

    def __getitem__(self, name: str) -> Any:
        return super().__getitem__(name.lower())

    def __contains__(self, name: object) -> bool:
        return isinstance(name, str) and super().__contains__(name.lower())

    def get(self, name: str, default: Any = None) -> Any:
        return super().get(name.lower(), default)


Response = Tuple[int, CaseInsensitiveHeaders, Any]


class GitHubClient:
    """get, post, patch and put return (status, headers, json) for any status below 500, so callers decide what a 4xx means."""

    def __init__(self, token: Optional[str], repo: str, api_url: str = 'https://api.github.com',
                 opener: Optional[Any] = None, sleep: Callable[[float], None] = time.sleep) -> None:
        if token and re.fullmatch(r'[\x21-\x7e]+', token) is None:
            raise ValueError('the token must hold only visible ASCII characters; a line break or a space would break the request header')
        self._token = token
        self.repo = repo
        self.api_url = api_url.rstrip('/')
        self._opener = opener if opener is not None else urllib.request.build_opener()
        self._sleep = sleep

    def get(self, path: str, params: Optional[Dict[str, Any]] = None) -> Response:
        return self._send('GET', self._url(path, params), None)

    def post(self, path: str, body: Optional[Any] = None) -> Response:
        return self._send('POST', self._url(path), body)

    def patch(self, path: str, body: Optional[Any] = None) -> Response:
        return self._send('PATCH', self._url(path), body)

    def put(self, path: str, body: Optional[Any] = None) -> Response:
        return self._send('PUT', self._url(path), body)

    def paginate(self, path: str, params: Optional[Dict[str, Any]] = None) -> List[Any]:
        """A reply that wraps its list in one object (jobs, check_runs, items) is unwrapped."""
        query = dict(params or {})
        query.setdefault('per_page', 100)
        url: Optional[str] = self._url(path, query)
        collected: List[Any] = []
        while url:
            status, headers, data = self._send('GET', url, None)
            if not 200 <= status < 300:
                raise GitHubApiError('GET', path, status, _message_of(data))
            if isinstance(data, dict):
                lists = [value for value in data.values() if isinstance(value, list)]
                if len(lists) != 1:
                    raise GitHubApiError('GET', path, status, 'expected a list or an object holding exactly one list')
                data = lists[0]
            if not isinstance(data, list):
                raise GitHubApiError('GET', path, status, 'expected a list')
            collected.extend(data)
            url = self._next_page_url(path, headers.get('link'))
        return collected

    def graphql(self, query: str, variables: Optional[Dict[str, Any]] = None) -> Dict[str, Any]:
        operation = _GRAPHQL_OPERATION.search(query)
        path = '/graphql'
        status, _, data = self._send('POST', self.api_url + path,
                                     {'query': query, 'variables': variables or {}})
        name = operation.group(1) if operation else 'graphql'
        if not 200 <= status < 300:
            raise GitHubApiError('POST', path, status, _message_of(data))
        if not isinstance(data, dict):
            raise GitHubApiError('POST', path, status, '%s returned no JSON object' % name)
        if data.get('errors'):
            messages = '; '.join(str(error.get('message', error)) if isinstance(error, dict) else str(error)
                                 for error in data['errors'])
            raise GitHubApiError('POST', path, status, '%s: %s' % (name, messages))
        return data.get('data') or {}

    def get_text_following_redirect(self, path: str) -> str:
        """Job logs answer with a redirect to storage; urllib drops the unredirected token header on that hop."""
        status, _, text = self._send('GET', self._url(path), None, as_text=True)
        if not 200 <= status < 300:
            raise GitHubApiError('GET', path, status, _message_of(text))
        return text

    def _url(self, path: str, params: Optional[Dict[str, Any]] = None) -> str:
        url = path if path.startswith('http') else self.api_url + '/' + path.lstrip('/')
        if params:
            url += ('&' if '?' in url else '?') + urllib.parse.urlencode(params)
        return url

    def _next_page_url(self, path: str, link_header: Optional[str]) -> Optional[str]:
        match = _NEXT_LINK.search(link_header or '')
        if match is None:
            return None
        url = match.group(1)
        if urllib.parse.urlsplit(url).netloc != urllib.parse.urlsplit(self.api_url).netloc:
            raise GitHubApiError('GET', path, None, 'refusing to follow a next page on another host: %s' % url)
        return url

    def _send(self, method: str, url: str, body: Optional[Any], as_text: bool = False) -> Response:
        data = None if body is None else json.dumps(body).encode('utf-8')
        last_message = ''
        last_status: Optional[int] = None
        for attempt in range(ATTEMPTS):
            if attempt > 0:
                self._sleep(WAITS_BETWEEN_ATTEMPTS_SECONDS[attempt - 1])
            request = urllib.request.Request(url, data=data, method=method, headers={
                'Accept': 'application/vnd.github+json',
                'X-GitHub-Api-Version': API_VERSION,
                'User-Agent': USER_AGENT,
            })
            if data is not None:
                request.add_header('Content-Type', 'application/json')
            if self._token:
                request.add_unredirected_header('Authorization', 'Bearer ' + self._token)
            try:
                response = self._opener.open(request, timeout=TIMEOUT_SECONDS)
                status = getattr(response, 'status', None) or 200
                headers = CaseInsensitiveHeaders(dict(response.headers.items()))
                raw = response.read()
            except urllib.error.HTTPError as error:
                status = error.code
                headers = CaseInsensitiveHeaders(dict(error.headers.items()) if error.headers else {})
                raw = error.read()
            except (urllib.error.URLError, socket.timeout, ConnectionError, TimeoutError, OSError) as error:
                last_status, last_message = None, 'could not reach GitHub: %s' % error
                continue
            if is_transient_status(status):
                last_status, last_message = status, _message_of(_decode(raw, as_text))
                continue
            return status, headers, _decode(raw, as_text)
        raise GitHubApiError(method, url, last_status, last_message)


def _decode(raw: bytes, as_text: bool) -> Any:
    text = raw.decode('utf-8', errors='replace') if raw else ''
    if as_text:
        return text
    if not text.strip():
        return None
    try:
        return json.loads(text)
    except ValueError:
        return {'message': text[:500]}


def _message_of(data: Any) -> str:
    if isinstance(data, dict) and 'message' in data:
        return str(data['message'])
    return str(data)[:500] if data else ''
