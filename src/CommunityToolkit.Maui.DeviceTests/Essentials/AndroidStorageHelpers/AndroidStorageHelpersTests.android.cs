using Android.Content.PM;
using Android.Provider;
using CommunityToolkit.Maui.Core.Essentials;
using CommunityToolkit.Maui.Core.Extensions;
using CommunityToolkit.Maui.Storage;
using AndroidApplication = Android.App.Application;
using AndroidEnvironment = Android.OS.Environment;
using AndroidUri = Android.Net.Uri;

namespace CommunityToolkit.Maui.DeviceTests;

// Uri.ToPhysicalPath(), the path of a document that the Downloads provider created (Uri.ToPathOfDocumentCreatedInDownloads()),
// AndroidPathExtensions.GetExternalDirectory(), AndroidStorageConstants and AndroidRequestCode (Android only): the helpers that turn what
// the document picker returns into the path that FileSaver and FolderPicker report.
[Category(TestCategory.AndroidStorageHelpers)]
public class AndroidStorageHelpersTests : FilesTestBase
{
	const string tree = "content://com.android.externalstorage.documents/tree/";
	const string document = "content://com.android.externalstorage.documents/document/";
	const string downloads = "content://com.android.providers.downloads.documents/";

	[Theory]
	[InlineData(tree + "primary%3ADCIM", "{external}/DCIM")]
	[InlineData(tree + "primary%3ADCIM%2FCamera", "{external}/DCIM/Camera")]
	[InlineData(tree + "PRIMARY%3ADCIM", "{external}/DCIM")]
	[InlineData(tree + "primary%3AMy%20Pictures", "{external}/My Pictures")]
	[InlineData(document + "primary%3AMy%20Docs%2Fa.txt", "{external}/My Docs/a.txt")]
	[InlineData("CONTENT://com.android.externalstorage.documents/tree/primary%3ADCIM", "{external}/DCIM")]
	[InlineData(tree + "1B0B-0B1C%3ADCIM", "/storage/1B0B-0B1C/DCIM")]
	[InlineData(tree + "1B0B-0B1C%3ADCIM%2FCamera", "/storage/1B0B-0B1C/DCIM/Camera")]
	public void ToPhysicalPathResolvesAContentUriToTheFolderOnTheStorageVolume(string uri, string expectedPath)
	{
		var physicalPath = AndroidUri.Parse(uri)!.ToPhysicalPath();

		Assert.Equal(expectedPath.Replace("{external}", ExternalStorage, StringComparison.Ordinal), physicalPath);
	}

	[Theory]
	[InlineData("file:///storage/emulated/0/DCIM")]
	[InlineData("http://example.com/tree/primary%3ADCIM")]
	[InlineData("relative/path/primary%3ADCIM")]
	[InlineData("content://authority")]
	[InlineData("content://authority/onlyone")]
	[InlineData("content:opaque")]
	[InlineData("content://media/external/file/123")]
	[InlineData(tree + "nocolon")]
	public void ToPhysicalPathReturnsNullWhenTheUriCannotBeResolved(string uri)
	{
		Assert.Null(AndroidUri.Parse(uri)!.ToPhysicalPath());
	}

	[Fact]
	public void ToPhysicalPathKeepsEverythingAfterTheFirstColon()
	{
		var physicalPath = AndroidUri.Parse(tree + "primary%3AMy%3AFolder")!.ToPhysicalPath();

		Assert.Equal($"{ExternalStorage}/My:Folder", physicalPath);
	}

	[Fact]
	public void ToPhysicalPathResolvesADocumentInsideAPickedTreeToTheDocument()
	{
		var physicalPath = AndroidUri.Parse(tree + "primary%3ADCIM/document/primary%3ADCIM%2FSub")!.ToPhysicalPath();

		Assert.Equal($"{ExternalStorage}/DCIM/Sub", physicalPath);
	}

	// The folder that the root of a volume resolves to; that the path does not end with a separator is the test below.
	[Fact]
	public void ToPhysicalPathResolvesTheRootOfAVolume()
	{
		var primaryRoot = AndroidUri.Parse(tree + "primary%3A")!.ToPhysicalPath();
		var otherRoot = AndroidUri.Parse(tree + "1B0B-0B1C%3A")!.ToPhysicalPath();

		Assert.NotNull(primaryRoot);
		Assert.NotNull(otherRoot);
		Assert.Equal(ExternalStorage, Path.TrimEndingDirectorySeparator(primaryRoot));
		Assert.Equal("/storage/1B0B-0B1C", Path.TrimEndingDirectorySeparator(otherRoot));
	}

	[Theory]
	[InlineData(tree + "primary%3A", "{external}")]
	[InlineData(tree + "1B0B-0B1C%3A", "/storage/1B0B-0B1C")]
	public void ToPhysicalPathReturnsTheRootOfAVolumeWithoutATrailingSlash(string uri, string expectedPath)
	{
		var physicalPath = AndroidUri.Parse(uri)!.ToPhysicalPath();

		Assert.Equal(expectedPath.Replace("{external}", ExternalStorage, StringComparison.Ordinal), physicalPath);
	}

