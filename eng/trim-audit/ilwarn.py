#!/usr/bin/env python3
"""ilwarn.py: the trim (IL2xxx), AOT (IL3xxx) and feature-guard (IL4xxx) warnings of an audit build.

  ilwarn.py summarize <warnings.log> <out-dir> [<packages-dir> ...]
      writes <out-dir>/warnings-unique.tsv, warnings-summary.txt and warnings-counts.json, and prints the summary.
      Each packages-dir (the audit's restored packages folder) is searched for the toolkit DLLs, so that a warning on a toolkit
      member can be attributed to the assembly that defines it. Their type counts go to warnings-counts.json (toolkit_type_counts); a DLL
      without readable types, or no toolkit DLL at all, makes summarize exit 4 and the gate refuse the leg (exit 4).
  ilwarn.py diff <a/warnings-unique.tsv> <b/warnings-unique.tsv>
      the toolkit and Sample warnings that only one of two builds has.
  ilwarn.py gate <warnings-unique.tsv> [--allow allow.tsv] [--log warnings.log] [--assets-check assets-check.txt]
                 [--result result.json] [--github] [--max-annotations <n>] [--summary <file>] [--report-only]
      the CI check: fails when a warning of bucket 'toolkit' or 'toolkit-api' is not allow-listed. allow.tsv has tab separated lines
      'code<TAB>member pattern<TAB>reason[<TAB>scope]' (# starts a comment); the optional scope is a comma separated list of globs over
      the leg name '<platform>-<variant>' (for example 'android-*,ios-aot*'); an entry applies, and is judged stale, only on the legs
      in its scope, and no scope means every leg. A scope glob that matches none of KNOWN_LEGS is a bad allow-list (exit 2), so a
      typo cannot leave an entry that applies nowhere. A gate without --result does not know its leg, so every entry applies.
      The pattern is a glob over the warning's subject and '*' allows the code everywhere. The subject of a toolkit warning is its
      member (or, for analyzer warnings that have none, its file:line); the subject of a toolkit-api warning is the toolkit API
      that the app calls (the fix lives there), else the member or file:line.
      Warnings of the other buckets are listed, never failed.
      --github prints one ::error annotation per new warning, or one ::error for the reason the leg failed (then no per-warning ones).
      With new warnings, the first annotation is the verdict ('GATE: FAIL, n ...') and it counts in the budget below.
      GitHub shows at most 10 error and 10 warning annotations per step and 50 per job, summed over all steps, and earlier steps can use
      all 50, so the job summary and the GATE-EXIT line are the reliable channels; the verdict first helps only when budget is left.
      --max-annotations <n> (default 10) caps the per-warning ones of this gate; a ::warning says how many were left out. `audit.sh run` splits one budget of 10
      among the legs of its step; audit-win.ps1 is the only gate of its step and passes all of it.
      --summary appends a Markdown report (counts per bucket, the new warnings, stale allow-list lines) to the file (the job summary).
      It lists at most 200 rows per list and says how many more there are (GitHub rejects a step summary over 1 MiB); the step log lists every row.
      --report-only is for a leg whose result can't gate the build (Windows NativeAOT apps crash at run time, dotnet/maui#39053):
      the same report, but annotations are warnings and the exit code is 0 whatever the leg did. When the leg failed but wrote a
      warning list, the summary still lists the counts and the toolkit and toolkit-api warnings that were parsed.
      A bad command line or allow-list is still exit 2.
      --result is the leg's result.json: the gate refuses a leg whose build failed or whose result is missing, and for variant 'aotroot'
      it needs ROOTS-CHECK=OK in the roots-check.txt next to it.
      Exit codes: 0 clean; 1 new toolkit or toolkit-api warnings; 2 bad arguments or a bad allow-list; 3 the assets check did not say OK
      (the build did not use the feed's packages) or an aotroot leg's roots check did not; 4 the build failed, result.json, the tsv or
      the --log file or the warnings-counts.json next to the tsv is missing or unreadable, the parser could not read every IL warning line of the log, or a toolkit DLL's types could not be read (a broken run must not pass as a clean one).
      A stale allow-list entry (in scope on this leg, matches nothing) is reported and does not fail.

warnings-unique.tsv has one row per distinct warning, tab separated: bucket, tool, code, origin_assembly, member, location, message
bucket: where the member that triggers the warning lives.
  toolkit      a member of CommunityToolkit.Maui, .Core, .Camera or .MediaElement, or of generated code that one of their DLLs defines
               (judged by the type names in the DLLs and the warning's source path), or code of the toolkit's own source generators in an app
               (a source path under /CommunityToolkit.Maui.SourceGenerators/ or /CommunityToolkit.Maui.MediaElement.SourceGenerators/)
  toolkit-api  Sample (or device-test) code that calls a toolkit API marked RequiresUnreferencedCode/RequiresDynamicCode/RequiresAssemblyFiles
  sample       other warnings on Sample code
  maui         Microsoft.Maui.* members
  (CommunityToolkit.Maui.Markup, a separate package the Sample uses, counts as other)
  other        everything else (BCL, platform bindings, AndroidX, xunit)
tool: 'analyzer' = the Roslyn analyzer while compiling the app (the warning sits on the app's call site),
      'illink'   = the trimmer, 'ilc' = the NativeAOT compiler (both name the member whose code they could not prove safe).
location: file:line when the log has one. The same warning repeated per RID, target or generic instantiation counts once."""
import collections
import fnmatch
import json
import os
import re
import sys

