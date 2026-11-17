"""Self-test of ilwarn.py on fixture logs. Run from the repo root (python -I ignores the working folder, so use discover):

    python3 -I -m unittest discover -s eng/trim-audit -p 'test_*.py'

Run it under tr_TR.UTF-8 and C locales: the parser and the gate handle build output, not user text, so neither may depend on the
locale (Turkish casing turns I into a dotless i and would split a member name from its pattern)."""
import collections
import contextlib
import io
import json
import os
import runpy
import struct
import tempfile
import unittest
import unittest.mock

import ilwarn

PREFIX = '     1>/_/'
ISINRANGE_ILC = (PREFIX + "src/CommunityToolkit.Maui/obj/Release/net11.0-ios/CommunityToolkit.Maui.SourceGenerators/CommunityToolkit.Maui.SourceGenerators.BindablePropertyAttributeSourceGenerator/IsInRangeConverter.g.cs(11): Trim analysis warning IL2087: "
                 "CommunityToolkit.Maui.Converters.IsInRangeConverter`2..cctor(): 'returnType' argument does not satisfy "
                 "'DynamicallyAccessedMemberTypes.PublicParameterlessConstructor' in call to 'BindableProperty.Create'. [/_/Sample.csproj]")
ISINRANGE_ILLINK = (PREFIX + "src/CommunityToolkit.Maui/obj/Release/net11.0-android/CommunityToolkit.Maui.SourceGenerators/CommunityToolkit.Maui.SourceGenerators.BindablePropertyAttributeSourceGenerator/IsInRangeConverter.g.cs(11,5): Trim analysis warning IL2087: "
                    "CommunityToolkit.Maui.Converters.IsInRangeConverter<TValue,TReturnObject>..cctor(): 'returnType' argument does not satisfy "
                    "'DynamicallyAccessedMemberTypes.PublicParameterlessConstructor' in call to 'BindableProperty.Create'. [/_/Sample.csproj]")
TOOLKIT_API_ROW = ("     1>/repo/obj/Microsoft.Maui.Controls.SourceGen/Pages_EmailValidationBehaviorPage.xaml.xsg.cs(128,36): warning IL2026: "
                   "Using member 'CommunityToolkit.Maui.Behaviors.ValidationBehavior.FlagsProperty' which has 'RequiresUnreferencedCodeAttribute' "
                   "can break functionality when trimming application code. Use it from code. [/_/Sample.csproj]")
MARKUP_ROW = (PREFIX + "src/CommunityToolkit.Maui.Markup/TypedBindingsExtensions.Handlers.cs(296,5): Trim analysis warning IL2026: "
              "CommunityToolkit.Maui.Markup.TypedBindingExtensions.ApplyBinding(): Using member 'Microsoft.Maui.Controls.Binding.Binding(String)' "
              "which has 'RequiresUnreferencedCodeAttribute' can break functionality. [/_/Markup.csproj]")
SAMPLE_ROW = ("     1>/repo/samples/CommunityToolkit.Maui.Sample/Pages/TouchPage.cs(70,15): warning IL2026: Using member "
              "'CommunityToolkit.Maui.Markup.BindableObjectExtensions.Bind<TBindable>(TBindable)' which has 'RequiresUnreferencedCodeAttribute' "
              "can break functionality. [/_/Sample.csproj]")
MAUI_ROW = ("ILC : Trim analysis warning IL2026: Microsoft.Maui.Controls.Thing.Run(): Using member 'System.Type.GetType(String)' "
            "which has 'RequiresUnreferencedCodeAttribute' can break functionality.")


FIXTURES = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'fixtures')
IL3002_SAMPLE_ROW = ("     1>/repo/samples/CommunityToolkit.Maui.Sample/Pages/FilePage.cs(10,5): warning IL3002: Using member "
                     "'CommunityToolkit.Maui.Storage.Thing.Save()' which has 'RequiresAssemblyFilesAttribute' can break functionality "
                     "when embedded in a single-file app. [/_/Sample.csproj]")


def turkish_member_row(name):
    return (PREFIX + "src/CommunityToolkit.Maui/obj/Gen/X.g.cs(1,1): Trim analysis warning IL2087: "
            "CommunityToolkit.Maui.Converters.%s`2..cctor(): message. [/_/Sample.csproj]" % name)


class Workspace(unittest.TestCase):
    def setUp(self):
        self.directory = tempfile.TemporaryDirectory()
        self.addCleanup(self.directory.cleanup)

    def path(self, name):
        return os.path.join(self.directory.name, name)

    def write(self, name, text):
        with open(self.path(name), 'w', encoding='utf-8') as out:
            out.write(text)
        return self.path(name)

    def write_toolkit_dll(self, content):
        folder = self.path('packages/communitytoolkit.maui/1.0.0/lib/net11.0')
        os.makedirs(folder, exist_ok=True)
        with open(os.path.join(folder, 'CommunityToolkit.Maui.dll'), 'wb') as out:
            out.write(content)

    def summarize(self, lines):
        # A summarize without any toolkit DLL fails closed, so these tests index one DLL whose only type matches none of their members.
        self.write_toolkit_dll(metadata_only_assembly([(1, 'Unrelated.Namespace', 'Marker')]))
        log = self.write('warnings.log', '\n'.join(lines) + ('\n' if lines else ''))
        with contextlib.redirect_stdout(io.StringIO()):
            rc = ilwarn.summarize(log, self.directory.name, [self.path('packages')])
        return rc, log

    def rows(self):
        with open(self.path('warnings-unique.tsv'), encoding='utf-8') as tsv:
            return [line.rstrip('\n').split('\t') for line in tsv if line.strip()]

    def gate(self, allow='', **options):
        allow_path = self.write('allow.tsv', allow)
        output = io.StringIO()
        with contextlib.redirect_stdout(output):
            rc = ilwarn.gate(self.path('warnings-unique.tsv'), allow_path, options.pop('log', None), options.pop('assets', None), **options)
        return rc, output.getvalue()


