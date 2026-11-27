from __future__ import annotations

import http.server
import io
import json
import threading
import unittest
import urllib.error
import urllib.request
from typing import Any, Dict, List, Optional

from ensure_ci_always_tests_latest_operating_systems.errors import GitHubApiError
from ensure_ci_always_tests_latest_operating_systems.github_client import GitHubClient

from .helpers import FakeGitHub, InOrder, UnscriptedCall


class FakeResponse:
    def __init__(self, body: Any, status: int = 200, headers: Optional[Dict[str, str]] = None) -> None:
        self._body = body if isinstance(body, bytes) else json.dumps(body).encode('utf-8')
        self.status = status
        self.headers = _Headers(headers or {})

    def read(self) -> bytes:
        return self._body


class _Headers(dict):
    def items(self):  # type: ignore[override]
        return list(super().items())


class FakeOpener:
    def __init__(self, answers: List[Any]) -> None:
        self.answers = list(answers)
        self.requests: List[urllib.request.Request] = []

    def open(self, request: urllib.request.Request, timeout: Optional[float] = None) -> Any:
        self.requests.append(request)
        answer = self.answers.pop(0)
        if isinstance(answer, BaseException):
            raise answer
        return answer


def http_error(code: int, body: Any = None, headers: Optional[Dict[str, str]] = None) -> urllib.error.HTTPError:
    payload = json.dumps(body if body is not None else {'message': 'status %d' % code}).encode('utf-8')
    return urllib.error.HTTPError('https://api.github.com/x', code, 'status', headers or {}, io.BytesIO(payload))  # type: ignore[arg-type]


def make_client(answers: List[Any], token: Optional[str] = 'secret-token') -> tuple:
    opener = FakeOpener(answers)
    waits: List[float] = []
    return GitHubClient(token, 'CommunityToolkit/Maui', opener=opener, sleep=waits.append), opener, waits


class RequestTests(unittest.TestCase):
    def test_get_returns_status_headers_and_json(self) -> None:
        client, opener, _ = make_client([FakeResponse({'login': 'octocat'}, headers={'X-OAuth-Scopes': 'public_repo, workflow'})])
        status, headers, data = client.get('/user')
        self.assertEqual((status, data), (200, {'login': 'octocat'}))
        self.assertEqual(headers.get('x-oauth-scopes'), 'public_repo, workflow')
        self.assertEqual(headers['X-OAuth-Scopes'], 'public_repo, workflow')
        self.assertEqual(opener.requests[0].full_url, 'https://api.github.com/user')

    def test_sends_the_documented_headers(self) -> None:
        client, opener, _ = make_client([FakeResponse({})])
        client.get('repos/CommunityToolkit/Maui', {'a': 'b c'})
        request = opener.requests[0]
        self.assertEqual(request.full_url, 'https://api.github.com/repos/CommunityToolkit/Maui?a=b+c')
        self.assertEqual(request.get_header('Accept'), 'application/vnd.github+json')
        self.assertEqual(request.get_header('X-github-api-version'), '2022-11-28')

    def test_token_goes_in_an_unredirected_header(self) -> None:
        client, opener, _ = make_client([FakeResponse({})])
        client.get('/user')
        request = opener.requests[0]
        self.assertEqual(request.unredirected_hdrs.get('Authorization'), 'Bearer secret-token')
        self.assertNotIn('Authorization', request.headers)

    def test_no_header_value_ever_holds_a_line_break(self) -> None:
        client, opener, _ = make_client([FakeResponse({})])
        client.get('/user')
        request = opener.requests[0]
        values = list(request.headers.values()) + list(request.unredirected_hdrs.values())
        for value in values:
            self.assertNotIn('\r', value)
            self.assertNotIn('\n', value)

    def test_a_token_with_a_line_break_or_space_is_refused_before_any_request(self) -> None:
        for token in ('s3cr3t\n', 's3cr3t\r\n', 'ab cd', 'ab\ncd', ' s3cr3t'):
            with self.subTest(token=token):
                with self.assertRaises(ValueError) as caught:
                    make_client([], token=token)
                self.assertNotIn(token.strip(), str(caught.exception))

    def test_no_token_means_no_authorization_header(self) -> None:
        client, opener, _ = make_client([FakeResponse({})], token=None)
        client.get('/user')
        self.assertNotIn('Authorization', opener.requests[0].unredirected_hdrs)
        self.assertNotIn('Authorization', opener.requests[0].headers)

    def test_post_patch_and_put_send_json(self) -> None:
        client, opener, _ = make_client([FakeResponse({'ok': 1}, 201), FakeResponse({}), FakeResponse({})])
        self.assertEqual(client.post('/p', {'a': 1})[0], 201)
        client.patch('/p', {'state': 'closed'})
        client.put('/p', {'sha': 'abc'})
        self.assertEqual([request.get_method() for request in opener.requests], ['POST', 'PATCH', 'PUT'])
        self.assertEqual(json.loads(opener.requests[0].data), {'a': 1})
        self.assertEqual(opener.requests[0].get_header('Content-type'), 'application/json')

    def test_client_errors_are_returned_not_raised(self) -> None:
        client, _, waits = make_client([http_error(403, {'message': 'Forbidden: personal access token (classic)'})])
        status, _, data = client.get('/repos/CommunityToolkit/Maui')
        self.assertEqual(status, 403)
        self.assertIn('classic', data['message'])
        self.assertEqual(waits, [])

    def test_retries_a_502_then_succeeds(self) -> None:
        client, opener, waits = make_client([http_error(502), http_error(503), FakeResponse({'ok': True})])
        self.assertEqual(client.get('/x')[2], {'ok': True})
        self.assertEqual(waits, [2, 4])
        self.assertEqual(len(opener.requests), 3)

    def test_gives_up_after_three_server_errors(self) -> None:
        client, _, _ = make_client([http_error(502)] * 3)
        with self.assertRaises(GitHubApiError) as caught:
            client.get('/x')
        self.assertEqual(caught.exception.status, 502)

    def test_retries_network_errors(self) -> None:
        client, _, waits = make_client([urllib.error.URLError('down'), FakeResponse({'ok': True})])
        self.assertEqual(client.get('/x')[0], 200)
        self.assertEqual(waits, [2])

    def test_empty_body_is_none(self) -> None:
        client, _, _ = make_client([FakeResponse(b'', 204)])
        self.assertEqual(client.put('/x')[0:3:2], (204, None))