LINE = re.compile(
    r'^\s*(?:\d+(?::\d+)?>)?(?P<loc>.*?):\s+(?P<tool>Trim analysis |AOT analysis )?warning (?P<code>IL[234]\d{3}):\s*(?P<text>.*?)\s*(?:\[[^\]]*\])?\s*$')
TOOLKIT_ASSEMBLIES = ('CommunityToolkit.Maui.Core', 'CommunityToolkit.Maui.Camera', 'CommunityToolkit.Maui.MediaElement', 'CommunityToolkit.Maui')
# Only the four toolkit packages: CommunityToolkit.Maui.Markup is a separate package, and the Sample's own code and MAUI's Binding(String, ...) are not toolkit APIs.
TOOLKIT_API = re.compile(
    r"Using member '(CommunityToolkit\.Maui\.(?!Sample\b|DeviceTests\b|Markup\b)[^']+)' which has "
    r"'Requires(?:UnreferencedCode|DynamicCode|AssemblyFiles)Attribute'")
SOURCE_LOCATION = re.compile(r'^(?P<path>.*?)\((?P<line>\d+)(?:,(?P<column>\d+))?(?:,\d+,\d+)?\)$')
ASSEMBLY_QUOTED = re.compile(r"Assembly '([^']+)'")
SAMPLE = 'CommunityToolkit.Maui.Sample'
DEVICE_TESTS = 'CommunityToolkit.Maui.DeviceTests'


def read_lines(path):
    with open(path, encoding='utf-8', errors='replace') as source:
        return list(source)


def toolkit_string_index(packages_dirs):
    """assembly name -> bytes of the toolkit DLL in the packages folders. The metadata string heap shares tails, so a type name is looked up
    as 'Name' + NUL, which also matches the tail of a longer name."""
    index = {}
    for root in packages_dirs:
        for directory, _, files in os.walk(root):
            if '/lib/' not in directory.replace(os.sep, '/') + '/':
                continue
            for name in TOOLKIT_ASSEMBLIES:
                if name + '.dll' in files and name not in index:
                    with open(os.path.join(directory, name + '.dll'), 'rb') as dll:
                        index[name] = dll.read()
    return index


EMBEDDED_NAMESPACES = ('System.Runtime.CompilerServices', 'Microsoft.CodeAnalysis')
# Every assembly has these synthesized types, so a member of another assembly's copy would land on the toolkit.
SYNTHESIZED_TYPE_PREFIXES = ('<Module>', '<PrivateImplementationDetails>', '<>')
ARITY_SUFFIX = re.compile(r'`\d+$')
# The generic argument list that ILLink appends to a file-local type '<F>F..__Gen<T>'; a lone '<.ctor>' has nothing before its '<'.
GENERIC_LIST = re.compile(r'(?<=[^<>])<[^<>]*(?:<[^<>]*>[^<>]*)*>$')
TOP_LEVEL_VISIBILITIES = (0, 1)
STRINGS_HEAP_WIDE, GUID_HEAP_WIDE, EXTRA_DATA = 0x01, 0x02, 0x40
# ECMA-335 table numbers and the coded indexes that the reader needs to step over Module and TypeRef to reach TypeDef.
MODULE, TYPE_REF, TYPE_DEF, FIELD, METHOD_DEF, MODULE_REF, ASSEMBLY_REF, TYPE_SPEC = 0x00, 0x01, 0x02, 0x04, 0x06, 0x1A, 0x23, 0x1B


def number_at(data, offset, width):
    return int.from_bytes(data[offset:offset + width], 'little')


def metadata_streams(dll):
    """name -> (offset, size) of the streams of the metadata root of a .NET assembly; raises on anything else."""
    pe = number_at(dll, 0x3C, 4)
    if dll[pe:pe + 4] != b'PE\0\0':
        raise ValueError('not a PE file')
    optional = pe + 24
    directories = optional + (96 if number_at(dll, optional, 2) == 0x10B else 112)
    first_section = optional + number_at(dll, pe + 20, 2)
    sections = [(number_at(dll, entry + 12, 4), number_at(dll, entry + 16, 4), number_at(dll, entry + 20, 4))
                for entry in (first_section + 40 * index for index in range(number_at(dll, pe + 6, 2)))]

    def offset_of(rva):
        for virtual_address, raw_size, raw_pointer in sections:
            if virtual_address <= rva < virtual_address + raw_size:
                return rva - virtual_address + raw_pointer
        raise ValueError('RVA %#x is in no section' % rva)
    root = offset_of(number_at(dll, offset_of(number_at(dll, directories + 14 * 8, 4)) + 8, 4))
    if number_at(dll, root, 4) != 0x424A5342:
        raise ValueError('no metadata root')
    position = root + 20 + number_at(dll, root + 12, 4)
    streams = {}
    for _ in range(number_at(dll, position - 2, 2)):
        end = dll.index(b'\0', position + 8)
        streams[dll[position + 8:end].decode('ascii')] = (root + number_at(dll, position, 4), number_at(dll, position + 4, 4))
        position += 8 + ((end - position - 8 + 4) & ~3)
    return streams