class ParserTests(Workspace):
    def test_ilc_and_illink_rows_of_the_toolkit_are_bucket_toolkit(self):
        for line, tool in ((ISINRANGE_ILC, 'ilc'), (ISINRANGE_ILLINK, 'illink')):
            with self.subTest(tool=tool):
                self.assertEqual(self.summarize([line])[0], 0)
                (bucket, parsed_tool, code, assembly, *_), = self.rows()
                self.assertEqual((bucket, parsed_tool, code, assembly), ('toolkit', tool, 'IL2087', 'CommunityToolkit.Maui'))

    def test_xaml_call_site_of_a_toolkit_api_is_bucket_toolkit_api(self):
        self.summarize([TOOLKIT_API_ROW])
        (bucket, tool, code, assembly, *_), = self.rows()
        self.assertEqual((bucket, tool, code), ('toolkit-api', 'analyzer', 'IL2026'))
        self.assertEqual(assembly, 'CommunityToolkit.Maui.Sample')

    def test_markup_sample_and_maui_rows_never_count_as_toolkit(self):
        self.summarize([MARKUP_ROW, SAMPLE_ROW, MAUI_ROW])
        self.assertEqual(sorted(row[0] for row in self.rows()), ['maui', 'other', 'sample'])

    def test_a_warning_repeated_for_every_target_counts_once(self):
        self.summarize([ISINRANGE_ILC, ISINRANGE_ILC, '1>' + ISINRANGE_ILC])
        self.assertEqual(len(self.rows()), 1)

    def test_empty_log_is_clean_and_not_a_broken_parser(self):
        rc, log = self.summarize([])
        self.assertEqual(rc, 0)
        self.assertEqual(self.rows(), [])
        self.assertEqual(self.gate(log=log)[0], 0)

    def test_malformed_il_line_is_a_broken_parser_not_a_clean_run(self):
        rc, log = self.summarize(['this line mentions warning IL2026 but has no location or colon'])
        self.assertEqual(rc, 4)
        self.assertEqual(self.gate(log=log)[0], 4)

    def test_rows_are_sorted_by_code_point_not_by_locale(self):
        self.summarize([turkish_member_row('ısB'), turkish_member_row('İsA'), turkish_member_row('IsC')])
        members = [row[4] for row in self.rows()]
        self.assertEqual(members, sorted(members))
        self.assertEqual([m.split('.')[3][:1] for m in members], ['I', 'İ', 'ı'])

    def test_il3002_on_a_toolkit_api_is_bucket_toolkit_api(self):
        self.summarize([IL3002_SAMPLE_ROW])
        (bucket, _, code, *_), = self.rows()
        self.assertEqual((bucket, code), ('toolkit-api', 'IL3002'))

    def test_one_unreadable_il_line_among_good_ones_is_a_broken_parser(self):
        lines = [ISINRANGE_ILC, 'ILC : something warning IL3050 in a format the parser does not know', 'another warning IL2026 line']
        rc, log = self.summarize(lines)
        self.assertEqual(rc, 4)
        with open(self.path('warnings-summary.txt'), encoding='utf-8') as source:
            summary = source.read()
        self.assertIn('PARSER-SANITY=FAIL', summary)
        self.assertIn('another warning IL2026 line', summary)
        rc, output = self.gate(log=log)
        self.assertEqual(rc, 4)
        self.assertIn('could not read 2 IL warning lines', output)

    def test_dotted_and_dotless_i_in_a_member_name_do_not_change_the_bucket(self):
        self.summarize([turkish_member_row(name) for name in ('IsInRangeConverter', 'İsInRangeConverter', 'ısInRangeConverter')])
        self.assertEqual({row[0] for row in self.rows()}, {'toolkit'})


class FixtureTests(Workspace):
    """Verbatim excerpts of real audit logs (paths shortened): Android lines start with '1:2>' or '1:3>', iOS and analyzer lines with '1>'."""

    def buckets_of(self, name):
        rc, _ = self.summarize(read_fixture(name))
        self.assertEqual(rc, 0)
        rows = self.rows()
        return rows, collections.Counter(row[0] for row in rows)

    def test_android_full_illink_log(self):
        rows, counts = self.buckets_of('android-full.log')
        self.assertEqual(counts, {'toolkit': 3, 'toolkit-api': 2})
        self.assertTrue(all('IsInRangeConverter' in row[4] and row[1] == 'illink' for row in rows if row[0] == 'toolkit'))
        self.assertTrue(all('ValidationBehavior' in row[6] for row in rows if row[0] == 'toolkit-api'))

    def test_android_aot_ilc_log(self):
        rows, counts = self.buckets_of('android-aot.log')
        self.assertEqual(counts, {'toolkit': 3, 'toolkit-api': 2})
        self.assertTrue(all(row[1] == 'ilc' and '`2' in row[4] for row in rows if row[0] == 'toolkit'))

    def test_ios_aot_ilc_log(self):
        rows, counts = self.buckets_of('ios-aot.log')
        self.assertEqual(counts, {'toolkit': 3, 'toolkit-api': 2})
        self.assertEqual({row[1] for row in rows if row[0] == 'toolkit'}, {'ilc'})


def read_fixture(name):
    with open(os.path.join(FIXTURES, name), encoding='utf-8') as source:
        return source.read().splitlines()


