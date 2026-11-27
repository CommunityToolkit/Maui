from __future__ import annotations

from typing import Optional


class UnreadableSourceError(Exception):
    """A source the bot reads is broken; the message is one line a person can act on."""


class PinnedVersionsFormatError(UnreadableSourceError):
    pass


class AndroidIndexFormatError(UnreadableSourceError):
    pass


class WorkloadSetError(UnreadableSourceError):
    pass


class RunnerImagesReadmeError(UnreadableSourceError):
    pass


class RunnerSoftwareError(UnreadableSourceError):
    pass


class DownloadError(UnreadableSourceError):
    def __init__(self, url: str, status: Optional[int], message: Optional[str] = None) -> None:
        self.url = url
        self.status = status
        self.message = message or 'download of %s failed with status %s' % (url, status)
        super().__init__(self.message)


class NotFoundError(DownloadError):
    def __init__(self, url: str, status: Optional[int] = 404, message: Optional[str] = None) -> None:
        super().__init__(url, status, message or '%s was not found (status %s)' % (url, status))


class GitHubApiError(Exception):
    def __init__(self, method: str, path: str, status: Optional[int], message: str) -> None:
        self.method = method
        self.path = path
        self.status = status
        self.message = message
        super().__init__('%s %s failed with status %s: %s' % (method, path, status, message))