def top_level_type_names(dll):
    """Full names of the top-level types in the TypeDef table (not the string heap, which also holds the names of referenced types).
    Generic arity is dropped. Nested types are left out: a member of one starts with the name of its top-level type."""
    streams = metadata_streams(dll)
    tables, _ = streams['#~']
    strings, _ = streams['#Strings']
    heap_sizes = dll[tables + 6]
    valid = number_at(dll, tables + 8, 8)
    position = tables + 24
    rows = {}
    for table in range(64):
        if valid >> table & 1:
            rows[table] = number_at(dll, position, 4)
            position += 4
    if heap_sizes & EXTRA_DATA:
        position += 4

    def coded_width(*coded_tables):
        return 4 if max(rows.get(table, 0) for table in coded_tables) >= 1 << 14 else 2

    string_width = 4 if heap_sizes & STRINGS_HEAP_WIDE else 2
    guid_width = 4 if heap_sizes & GUID_HEAP_WIDE else 2
    module_row = 2 + string_width + 3 * guid_width
    type_ref_row = coded_width(MODULE, MODULE_REF, ASSEMBLY_REF, TYPE_REF) + 2 * string_width
    extends_width = coded_width(TYPE_DEF, TYPE_REF, TYPE_SPEC)
    field_width = 4 if rows.get(FIELD, 0) >= 1 << 16 else 2
    method_width = 4 if rows.get(METHOD_DEF, 0) >= 1 << 16 else 2
    type_def_row = 4 + 2 * string_width + extends_width + field_width + method_width
    position += rows.get(MODULE, 0) * module_row + rows.get(TYPE_REF, 0) * type_ref_row

    def string_at(index):
        start = strings + index
        return dll[start:dll.index(b'\0', start)].decode('utf-8')
    names = set()
    for row in range(rows.get(TYPE_DEF, 0)):
        start = position + row * type_def_row
        if number_at(dll, start, 4) & 0x7 not in TOP_LEVEL_VISIBILITIES:
            continue
        name = ARITY_SUFFIX.sub('', string_at(number_at(dll, start + 4, string_width)))
        namespace = string_at(number_at(dll, start + 4 + string_width, string_width))
        if not name or name.startswith(SYNTHESIZED_TYPE_PREFIXES) or any(namespace == embedded or namespace.startswith(embedded + '.') for embedded in EMBEDDED_NAMESPACES):
            continue
        names.add(namespace + '.' + name if namespace else name)
    return names


def toolkit_type_index(packages_dirs):
    """(type full name -> toolkit assembly, DLL path -> number of top-level types it defines) over every target framework of the four DLLs.
    A DLL that cannot be read counts 0, and summarize and the gate treat that as a broken reader."""
    types, counts = {}, {}
    for root in packages_dirs:
        for directory, _, files in os.walk(root):
            if '/lib/' not in directory.replace(os.sep, '/') + '/':
                continue
            for name in TOOLKIT_ASSEMBLIES:
                if name + '.dll' not in files:
                    continue
                path = os.path.join(directory, name + '.dll')
                try:
                    with open(path, 'rb') as dll:
                        defined = top_level_type_names(dll.read())
                except (OSError, ValueError, KeyError, IndexError):
                    defined = set()
                counts[os.path.relpath(path, root).replace(os.sep, '/')] = len(defined)
                for type_name in defined:
                    types.setdefault(type_name, name)
    return types, counts


def member_type_prefixes(member):
    """Candidate declaring-type names of an ILLink or ILC member display name, shortest first.
    Split at '.' outside <> and (); a generic argument list (ILLink) or arity (ILC) goes, except on compiler names that start with '<'."""
    segments, depth, start = [], 0, 0
    text = strip_arguments(member)
    for position, char in enumerate(text):
        depth += char == '<'
        depth -= char == '>'
        if char == '.' and depth == 0:
            segments.append(text[start:position])
            start = position + 1
    segments.append(text[start:])
    segments = [GENERIC_LIST.sub('', segment) if segment.startswith('<') else segment.split('<')[0] for segment in segments]
    segments = [ARITY_SUFFIX.sub('', segment) for segment in segments]
    return ['.'.join(segments[:count]) for count in range(1, len(segments) + 1)]


def defining_toolkit_assembly(member, types):
    return next((types[prefix] for prefix in member_type_prefixes(member) if prefix in types), None)


def strip_arguments(member):
    depth, cut = 0, len(member)
    for position, char in enumerate(member):
        if char == '<':
            depth += 1
        elif char == '>':
            depth -= 1
        elif char == '(' and depth == 0:
            cut = position
            break
    return member[:cut]


