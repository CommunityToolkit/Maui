"""Makes text from outside the repo (gists, README cells, NuGet, git, logins) safe to show in a pull request or issue."""
from __future__ import annotations

import unicodedata

MAX_CODE_LENGTH = 200
_LINE_BREAKS_AND_TABS = ('\r', '\n', '\t', '\u2028', '\u2029')


def _printable(text: str) -> str:
    kept = []
    for character in text:
        if character in _LINE_BREAKS_AND_TABS:
            kept.append(' ')
        # Cc and Cf cover control characters and the invisible bidi overrides that can disguise text.
        elif unicodedata.category(character) not in ('Cc', 'Cf'):
            kept.append(character)
    return ''.join(kept)


def markdown_code(text: object, in_table: bool = False) -> str:
    """A code span renders no mention, link or HTML, so the outside text can't act on the reader."""
    cleaned = _printable('' if text is None else str(text)).strip()
    if len(cleaned) > MAX_CODE_LENGTH:
        cleaned = cleaned[:MAX_CODE_LENGTH - 1] + '\u2026'
    cleaned = cleaned.replace('`', "'")
    if in_table:
        cleaned = cleaned.replace('|', '\\|')
    return '`' + (cleaned or ' ') + '`'