class PaginationTests(unittest.TestCase):
    LINK = '<https://api.github.com/repositories/1/pulls?per_page=100&page=2>; rel="next", <https://api.github.com/x>; rel="last"'

    def test_follows_next_links(self) -> None:
        client, opener, _ = make_client([
            FakeResponse([1, 2], headers={'Link': self.LINK}),
            FakeResponse([3]),
        ])
        self.assertEqual(client.paginate('/repos/CommunityToolkit/Maui/pulls'), [1, 2, 3])
        self.assertEqual(opener.requests[0].full_url, 'https://api.github.com/repos/CommunityToolkit/Maui/pulls?per_page=100')
        self.assertEqual(opener.requests[1].full_url, 'https://api.github.com/repositories/1/pulls?per_page=100&page=2')
        self.assertEqual(opener.requests[1].unredirected_hdrs.get('Authorization'), 'Bearer secret-token')

    def test_unwraps_a_reply_holding_one_list(self) -> None:
        client, _, _ = make_client([FakeResponse({'total_count': 2, 'jobs': [{'id': 1}, {'id': 2}]})])
        self.assertEqual(client.paginate('/runs/1/jobs'), [{'id': 1}, {'id': 2}])

    def test_refuses_a_next_page_on_another_host(self) -> None:
        link = '<https://evil.test/steal?page=2>; rel="next"'
        client, opener, _ = make_client([FakeResponse([1], headers={'Link': link})])
        with self.assertRaises(GitHubApiError):
            client.paginate('/x')
        self.assertEqual(len(opener.requests), 1)

    def test_an_error_status_raises(self) -> None:
        client, _, _ = make_client([http_error(404)])
        with self.assertRaises(GitHubApiError) as caught:
            client.paginate('/x')
        self.assertEqual(caught.exception.status, 404)


class GraphQlTests(unittest.TestCase):
    def test_returns_the_data_object(self) -> None:
        client, opener, _ = make_client([FakeResponse({'data': {'viewer': {'login': 'x'}}})])
        self.assertEqual(client.graphql('query Viewer { viewer { login } }', {'a': 1}), {'viewer': {'login': 'x'}})
        request = opener.requests[0]
        self.assertEqual(request.full_url, 'https://api.github.com/graphql')
        self.assertEqual(json.loads(request.data)['variables'], {'a': 1})

    def test_errors_raise_with_every_message(self) -> None:
        client, _, _ = make_client([FakeResponse({'errors': [{'message': 'first'}, {'message': 'second'}], 'data': None})])
        with self.assertRaises(GitHubApiError) as caught:
            client.graphql('mutation EnableAutoMerge { x }')
        self.assertIn('first', str(caught.exception))
        self.assertIn('second', str(caught.exception))
        self.assertIn('EnableAutoMerge', str(caught.exception))

    def test_a_failed_http_status_raises(self) -> None:
        client, _, _ = make_client([http_error(401, {'message': 'Bad credentials'})])
        with self.assertRaises(GitHubApiError) as caught:
            client.graphql('query { viewer { login } }')
        self.assertEqual(caught.exception.status, 401)