def toolkit_assembly_of(member, location, index):
    """Best effort: a source path names its project; else the type names are looked up in the DLLs' strings; else the namespace decides."""
    from_path = re.search(r'src/(CommunityToolkit\.Maui[A-Za-z.]*)/', location)
    if from_path and from_path.group(1) in TOOLKIT_ASSEMBLIES:
        return from_path.group(1)
    parts = [part.split('<')[0].encode() for part in strip_arguments(member).split('.')[3:]]

    def rank(names):
        return sorted(((sum(1 for n in names if n + b'\0' in strings or n.split(b'`')[0] + b'\0' in strings), len(name), name)
                       for name, strings in index.items()), reverse=True)
    scored = rank(parts[:-1])
    if not scored or scored[0][0] == 0:
        scored = rank(parts)
    if scored and scored[0][0] > 0 and (len(scored) == 1 or scored[0][0] > scored[1][0] or scored[0][1] > scored[1][1]):
        return scored[0][2]
    return 'CommunityToolkit.Maui.?'


# Only the project's own obj folder or a non-obj source counts, so a clone under .../src/CommunityToolkit.Maui does not make the Sample's obj copy a toolkit one.
TOOLKIT_SOURCE = re.compile(r'/src/CommunityToolkit\.Maui(?:\.Core|\.Camera|\.MediaElement)?/(?:obj/|(?!.*/obj/))')
# Roslyn names a generated file <base>/<generator assembly>/<generator type>/<hint>, so these folders hold code that the toolkit's own generators emit into an app.
TOOLKIT_GENERATOR_FOLDERS = ('/CommunityToolkit.Maui.SourceGenerators/', '/CommunityToolkit.Maui.MediaElement.SourceGenerators/')
TOOLKIT_GENERATOR_ASSEMBLIES = tuple(folder.strip('/') for folder in TOOLKIT_GENERATOR_FOLDERS)


def toolkit_generator_of(source_path):
    """The toolkit generator assembly whose folder holds this path, or None."""
    for folder, assembly in zip(TOOLKIT_GENERATOR_FOLDERS, TOOLKIT_GENERATOR_ASSEMBLIES):
        if folder in '/' + source_path:
            return assembly
    return None


def generated_by_toolkit(source_path):
    return toolkit_generator_of(source_path) is not None


def compiled_elsewhere(source_path):
    # The toolkit DLLs embed their PDB, so ILLink and ILC name a toolkit member's own source file. The Sample, MAUI and Markup define
    # some of the same generated type names (GeneratedBindingInterceptors) and print their own paths.
    return bool(source_path) and not TOOLKIT_SOURCE.search('/' + source_path)


def assembly_of(member, location, index, types, source_path=''):
    # The generators emit into the app's own types too, so this runs before the Sample and device-test prefixes.
    generator = toolkit_generator_of(source_path)
    if generator and compiled_elsewhere(source_path):
        return generator
    if member.startswith(SAMPLE):
        return SAMPLE
    if member.startswith(DEVICE_TESTS):
        return DEVICE_TESTS
    if member.startswith('CommunityToolkit.Maui.Markup'):
        return 'CommunityToolkit.Maui.Markup'
    if member.startswith('CommunityToolkit.Maui'):
        return toolkit_assembly_of(member, location, index)
    defining = defining_toolkit_assembly(member, types)
    if defining and not compiled_elsewhere(source_path):
        return defining
    if member.startswith('Microsoft.Maui'):
        return 'Microsoft.Maui'
    if member.startswith(('Android.', 'Java.', 'AndroidX.', 'Google.')):
        return 'Android bindings'
    if member.startswith(('UIKit.', 'Foundation.', 'ObjCRuntime.', 'AVFoundation.', 'CoreFoundation.')):
        return 'Apple bindings'
    if member.startswith(('System.', 'Microsoft.')):
        return 'System'
    return member.split('.')[0]


def bucket_of(assembly, calls_toolkit_api):
    if assembly in (SAMPLE, DEVICE_TESTS):
        return 'toolkit-api' if calls_toolkit_api else 'sample'
    if assembly in TOOLKIT_ASSEMBLIES or assembly == 'CommunityToolkit.Maui.?' or assembly in TOOLKIT_GENERATOR_ASSEMBLIES:
        return 'toolkit'
    if assembly == 'Microsoft.Maui':
        return 'maui'
    return 'other'


def split_origin(text):
    """'Origin.Member(Args): message'; the member's own signature can hold ': ' only inside brackets."""
    depth = 0
    for position, char in enumerate(text):
        depth += char in '(<['
        depth -= char in ')>]'
        if depth == 0 and text.startswith(': ', position):
            return text[:position], text[position + 2:]
    return '', text


