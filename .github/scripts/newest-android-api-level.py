#!/usr/bin/env python3
"""Prints the highest api-level in a workflow file, so the trimmed Android leg follows the untrimmed matrix.

Usage: newest-android-api-level.py <workflow.yml>
Exit code 1 when the file has no api-level value.
"""
import re
import sys

LEVEL = re.compile(r'^[0-9]+(\.[0-9]+)?$')
KEY = re.compile(r'\bapi-level:')


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
        levels = list(levels_in(workflow.read()))
    if not levels:
        print(f'::error::No api-level value found in {sys.argv[1]}.', file=sys.stderr)
        return 1
    print(max(levels, key=parse))
    return 0


sys.exit(main())