	// The Downloads provider names a file or folder that it serves from the storage, such as one in a folder of the Download folder, by its
	// absolute path after "raw:": the path is that one, not a folder of a storage volume named "raw".
	[Theory]
	[InlineData(downloads + "document/raw%3A%2Fstorage%2Femulated%2F0%2FDownload%2Fsub%2Freport.txt", "/storage/emulated/0/Download/sub/report.txt")]
	[InlineData(downloads + "document/raw%3A%2Fstorage%2Femulated%2F0%2FDownload%2FMy%20Docs%2Fa%3Ab.txt", "/storage/emulated/0/Download/My Docs/a:b.txt")]
	[InlineData(downloads + "tree/raw%3A%2Fstorage%2Femulated%2F0%2FDownload%2Fsub", "/storage/emulated/0/Download/sub")]
	[InlineData(downloads + "tree/raw%3A%2Fstorage%2Femulated%2F0%2FDownload%2Fsub/document/raw%3A%2Fstorage%2Femulated%2F0%2FDownload%2Fsub%2Finner", "/storage/emulated/0/Download/sub/inner")]
	public void ToPhysicalPathResolvesARawDocumentOfTheDownloadsProviderToThePathInItsId(string uri, string expectedPath)
	{
		Assert.Equal(expectedPath, AndroidUri.Parse(uri)!.ToPhysicalPath());
	}

	// Any other id of the Downloads provider holds no path: the number of a download (such as a file that the provider created in its root),
	// msf: or msd: and the MediaStore id of a file or folder that MediaStore indexed (in any folder of the Download folder), the root of the
	// provider, and "raw:" without an absolute path. None of them is a folder of a storage volume named "msf".
	[Theory]
	[InlineData(downloads + "document/19")]
	[InlineData(downloads + "document/msf%3A1545")]
	[InlineData(downloads + "document/msd%3A12")]
	[InlineData(downloads + "document/downloads")]
	[InlineData(downloads + "tree/downloads")]
	[InlineData(downloads + "tree/msd%3A12/document/msf%3A13")]
	[InlineData(downloads + "document/raw%3ADownload%2Freport.txt")]
	public void ToPhysicalPathReturnsNullForADocumentOfTheDownloadsProviderWhoseIdHoldsNoPath(string uri)
	{
		Assert.Null(AndroidUri.Parse(uri)!.ToPhysicalPath());
	}

	// A document that the Downloads provider created in its root for a document picker is a file of the public Download folder, which the
	// provider registered with the DownloadManager under the file's name as both the title (the display name) and the description (the summary)
	// of the download. The name is the one that the provider gave the file, which it can have made unique.
	[Theory]
	[InlineData("19", "report.txt")]
	[InlineData("20", "report (1).txt")]
	[InlineData("21", "My résumé.pdf")]
	public void ToPathOfDocumentCreatedInDownloadsIsTheFileOfThatNameInTheDownloadFolder(string documentId, string fileName)
	{
		var path = AndroidUri.Parse($"{downloads}document/{documentId}")!.ToPathOfDocumentCreatedInDownloads(fileName, fileName);

		Assert.Equal($"{ExternalStorage}/Download/{fileName}", path);
	}

	// Any other document has no path that this tells: a download that the provider did not create for a picker (its title, its description and
	// its folder are its own), a document with an empty name or with a name that is no file name, a file that MediaStore indexed (msf:, in any
	// folder of the Download folder), a raw: document (ToPhysicalPath reads its path from its id) and a document of another provider.
	[Theory]
	[InlineData(downloads + "document/19", "Invoice March", "Sent by the mail app")]
	[InlineData(downloads + "document/19", "report.txt", "")]
	[InlineData(downloads + "document/19", "", "")]
	[InlineData(downloads + "document/19", "..", "..")]
	[InlineData(downloads + "document/19", "sub/report.txt", "sub/report.txt")]
	[InlineData(downloads + "document/msf%3A1545", "report.txt", "report.txt")]
	[InlineData(downloads + "document/raw%3A%2Fstorage%2Femulated%2F0%2FDownload%2Freport.txt", "report.txt", "report.txt")]
	[InlineData(document + "19", "report.txt", "report.txt")]
	public void ToPathOfDocumentCreatedInDownloadsIsNullForAnyOtherDocument(string uri, string displayName, string summary)
	{
		Assert.Null(AndroidUri.Parse(uri)!.ToPathOfDocumentCreatedInDownloads(displayName, summary));
	}

	// A provider that reports no display name or no summary (a null column) describes no document that it created for a picker.
	[Fact]
	public void ToPathOfDocumentCreatedInDownloadsIsNullWithoutADisplayNameOrASummary()
	{
		var uri = AndroidUri.Parse($"{downloads}document/19")!;

		Assert.Null(uri.ToPathOfDocumentCreatedInDownloads(null, null));
		Assert.Null(uri.ToPathOfDocumentCreatedInDownloads("report.txt", null));
		Assert.Null(uri.ToPathOfDocumentCreatedInDownloads(null, "report.txt"));
	}