def parse(path, index, types):
    rows = set()
    if not os.path.exists(path):
        return []
    for raw in read_lines(path):
        match = LINE.match(raw)
        if not match:
            continue
        code, text, loc = match.group('code'), match.group('text'), match.group('loc').strip().replace('\\', '/')
        kind = match.group('tool')
        located = SOURCE_LOCATION.match(loc)
        location = ''
        if located:
            location = '%s:%s' % (re.sub(r'^.*/(samples|src)/', r'\1/', located.group('path')), located.group('line'))
        elif loc.endswith(('.dll', '.cs')):
            location = re.sub(r'^.*/(samples|src)/', r'\1/', loc)
        location = re.sub(r'^.*?/obj/', 'obj/', location)
        calls_toolkit_api = TOOLKIT_API.search(text) is not None

        if kind is None:
            tool, member, message = 'analyzer', '', text
            source = location.split(':')[0]
            generator = toolkit_generator_of(located.group('path') if located else '')
            if generator:
                assembly = generator
            elif source.startswith('samples/') or '/Microsoft.Maui.Controls.SourceGen/' in source:
                assembly = SAMPLE
            elif source.startswith('src/' + DEVICE_TESTS):
                assembly = DEVICE_TESTS
            else:
                assembly = toolkit_assembly_of('', location, index) if source.startswith('src/CommunityToolkit.Maui') else 'other'
        else:
            # ILC prints 'ILC : ' or file(line) without a column; ILLink prints 'ILLink : ' or file(line,column).
            tool = 'ilc' if re.search(r'(?i)\bilc\b', loc) or (located and not located.group('column')) else 'illink'
            member, message = split_origin(text)
            if member:
                assembly = assembly_of(member, location, index, types, located.group('path') if located else '')
            else:
                quoted = ASSEMBLY_QUOTED.search(text)
                assembly = quoted.group(1) if quoted else 'unknown'
        rows.add((bucket_of(assembly, calls_toolkit_api), tool, code, assembly, member, location, message))
    return sorted(rows)


TYPE_INDEX_FAILURE = 'TYPE-INDEX=FAIL: no top-level types could be read from %s (the metadata reader is broken, the type index is missing or the toolkit DLLs were not found, so generated toolkit code would pass unseen)'
IL_LINE = re.compile(r'warning IL[234]\d{3}')


def il_line_count(log):
    if not os.path.exists(log):
        return 0
    return sum(1 for raw in read_lines(log) if IL_LINE.search(raw))


def unparsed_il_lines(log):
    """IL warning lines of the log that LINE cannot read: an SDK format change would otherwise drop them silently."""
    if not os.path.exists(log):
        return []
    return [raw.strip() for raw in read_lines(log) if IL_LINE.search(raw) and not LINE.match(raw)]


def first_unparsed(unparsed):
    return ' | '.join(line[:200] for line in unparsed[:3])


def summarize(log, out_dir, packages_dirs):
    types, type_counts = toolkit_type_index(packages_dirs)
    rows = parse(log, toolkit_string_index(packages_dirs), types)
    lines_in_log = il_line_count(log)
    unparsed = unparsed_il_lines(log)
    parser_broken = bool(unparsed) or (lines_in_log > 0 and not rows)
    no_dll_found = [] if type_counts else ['(no toolkit DLL found in %s)' % (', '.join(packages_dirs) or 'no packages folder')]
    empty_type_indexes = [path for path, count in sorted(type_counts.items()) if count == 0] or no_dll_found
    with open(os.path.join(out_dir, 'warnings-unique.tsv'), 'w', encoding='utf-8') as tsv:
        for row in rows:
            tsv.write('\t'.join(field.replace('\t', ' ') for field in row) + '\n')
    by_bucket = collections.Counter(r[0] for r in rows)
    by_assembly = collections.Counter((r[0], r[3]) for r in rows)
    by_code = collections.Counter((r[0], r[1], r[2]) for r in rows)
    with open(os.path.join(out_dir, 'warnings-counts.json'), 'w', encoding='utf-8') as counts_file:
        json.dump({'distinct': len(rows), 'by_bucket': dict(by_bucket), 'toolkit_type_counts': type_counts,
                   'by_origin_assembly': {'%s | %s' % key: value for key, value in sorted(by_assembly.items())}}, counts_file, indent=1)
    lines = ['distinct IL warnings: %d (log: %s)' % (len(rows), log), '', 'bucket        count']
    lines += ['%-13s %5d' % item for item in sorted(by_bucket.items())]
    lines += ['', 'bucket        tool     code    count']
    lines += ['%-13s %-8s %-7s %5d' % (b, t, c, n) for (b, t, c), n in sorted(by_code.items())]
    lines += ['', 'bucket        origin assembly                              count']
    lines += ['%-13s %-44s %5d' % (b, a, n) for (b, a), n in sorted(by_assembly.items())]
    for wanted in ('toolkit', 'toolkit-api'):
        picked = [r for r in rows if r[0] == wanted]
        lines += ['', '== %s (%d)' % (wanted, len(picked))]
        lines += ['%s %s %s %s [%s]: %s' % (t, c, a, m or '-', loc or '-', msg) for _, t, c, a, m, loc, msg in picked]
    if parser_broken:
        lines += ['', 'PARSER-SANITY=FAIL: %d of %d IL warning lines in the log could not be parsed (first 3: %s)'
                  % (len(unparsed) or lines_in_log, lines_in_log, first_unparsed(unparsed) or 'all of them')]
    if empty_type_indexes:
        lines += ['', TYPE_INDEX_FAILURE % ', '.join(empty_type_indexes)]
    lines += ['', 'toolkit types indexed per DLL: ' + ', '.join('%s %d' % item for item in sorted(type_counts.items()))] if type_counts else []
    text = '\n'.join(lines) + '\n'
    with open(os.path.join(out_dir, 'warnings-summary.txt'), 'w', encoding='utf-8') as summary_file:
        summary_file.write(text)
    sys.stdout.write(text)
    return 4 if parser_broken or empty_type_indexes else 0


