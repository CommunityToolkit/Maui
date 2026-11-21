from __future__ import annotations

import gzip
import json
import unittest
from types import SimpleNamespace

from ensure_ci_always_tests_latest_operating_systems import required_xcode
from ensure_ci_always_tests_latest_operating_systems.errors import NotFoundError, WorkloadSetError
from ensure_ci_always_tests_latest_operating_systems.required_xcode import (
    NUGET_PACKAGES, NUGET_REGISTRATION, DOTNET_RELEASES_INDEX, latest_sdk_of_channel, manifest_reference,
    newest_listed_ga, read_recommended_xcode, read_registration, read_required_xcode, read_workload_set, resolve_band)

from .helpers import FakeDownloader, fixture, nupkg

SET_11 = 'microsoft.net.workloads.11.0.100'
SET_10 = 'microsoft.net.workloads.10.0.200'


def package_url(package_id, version):
    return '%s/%s/%s/%s.%s.nupkg' % (NUGET_PACKAGES, package_id, version, package_id, version)


def set_package(fixture_name):
    return nupkg({'data/microsoft.net.workloads.workloadset.json': fixture('xcode/' + fixture_name)})


def dependencies_package(fixture_name):
    return nupkg({'data/WorkloadDependencies.json': fixture('xcode/' + fixture_name)})


def downloads_for_band_11():
    return {
        '%s/%s/index.json' % (NUGET_PACKAGES, SET_11): fixture('xcode/idx-microsoft.net.workloads.11.0.100.json'),
        '%s/%s/index.json' % (NUGET_REGISTRATION, SET_11):
            gzip.compress(fixture('xcode/reg-microsoft.net.workloads.11.0.100.json')),
        package_url(SET_11, '11.100.1'): set_package('workloadset-11.100.1.json'),
        package_url('microsoft.net.sdk.ios.manifest-11.0.100', '27.2.11039'): dependencies_package('deps-ios-27.2.11039.json'),
        package_url('microsoft.net.sdk.maccatalyst.manifest-11.0.100', '27.2.11039'):
            dependencies_package('deps-maccatalyst-27.2.11039.json'),
    }


def pinned_with(toolkit_net_version, latest_net_version):
    return SimpleNamespace(shared_env={
        'TOOLKIT_NET_VERSION': SimpleNamespace(value=toolkit_net_version, line=1),
        'LATEST_NET_VERSION': SimpleNamespace(value=latest_net_version, line=2)})


def registration_item(version, listed=True, published='2026-01-01T00:00:00+00:00'):
    entry = {'version': version, 'published': published}
    if listed is not None:
        entry['listed'] = listed
    return {'catalogEntry': entry}


def index_of(*versions):
    return json.dumps({'versions': list(versions)}).encode('utf-8')


class LatestSdkOfChannelTests(unittest.TestCase):
    def test_reads_the_latest_sdk_of_the_channel(self):
        self.assertEqual('11.0.100', latest_sdk_of_channel(fixture('xcode/releases-index.json'), '11.0'))
        self.assertEqual('10.0.404', latest_sdk_of_channel(fixture('xcode/releases-index.json'), '10.0'))

    def test_a_preview_latest_sdk_is_an_error(self):
        index = json.loads(fixture('xcode/releases-index.json'))
        index['releases-index'][0]['latest-sdk'] = '11.0.100-preview.7.26401.1'
        with self.assertRaises(WorkloadSetError):
            latest_sdk_of_channel(json.dumps(index).encode('utf-8'), '11.0')

    def test_an_unknown_channel_is_an_error(self):
        with self.assertRaises(WorkloadSetError):
            latest_sdk_of_channel(fixture('xcode/releases-index.json'), '99.0')

    def test_a_byte_order_mark_before_the_json_is_read(self):
        data = b'\xef\xbb\xbf' + fixture('xcode/releases-index.json')
        self.assertEqual('11.0.100', latest_sdk_of_channel(data, '11.0'))