class _Recorder(http.server.BaseHTTPRequestHandler):
    seen: List[Dict[str, Optional[str]]] = []
    redirect_to: Optional[str] = None

    def do_GET(self) -> None:  # noqa: N802
        type(self).seen.append({'path': self.path, 'authorization': self.headers.get('Authorization')})
        if type(self).redirect_to and self.path.startswith('/api'):
            self.send_response(302)
            self.send_header('Location', type(self).redirect_to + '/storage/log.txt')
            self.end_headers()
            return
        body = b'log line 1\nlog line 2\n'
        self.send_response(200)
        self.send_header('Content-Length', str(len(body)))
        self.end_headers()
        self.wfile.write(body)

    def log_message(self, *args: Any) -> None:
        pass


def start_server(handler: type) -> tuple:
    server = http.server.HTTPServer(('127.0.0.1', 0), handler)
    thread = threading.Thread(target=server.serve_forever, daemon=True)
    thread.start()
    return server, 'http://127.0.0.1:%d' % server.server_address[1]


class RedirectTests(unittest.TestCase):
    def test_token_never_reaches_the_redirect_host(self) -> None:
        class StorageHandler(_Recorder):
            seen: List[Dict[str, Optional[str]]] = []
            redirect_to = None

        class ApiHandler(_Recorder):
            seen: List[Dict[str, Optional[str]]] = []
            redirect_to = None

        storage, storage_url = start_server(StorageHandler)
        api, api_url = start_server(ApiHandler)
        ApiHandler.redirect_to = storage_url
        try:
            client = GitHubClient('secret-token', 'CommunityToolkit/Maui', api_url=api_url)
            text = client.get_text_following_redirect('/api/jobs/1/logs')
        finally:
            api.shutdown()
            storage.shutdown()
            api.server_close()
            storage.server_close()
        self.assertEqual(text, 'log line 1\nlog line 2\n')
        self.assertEqual(ApiHandler.seen[0]['authorization'], 'Bearer secret-token')
        self.assertEqual(len(StorageHandler.seen), 1)
        self.assertIsNone(StorageHandler.seen[0]['authorization'])

    def test_an_error_status_raises(self) -> None:
        client, _, _ = make_client([http_error(404)])
        with self.assertRaises(GitHubApiError):
            client.get_text_following_redirect('/runs/1/logs')


class FakeGitHubTests(unittest.TestCase):
    def test_records_calls_with_the_token_name_in_order(self) -> None:
        journal: list = []
        github_token = FakeGitHub({'GET /user': {'login': 'a'}, 'POST /x': (201, {}, {})}, 'GITHUB_TOKEN', journal)
        pat = FakeGitHub({'PATCH /y': {}}, 'PAT', journal)
        github_token.get('/user')
        pat.patch('/y', {'a': 1})
        github_token.post('/x', {'b': 2})
        self.assertEqual([(call.token_name, call.method, call.path) for call in journal],
                         [('GITHUB_TOKEN', 'GET', '/user'), ('PAT', 'PATCH', '/y'), ('GITHUB_TOKEN', 'POST', '/x')])
        self.assertEqual([call.path for call in github_token.write_calls()], ['/x'])

    def test_unscripted_calls_raise(self) -> None:
        with self.assertRaises(UnscriptedCall):
            FakeGitHub({}).get('/nothing')

    def test_graphql_is_keyed_by_operation_and_writes_are_mutations(self) -> None:
        fake = FakeGitHub({'graphql EnableAutoMerge': {'ok': True}, 'graphql Lookup': {'found': 1}})
        fake.graphql('query Lookup { x }')
        fake.graphql('mutation EnableAutoMerge($id: ID!) { y }', {'id': 'z'})
        self.assertEqual([call.operation for call in fake.calls], ['Lookup', 'EnableAutoMerge'])
        self.assertEqual([call.operation for call in fake.write_calls()], ['EnableAutoMerge'])

    def test_in_order_answers_and_exceptions(self) -> None:
        fake = FakeGitHub({'GET /a': InOrder([{'n': 1}, GitHubApiError('GET', '/a', 500, 'boom')])})
        self.assertEqual(fake.get('/a')[2], {'n': 1})
        with self.assertRaises(GitHubApiError):
            fake.get('/a')
        with self.assertRaises(UnscriptedCall):
            fake.get('/a')

    def test_a_plain_list_is_a_paginated_answer(self) -> None:
        self.assertEqual(FakeGitHub({'GET /pulls': [{'n': 1}, {'n': 2}]}).paginate('/pulls'), [{'n': 1}, {'n': 2}])


if __name__ == '__main__':
    unittest.main()