def diff(a_path, b_path):
    def load(path):
        with open(path, encoding='utf-8') as source:
            return {tuple(line.rstrip('\n').split('\t')) for line in source if line.strip()}
    a, b = load(a_path), load(b_path)
    for title, rows in (('only in ' + a_path, a - b), ('only in ' + b_path, b - a)):
        picked = sorted(r for r in rows if r[0] in ('toolkit', 'toolkit-api', 'sample'))
        print('== %s: %d toolkit/sample warnings' % (title, len(picked)))
        for bucket, tool, code, assembly, member, _, message in picked:
            print('%s %s %s %s %s: %s' % (bucket, tool, code, assembly, member, message))


def load_allow_list(path):
    entries = []
    for number, line in enumerate(read_lines(path), 1):
        if not line.strip() or line.lstrip().startswith('#'):
            continue
        fields = line.rstrip('\n').split('\t')
        if not 3 <= len(fields) <= 4 or not fields[2].strip():
            raise ValueError('%s:%d: an allow-list line is code<TAB>member pattern<TAB>reason[<TAB>scope] (the reason is required)' % (path, number))
        scope = fields[3].strip() if len(fields) == 4 else ''
        if scope:
            check_scope(scope, path, number)
        entries.append((fields[0].strip(), fields[1].strip(), fields[2].strip(), scope))
    return entries


KNOWN_LEGS = (['%s-%s' % (platform, variant) for platform in ('android', 'ios', 'maccatalyst')
               for variant in ('none', 'partial', 'full', 'rootlibs', 'aot', 'aotroot')]
              + ['windows-%s' % variant for variant in ('none', 'full', 'rootlibs', 'aot')])


def check_scope(scope, path, number):
    globs = [glob.strip() for glob in scope.split(',') if glob.strip()]
    if not globs:
        raise ValueError('%s:%d: the scope %r names no leg (comma separated globs over <platform>-<variant>)' % (path, number, scope))
    for glob in globs:
        if not any(fnmatch.fnmatchcase(leg, glob) for leg in KNOWN_LEGS):
            raise ValueError('%s:%d: the scope glob %r matches no leg (%s)' % (path, number, glob, ', '.join(KNOWN_LEGS)))


GATED_BUCKETS = ('toolkit', 'toolkit-api')
DEFAULT_MAX_ANNOTATIONS = 10
GITHUB_ANNOTATIONS_PER_STEP = 10
MAX_SUMMARY_ROWS = 200


def gate_subject(bucket, member, location, message):
    if bucket == 'toolkit-api':
        called = TOOLKIT_API.search(message)
        if called:
            return called.group(1)
    return member or location


def entry_applies_on_leg(scope, leg):
    """An entry without a scope applies everywhere, and so does every entry when the leg is unknown."""
    if not scope or not leg:
        return True
    return any(fnmatch.fnmatchcase(leg, glob.strip()) for glob in scope.split(',') if glob.strip())


def escape_annotation(text):
    return text.replace('%', '%25').replace('\r', '%0D').replace('\n', '%0A')


def escape_property(text):
    return escape_annotation(text).replace(':', '%3A').replace(',', '%2C')


def repo_file_and_line(location):
    file, _, line = location.rpartition(':')
    return (file, line) if file.startswith(('src/', 'samples/')) and line.isdigit() else (None, None)


def where_listed(total):
    if total <= MAX_SUMMARY_ROWS:
        return 'the step log and the job summary list all %d' % total
    return 'the step log lists all %d, the job summary the first %d' % (total, MAX_SUMMARY_ROWS)


def print_annotations(new, stale, level='error', max_annotations=DEFAULT_MAX_ANNOTATIONS):
    # The verdict comes first and is one of the budget: GitHub keeps only the first annotations of a job, so the one that says why the step failed must not be last.
    per_warning = max_annotations
    verdict_printed = bool(new) and max_annotations > 0
    if verdict_printed:
        print('::%s title=%s::%s' % (level, escape_property('Trim and AOT gate'),
                                     escape_annotation('GATE: %s, %d toolkit and toolkit-api warnings are not allow-listed (listed below, in the step log and in the job summary)'
                                                       % ('REPORT ONLY' if level == 'warning' else 'FAIL', len(new)))))
        per_warning -= 1
    for bucket, tool, code, assembly, subject, location, message in new[:per_warning]:
        file, line = repo_file_and_line(location)
        where = 'file=%s,line=%s,' % (escape_property(file), line) if file else ''
        print('::%s %stitle=%s::%s' % (level, where, escape_property('%s (%s %s)' % (code, bucket, tool)),
                                          escape_annotation('%s [%s]: %s' % (subject, location or '-', message))))
    if len(new) > per_warning:
        print('::warning::%d more new warnings are not annotated (GitHub shows %d annotations per step, this gate annotates %d%s); %s'
              % (len(new) - per_warning, GITHUB_ANNOTATIONS_PER_STEP, max_annotations, ', the first is its verdict' if verdict_printed else '', where_listed(len(new))))
    for a_code, pattern, reason, _ in stale:
        print('::warning::%s' % escape_annotation('stale allow-list entry on this leg (matches nothing here; delete it when no leg needs it): %s %s -- %s'
                                                  % (a_code, pattern, reason)))