class NewestListedGaTests(unittest.TestCase):
    def test_the_fixture_gives_11_100_1(self):
        pages = read_registration(FakeDownloader(downloads_for_band_11()), SET_11)
        version, published = newest_listed_ga(fixture('xcode/idx-microsoft.net.workloads.11.0.100.json'), pages)
        self.assertEqual('11.100.1', version)
        self.assertEqual('2026-11-10T18:18:09.66+00:00', published)

    def test_an_unordered_index_with_a_prerelease_and_an_unlisted_version_gives_the_newest_listed_ga(self):
        pages = [{'items': [registration_item('11.100.2', listed=False), registration_item('11.100.1'),
                            registration_item('11.100.0'), registration_item('11.101.0-preview.1'),
                            registration_item('11.99.9')]}]
        index = index_of('11.100.1', '11.101.0-preview.1', '11.100.2', '11.99.9', '11.100.0')
        self.assertEqual('11.100.1', newest_listed_ga(index, pages)[0])

    def test_numeric_order_beats_text_order(self):
        pages = [{'items': [registration_item('10.9.0'), registration_item('10.10.0')]}]
        self.assertEqual('10.10.0', newest_listed_ga(index_of('10.10.0', '10.9.0'), pages)[0])

    def test_a_missing_listed_property_counts_as_listed(self):
        pages = [{'items': [registration_item('11.100.0', listed=None)]}]
        self.assertEqual('11.100.0', newest_listed_ga(index_of('11.100.0'), pages)[0])

    def test_no_listed_ga_version_is_an_error(self):
        pages = [{'items': [registration_item('11.100.0', listed=False)]}]
        with self.assertRaises(WorkloadSetError):
            newest_listed_ga(index_of('11.100.0', '11.101.0-rc.1'), pages)


class ReadRegistrationTests(unittest.TestCase):
    def test_pages_without_inline_items_are_fetched_from_their_id(self):
        root_url = '%s/%s/index.json' % (NUGET_REGISTRATION, SET_11)
        page_url = '%s/%s/page/1.json' % (NUGET_REGISTRATION, SET_11)
        root = {'items': [{'@id': page_url, 'count': 1}]}
        page = {'items': [registration_item('11.100.1')]}
        downloader = FakeDownloader({root_url: json.dumps(root), page_url: gzip.compress(json.dumps(page).encode('utf-8'))})
        pages = read_registration(downloader, SET_11)
        self.assertEqual([page], pages)
        self.assertEqual([root_url, page_url], downloader.urls_requested())

    def test_a_gzip_body_that_the_downloader_left_compressed_is_decompressed(self):
        pages = read_registration(FakeDownloader(downloads_for_band_11()), SET_11)
        self.assertEqual(1, len(pages))
        self.assertEqual(2, len(pages[0]['items']))


class ManifestReferenceTests(unittest.TestCase):
    def test_a_set_of_band_10_0_200_references_band_10_0_100_manifests(self):
        workload_set = read_workload_set(set_package('workloadset-10.204.1.json'))
        version, band = manifest_reference(workload_set, 'Microsoft.NET.Sdk.iOS')
        self.assertEqual('10.0.100', band)
        self.assertTrue(version[0].isdigit())

    def test_the_manifest_band_is_the_part_after_the_slash(self):
        workload_set = read_workload_set(set_package('workloadset-11.100.1.json'))
        self.assertEqual(('27.2.11039', '11.0.100'), manifest_reference(workload_set, 'Microsoft.NET.Sdk.iOS'))

    def test_a_missing_entry_is_an_error(self):
        with self.assertRaises(WorkloadSetError):
            manifest_reference({}, 'Microsoft.NET.Sdk.iOS')

    def test_a_broken_workload_set_package_is_an_error(self):
        with self.assertRaises(WorkloadSetError):
            read_workload_set(nupkg({'data/other.json': '{}'}))
        with self.assertRaises(WorkloadSetError):
            read_workload_set(b'not a zip')


