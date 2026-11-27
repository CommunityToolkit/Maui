from __future__ import annotations

import gzip
import io
import os
import tempfile
import unittest
import urllib.error
from typing import Any, List, Optional

from ensure_ci_always_tests_latest_operating_systems.downloads import Downloader
from ensure_ci_always_tests_latest_operating_systems.errors import DownloadError, NotFoundError


class FakeResponse:
    def __init__(self, body: bytes, status: int = 200, headers: Optional[dict] = None) -> None:
        self._stream = io.BytesIO(body)
        self.status = status
        self.headers = headers or {}
        self.closed = False

    def read(self, size: int = -1) -> bytes:
        return self._stream.read(size)

    def close(self) -> None:
        self.closed = True


class FakeOpener:
    def __init__(self, answers: List[Any]) -> None:
        self.answers = list(answers)
        self.requests: List[Any] = []

    def open(self, request: Any, timeout: Optional[float] = None) -> Any:
        self.requests.append((request, timeout))
        answer = self.answers.pop(0)
        if isinstance(answer, BaseException):
            raise answer
        return answer


def read_bytes(path: str) -> bytes:
    with open(path, 'rb') as file:
        return file.read()


def http_error(url: str, code: int) -> urllib.error.HTTPError:
    return urllib.error.HTTPError(url, code, 'status %d' % code, {}, io.BytesIO(b''))  # type: ignore[arg-type]