def code_span(text):
    """Double backticks with padding: ILC spells generic arity with a backtick (Name`2), which closes a single-backtick span."""
    return '`` %s ``' % text


def leg_name(result_path):
    """'<platform>-<variant>' from result.json, so that two gates in one job summary can be told apart; '' when it cannot be read."""
    if result_path is None:
        return ''
    try:
        with open(result_path, encoding='utf-8') as source:
            result = json.load(source)
        return '%s-%s' % (result['platform'], result['variant'])
    except (OSError, ValueError, KeyError, TypeError):
        return ''


def more_rows_line(total):
    return ['', '... and %d more (the step log lists every one)' % (total - MAX_SUMMARY_ROWS)] if total > MAX_SUMMARY_ROWS else []


def write_summary(path, rows, new, stale, verdict, leg=''):
    counts = collections.Counter(r[0] for r in rows)
    lines = ['### Trim and AOT gate%s: %s' % (' (%s)' % leg if leg else '', verdict), '', '| bucket | warnings |', '|---|---|']
    lines += ['| %s | %d |' % item for item in sorted(counts.items())] or ['| none | 0 |']
    if new:
        lines += ['', '**New toolkit and toolkit-api warnings (%d)**' % len(new), '']
        lines += ['- %s %s %s %s at %s: %s' % (code_span(code), bucket, tool, code_span(subject), code_span(location or '-'), message)
                  for bucket, tool, code, _, subject, location, message in new[:MAX_SUMMARY_ROWS]]
        lines += more_rows_line(len(new))
    if stale:
        lines += ['', '**Stale allow-list entries on this leg (delete them when no leg needs them)**', '']
        lines += ['- %s %s: %s' % (code_span(a_code), code_span(pattern), reason) for a_code, pattern, reason, _ in stale[:MAX_SUMMARY_ROWS]]
        lines += more_rows_line(len(stale))
    with open(path, 'a', encoding='utf-8') as out:
        out.write('\n'.join(lines) + '\n')


def read_result(result_path):
    """(result dict, None) or (None, why not)."""
    try:
        with open(result_path, encoding='utf-8') as source:
            result = json.load(source)
    except (OSError, ValueError) as error:
        return None, 'GATE: %s is missing or unreadable (%s): the leg did not finish' % (result_path, error)
    if not isinstance(result, dict):
        return None, 'GATE: %s is not a JSON object' % result_path
    if result.get('build_exit') != 0:
        return None, 'GATE: the build of this leg failed (build_exit %s in %s): a failed build must not pass as a clean one' % (result.get('build_exit'), result_path)
    return result, None


def roots_check_failure(result, result_path):
    if result.get('variant') != 'aotroot':
        return None
    roots_path = os.path.join(os.path.dirname(result_path), 'roots-check.txt')
    text = ''.join(read_lines(roots_path)) if os.path.exists(roots_path) else ''
    if 'ROOTS-CHECK=OK' not in text:
        return 'GATE: roots check is not OK (%s): ILC did not analyze all toolkit assemblies' % roots_path
    return None


def split_by_allow_list(rows, allow, leg=''):
    """(new gated warnings as gate tuples, indexes of the in-scope allow-list entries that matched)."""
    used = set()
    new = []
    for bucket, tool, code, assembly, member, location, message in (r + [''] * (7 - len(r)) for r in rows):
        if bucket not in GATED_BUCKETS:
            continue
        subject = gate_subject(bucket, member, location, message)
        hit = next((i for i, (a_code, pattern, _, scope) in enumerate(allow)
                    if entry_applies_on_leg(scope, leg) and a_code in (code, '*') and fnmatch.fnmatchcase(subject, pattern)), None)
        if hit is None:
            new.append((bucket, tool, code, assembly, subject, location or '-', message))
        else:
            used.add(hit)
    return new, used


def parsed_warnings_of_failed_leg(tsv, allow_path, leg=''):
    """(rows, new) for the summary of a report-only leg that failed; ([], []) when the list is missing or unreadable."""
    try:
        rows = [line.rstrip('\n').split('\t') for line in read_lines(tsv) if line.strip()]
        allow = load_allow_list(allow_path) if allow_path else []
    except (OSError, ValueError):
        return [], []
    return rows, split_by_allow_list(rows, allow, leg)[0]


def toolkit_dlls_without_types(counts_path):
    try:
        with open(counts_path, encoding='utf-8') as source:
            counts = json.load(source).get('toolkit_type_counts', {})
        return sorted(path for path, count in counts.items() if count == 0) or ([] if counts else ['(no toolkit DLL was indexed)'])
    except (OSError, ValueError, AttributeError):
        return ['(%s is missing or unreadable)' % counts_path]