class ReadRecommendedXcodeTests(unittest.TestCase):
    def test_xcode_27_2_for_ios_and_mac_catalyst(self):
        self.assertEqual('27.2', read_recommended_xcode(dependencies_package('deps-ios-27.2.11039.json'), 'Microsoft.NET.Sdk.iOS'))
        self.assertEqual('27.2', read_recommended_xcode(
            dependencies_package('deps-maccatalyst-27.2.11039.json'), 'Microsoft.NET.Sdk.MacCatalyst'))

    def test_the_opt_in_pack_case_recommends_26_6(self):
        self.assertEqual('26.6', read_recommended_xcode(dependencies_package('deps-ios-26.5.10315.json'), 'Microsoft.NET.Sdk.iOS'))

    def test_a_byte_order_mark_before_the_json_is_read(self):
        data = b'\xef\xbb\xbf' + fixture('xcode/deps-ios-27.2.11039.json')
        package = nupkg({'data/WorkloadDependencies.json': data})
        self.assertEqual('27.2', read_recommended_xcode(package, 'Microsoft.NET.Sdk.iOS'))

    def test_a_missing_xcode_key_is_a_workload_set_error(self):
        dependencies = json.loads(fixture('xcode/deps-ios-27.2.11039.json'))
        del dependencies['microsoft.net.sdk.ios']['xcode']
        package = nupkg({'data/WorkloadDependencies.json': json.dumps(dependencies)})
        with self.assertRaises(WorkloadSetError):
            read_recommended_xcode(package, 'Microsoft.NET.Sdk.iOS')

    def test_a_three_part_recommendation_is_cut_to_major_minor(self):
        package = nupkg({'data/WorkloadDependencies.json': json.dumps(
            {'microsoft.net.sdk.ios': {'xcode': {'recommendedVersion': '26.4.1'}}})})
        self.assertEqual('26.4', read_recommended_xcode(package, 'Microsoft.NET.Sdk.iOS'))


class ResolveBandTests(unittest.TestCase):
    def test_band_11_0_100_resolves_set_11_0_100_1_and_xcode_27_2(self):
        result = resolve_band(FakeDownloader(downloads_for_band_11()), '11.0.100')
        self.assertEqual('ok', result['status'])
        self.assertEqual('11.100.1', result['set_package_version'])
        self.assertEqual('11.0.100.1', result['set_version'])
        self.assertEqual('2026-11-10T18:18:09.660000+00:00', result['published'])
        self.assertEqual({'manifest': '27.2.11039', 'band': '11.0.100', 'xcode': '27.2'}, result['ios'])
        self.assertEqual({'manifest': '27.2.11039', 'band': '11.0.100', 'xcode': '27.2'}, result['maccatalyst'])

    def test_band_10_0_200_resolves_manifests_of_band_10_0_100(self):
        index = index_of('10.204.1')
        registration = {'items': [{'items': [registration_item('10.204.1')]}]}
        mapping = {
            '%s/%s/index.json' % (NUGET_PACKAGES, SET_10): index,
            '%s/%s/index.json' % (NUGET_REGISTRATION, SET_10): json.dumps(registration),
            package_url(SET_10, '10.204.1'): set_package('workloadset-10.204.1.json'),
        }
        workload_set = json.loads(fixture('xcode/workloadset-10.204.1.json'))
        for platform in ('ios', 'maccatalyst'):
            key = 'Microsoft.NET.Sdk.iOS' if platform == 'ios' else 'Microsoft.NET.Sdk.MacCatalyst'
            version, band = workload_set[key].split('/')
            self.assertEqual('10.0.100', band)
            dependencies = json.loads(fixture('xcode/deps-ios-26.5.10315.json'))
            dependencies['microsoft.net.sdk.' + platform] = dependencies.pop('microsoft.net.sdk.ios')
            mapping[package_url('microsoft.net.sdk.%s.manifest-10.0.100' % platform, version)] = nupkg(
                {'data/WorkloadDependencies.json': json.dumps(dependencies)})
        result = resolve_band(FakeDownloader(mapping), '10.0.200')
        self.assertEqual('10.0.100', result['ios']['band'])
        self.assertEqual('26.6', result['ios']['xcode'])
        self.assertEqual('10.0.204.1', result['set_version'])

    def test_a_404_set_package_is_missing_not_an_error(self):
        url = '%s/microsoft.net.workloads.11.0.200/index.json' % NUGET_PACKAGES
        result = resolve_band(FakeDownloader({url: NotFoundError(url)}), '11.0.200')
        self.assertEqual('missing', result['status'])