class GateTests(Workspace):
    def gate_on(self, lines, allow='', **options):
        _, log = self.summarize(lines)
        return self.gate(allow, log=log, **options)

    def test_new_toolkit_warning_fails(self):
        rc, output = self.gate_on([ISINRANGE_ILC])
        self.assertEqual(rc, 1)
        self.assertIn('IsInRangeConverter', output)

    def test_toolkit_api_warning_fails_and_names_the_api(self):
        rc, output = self.gate_on([TOOLKIT_API_ROW])
        self.assertEqual(rc, 1)
        self.assertIn('CommunityToolkit.Maui.Behaviors.ValidationBehavior.FlagsProperty', output)

    def test_other_buckets_are_listed_never_failed(self):
        self.assertEqual(self.gate_on([MARKUP_ROW, SAMPLE_ROW, MAUI_ROW])[0], 0)

    def test_allow_list_hit_passes_and_a_stale_line_is_reported_without_failing(self):
        allow = ('IL2087\tCommunityToolkit.Maui.Converters.IsInRangeConverter*\tissue 1\n'
                 'IL2026\tCommunityToolkit.Maui.Behaviors.Gone*\tstale one\n')
        rc, output = self.gate_on([ISINRANGE_ILC], allow)
        self.assertEqual(rc, 0)
        self.assertIn('stale allow-list entry', output)
        self.assertIn('Gone', output)

    def test_allow_list_can_allow_a_toolkit_api_by_the_api_name(self):
        allow = 'IL2026\tCommunityToolkit.Maui.Behaviors.ValidationBehavior.*\tuntil the validation work lands\n'
        self.assertEqual(self.gate_on([TOOLKIT_API_ROW], allow)[0], 0)

    def test_pattern_matching_is_case_sensitive_and_ignores_the_culture(self):
        allow = 'IL2087\tCommunityToolkit.Maui.Converters.ısInRange*\tdotless i must not match I\n'
        self.assertEqual(self.gate_on([ISINRANGE_ILC], allow)[0], 1)

    def test_an_allow_line_without_a_reason_is_exit_2_and_reaches_the_summary(self):
        _, log = self.summarize([ISINRANGE_ILC])
        summary = self.path('summary.md')
        rc, output = self.gate('IL2087\t*\t\n', log=log, summary=summary, github=True)
        self.assertEqual(rc, 2)
        self.assertIn('::error::', output)
        self.assertTrue(os.path.exists(summary))

    def test_a_missing_allow_file_is_exit_2(self):
        self.summarize([])
        with contextlib.redirect_stdout(io.StringIO()):
            self.assertEqual(ilwarn.gate(self.path('warnings-unique.tsv'), self.path('nothing.tsv'), None, None), 2)

    def test_failed_assets_check_is_exit_3(self):
        self.summarize([])
        self.assertEqual(self.gate(assets=self.write('assets-check.txt', 'ASSETS-CHECK=FAIL'))[0], 3)
        self.assertEqual(self.gate(assets=self.write('assets-check.txt', 'ASSETS-CHECK=OK'))[0], 0)

    def write_result(self, **fields):
        return self.write('result.json', json.dumps({'build_exit': 0, 'variant': 'full', **fields}, indent=fields.pop('indent', 1)))

    def test_failed_build_is_exit_4_even_with_a_clean_log(self):
        self.summarize([])
        rc, output = self.gate(result=self.write_result(build_exit=1))
        self.assertEqual(rc, 4)
        self.assertIn('build of this leg failed', output)
        self.assertEqual(self.gate(result=self.write_result(build_exit=0))[0], 0)

    def test_missing_unreadable_or_incomplete_result_is_exit_4(self):
        self.summarize([])
        self.assertEqual(self.gate(result=self.path('result.json'))[0], 4)
        self.assertEqual(self.gate(result=self.write('result.json', '{not json'))[0], 4)
        self.assertEqual(self.gate(result=self.write('result.json', '{"variant": "full"}'))[0], 4)

    def test_failed_build_reaches_the_annotation_and_the_summary(self):
        self.summarize([])
        summary = self.path('summary.md')
        rc, output = self.gate(result=self.write_result(build_exit=2), github=True, summary=summary)
        self.assertEqual(rc, 4)
        self.assertIn('::error::', output)
        with open(summary, encoding='utf-8') as source:
            self.assertIn('FAIL (exit 4)', source.read())

    def test_aotroot_needs_roots_check_ok_whatever_the_json_spacing(self):
        self.summarize([])
        for indent in (1, 2, None):
            with self.subTest(indent=indent):
                result = self.write_result(variant='aotroot', indent=indent)
                if os.path.exists(self.path('roots-check.txt')):
                    os.remove(self.path('roots-check.txt'))
                self.assertEqual(self.gate(result=result)[0], 3)
                self.write('roots-check.txt', 'ROOTS-CHECK=FAIL\n')
                self.assertEqual(self.gate(result=result)[0], 3)
                self.write('roots-check.txt', 'ROOTS-CHECK=OK\n')
                self.assertEqual(self.gate(result=result)[0], 0)

    def test_the_roots_check_is_not_needed_for_other_variants(self):
        self.summarize([])
        self.assertEqual(self.gate(result=self.write_result(variant='aot'))[0], 0)

    def test_summary_shows_the_location_and_keeps_a_generic_arity_in_one_code_span(self):
        other_line = ISINRANGE_ILC.replace('IsInRangeConverter.g.cs(11)', 'IsInRangeConverter.g.cs(17)')
        summary = self.path('summary.md')
        rc, output = self.gate_on([ISINRANGE_ILC, other_line], summary=summary, github=True)
        self.assertEqual(rc, 1)
        with open(summary, encoding='utf-8') as source:
            text = source.read()
        self.assertIn('IsInRangeConverter.g.cs:11', text)
        self.assertIn('IsInRangeConverter.g.cs:17', text)
        self.assertIn('`` CommunityToolkit.Maui.Converters.IsInRangeConverter`2..cctor() ``', text)
        self.assertIn('IsInRangeConverter.g.cs:11]', output)

    def test_annotation_of_a_row_in_a_repo_file_carries_file_and_line(self):
        analyzer_row = ("     1>/_/src/CommunityToolkit.Maui/Views/Thing.cs(5,3): warning IL2026: Using member 'System.Type.GetType(String)' "
                        "which has 'RequiresUnreferencedCodeAttribute' can break functionality. [/_/x.csproj]")
        _, output = self.gate_on([analyzer_row], github=True)
        self.assertIn('::error file=src/CommunityToolkit.Maui/Views/Thing.cs,line=5,title=', output)

    def test_summary_is_appended_and_stale_entries_become_warnings_with_github(self):
        summary = self.write('summary.md', 'earlier step\n')
        _, log = self.summarize([])
        rc, output = self.gate('IL2026\tGone*\tstale\n', log=log, github=True, summary=summary)
        self.assertEqual(rc, 0)
        self.assertIn('::warning::stale allow-list entry', output)
        with open(summary, encoding='utf-8') as source:
            text = source.read()
        self.assertTrue(text.startswith('earlier step\n'))
        self.assertIn('Stale allow-list entries', text)

    def test_overflow_note_says_the_job_summary_lists_all_when_there_are_at_most_200(self):
        rc, output = self.gate_on([turkish_member_row('Converter%03d' % number) for number in range(11)], github=True)
        self.assertEqual(rc, 1)
        self.assertIn('the step log and the job summary list all 11', output)

    def test_overflow_note_says_the_job_summary_lists_the_first_200_when_there_are_more(self):
        rc, output = self.gate_on([turkish_member_row('Converter%03d' % number) for number in range(250)], github=True)
        self.assertEqual(rc, 1)
        self.assertIn('the step log lists all 250, the job summary the first 200', output)

    def test_a_scoped_allow_entry_is_stale_only_on_a_leg_in_its_scope(self):
        allow = 'IL2026\tGone*\tAndroid only\tandroid-*,ios-aot*\n'
        _, log = self.summarize([])
        for platform, variant, stale in (('android', 'full', True), ('ios', 'aot', True), ('ios', 'full', False), ('maccatalyst', 'aot', False)):
            rc, output = self.gate(allow, log=log, result=self.write_result(platform=platform, variant=variant))
            self.assertEqual(rc, 0)
            self.assertEqual('stale allow-list entry on this leg' in output, stale, '%s-%s' % (platform, variant))

    def test_a_scoped_allow_entry_does_not_allow_a_warning_on_a_leg_outside_its_scope(self):
        allow = 'IL2087\tCommunityToolkit.Maui.Converters.IsInRangeConverter*\tAndroid only\tandroid-*\n'
        self.summarize([ISINRANGE_ILC])
        self.assertEqual(self.gate(allow, result=self.write_result(platform='android', variant='full'))[0], 0)
        self.assertEqual(self.gate(allow, result=self.write_result(platform='ios', variant='full'))[0], 1)

    def test_a_scoped_allow_entry_applies_when_the_leg_is_unknown(self):
        allow = 'IL2026\tGone*\tAndroid only\tandroid-*\n'
        _, log = self.summarize([])
        self.assertIn('stale allow-list entry on this leg', self.gate(allow, log=log)[1])

    def test_a_scope_with_a_dotless_i_matches_no_leg_whatever_the_culture_and_is_exit_2(self):
        allow = 'IL2026\tGone*\tdotless i must not match\tıos-*\n'
        _, log = self.summarize([])
        rc, output = self.gate(allow, log=log, result=self.write_result(platform='ios', variant='full'))
        self.assertEqual(rc, 2)
        self.assertIn("'ıos-*' matches no leg", output)

    def test_a_scope_of_only_separators_is_exit_2(self):
        _, log = self.summarize([])
        rc, output = self.gate('IL2026\tGone*\treason\t,\n', log=log, result=self.write_result(platform='ios', variant='full'))
        self.assertEqual(rc, 2)
        self.assertIn('names no leg', output)

    def test_a_scope_glob_with_a_typo_is_exit_2_even_in_report_only_mode(self):
        _, log = self.summarize([])
        for report_only in (False, True):
            rc, output = self.gate('IL2026\tGone*\treason\tandroid-full,andriod-*\n', log=log, report_only=report_only,
                                   result=self.write_result(platform='android', variant='full'))
            self.assertEqual(rc, 2)
            self.assertIn("'andriod-*' matches no leg", output)

    def test_every_platform_and_variant_of_both_scripts_is_a_known_leg(self):
        for leg in ('android-aotroot', 'ios-aot', 'maccatalyst-partial', 'windows-rootlibs', 'windows-aot', 'ios-none'):
            self.assertIn(leg, ilwarn.KNOWN_LEGS)
        self.assertNotIn('windows-aotroot', ilwarn.KNOWN_LEGS)
        self.assertNotIn('windows-partial', ilwarn.KNOWN_LEGS)

    def test_an_allow_line_with_five_fields_is_exit_2(self):
        self.summarize([])
        self.assertEqual(self.gate('IL2026\tGone*\treason\tios-*\textra\n')[0], 2)

    def test_missing_warning_list_is_exit_4(self):
        self.assertEqual(self.gate()[0], 4)

    def test_github_annotations_are_capped_at_10_by_default_and_escaped(self):
        lines = [turkish_member_row('Converter%03d' % number) for number in range(60)]
        rc, output = self.gate_on(lines, github=True)
        self.assertEqual(rc, 1)
        self.assertEqual(sum(1 for line in output.splitlines() if line.startswith('::error title=')), 10)
        self.assertIn('::warning::51 more new warnings are not annotated (GitHub shows 10 annotations per step, this gate annotates 10, the first is its verdict); the step log and the job summary list all 60', output)
        self.assertEqual(ilwarn.escape_annotation('a%b\nc'), 'a%25b%0Ac')

    def test_max_annotations_option_changes_the_cap_and_the_summary_lists_every_warning(self):
        lines = [turkish_member_row('Converter%03d' % number) for number in range(8)]
        summary = self.path('summary.md')
        rc, output = self.gate_on(lines, github=True, max_annotations=3, summary=summary)
        self.assertEqual(rc, 1)
        self.assertEqual(sum(1 for line in output.splitlines() if line.startswith('::error title=')), 3)
        self.assertIn('::warning::6 more new warnings', output)
        with open(summary, encoding='utf-8') as source:
            self.assertIn('**New toolkit and toolkit-api warnings (8)**', source.read())

    def test_the_verdict_annotation_comes_first_and_counts_in_the_budget(self):
        lines = [turkish_member_row('Converter%03d' % number) for number in range(5)]
        _, output = self.gate_on(lines, github=True, max_annotations=3)
        errors = [line for line in output.splitlines() if line.startswith('::error')]
        self.assertEqual(len(errors), 3)
        self.assertTrue(errors[0].startswith('::error title=Trim and AOT gate::GATE: FAIL, 5 toolkit'))

    def test_a_report_only_gate_annotates_its_verdict_as_a_warning(self):
        _, output = self.gate_on([ISINRANGE_ILC], github=True, report_only=True)
        self.assertIn('::warning title=Trim and AOT gate::GATE: REPORT ONLY', output)
        self.assertNotIn('::error', output)

    def test_a_failed_leg_prints_one_annotation_and_none_per_warning(self):
        self.summarize([ISINRANGE_ILC])
        rc, output = self.gate(result=self.write_result(build_exit=1), github=True)
        self.assertEqual(rc, 4)
        self.assertEqual(output.splitlines()[1][:9], '::error::')
        self.assertNotIn('::error title=', output)

    def test_a_share_of_zero_annotates_no_warning_and_still_says_how_many_were_left_out(self):
        rc, output = self.gate_on([turkish_member_row('Converter%03d' % number) for number in range(4)], github=True, max_annotations=0)
        self.assertEqual(rc, 1)
        self.assertEqual(sum(1 for line in output.splitlines() if line.startswith('::error')), 0)
        self.assertIn('::warning::4 more new warnings are not annotated (GitHub shows 10 annotations per step, this gate annotates 0)', output)

    def test_two_gates_with_shares_that_add_up_to_10_print_at_most_10_errors(self):
        lines = [turkish_member_row('Converter%03d' % number) for number in range(30)]
        printed = 0
        for share in (5, 5):
            with self.subTest(share=share):
                printed += sum(1 for line in self.gate_on(lines, github=True, max_annotations=share)[1].splitlines() if line.startswith('::error'))
        self.assertEqual(printed, 10)

    def test_the_summary_lists_at_most_200_new_warnings_and_says_how_many_more_there_are(self):
        summary = self.path('summary.md')
        lines = [turkish_member_row('Converter%03d' % number) for number in range(250)]
        rc, _ = self.gate_on(lines, summary=summary)
        self.assertEqual(rc, 1)
        with open(summary, encoding='utf-8') as source:
            text = source.read()
        self.assertIn('**New toolkit and toolkit-api warnings (250)**', text)
        self.assertEqual(sum(1 for line in text.splitlines() if line.startswith('- ``')), 200)
        self.assertIn('... and 50 more (the step log lists every one)', text)
        self.assertLess(len(text.encode('utf-8')), 100 * 1024)

    def test_the_summary_cap_leaves_a_list_of_exactly_200_whole(self):
        summary = self.path('summary.md')
        self.gate_on([turkish_member_row('Converter%03d' % number) for number in range(200)], summary=summary)
        with open(summary, encoding='utf-8') as source:
            self.assertNotIn('more (the step log', source.read())

    def test_the_summary_caps_stale_allow_list_entries_too(self):
        summary = self.path('summary.md')
        allow = ''.join('IL2026\tGone%03d*\tstale\n' % number for number in range(230))
        _, log = self.summarize([])
        self.gate(allow, log=log, summary=summary)
        with open(summary, encoding='utf-8') as source:
            text = source.read()
        self.assertEqual(sum(1 for line in text.splitlines() if line.startswith('- ``')), 200)
        self.assertIn('... and 30 more', text)

    def test_il4xxx_codes_are_parsed_and_counted_as_il_lines(self):
        line = ("     1>/repo/samples/CommunityToolkit.Maui.Sample/Pages/FilePage.cs(10,5): warning IL4000: Using member "
                "'CommunityToolkit.Maui.Storage.Thing.Save()' which has 'RequiresUnreferencedCodeAttribute' can break functionality. [/_/Sample.csproj]")
        rc, log = self.summarize([line])
        self.assertEqual(rc, 0)
        (bucket, _, code, *_), = self.rows()
        self.assertEqual((bucket, code), ('toolkit-api', 'IL4000'))
        self.assertEqual(ilwarn.il_line_count(log), 1)

    def test_summary_file_lists_counts_and_the_new_warnings(self):
        summary = self.path('summary.md')
        rc, _ = self.gate_on([ISINRANGE_ILC, MAUI_ROW], summary=summary)
        self.assertEqual(rc, 1)
        with open(summary, encoding='utf-8') as source:
            text = source.read()
        self.assertIn('FAIL', text)
        self.assertIn('**New toolkit and toolkit-api warnings (1)**', text)
        self.assertIn('| toolkit | 1 |', text)
        self.assertIn('IsInRangeConverter', text)

    def test_a_missing_warning_log_is_exit_4_even_when_everything_else_is_clean(self):
        self.summarize([])
        rc, output = self.gate(assets=self.write('assets-check.txt', 'ASSETS-CHECK=OK'), result=self.write_result(), log=self.path('gone.log'))
        self.assertEqual(rc, 4)
        self.assertIn('no warning log', output)

    def test_summary_heading_names_the_leg_when_the_result_is_given(self):
        self.summarize([])
        summary = self.path('summary.md')
        self.gate(result=self.write_result(platform='android', variant='rootlibs'), summary=summary)
        self.gate(result=self.write_result(platform='ios', variant='aot'), summary=summary)
        with open(summary, encoding='utf-8') as source:
            text = source.read()
        self.assertIn('### Trim and AOT gate (android-rootlibs): OK', text)
        self.assertIn('### Trim and AOT gate (ios-aot): OK', text)

    def test_summary_heading_names_the_leg_of_a_failed_build_too(self):
        self.summarize([])
        summary = self.path('summary.md')
        self.gate(result=self.write_result(platform='android', variant='full', build_exit=1), summary=summary)
        with open(summary, encoding='utf-8') as source:
            self.assertIn('### Trim and AOT gate (android-full): FAIL (exit 4)', source.read())

    def test_report_only_reports_new_warnings_as_warnings_and_exits_0(self):
        summary = self.path('summary.md')
        rc, output = self.gate_on([ISINRANGE_ILC], github=True, summary=summary, report_only=True)
        self.assertEqual(rc, 0)
        self.assertIn('REPORT ONLY', output)
        self.assertIn('::warning title=', output)
        self.assertNotIn('::error', output)
        with open(summary, encoding='utf-8') as source:
            self.assertIn('REPORT ONLY', source.read())

    def test_report_only_exits_0_for_a_failed_build_but_a_bad_allow_list_is_still_exit_2(self):
        self.summarize([])
        self.assertEqual(self.gate(result=self.write_result(build_exit=1), report_only=True)[0], 0)
        self.assertEqual(self.gate('IL2087\t*\t\n', report_only=True)[0], 2)

    def test_report_only_summary_of_a_failed_leg_still_lists_the_parsed_warnings(self):
        self.summarize([ISINRANGE_ILC, TOOLKIT_API_ROW, MAUI_ROW])
        summary = self.path('summary.md')
        rc, _ = self.gate(result=self.write_result(build_exit=1, platform='windows', variant='aot'), summary=summary, report_only=True)
        self.assertEqual(rc, 0)
        with open(summary, encoding='utf-8') as source:
            text = source.read()
        self.assertIn('REPORT ONLY, did not finish', text)
        self.assertIn('| toolkit | 1 |', text)
        self.assertIn('| maui | 1 |', text)
        self.assertIn('IsInRangeConverter', text)
        self.assertIn('ValidationBehavior.FlagsProperty', text)

    def test_report_only_summary_of_a_failed_leg_without_a_warning_list_has_no_rows(self):
        summary = self.path('summary.md')
        rc, _ = self.gate(result=self.write_result(build_exit=1), summary=summary, report_only=True)
        self.assertEqual(rc, 0)
        with open(summary, encoding='utf-8') as source:
            self.assertIn('| none | 0 |', source.read())

    def test_command_line_max_annotations_is_parsed(self):
        _, log = self.summarize([turkish_member_row('Converter%03d' % number) for number in range(5)])
        self.write('allow.tsv', '')
        arguments = ['ilwarn.py', 'gate', self.path('warnings-unique.tsv'), '--allow', self.path('allow.tsv'), '--log', log, '--github', '--max-annotations', '2']
        output = io.StringIO()
        with unittest.mock.patch('sys.argv', arguments), contextlib.redirect_stdout(output), self.assertRaises(SystemExit):
            runpy.run_path(ilwarn.__file__, run_name='__main__')
        self.assertEqual(sum(1 for line in output.getvalue().splitlines() if line.startswith('::error title=')), 2)
        arguments[-1] = 'many'
        with unittest.mock.patch('sys.argv', arguments), contextlib.redirect_stderr(io.StringIO()), self.assertRaises(SystemExit) as raised:
            runpy.run_path(ilwarn.__file__, run_name='__main__')
        self.assertEqual(raised.exception.code, 2)

    def test_command_line_report_only_flag_is_parsed(self):
        _, log = self.summarize([ISINRANGE_ILC])
        self.write('allow.tsv', '')
        arguments = ['ilwarn.py', 'gate', self.path('warnings-unique.tsv'), '--allow', self.path('allow.tsv'), '--log', log, '--report-only']
        for flag, expected in ((['--report-only'], 0), ([], 1)):
            with self.subTest(flags=flag), unittest.mock.patch('sys.argv', arguments[:-1] + flag), contextlib.redirect_stdout(io.StringIO()):
                with self.assertRaises(SystemExit) as raised:
                    runpy.run_path(ilwarn.__file__, run_name='__main__')
                self.assertEqual(raised.exception.code, expected)