def gate(tsv, allow_path, log, assets_check, github=False, summary=None, result=None, report_only=False, max_annotations=DEFAULT_MAX_ANNOTATIONS):
    leg = leg_name(result)
    level = 'warning' if report_only else 'error'

    def fail(code, message):
        print(message)
        if github:
            print('::%s::%s' % (level, escape_annotation(message)))
        if summary:
            rows, listed = parsed_warnings_of_failed_leg(tsv, allow_path, leg) if report_only and code != 2 else ([], [])
            write_summary(summary, rows, listed, [], '%s (exit %d): %s' % ('REPORT ONLY, did not finish' if report_only else 'FAIL', code, message), leg)
        return 0 if report_only and code != 2 else code

    if result is not None:
        parsed, problem = read_result(result)
        if problem:
            return fail(4, problem)
        problem = roots_check_failure(parsed, result)
        if problem:
            return fail(3, problem)
    if assets_check is not None:
        text = ''.join(read_lines(assets_check)) if os.path.exists(assets_check) else ''
        if 'ASSETS-CHECK=OK' not in text:
            return fail(3, 'GATE: assets check is not OK (%s): the build did not use the feed packages' % assets_check)
    if not os.path.exists(tsv):
        return fail(4, 'GATE: %s does not exist: the build produced no warning list' % tsv)
    empty_type_indexes = toolkit_dlls_without_types(os.path.join(os.path.dirname(tsv), 'warnings-counts.json'))
    if empty_type_indexes:
        return fail(4, 'GATE: ' + TYPE_INDEX_FAILURE % ', '.join(empty_type_indexes))
    rows = [line.rstrip('\n').split('\t') for line in read_lines(tsv) if line.strip()]
    if log is not None:
        if not os.path.exists(log):
            return fail(4, 'GATE: %s does not exist: the build produced no warning log' % log)
        unparsed = unparsed_il_lines(log)
        if unparsed:
            return fail(4, 'GATE: the parser could not read %d IL warning lines of %s (first 3: %s)' % (len(unparsed), log, first_unparsed(unparsed)))
        if il_line_count(log) > 0 and not rows:
            return fail(4, 'GATE: %s has IL warning lines but %s has no rows: the parser is broken' % (log, tsv))
    try:
        allow = load_allow_list(allow_path) if allow_path else []
    except (OSError, ValueError) as error:
        return fail(2, 'GATE: the allow-list is unusable: %s' % error)
    new, used = split_by_allow_list(rows, allow, leg)
    counts = collections.Counter(r[0] for r in rows)
    gated = sum(counts.get(b, 0) for b in GATED_BUCKETS)
    print('GATE: %d warnings (%s); %d toolkit and toolkit-api warnings allow-listed by %d entries'
          % (len(rows), ', '.join('%s %d' % item for item in sorted(counts.items())) or 'none', gated - len(new), len(used)))
    stale = [entry for index, entry in enumerate(allow) if index not in used and entry_applies_on_leg(entry[3], leg)]
    for a_code, pattern, reason, _ in stale:
        print('GATE: stale allow-list entry on this leg (matches nothing here): %s %s -- %s' % (a_code, pattern, reason))
    if new and report_only:
        print('GATE: REPORT ONLY, %d toolkit and toolkit-api warnings that are not allow-listed (not failing this leg):' % len(new))
        for bucket, tool, code, assembly, subject, location, message in new:
            print('  %s %s %s %s %s [%s]: %s' % (bucket, tool, code, assembly, subject, location, message))
    elif new:
        print('GATE: FAIL, %d toolkit and toolkit-api warnings that are not allow-listed:' % len(new))
        for bucket, tool, code, assembly, subject, location, message in new:
            print('  %s %s %s %s %s [%s]: %s' % (bucket, tool, code, assembly, subject, location, message))
    else:
        print('GATE: OK')
    if github:
        print_annotations(new, stale, level, max_annotations)
    if summary:
        write_summary(summary, rows, new, stale, ('REPORT ONLY' if report_only else 'FAIL') if new else 'OK', leg)
    return 1 if new and not report_only else 0


def usage():
    sys.stderr.write(__doc__ + '\n')
    sys.exit(2)


if __name__ == '__main__':
    if len(sys.argv) >= 4 and sys.argv[1] == 'summarize':
        sys.exit(summarize(sys.argv[2], sys.argv[3], sys.argv[4:]))
    elif len(sys.argv) == 4 and sys.argv[1] == 'diff':
        diff(sys.argv[2], sys.argv[3])
    elif len(sys.argv) >= 3 and sys.argv[1] == 'gate':
        options = {'--allow': None, '--log': None, '--assets-check': None, '--summary': None, '--result': None,
                   '--max-annotations': str(DEFAULT_MAX_ANNOTATIONS)}
        flags = {'--github': False, '--report-only': False}
        rest = sys.argv[3:]
        position = 0
        while position < len(rest):
            if rest[position] in flags:
                flags[rest[position]] = True
                position += 1
            elif rest[position] in options and position + 1 < len(rest):
                options[rest[position]] = rest[position + 1]
                position += 2
            else:
                usage()
        if not options['--max-annotations'].isdigit():
            usage()
        sys.exit(gate(sys.argv[2], options['--allow'], options['--log'], options['--assets-check'], flags['--github'], options['--summary'], options['--result'],
                      flags['--report-only'], int(options['--max-annotations'])))
    else:
        usage()
