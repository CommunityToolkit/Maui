"""Which Xcode the iOS and Mac Catalyst workloads of each .NET SDK feature band need, read from NuGet."""
from __future__ import annotations

import gzip
import io
import json
import re
import zipfile
from typing import Any, Dict, List, Optional, Tuple

from .errors import NotFoundError, WorkloadSetError
from .version_numbers import channel_of, feature_band, major_minor, numeric_key, parse_utc, workload_set_version

NUGET_PACKAGES = 'https://api.nuget.org/v3-flatcontainer'
NUGET_REGISTRATION = 'https://api.nuget.org/v3/registration5-gz-semver2'
DOTNET_RELEASES_INDEX = 'https://builds.dotnet.microsoft.com/dotnet/release-metadata/releases-index.json'

WORKLOAD_SET_MEMBER = 'data/microsoft.net.workloads.workloadset.json'
WORKLOAD_DEPENDENCIES_MEMBER = 'data/WorkloadDependencies.json'
IOS_KEY = 'Microsoft.NET.Sdk.iOS'
MAC_CATALYST_KEY = 'Microsoft.NET.Sdk.MacCatalyst'

_GZIP_MAGIC = b'\x1f\x8b'
_BAND = re.compile(r'(\d+)\.(\d+)\.(\d+)', re.ASCII)


def parse_json_bytes(data: bytes, what: str) -> Any:
    """utf-8-sig because NuGet packages sometimes start their JSON with a byte order mark."""
    try:
        return json.loads(data.decode('utf-8-sig'))
    except (UnicodeDecodeError, ValueError) as error:
        raise WorkloadSetError('%s is not valid JSON: %s' % (what, error))


def _gunzip_when_compressed(data: bytes, what: str) -> bytes:
    if not data.startswith(_GZIP_MAGIC):
        return data
    try:
        return gzip.decompress(data)
    except (OSError, EOFError) as error:
        raise WorkloadSetError('%s has a broken gzip body: %s' % (what, error))


def latest_sdk_of_channel(releases_index: bytes, channel: str) -> str:
    index = parse_json_bytes(releases_index, 'the .NET releases index')
    entries = index.get('releases-index') if isinstance(index, dict) else None
    if not isinstance(entries, list):
        raise WorkloadSetError('the .NET releases index has no "releases-index" list')
    for entry in entries:
        if isinstance(entry, dict) and entry.get('channel-version') == channel:
            latest_sdk = entry.get('latest-sdk')
            if not isinstance(latest_sdk, str) or not latest_sdk:
                raise WorkloadSetError('the .NET releases index has no latest-sdk for channel %s' % channel)
            if '-' in latest_sdk:
                raise WorkloadSetError(
                    'latest-sdk %s of channel %s is a preview, but the workflows install GA SDKs only' % (latest_sdk, channel))
            return latest_sdk
    raise WorkloadSetError('the .NET releases index has no channel %s' % channel)


def newest_listed_ga(index_json: bytes, registration_pages: List[Dict[str, Any]]) -> Tuple[str, str]:
    """Numeric maximum of the listed GA versions: the flat container's order is undocumented and includes unlisted versions."""
    index = parse_json_bytes(index_json, 'the package version index')
    versions = index.get('versions') if isinstance(index, dict) else None
    if not isinstance(versions, list):
        raise WorkloadSetError('the package version index has no "versions" list')
    published_by_listed_version: Dict[str, str] = {}
    for page in registration_pages:
        for item in page.get('items') or []:
            entry = item.get('catalogEntry') if isinstance(item, dict) else None
            if not isinstance(entry, dict) or entry.get('listed') is False:
                continue
            version = entry.get('version')
            if isinstance(version, str):
                published_by_listed_version[version.lower()] = entry.get('published') or ''
    candidates = []
    for version in versions:
        if not isinstance(version, str) or '-' in version:
            continue
        if version.lower() not in published_by_listed_version:
            continue
        try:
            candidates.append((numeric_key(version), version))
        except ValueError:
            continue
    if not candidates:
        raise WorkloadSetError('no listed GA version found in the package version index')
    _, newest = max(candidates)
    return newest, published_by_listed_version[newest.lower()]


def read_registration(downloader: Any, package_id: str) -> List[Dict[str, Any]]:
    root_url = '%s/%s/index.json' % (NUGET_REGISTRATION, package_id)
    root = parse_json_bytes(_gunzip_when_compressed(downloader.get(root_url), root_url), root_url)
    pages = []
    for page in (root.get('items') if isinstance(root, dict) else None) or []:
        if 'items' not in page:
            page_url = page.get('@id')
            if not isinstance(page_url, str):
                raise WorkloadSetError('a registration page of %s has neither items nor an @id' % package_id)
            page = parse_json_bytes(_gunzip_when_compressed(downloader.get(page_url), page_url), page_url)
        pages.append(page)
    return pages


def _read_zip_member(nupkg: bytes, member: str) -> bytes:
    try:
        with zipfile.ZipFile(io.BytesIO(nupkg)) as archive:
            return archive.read(member)
    except (zipfile.BadZipFile, KeyError) as error:
        raise WorkloadSetError('the package has no readable %s: %s' % (member, error))


def read_workload_set(nupkg: bytes) -> Dict[str, Any]:
    workload_set = parse_json_bytes(_read_zip_member(nupkg, WORKLOAD_SET_MEMBER), 'the workload set')
    if not isinstance(workload_set, dict):
        raise WorkloadSetError('the workload set is not a JSON object')
    return workload_set


