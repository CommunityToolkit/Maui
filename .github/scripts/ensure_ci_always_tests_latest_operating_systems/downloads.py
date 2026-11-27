"""Every download of a public source goes through Downloader, so tests swap in a fake and no token can leak."""
from __future__ import annotations

import hashlib
import os
import socket
import time
import urllib.error
import urllib.parse
import urllib.request
import zlib
from typing import Any, Callable, Dict, Optional

from .errors import DownloadError, NotFoundError

USER_AGENT = 'CommunityToolkit-Maui-ensure-ci-always-tests-latest-operating-systems'
ATTEMPTS = 3
WAITS_BETWEEN_ATTEMPTS_SECONDS = (2, 4)
TIMEOUT_SECONDS = 60
DEFAULT_MAX_BYTES = 20_000_000
_READ_CHUNK = 1 << 16


def is_transient_status(status: int) -> bool:
    return status >= 500


def decompress_gzip_capped(url: str, body: bytes, max_bytes: int) -> bytes:
    """A gzip bomb counts against max_bytes like any other body."""
    decompressor = zlib.decompressobj(16 + zlib.MAX_WBITS)
    try:
        data = decompressor.decompress(body, max_bytes + 1)
        if decompressor.unconsumed_tail:
            raise DownloadError(url, None, '%s is larger than %d bytes after decompression' % (url, max_bytes))
        data += decompressor.flush()
    except zlib.error as error:
        raise DownloadError(url, None, '%s has a broken gzip body: %s' % (url, error))
    if len(data) > max_bytes:
        raise DownloadError(url, None, '%s is larger than %d bytes after decompression' % (url, max_bytes))
    return data


def read_capped(url: str, response: Any, max_bytes: int) -> bytes:
    chunks = []
    total = 0
    while True:
        chunk = response.read(min(_READ_CHUNK, max_bytes + 1 - total))
        if not chunk:
            break
        chunks.append(chunk)
        total += len(chunk)
        if total > max_bytes:
            raise DownloadError(url, None, '%s is larger than %d bytes' % (url, max_bytes))
    return b''.join(chunks)


class Downloader:
    def __init__(self, save_dir: Optional[str] = None, opener: Optional[Any] = None,
                 sleep: Callable[[float], None] = time.sleep) -> None:
        self.save_dir = save_dir
        self._opener = opener if opener is not None else urllib.request.build_opener()
        self._sleep = sleep

    def get(self, url: str, *, max_bytes: int = DEFAULT_MAX_BYTES, headers: Optional[Dict[str, str]] = None) -> bytes:
        extra_headers = dict(headers or {})
        if any(name.lower() == 'authorization' for name in extra_headers):
            raise ValueError('Downloader never sends credentials')
        request_headers = {'User-Agent': USER_AGENT, 'Accept-Encoding': 'gzip'}
        request_headers.update(extra_headers)
        last_failure: Optional[DownloadError] = None
        for attempt in range(ATTEMPTS):
            if attempt > 0:
                self._sleep(WAITS_BETWEEN_ATTEMPTS_SECONDS[attempt - 1])
            try:
                body = self._fetch_once(url, request_headers, max_bytes)
            except _Transient as failure:
                last_failure = failure.error
                continue
            self._save_for_evidence(url, body)
            return body
        assert last_failure is not None
        raise last_failure

    def _fetch_once(self, url: str, request_headers: Dict[str, str], max_bytes: int) -> bytes:
        request = urllib.request.Request(url, headers=request_headers, method='GET')
        try:
            response = self._opener.open(request, timeout=TIMEOUT_SECONDS)
        except urllib.error.HTTPError as error:
            self._raise_for_status(url, error.code)
            raise
        except (urllib.error.URLError, socket.timeout, ConnectionError, TimeoutError, OSError) as error:
            raise _Transient(DownloadError(url, None, 'could not reach %s: %s' % (url, error)))
        try:
            status = getattr(response, 'status', None) or 200
            self._raise_for_status(url, status)
            body = read_capped(url, response, max_bytes)
            encoding = ''
            response_headers = getattr(response, 'headers', None)
            if response_headers is not None:
                encoding = (response_headers.get('Content-Encoding') or '').strip().lower()
        except (socket.timeout, ConnectionError, TimeoutError) as error:
            raise _Transient(DownloadError(url, None, 'reading %s failed: %s' % (url, error)))
        finally:
            close = getattr(response, 'close', None)
            if close is not None:
                close()
        if encoding == 'gzip':
            body = decompress_gzip_capped(url, body, max_bytes)
        return body

    @staticmethod
    def _raise_for_status(url: str, status: int) -> None:
        if 200 <= status < 300:
            return
        if status == 404:
            raise NotFoundError(url, status)
        error = DownloadError(url, status)
        if is_transient_status(status):
            raise _Transient(error)
        raise error

    def _save_for_evidence(self, url: str, body: bytes) -> None:
        if not self.save_dir:
            return
        parts = urllib.parse.urlsplit(url)
        name = parts.path.strip('/').replace('/', '__') or 'index'
        if parts.query:
            name += '__query_' + hashlib.sha256(parts.query.encode('utf-8')).hexdigest()[:10]
        folder = os.path.join(self.save_dir, parts.netloc.replace(':', '_'))
        os.makedirs(folder, exist_ok=True)
        with open(os.path.join(folder, name), 'wb') as saved:
            saved.write(body)


class _Transient(Exception):
    def __init__(self, error: DownloadError) -> None:
        super().__init__(str(error))
        self.error = error
