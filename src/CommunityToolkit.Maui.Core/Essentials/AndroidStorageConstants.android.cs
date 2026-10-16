namespace CommunityToolkit.Maui.Core.Essentials;

static class AndroidStorageConstants
{
	public const string PrimaryStorage = "primary";
	public const string Storage = "storage";
	// The authority of the documents provider of the device's storage volumes
	public const string ExternalStorageDocumentsAuthority = "com.android.externalstorage.documents";
	// The authority of the documents provider of the Downloads root (the files of the public Download folder and the downloads of the DownloadManager)
	public const string DownloadsDocumentsAuthority = "com.android.providers.downloads.documents";
	// The prefix of the id of a document of the Downloads provider that names its file or folder by the absolute path
	public const string RawDocumentIdPrefix = "raw:";
}