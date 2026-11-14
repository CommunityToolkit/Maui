"""Self-test of ilwarn.py on fixture logs. Run from the repo root (python -I ignores the working folder, so use discover):

    python3 -I -m unittest discover -s eng/trim-audit -p 'test_*.py'

Run it under tr_TR.UTF-8 and C locales: the parser and the gate handle build output, not user text, so neither may depend on the
locale (Turkish casing turns I into a dotless i and would split a member name from its pattern)."""
import base64
import collections
import contextlib
import hashlib
import io
import json
import os
import re
import runpy
import shutil
import subprocess
import tempfile
import unittest
import unittest.mock
import zipfile

import ilwarn

PREFIX = '     1>/_/'
ISINRANGE_ILC = (PREFIX + "src/CommunityToolkit.Maui/obj/Release/net11.0-ios/Gen/IsInRangeConverter.g.cs(11): Trim analysis warning IL2087: "
                 "CommunityToolkit.Maui.Converters.IsInRangeConverter`2..cctor(): 'returnType' argument does not satisfy "
                 "'DynamicallyAccessedMemberTypes.PublicParameterlessConstructor' in call to 'BindableProperty.Create'. [/_/Sample.csproj]")
ISINRANGE_ILLINK = (PREFIX + "src/CommunityToolkit.Maui/obj/Release/net11.0-android/Gen/IsInRangeConverter.g.cs(11,5): Trim analysis warning IL2087: "
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

    def summarize(self, lines):
        log = self.write('warnings.log', '\n'.join(lines) + ('\n' if lines else ''))
        with contextlib.redirect_stdout(io.StringIO()):
            rc = ilwarn.summarize(log, self.directory.name, [])
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
                (bucket, parsed_tool, code, *_), = self.rows()
                self.assertEqual((bucket, parsed_tool, code), ('toolkit', tool, 'IL2087'))

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


class WindowsResultHelperTests(Workspace):
    helper = os.path.join(os.path.dirname(os.path.abspath(ilwarn.__file__)), 'gate-windows-result.sh')

    def windows_result(self, log_lines, assets='ASSETS-CHECK=OK', build_exit=0):
        self.write('warnings.log', '\n'.join(log_lines) + '\n')
        self.write('assets-check.txt', assets)
        self.write('result.json', json.dumps({'platform': 'windows', 'variant': 'rootlibs', 'build_exit': build_exit}))
        return self.directory.name

    def run_helper(self, *arguments):
        done = subprocess.run(['bash', self.helper, *arguments], capture_output=True, text=True, check=False)
        return done.returncode, done.stdout + done.stderr

    def test_a_clean_result_folder_gates_to_0_and_writes_gate_output(self):
        folder = self.windows_result([MARKUP_ROW])
        rc, output = self.run_helper(folder)
        self.assertEqual(rc, 0)
        self.assertIn('GATE-EXIT=0', output)
        with open(os.path.join(folder, 'gate-output.txt'), encoding='utf-8') as source:
            self.assertIn('GATE: OK', source.read())

    def test_a_new_toolkit_warning_gates_to_1(self):
        rc, output = self.run_helper(self.windows_result([ISINRANGE_ILC]), '--github')
        self.assertEqual(rc, 1)
        self.assertIn('GATE-EXIT=1', output)
        self.assertIn('::error title=Trim and AOT gate::', output)

    def test_a_zip_is_unpacked_next_to_itself_and_gated(self):
        folder = self.windows_result([ISINRANGE_ILC])
        archive = os.path.join(folder, 'windows-rootlibs.zip')
        subprocess.run(['zip', '-q', '-j', archive, *(os.path.join(folder, name) for name in ('warnings.log', 'assets-check.txt', 'result.json'))], check=True)
        rc, _ = self.run_helper(archive)
        self.assertEqual(rc, 1)
        self.assertTrue(os.path.exists(os.path.join(folder, 'windows-rootlibs', 'gate-output.txt')))

    def test_a_zip_with_backslash_entry_names_is_unpacked_and_attributes_warnings_to_the_toolkit_dll(self):
        # Windows PowerShell 5.1 writes entry names like this, and unzip only warns about them (exit 1).
        dll_name = 'CommunityToolkit.Maui.Core'
        warning = (PREFIX + "obj/Release/Gen/Thing.g.cs(11): Trim analysis warning IL2087: "
                   "CommunityToolkit.Maui.Core.Views.PopupThing.Run(): message. [/_/Sample.csproj]")
        entries = {'warnings.log': warning + '\n', 'assets-check.txt': 'ASSETS-CHECK=OK',
                   'result.json': json.dumps({'platform': 'windows', 'variant': 'rootlibs', 'build_exit': 0})}
        archive = self.path('windows-rootlibs.zip')
        with zipfile.ZipFile(archive, 'w') as out:
            for name, text in entries.items():
                out.writestr(zipfile.ZipInfo(name), text)
            dll = zipfile.ZipInfo('toolkit-dlls\\lib\\%s.dll' % dll_name)
            dll.create_system = 0
            out.writestr(dll, b'MZ\0PopupThing\0Views\0')
        rc, output = self.run_helper(archive)
        self.assertEqual(rc, 1, output)
        folder = self.path('windows-rootlibs')
        self.assertTrue(os.path.exists(os.path.join(folder, 'toolkit-dlls', 'lib', dll_name + '.dll')))
        with open(os.path.join(folder, 'warnings-unique.tsv'), encoding='utf-8') as tsv:
            self.assertIn(dll_name, tsv.read())

    def test_a_zip_entry_that_leaves_the_target_folder_is_refused(self):
        archive = self.path('evil.zip')
        with zipfile.ZipFile(archive, 'w') as out:
            out.writestr('..\\..\\escaped.txt', 'x')
        self.assertEqual(self.run_helper(archive)[0], 2)
        self.assertFalse(os.path.exists(os.path.join(self.directory.name, '..', 'escaped.txt')))

    def test_a_missing_target_is_a_usage_error_2(self):
        self.assertEqual(self.run_helper(os.path.join(self.directory.name, 'nothing'))[0], 2)
        self.assertEqual(self.run_helper()[0], 2)


@unittest.skipUnless(shutil.which('pwsh'), 'PowerShell 7 (pwsh) is not installed')
class WithoutPythonAssetsCheckParityTests(Workspace):
    """audit-win.ps1 repeats verify-assets.py for a machine without Python: the two must print the same lines and return the same code."""
    audit_folder = os.path.dirname(os.path.abspath(ilwarn.__file__))
    version = '99.0.0-parity'
    ids = ('CommunityToolkit.Maui', 'CommunityToolkit.Maui.Core', 'CommunityToolkit.Maui.Camera', 'CommunityToolkit.Maui.MediaElement')
    harness = r'''param([string]$Script, [string]$Fixture)
$m = [regex]::Match((Get-Content $Script -Raw), '(?s)# Same checks as verify-assets\.py, for a machine.*?\n}\n')
. ([scriptblock]::Create($m.Value))
$ErrorActionPreference = 'Stop'
$feedFolder = Join-Path $Fixture 'feed'; $packages = Join-Path $Fixture 'pk'
$ids = 'CommunityToolkit.Maui', 'CommunityToolkit.Maui.Core', 'CommunityToolkit.Maui.Camera', 'CommunityToolkit.Maui.MediaElement'
function Get-FeedVersion([string]$Id) {
    $found = @(Get-ChildItem $feedFolder -Filter "$Id.*.nupkg" | Where-Object { $_.Name -match ('^' + [regex]::Escape($Id) + '\.\d') })
    $found[0].Name.Substring($Id.Length + 1) -replace '\.nupkg$', ''
}
$versionOf = @{}; foreach ($id in $ids) { $versionOf[$id] = Get-FeedVersion $id }
exit (Write-AssetsCheckWithoutPython (Join-Path $Fixture 'assets.json') (Join-Path $Fixture 'ps-check.txt'))
'''

    def build_fixture(self, name, mutate=None):
        fixture = self.path('fx-' + name)
        feed, packages = os.path.join(fixture, 'feed'), os.path.join(fixture, 'pk')
        os.makedirs(feed)
        assets = {'libraries': {'%s/%s' % (i, self.version): {'type': 'package'} for i in self.ids}, 'packageFolders': {packages: {}}}
        assets['libraries']['CommunityToolkit.Maui.DeviceTests/1.0.0'] = {'type': 'project'}
        assets['libraries']['Microsoft.Maui.Controls/10.0.0'] = {'type': 'package'}
        for i in self.ids:
            nupkg = os.path.join(feed, '%s.%s.nupkg' % (i, self.version))
            with open(nupkg, 'wb') as out:
                out.write(('payload ' + i).encode())
            restored = os.path.join(packages, i.lower(), self.version)
            os.makedirs(restored)
            with open(os.path.join(restored, '%s.%s.nupkg.sha512' % (i.lower(), self.version)), 'w') as out:
                out.write(base64.b64encode(hashlib.sha512(('payload ' + i).encode()).digest()).decode())
        if mutate:
            mutate(assets, feed, packages)
        with open(os.path.join(fixture, 'assets.json'), 'w', encoding='utf-8') as out:
            json.dump(assets, out)
        return fixture

    def python_result(self, fixture):
        done = subprocess.run(['python3', '-I', os.path.join(self.audit_folder, 'verify-assets.py'), os.path.join(fixture, 'assets.json'),
                               os.path.join(fixture, 'feed'), os.path.join(fixture, 'pk')], capture_output=True, text=True, check=False)
        lines = [re.sub(r' \(.*\)$', '', line) for line in done.stdout.splitlines()]
        return done.returncode, lines

    def powershell_result(self, fixture):
        harness = self.write('harness.ps1', self.harness)
        done = subprocess.run(['pwsh', '-NoProfile', '-File', harness, os.path.join(self.audit_folder, 'audit-win.ps1'), fixture],
                              capture_output=True, text=True, check=False)
        with open(os.path.join(fixture, 'ps-check.txt'), encoding='utf-8') as source:
            lines = [re.sub(r' \(.*\)$', '', line) for line in source.read().splitlines()]
        return done.returncode, lines

    def assert_parity(self, name, mutate=None, expected_code=1):
        fixture = self.build_fixture(name, mutate)
        python_code, python_lines = self.python_result(fixture)
        powershell_code, powershell_lines = self.powershell_result(fixture)
        self.assertEqual(python_code, expected_code)
        self.assertEqual(powershell_code, python_code)
        self.assertEqual(powershell_lines, python_lines)

    def test_ok(self):
        self.assert_parity('ok', expected_code=0)

    def test_a_tampered_restored_nupkg(self):
        def tamper(assets, feed, packages):
            with open(os.path.join(packages, 'communitytoolkit.maui.camera', self.version, 'communitytoolkit.maui.camera.%s.nupkg.sha512' % self.version), 'w') as out:
                out.write('bad')
        self.assert_parity('tampered', tamper)

    def test_a_feed_with_another_version(self):
        def rename(assets, feed, packages):
            os.rename(os.path.join(feed, 'CommunityToolkit.Maui.MediaElement.%s.nupkg' % self.version), os.path.join(feed, 'CommunityToolkit.Maui.MediaElement.99.0.1.nupkg'))
        self.assert_parity('feed_version', rename)

    def test_a_restore_folder_that_is_not_the_audit_packages_folder(self):
        self.assert_parity('folder', lambda assets, feed, packages: assets.update(packageFolders={'/elsewhere/': {}}))

    def test_a_project_reference_to_toolkit_code(self):
        def reference(assets, feed, packages):
            assets['libraries']['CommunityToolkit.Maui.Core/%s' % self.version]['type'] = 'project'
            assets['libraries']['CommunityToolkit.Maui.Extra/1.0.0'] = {'type': 'project'}
        self.assert_parity('project', reference)

    def test_a_missing_main_library(self):
        self.assert_parity('missing_maui', lambda assets, feed, packages: assets['libraries'].pop('CommunityToolkit.Maui/%s' % self.version))

    def test_a_library_that_was_not_restored(self):
        def remove(assets, feed, packages):
            os.remove(os.path.join(packages, 'communitytoolkit.maui.core', self.version, 'communitytoolkit.maui.core.%s.nupkg.sha512' % self.version))
        self.assert_parity('not_restored', remove)

    def test_an_assets_file_that_cannot_be_read_fails_the_check_and_still_writes_it(self):
        fixture = self.build_fixture('truncated')
        with open(os.path.join(fixture, 'assets.json'), 'w', encoding='utf-8') as out:
            out.write('{"libraries": {')
        code, lines = self.powershell_result(fixture)
        self.assertEqual(code, 1)
        self.assertEqual(lines[-1], 'ASSETS-CHECK=FAIL')
        self.assertTrue(any(line.startswith('FAIL cannot read ') for line in lines))

    def test_an_assets_file_with_the_wrong_shape_fails_like_verify_assets(self):
        for index, content in enumerate(('', 'null', '[]', '0', '{}', '{"libraries": null}', '"x"')):
            with self.subTest(content=content):
                fixture = self.build_fixture('shape-%d' % index)
                with open(os.path.join(fixture, 'assets.json'), 'w', encoding='utf-8') as out:
                    out.write(content)
                code, lines = self.powershell_result(fixture)
                self.assertEqual(code, 1)
                self.assertEqual(lines[-1], 'ASSETS-CHECK=FAIL')
                self.assertNotEqual(self.python_result(fixture)[0], 0)


if __name__ == '__main__':
    unittest.main()