def metadata_only_assembly(type_defs, type_refs=(), heap_sizes=0):
    """Smallest PE that ilwarn's reader accepts: headers, one section, a CLI header and the Module, TypeRef and TypeDef tables.
    type_defs and type_refs are (flags, namespace, name) and (namespace, name) tuples."""
    heap = bytearray(b'\0')
    offsets = {}

    def string(text):
        if text not in offsets:
            offsets[text] = len(heap)
            heap.extend(text.encode('utf-8') + b'\0')
        return offsets[text]
    module = struct.pack('<5H', 0, string('Test.dll'), 0, 0, 0)
    refs = b''.join(struct.pack('<3H', 0, string(name), string(namespace)) for namespace, name in type_refs)
    defs = b''.join(struct.pack('<IHHHHH', flags, string(name), string(namespace), 0, 1, 1) for flags, namespace, name in type_defs)
    extra_data = struct.pack('<I', 0xDEADBEEF) if heap_sizes & 0x40 else b''
    tables = (struct.pack('<IBBBBQQ', 0, 2, 0, heap_sizes, 1, 0b111, 0) + struct.pack('<3I', 1, len(type_refs), len(type_defs)) + extra_data
              + module + refs + defs)
    strings = bytes(heap) + b'\0' * (-len(heap) % 4)
    tables += b'\0' * (-len(tables) % 4)
    version = b'v4.0.30319\0\0'
    headers_size = 16 + len(version) + 4 + (8 + 4) + (8 + 12)
    stream_headers = (struct.pack('<II', headers_size, len(tables)) + b'#~\0\0'
                      + struct.pack('<II', headers_size + len(tables), len(strings)) + b'#Strings\0\0\0\0')
    root = struct.pack('<IHHII', 0x424A5342, 1, 1, 0, len(version)) + version + struct.pack('<HH', 0, 2) + stream_headers + tables + strings
    section_rva, section_pointer = 0x1000, 0x200
    cli_header = struct.pack('<IHHII', 72, 2, 5, section_rva + 72, len(root)).ljust(72, b'\0')
    section = cli_header + root
    optional = bytearray(0xE0)
    struct.pack_into('<H', optional, 0, 0x10B)
    struct.pack_into('<I', optional, 96 + 14 * 8, section_rva)
    section_header = b'.text\0\0\0' + struct.pack('<IIII', len(section), section_rva, len(section), section_pointer).ljust(32, b'\0')
    pe = b'PE\0\0' + struct.pack('<HHIIIHH', 0x14C, 1, 0, 0, 0, len(optional), 0) + bytes(optional) + section_header
    dos = bytearray(0x40)
    struct.pack_into('<I', dos, 0x3C, 0x40)
    return (bytes(dos) + pe).ljust(section_pointer, b'\0') + section