def manifest_reference(set_json: Dict[str, Any], platform_key: str) -> Tuple[str, str]:
    """The manifest band is the part after the slash: sets of bands 10.0.200-400 reference band-10.0.100 manifests."""
    value = set_json.get(platform_key)
    if not isinstance(value, str) or value.count('/') != 1:
        raise WorkloadSetError('the workload set has no "<version>/<band>" entry for %s' % platform_key)
    version, band = value.split('/')
    if not version or not band:
        raise WorkloadSetError('the workload set entry for %s is malformed: %s' % (platform_key, value))
    return version, band


def read_recommended_xcode(nupkg: bytes, platform_key: str) -> str:
    dependencies = parse_json_bytes(_read_zip_member(nupkg, WORKLOAD_DEPENDENCIES_MEMBER), 'WorkloadDependencies.json')
    section = dependencies.get(platform_key.lower()) if isinstance(dependencies, dict) else None
    xcode = section.get('xcode') if isinstance(section, dict) else None
    recommended = xcode.get('recommendedVersion') if isinstance(xcode, dict) else None
    if not isinstance(recommended, str):
        raise WorkloadSetError('WorkloadDependencies.json has no xcode recommendedVersion for %s' % platform_key.lower())
    try:
        return major_minor(recommended)
    except ValueError:
        raise WorkloadSetError('the recommended Xcode %r of %s is not a version' % (recommended, platform_key.lower()))


def _set_package_id(band: str) -> str:
    return 'microsoft.net.workloads.%s' % band.lower()


def _resolve_manifest(downloader: Any, set_json: Dict[str, Any], platform_key: str) -> Dict[str, str]:
    manifest_version, manifest_band = manifest_reference(set_json, platform_key)
    package_id = 'microsoft.net.sdk.%s.manifest-%s' % (platform_key.split('.')[-1].lower(), manifest_band.lower())
    nupkg = downloader.get('%s/%s/%s/%s.%s.nupkg' % (
        NUGET_PACKAGES, package_id, manifest_version.lower(), package_id, manifest_version.lower()))
    return {'manifest': manifest_version, 'band': manifest_band, 'xcode': read_recommended_xcode(nupkg, platform_key)}


def resolve_band(downloader: Any, band: str) -> Dict[str, Any]:
    package_id = _set_package_id(band)
    index_url = '%s/%s/index.json' % (NUGET_PACKAGES, package_id)
    try:
        index_json = downloader.get(index_url)
    except NotFoundError:
        return {'status': 'missing', 'band': band}
    registration = read_registration(downloader, package_id)
    package_version, published = newest_listed_ga(index_json, registration)
    set_nupkg = downloader.get('%s/%s/%s/%s.%s.nupkg' % (
        NUGET_PACKAGES, package_id, package_version.lower(), package_id, package_version.lower()))
    set_json = read_workload_set(set_nupkg)
    try:
        set_version = workload_set_version(package_version)
        published_utc = parse_utc(published).isoformat() if published else ''
    except ValueError as error:
        raise WorkloadSetError('the workload set package %s has an unreadable version or date: %s' % (package_version, error))
    return {
        'status': 'ok',
        'band': band,
        'set_package_version': package_version,
        'set_version': set_version,
        'published': published_utc,
        'ios': _resolve_manifest(downloader, set_json, IOS_KEY),
        'maccatalyst': _resolve_manifest(downloader, set_json, MAC_CATALYST_KEY),
    }


def _band_of_net_version(version: str, what: str) -> str:
    try:
        return feature_band(version)
    except ValueError:
        raise WorkloadSetError('%s %r is not a .NET SDK version' % (what, version))


def _bands_from_to(library_pinned_band: str, sample_band: str) -> List[str]:
    library = _BAND.fullmatch(library_pinned_band)
    sample = _BAND.fullmatch(sample_band)
    if library is None or sample is None or library.group(1, 2) != sample.group(1, 2):
        return [library_pinned_band] if library_pinned_band == sample_band else [library_pinned_band, sample_band]
    prefix = '%s.%s' % library.group(1, 2)
    lower, higher = sorted((int(library.group(3)), int(sample.group(3))))
    patches = sorted(set(range(lower, higher + 1, 100)) | {lower, higher})
    return ['%s.%d' % (prefix, patch) for patch in patches]


def _optional_env_value(pinned: Any, name: str) -> Optional[str]:
    entry = pinned.shared_env.get(name)
    return entry.value if entry is not None else None


def read_required_xcode(downloader: Any, pinned: Any) -> Dict[str, Any]:
    toolkit_net_version = _optional_env_value(pinned, 'TOOLKIT_NET_VERSION')
    latest_net_version = _optional_env_value(pinned, 'LATEST_NET_VERSION')
    if toolkit_net_version is None or latest_net_version is None:
        raise WorkloadSetError('shared.env needs TOOLKIT_NET_VERSION and LATEST_NET_VERSION')
    library_pinned_band = _band_of_net_version(toolkit_net_version, 'TOOLKIT_NET_VERSION')
    channel = channel_of(latest_net_version)
    latest_sdk: Optional[str] = None
    if channel is not None:
        latest_sdk = latest_sdk_of_channel(downloader.get(DOTNET_RELEASES_INDEX), channel)
        sample_band = _band_of_net_version(latest_sdk, 'latest-sdk')
    else:
        sample_band = _band_of_net_version(latest_net_version, 'LATEST_NET_VERSION')
    bands: Dict[str, Any] = {}
    for band in _bands_from_to(library_pinned_band, sample_band):
        result = resolve_band(downloader, band)
        if result['status'] == 'missing' and band == library_pinned_band:
            raise WorkloadSetError('there is no workload set package for the library band %s' % band)
        bands[band] = result
    return {
        'status': 'ok',
        'toolkit_net_version': toolkit_net_version,
        'latest_net_version': latest_net_version,
        'latest_sdk': latest_sdk,
        'library_pinned_band': library_pinned_band,
        'sample_band': sample_band,
        'bands': bands,
    }