class DownloaderTests(unittest.TestCase):
    URL = 'https://example.test/some/path/file.json'

    def make(self, answers: List[Any], save_dir: Optional[str] = None) -> tuple:
        opener = FakeOpener(answers)
        waits: List[float] = []
        return Downloader(save_dir, opener=opener, sleep=waits.append), opener, waits

    def test_returns_the_body_and_sends_the_user_agent(self) -> None:
        downloader, opener, waits = self.make([FakeResponse(b'hello')])
        self.assertEqual(downloader.get(self.URL), b'hello')
        request, timeout = opener.requests[0]
        self.assertEqual(request.get_header('User-agent'), 'CommunityToolkit-Maui-ensure-ci-always-tests-latest-operating-systems')
        self.assertEqual(timeout, 60)
        self.assertEqual(waits, [])

    def test_never_sends_an_authorization_header(self) -> None:
        downloader, opener, _ = self.make([FakeResponse(b'x')])
        downloader.get(self.URL)
        request = opener.requests[0][0]
        self.assertFalse(any(name.lower() == 'authorization' for name in dict(request.header_items())))
        self.assertNotIn('Authorization', request.unredirected_hdrs)

    def test_refuses_to_be_given_credentials(self) -> None:
        downloader, opener, _ = self.make([FakeResponse(b'x')])
        for name in ('Authorization', 'authorization', 'AUTHORIZATION'):
            with self.subTest(name):
                with self.assertRaises(ValueError):
                    downloader.get(self.URL, headers={name: 'Bearer secret'})
        self.assertEqual(opener.requests, [])

    def test_sends_extra_headers(self) -> None:
        downloader, opener, _ = self.make([FakeResponse(b'x')])
        downloader.get(self.URL, headers={'Accept': 'text/plain'})
        self.assertEqual(opener.requests[0][0].get_header('Accept'), 'text/plain')

    def test_retries_server_errors_with_two_then_four_seconds(self) -> None:
        downloader, opener, waits = self.make([http_error(self.URL, 502), http_error(self.URL, 503), FakeResponse(b'ok')])
        self.assertEqual(downloader.get(self.URL), b'ok')
        self.assertEqual(waits, [2, 4])
        self.assertEqual(len(opener.requests), 3)

    def test_gives_up_after_three_attempts(self) -> None:
        downloader, opener, waits = self.make([http_error(self.URL, 500)] * 3)
        with self.assertRaises(DownloadError) as caught:
            downloader.get(self.URL)
        self.assertEqual(caught.exception.status, 500)
        self.assertEqual(caught.exception.url, self.URL)
        self.assertEqual(len(opener.requests), 3)
        self.assertEqual(waits, [2, 4])

    def test_retries_network_errors(self) -> None:
        downloader, _, waits = self.make([urllib.error.URLError('down'), TimeoutError('slow'), FakeResponse(b'ok')])
        self.assertEqual(downloader.get(self.URL), b'ok')
        self.assertEqual(waits, [2, 4])

    def test_network_errors_every_time_become_a_download_error(self) -> None:
        downloader, _, _ = self.make([urllib.error.URLError('down')] * 3)
        with self.assertRaises(DownloadError) as caught:
            downloader.get(self.URL)
        self.assertIsNone(caught.exception.status)

    def test_404_is_not_found_and_not_retried(self) -> None:
        downloader, opener, waits = self.make([http_error(self.URL, 404)])
        with self.assertRaises(NotFoundError) as caught:
            downloader.get(self.URL)
        self.assertEqual(caught.exception.status, 404)
        self.assertIsInstance(caught.exception, DownloadError)
        self.assertEqual(len(opener.requests), 1)
        self.assertEqual(waits, [])

    def test_other_client_errors_are_not_retried(self) -> None:
        downloader, opener, _ = self.make([http_error(self.URL, 403)])
        with self.assertRaises(DownloadError) as caught:
            downloader.get(self.URL)
        self.assertEqual(caught.exception.status, 403)
        self.assertNotIsInstance(caught.exception, NotFoundError)
        self.assertEqual(len(opener.requests), 1)

    def test_status_on_the_response_object_is_checked_too(self) -> None:
        downloader, _, _ = self.make([FakeResponse(b'', status=404)])
        with self.assertRaises(NotFoundError):
            downloader.get(self.URL)

    def test_gzip_bodies_are_decompressed(self) -> None:
        compressed = gzip.compress(b'{"a": 1}')
        downloader, _, _ = self.make([FakeResponse(compressed, headers={'Content-Encoding': 'gzip'})])
        self.assertEqual(downloader.get(self.URL), b'{"a": 1}')

    def test_a_body_without_the_gzip_header_is_left_alone(self) -> None:
        downloader, _, _ = self.make([FakeResponse(b'plain', headers={'Content-Encoding': 'identity'})])
        self.assertEqual(downloader.get(self.URL), b'plain')

    def test_broken_gzip_is_a_download_error(self) -> None:
        downloader, _, _ = self.make([FakeResponse(b'not gzip', headers={'Content-Encoding': 'gzip'})])
        with self.assertRaises(DownloadError):
            downloader.get(self.URL)

    def test_size_cap_applies_to_the_body(self) -> None:
        downloader, _, _ = self.make([FakeResponse(b'x' * 101)])
        with self.assertRaises(DownloadError):
            downloader.get(self.URL, max_bytes=100)

    def test_a_body_exactly_at_the_cap_is_fine(self) -> None:
        downloader, _, _ = self.make([FakeResponse(b'x' * 100)])
        self.assertEqual(len(downloader.get(self.URL, max_bytes=100)), 100)

    def test_size_cap_applies_after_decompression(self) -> None:
        bomb = gzip.compress(b'\0' * 100_000)
        downloader, _, _ = self.make([FakeResponse(bomb, headers={'Content-Encoding': 'gzip'})])
        with self.assertRaises(DownloadError):
            downloader.get(self.URL, max_bytes=1000)

    def test_response_is_closed(self) -> None:
        response = FakeResponse(b'x')
        downloader, _, _ = self.make([response])
        downloader.get(self.URL)
        self.assertTrue(response.closed)

    def test_save_dir_naming(self) -> None:
        with tempfile.TemporaryDirectory() as folder:
            downloader, _, _ = self.make([FakeResponse(b'one'), FakeResponse(b'two'), FakeResponse(b'')], save_dir=folder)
            downloader.get(self.URL)
            downloader.get('https://api.nuget.org:443/v3/index.json')
            downloader.get('https://example.test/')
            self.assertEqual(read_bytes(os.path.join(folder, 'example.test', 'some__path__file.json')), b'one')
            self.assertEqual(read_bytes(os.path.join(folder, 'api.nuget.org_443', 'v3__index.json')), b'two')
            self.assertTrue(os.path.exists(os.path.join(folder, 'example.test', 'index')))

    def test_save_dir_keeps_urls_that_differ_only_in_the_query_apart(self) -> None:
        with tempfile.TemporaryDirectory() as folder:
            downloader, _, _ = self.make([FakeResponse(b'a'), FakeResponse(b'b')], save_dir=folder)
            downloader.get('https://example.test/x?page=1')
            downloader.get('https://example.test/x?page=2')
            self.assertEqual(len(os.listdir(os.path.join(folder, 'example.test'))), 2)

    def test_nothing_is_saved_without_a_save_dir(self) -> None:
        downloader, _, _ = self.make([FakeResponse(b'x')])
        self.assertIsNone(downloader.save_dir)
        downloader.get(self.URL)


if __name__ == '__main__':
    unittest.main()