class ReadRequiredXcodeTests(unittest.TestCase):
    def downloads_with_releases_index(self):
        mapping = downloads_for_band_11()
        mapping[DOTNET_RELEASES_INDEX] = fixture('xcode/releases-index.json')
        return mapping

    def test_library_and_sample_band_11_0_100_need_xcode_27_2(self):
        result = read_required_xcode(FakeDownloader(self.downloads_with_releases_index()), pinned_with('11.0.100', '11.0.x'))
        self.assertEqual('ok', result['status'])
        self.assertEqual('11.0.100', result['library_pinned_band'])
        self.assertEqual('11.0.100', result['sample_band'])
        self.assertEqual('11.0.100', result['latest_sdk'])
        self.assertEqual(['11.0.100'], list(result['bands']))
        self.assertEqual('27.2', result['bands']['11.0.100']['ios']['xcode'])
        self.assertEqual('27.2', result['bands']['11.0.100']['maccatalyst']['xcode'])
        json.dumps(result)

    def test_a_full_latest_version_needs_no_releases_index(self):
        result = read_required_xcode(FakeDownloader(downloads_for_band_11()), pinned_with('11.0.103', '11.0.100'))
        self.assertIsNone(result['latest_sdk'])
        self.assertEqual('11.0.100', result['sample_band'])

    def test_every_band_between_the_library_and_the_sample_is_resolved(self):
        mapping = self.downloads_with_releases_index()
        url = '%s/microsoft.net.workloads.11.0.100/index.json' % NUGET_PACKAGES
        index = json.loads(fixture('xcode/releases-index.json'))
        index['releases-index'][0]['latest-sdk'] = '11.0.201'
        mapping[DOTNET_RELEASES_INDEX] = json.dumps(index)
        missing_url = '%s/microsoft.net.workloads.11.0.200/index.json' % NUGET_PACKAGES
        mapping[missing_url] = NotFoundError(missing_url)
        downloader = FakeDownloader(mapping)
        result = read_required_xcode(downloader, pinned_with('11.0.100', '11.0.x'))
        self.assertEqual(['11.0.100', '11.0.200'], list(result['bands']))
        self.assertEqual('missing', result['bands']['11.0.200']['status'])
        self.assertIn(url, downloader.urls_requested())

    def test_a_sample_band_below_the_library_band_still_resolves_both(self):
        from ensure_ci_always_tests_latest_operating_systems.required_xcode import _bands_from_to
        self.assertEqual(['11.0.100', '11.0.200'], _bands_from_to('11.0.200', '11.0.100'))
        self.assertEqual(['11.0.100', '11.0.200', '11.0.300'], _bands_from_to('11.0.100', '11.0.300'))
        self.assertEqual(['11.0.100'], _bands_from_to('11.0.100', '11.0.100'))

    def test_a_missing_library_band_is_an_error(self):
        url = '%s/microsoft.net.workloads.11.0.100/index.json' % NUGET_PACKAGES
        mapping = self.downloads_with_releases_index()
        mapping[url] = NotFoundError(url)
        with self.assertRaises(WorkloadSetError):
            read_required_xcode(FakeDownloader(mapping), pinned_with('11.0.100', '11.0.x'))

    def test_a_preview_latest_sdk_is_an_error(self):
        index = json.loads(fixture('xcode/releases-index.json'))
        index['releases-index'][0]['latest-sdk'] = '11.0.100-rc.1.1'
        mapping = self.downloads_with_releases_index()
        mapping[DOTNET_RELEASES_INDEX] = json.dumps(index)
        with self.assertRaises(WorkloadSetError):
            read_required_xcode(FakeDownloader(mapping), pinned_with('11.0.100', '11.0.x'))

    def test_the_constants_name_the_documented_hosts(self):
        self.assertTrue(required_xcode.NUGET_PACKAGES.startswith('https://api.nuget.org/'))


if __name__ == '__main__':
    unittest.main()