PUBLIC, NESTED_PUBLIC = 1, 2
GENERATED = 'Microsoft.Maui.Controls.Generated'
REGEX_GENERATED = 'System.Text.RegularExpressions.Generated'


class TypeIndexTests(Workspace):
    """Warnings of generated code in the toolkit DLLs that sit outside the CommunityToolkit.Maui namespace belong to the toolkit."""

    def summarize_with_toolkit_dll(self, type_defs, member_names, type_refs=(), dll=None):
        self.write_toolkit_dll(dll if dll is not None else metadata_only_assembly(type_defs, type_refs))
        rows = ['ILC : Trim analysis warning IL2026: %s: Using member.' % member for member in member_names]
        log = self.write('warnings.log', '\n'.join(rows) + '\n')
        with contextlib.redirect_stdout(io.StringIO()):
            rc = ilwarn.summarize(log, self.directory.name, [self.path('packages')])
        return rc, {row[4]: row[0] for row in self.rows()}

    def test_the_reader_lists_top_level_types_only_and_drops_generic_arity(self):
        names = ilwarn.top_level_type_names(metadata_only_assembly([
            (PUBLIC, GENERATED, 'Gen`2'), (NESTED_PUBLIC, '', 'Inner'), (0, '', '<Module>'), (PUBLIC, '', '<>z__ReadOnlyList')],
            type_refs=[(GENERATED, 'OnlyReferenced')]))
        self.assertEqual(names, {GENERATED + '.Gen'})

    def test_generic_arity_in_ilc_and_illink_spelling_attributes_to_the_toolkit(self):
        _, buckets = self.summarize_with_toolkit_dll([(PUBLIC, GENERATED, 'Gen`1')], [GENERATED + '.Gen`1.Run()', GENERATED + '.Gen<T>.Run()'])
        self.assertEqual(buckets, {GENERATED + '.Gen`1.Run()': 'toolkit', GENERATED + '.Gen<T>.Run()': 'toolkit'})

    def test_a_member_of_a_nested_type_matches_through_its_top_level_type(self):
        _, buckets = self.summarize_with_toolkit_dll([(PUBLIC, GENERATED, 'Gen'), (NESTED_PUBLIC, '', 'Inner')], [GENERATED + '.Gen.Inner.Run()'])
        self.assertEqual(buckets, {GENERATED + '.Gen.Inner.Run()': 'toolkit'})

    def test_a_file_local_type_name_starting_with_an_angle_bracket_is_kept_verbatim(self):
        defined = REGEX_GENERATED + '.<RegexGenerator_g>F1A2B__Utilities'
        other = REGEX_GENERATED + '.<RegexGenerator_g>FFFFF__Utilities'
        _, buckets = self.summarize_with_toolkit_dll([(PUBLIC, REGEX_GENERATED, '<RegexGenerator_g>F1A2B__Utilities')],
                                                     [defined + '.IsMatch(String)', other + '.IsMatch(String)'])
        self.assertEqual(buckets, {defined + '.IsMatch(String)': 'toolkit', other + '.IsMatch(String)': 'other'})

    def test_attributes_that_every_assembly_embeds_are_not_indexed(self):
        member = 'System.Runtime.CompilerServices.NullableAttribute..ctor(Byte)'
        _, buckets = self.summarize_with_toolkit_dll(
            [(PUBLIC, 'System.Runtime.CompilerServices', 'NullableAttribute'), (PUBLIC, 'Microsoft.CodeAnalysis', 'EmbeddedAttribute'), (PUBLIC, GENERATED, 'Gen')],
            [member])
        self.assertEqual(buckets, {member: 'other'})

    def test_a_type_that_the_dll_only_references_keeps_its_bucket(self):
        member = GENERATED + '.OnlyReferenced.Run()'
        _, buckets = self.summarize_with_toolkit_dll([(PUBLIC, GENERATED, 'Gen')], [member], type_refs=[(GENERATED, 'OnlyReferenced')])
        self.assertEqual(buckets, {member: 'maui'})

    def test_a_defined_generated_type_gates_while_an_undefined_one_in_the_same_namespace_stays_maui(self):
        defined, undefined = GENERATED + '.Gen.Run()', GENERATED + '.NotTheToolkits.Run()'
        _, buckets = self.summarize_with_toolkit_dll([(PUBLIC, GENERATED, 'Gen')], [defined, undefined])
        self.assertEqual(buckets, {defined: 'toolkit', undefined: 'maui'})
        rc, output = self.gate()
        self.assertEqual(rc, 1)
        self.assertIn(defined, output)

    def test_a_toolkit_dll_without_readable_types_fails_summarize_and_the_gate_closed(self):
        for label, dll in (('no types', metadata_only_assembly([(0, '', '<Module>')])), ('not a PE', b'MZ not an assembly')):
            with self.subTest(label):
                rc, buckets = self.summarize_with_toolkit_dll([], [GENERATED + '.Gen.Run()'], dll=dll)
                self.assertEqual(rc, 4)
                with open(self.path('warnings-summary.txt'), encoding='utf-8') as summary:
                    self.assertIn('TYPE-INDEX=FAIL', summary.read())
                gate_rc, output = self.gate()
                self.assertEqual(gate_rc, 4)
                self.assertIn('TYPE-INDEX=FAIL', output)

    def test_code_that_the_media_element_generator_emits_into_an_app_is_toolkit(self):
        folder = 'obj/Release/net11.0-android/CommunityToolkit.Maui.MediaElement.SourceGenerators/CommunityToolkit.Maui.MediaElement.SourceGenerators.AndroidMediaElementForegroundServiceConfigurationGenerator'
        generated = '/repo/samples/CommunityToolkit.Maui.Sample/%s/AndroidMediaElementServiceConfiguration.g.cs' % folder
        lines = ['%s(12,5): warning IL2026: Using member Y which has RequiresUnreferencedCodeAttribute can break functionality.' % generated,
                 '%s(12,5): Trim analysis warning IL2026: CommunityToolkit.Maui.Sample.Pages.Page.Run(): Using member.' % generated]
        self.summarize_lines_with_toolkit_dll([(PUBLIC, GENERATED, 'Gen')], lines)
        origins = {tool: (bucket, origin_assembly) for bucket, tool, _, origin_assembly, *_ in self.rows()}
        self.assertEqual(origins['analyzer'], ('toolkit', 'CommunityToolkit.Maui.MediaElement.SourceGenerators'))
        self.assertEqual(origins['illink'], ('toolkit', 'CommunityToolkit.Maui.MediaElement.SourceGenerators'))

    def test_a_gate_without_the_counts_file_next_to_the_tsv_is_exit_4(self):
        self.summarize([])
        os.remove(self.path('warnings-counts.json'))
        rc, output = self.gate()
        self.assertEqual(rc, 4)
        self.assertIn('TYPE-INDEX=FAIL', output)

    def test_a_gate_with_an_unreadable_counts_file_is_exit_4(self):
        self.summarize([])
        for label, content in (('not json', '{not json'), ('not an object', '[]')):
            with self.subTest(label):
                self.write('warnings-counts.json', content)
                self.assertEqual(self.gate()[0], 4)

    def summarize_lines_with_toolkit_dll(self, type_defs, lines):
        self.write_toolkit_dll(metadata_only_assembly(type_defs))
        log = self.write('warnings.log', '\n'.join(lines) + '\n')
        with contextlib.redirect_stdout(io.StringIO()):
            rc = ilwarn.summarize(log, self.directory.name, [self.path('packages')])
        return rc, {row[4]: (row[0], row[3]) for row in self.rows()}

    def test_a_type_name_that_other_assemblies_define_too_is_toolkit_only_by_its_source_path(self):
        interceptors = GENERATED + '.GeneratedBindingInterceptors'
        shapes = {
            'none': 'ILC',
            'toolkit': '/_/src/CommunityToolkit.Maui/obj/Release/net11.0-ios/Gen/Interceptors.g.cs(5)',
            'toolkit core on windows': 'D:\\a\\Maui\\src\\CommunityToolkit.Maui.Core\\obj\\Release\\net11.0-windows10.0.19041.0\\Interceptors.g.cs(5,1)',
            'toolkit media element': '/_/src/CommunityToolkit.Maui.MediaElement/obj/Interceptors.g.cs(5,1)',
            'sample': '/Users/runner/trim-audit/ios-aotroot/obj/Release/net11.0-ios/Interceptors.g.cs(5,1)',
            'maui': '/_/src/Controls/src/Core/Interceptors.g.cs(5,1)',
            'markup': '/_/src/CommunityToolkit.Maui.Markup/obj/Release/net11.0/Interceptors.g.cs(5,1)',
        }
        expected = {'none': 'toolkit', 'toolkit': 'toolkit', 'toolkit core on windows': 'toolkit', 'toolkit media element': 'toolkit',
                    'sample': 'maui', 'maui': 'maui', 'markup': 'maui'}
        lines = ['%s: Trim analysis warning IL2026: %s.Run%s(): Using member.' % ('ILC ' if label == 'none' else path, interceptors, number)
                 for number, (label, path) in enumerate(shapes.items())]
        _, buckets = self.summarize_lines_with_toolkit_dll([(PUBLIC, GENERATED, 'GeneratedBindingInterceptors')], lines)
        for number, label in enumerate(shapes):
            with self.subTest(label):
                self.assertEqual(buckets['%s.Run%d()' % (interceptors, number)][0], expected[label])

    def test_a_global_constants_class_of_another_assembly_is_not_the_toolkits(self):
        lines = ['ILC : Trim analysis warning IL2026: Constants.Toolkit(): Using member.',
                 '/_/src/CommunityToolkit.Maui.Camera/Constants.cs(3): Trim analysis warning IL2026: Constants.Located(): Using member.',
                 '/_/src/Somewhere/Constants.cs(3): Trim analysis warning IL2026: Constants.Foreign(): Using member.']
        _, buckets = self.summarize_lines_with_toolkit_dll([(PUBLIC, '', 'Constants')], lines)
        self.assertEqual({member: bucket for member, (bucket, _) in buckets.items()},
                         {'Constants.Toolkit()': 'toolkit', 'Constants.Located()': 'toolkit', 'Constants.Foreign()': 'other'})

    def test_code_that_the_toolkit_generators_emit_into_an_app_is_toolkit_for_every_tool(self):
        folder = 'obj/Release/net11.0-android/CommunityToolkit.Maui.SourceGenerators/CommunityToolkit.Maui.SourceGenerators.TextColorToGenerator'
        generated = '/repo/samples/CommunityToolkit.Maui.Sample/%s/ColorAnimationExtensions.g.cs' % folder
        lines = [
            '%s(12,5): Trim analysis warning IL2026: Microsoft.Maui.Controls.ColorAnimationExtensions_Label.Run(): Using member.' % generated,
            'D:\\a\\repo\\samples\\CommunityToolkit.Maui.Sample\\%s\\Other.g.cs(12): Trim analysis warning IL2026: Microsoft.Maui.Controls.ColorAnimationExtensions_Button.Run(): Using member.' % folder.replace('/', '\\'),
            '%s(12,5): Trim analysis warning IL2026: CommunityToolkit.Maui.Sample.Pages.Page.Run(): Using member.' % generated,
            '%s(12,5): warning IL2026: Using member Y which has RequiresUnreferencedCodeAttribute can break functionality.' % generated,
            'CommunityToolkit.Maui.SourceGenerators/CommunityToolkit.Maui.SourceGenerators.X/Relative.g.cs(3,1): warning IL2026: Using member Z.',
            '/repo/obj/Elsewhere/Microsoft.Maui.Controls.SourceGen/Pages.xsg.cs(3,1): Trim analysis warning IL2026: Microsoft.Maui.Controls.Thing.Run(): Using member.',
        ]
        _, rows = self.summarize_lines_with_toolkit_dll([(PUBLIC, GENERATED, 'Gen')], lines)
        self.assertEqual(rows['Microsoft.Maui.Controls.ColorAnimationExtensions_Label.Run()'], ('toolkit', 'CommunityToolkit.Maui.SourceGenerators'))
        self.assertEqual(rows['Microsoft.Maui.Controls.ColorAnimationExtensions_Button.Run()'][0], 'toolkit')
        self.assertEqual(rows['CommunityToolkit.Maui.Sample.Pages.Page.Run()'], ('toolkit', 'CommunityToolkit.Maui.SourceGenerators'))
        self.assertEqual(rows['Microsoft.Maui.Controls.Thing.Run()'][0], 'maui')
        analyzer = [row for row in self.rows() if row[1] == 'analyzer']
        self.assertEqual([row[0] for row in analyzer], ['toolkit', 'toolkit'])
        self.assertEqual({row[3] for row in analyzer}, {'CommunityToolkit.Maui.SourceGenerators'})

    def test_an_origin_that_is_the_type_itself_matches_the_index(self):
        _, buckets = self.summarize_with_toolkit_dll([(PUBLIC, GENERATED, 'Gen'), (NESTED_PUBLIC, '', 'Inner')], [GENERATED + '.Gen', GENERATED + '.Gen.Inner'])
        self.assertEqual(buckets, {GENERATED + '.Gen': 'toolkit', GENERATED + '.Gen.Inner': 'toolkit'})

    def test_a_generic_file_local_type_matches_in_ilc_and_illink_spelling(self):
        local = REGEX_GENERATED + '.<RegexGenerator_g>F1__Gen'
        members = [local + '`1.Run()', local + '<T>.Run()', local + '`1<__Canon>.Run()']
        _, buckets = self.summarize_with_toolkit_dll([(PUBLIC, REGEX_GENERATED, '<RegexGenerator_g>F1__Gen`1')], members)
        self.assertEqual(set(buckets.values()), {'toolkit'})
        self.assertEqual(len(buckets), 3)

    def test_the_reader_skips_the_extra_data_of_a_heap_sizes_flag_of_0x40(self):
        names = ilwarn.top_level_type_names(metadata_only_assembly([(PUBLIC, GENERATED, 'Gen')], heap_sizes=0x40))
        self.assertEqual(names, {GENERATED + '.Gen'})

    def test_no_toolkit_dll_at_all_fails_summarize_and_the_gate_closed(self):
        log = self.write('warnings.log', 'ILC : Trim analysis warning IL2026: Microsoft.Maui.Controls.Thing.Run(): Using member.\n')
        os.makedirs(self.path('packages/empty'))
        for label, packages in (('no packages folder', []), ('a packages folder without the DLLs', [self.path('packages')])):
            with self.subTest(label), contextlib.redirect_stdout(io.StringIO()):
                self.assertEqual(ilwarn.summarize(log, self.directory.name, packages), 4)
                gate_rc, output = self.gate()
                self.assertEqual(gate_rc, 4)
                self.assertIn('TYPE-INDEX=FAIL', output)

    def test_the_type_count_of_every_dll_is_recorded(self):
        self.summarize_with_toolkit_dll([(PUBLIC, GENERATED, 'Gen'), (PUBLIC, GENERATED, 'Other')], [])
        with open(self.path('warnings-counts.json'), encoding='utf-8') as counts:
            self.assertEqual(json.load(counts)['toolkit_type_counts'], {'communitytoolkit.maui/1.0.0/lib/net11.0/CommunityToolkit.Maui.dll': 2})


class CompiledElsewhereTests(unittest.TestCase):
    def test_a_clone_under_a_toolkit_named_folder_does_not_make_the_sample_obj_copy_a_toolkit_one(self):
        compiled_elsewhere = ilwarn.compiled_elsewhere
        clone = '/home/me/src/CommunityToolkit.Maui/'
        self.assertTrue(compiled_elsewhere(clone + 'artifacts/trim-audit/android-aot/obj/Release/Gen.g.cs(5)'))
        self.assertFalse(compiled_elsewhere(clone + 'src/CommunityToolkit.Maui/obj/Release/net11.0/Gen.g.cs(5)'))
        self.assertFalse(compiled_elsewhere(clone + 'src/CommunityToolkit.Maui.Core/Views/Thing.cs(5,3)'))


if __name__ == '__main__':
    unittest.main()