	// The document that a document picker starts in (DocumentsContract.ExtraInitialUri) for the initial path of FileSaver and FolderPicker: a folder of
	// the primary storage under its own path or one of its aliases, a folder of another volume under /storage/<volume>, a relative path (in the primary
	// storage) and the root of the file system (the root of the primary storage).
	[Theory]
	[InlineData("{external}/DCIM", "primary:DCIM")]
	[InlineData("{external}", "primary:")]
	[InlineData("/sdcard/Download", "primary:Download")]
	[InlineData("/sdcard", "primary:")]
	[InlineData("/storage/self/primary/DCIM", "primary:DCIM")]
	[InlineData("/storage/1B0B-0B1C/DCIM", "1B0B-0B1C:DCIM")]
	[InlineData("/storage/1B0B-0B1C", "1B0B-0B1C:")]
	[InlineData("DCIM/Camera", "primary:DCIM/Camera")]
	[InlineData("/", "primary:")]
	public void ToExternalStorageDocumentUriNamesTheFolderOnItsStorageVolume(string path, string expectedDocumentId)
	{
		var uri = AndroidPathExtensions.ToExternalStorageDocumentUri(path.Replace("{external}", ExternalStorage, StringComparison.Ordinal));

		Assert.NotNull(uri);
		Assert.Equal(AndroidStorageConstants.ExternalStorageDocumentsAuthority, uri.Authority);
		Assert.Equal(expectedDocumentId, DocumentsContract.GetDocumentId(uri));
	}

	// A path that only starts like a storage folder is not in it (/sdcard2 is not in /sdcard), the folders that hold the primary storage of each user
	// (/storage/emulated, /storage/self) are no volume, and the app's private storage is on no volume that the picker shows: no document names them.
	[Theory]
	[InlineData("/sdcard2/Download")]
	[InlineData("/sdcardfoo")]
	[InlineData("/storage/self/primary2/DCIM")]
	[InlineData("{external}Backup/DCIM")]
	[InlineData("/storage/emulated/10/DCIM")]
	[InlineData("/storage/self/DCIM")]
	[InlineData("/storage/emulated")]
	[InlineData("{appdata}")]
	public void ToExternalStorageDocumentUriIgnoresAPathOnNoStorageVolume(string path)
	{
		var resolvedPath = path
			.Replace("{external}", ExternalStorage, StringComparison.Ordinal)
			.Replace("{appdata}", FileSystem.AppDataDirectory, StringComparison.Ordinal);

		Assert.Null(AndroidPathExtensions.ToExternalStorageDocumentUri(resolvedPath));
	}

	[Fact]
	public void GetExternalDirectoryReturnsTheRootOfThePrimaryStorage()
	{
		var directory = AndroidPathExtensions.GetExternalDirectory();

		Assert.Equal(ExternalStorage, directory);
		Assert.True(Directory.Exists(directory), $"The external directory '{directory}' does not exist.");
	}

	[Fact]
	public void StorageConstantsDescribeTheDocumentProviderOfThePrimaryStorage()
	{
		var rootUri = DocumentsContract.BuildDocumentUri(AndroidStorageConstants.ExternalStorageDocumentsAuthority, $"{AndroidStorageConstants.PrimaryStorage}:")!;

		Assert.Equal("primary", AndroidStorageConstants.PrimaryStorage);
		Assert.Equal("storage", AndroidStorageConstants.Storage);
		Assert.Equal("com.android.externalstorage.documents", AndroidStorageConstants.ExternalStorageDocumentsAuthority);
		Assert.Equal("content://com.android.externalstorage.documents/document/primary%3A", rootUri.ToString());
		Assert.Equal($"{AndroidStorageConstants.PrimaryStorage}:", DocumentsContract.GetDocumentId(rootUri));
	}

	[Fact]
	public void TheDocumentProviderOfTheStorageConstantsIsInstalled()
	{
		var authority = AndroidStorageConstants.ExternalStorageDocumentsAuthority;

		var provider = AndroidApplication.Context.PackageManager!.ResolveContentProvider(authority, PackageInfoFlags.MatchAll);

		Assert.NotNull(provider);
		Assert.Equal("com.android.externalstorage", provider.PackageName);
	}

	[Fact]
	public void TheDownloadsDocumentProviderOfTheStorageConstantsIsInstalled()
	{
		var authority = AndroidStorageConstants.DownloadsDocumentsAuthority;

		var provider = AndroidApplication.Context.PackageManager!.ResolveContentProvider(authority, PackageInfoFlags.MatchAll);

		Assert.NotNull(provider);
		Assert.Equal("com.android.providers.downloads", provider.PackageName);
	}

	[Fact]
	public void RequestCodesAreDistinctAndNameTheFolderAndTheFilePicker()
	{
		Assert.Equal(2000, (int)AndroidRequestCode.RequestCodeFolderPicker);
		Assert.Equal(2001, (int)AndroidRequestCode.RequestCodeSaveFilePicker);
		Assert.Equal(["RequestCodeFolderPicker", "RequestCodeSaveFilePicker"], Enum.GetNames<AndroidRequestCode>());
	}

	static string ExternalStorage => AndroidEnvironment.ExternalStorageDirectory!.Path;
}