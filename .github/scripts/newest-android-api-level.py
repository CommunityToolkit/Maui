#!/usr/bin/env python3
"""Prints the highest api-level of the google_apis image in a workflow file, so the trimmed Android legs, which boot that image, follow
the untrimmed matrix. An entry that names another target, such as API 27's default or a google_apis_ps16k (16 KB page size) leg,
does not count.

Usage: newest-android-api-level.py <workflow.yml>
Exit code 1 when the file has no api-level value of the google_apis image.
"""
import re
import sys

LEVEL = re.compile(r'^[0-9]+(\.[0-9]+)?$')
KEY = re.compile(r'\bapi-level:')
TARGET = re.compile(r'(?:^|[\s{,])target:\s*([^,}]*)')
TRIMMED_LEGS_TARGET = 'google_apis'


def parse(level):
    parts = level.split('.')
    return (int(parts[0]), int(parts[1]) if len(parts) > 1 else 0)


def without_comment(value):
    return re.sub(r'\s+#.*$', '', value)


def scalars_in(value):
    # Flow sequences ([ 36, 28 ]) and flow maps ({ target: default, api-level: 38 }) end in brackets or braces.
    for item in without_comment(value).split(','):
        item = item.strip(' []{}').strip('\'"')
        if LEVEL.match(item):
            yield item


def names_other_target(entry):
    for line in entry:
        if line.lstrip().startswith('#'):
            continue
        for target in TARGET.finditer(without_comment(line)):
            if target.group(1).strip().strip('\'"') != TRIMMED_LEGS_TARGET:
                return True
    return False


def without_other_targets(text):
    # A sequence entry is its '- ' line and the lines indented deeper than the dash, so a matrix entry's target can come before or
    # after its api-level.
    kept = []
    entry = []
    entry_column = None
    for line in text.splitlines():
        stripped = line.lstrip()
        indent = len(line) - len(stripped)
        if entry_column is not None and (not stripped or stripped.startswith('#') or indent > entry_column):
            entry.append(line)
            continue
        if not names_other_target(entry):
            kept.extend(entry)
        entry = []
        entry_column = None
        if stripped.startswith('- '):
            entry = [line]
            entry_column = indent
        else:
            kept.append(line)
    if not names_other_target(entry):
        kept.extend(entry)
    return '\n'.join(kept)


def levels_in(text):
    block_sequence_column = None
    for line in text.splitlines():
        stripped = line.lstrip()
        if stripped.startswith('#'):
            continue
        indent = len(line) - len(stripped)
        if block_sequence_column is not None:
            if stripped.startswith('- ') and indent >= block_sequence_column:
                yield from scalars_in(stripped[2:])
                continue
            if stripped:
                block_sequence_column = None
        keys = list(KEY.finditer(line))
        for key in keys:
            yield from scalars_in(line[key.end():].split('api-level:')[0])
        if keys and without_comment(line[keys[-1].end():]).strip() == '':
            block_sequence_column = keys[-1].start()


def main():
    with open(sys.argv[1], encoding='utf-8') as workflow:
        levels = list(levels_in(without_other_targets(workflow.read())))
    if not levels:
        print(f'::error::No api-level value of the {TRIMMED_LEGS_TARGET} image found in {sys.argv[1]}.', file=sys.stderr)
        return 1
    print(max(levels, key=parse))
    return 0


sys.exit(main())
