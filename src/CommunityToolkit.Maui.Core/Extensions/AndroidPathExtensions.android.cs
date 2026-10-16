using Android.Provider;
using CommunityToolkit.Maui.Core.Essentials;
using Environment = Android.OS.Environment;
using Uri = Android.Net.Uri;

namespace CommunityToolkit.Maui.Core.Extensions;

/// <summary>
/// Extensions for <see cref="Android.Net.Uri"/>
/// </summary>
public static class AndroidPathExtensions
{
	/// <summary>
	/// Convert <see cref="Uri"/> to an absolute path (<see cref="string"/>).
	/// It returns <see langword="null"/> if the <see cref="Uri"/> is not resolvable, such as a document of the Downloads provider whose id
	/// is the number of a download or the id of a file in MediaStore, which holds no path.
	/// </summary>
	public static string? ToPhysicalPath(this Uri uri)
	{
		const string uriSchemeFolder = "content";
		if (uri.Scheme is null || !uri.Scheme.Equals(uriSchemeFolder, StringComparison.OrdinalIgnoreCase))
		{
			return null;
		}

		var pathSegments = uri.PathSegments;
		if (pathSegments is null || pathSegments.Count < 2)
		{
			return null;
		}

		// Example path would be /tree/primary:DCIM, or /tree/SDCare:DCIM, and /tree/primary:DCIM/document/primary:DCIM/Camera for a
		// document inside a picked tree, which is named by its own id after the id of the tree
		var path = pathSegments is ["tree", _, "document", var documentId, ..] ? documentId : pathSegments[1];

		// The Downloads provider names a file or folder of the storage by its absolute path after "raw:" (a file in a folder of the Download
		// folder, for example), and anything else by an id that holds no path: a download by its number, a file or folder that MediaStore
		// indexed by msf: or msd: and its MediaStore id, its root by "downloads"
		if (AndroidStorageConstants.DownloadsDocumentsAuthority.Equals(uri.Authority, StringComparison.Ordinal))
		{
			return path.StartsWith($"{AndroidStorageConstants.RawDocumentIdPrefix}/", StringComparison.Ordinal)
				? path[AndroidStorageConstants.RawDocumentIdPrefix.Length..]
				: null;
		}

		// The volume, and the path in it, which may itself hold a colon
		var pathSplit = path.Split(':', 2);
		if (pathSplit.Length < 2)
		{
			return null;
		}

		// Primary is the device's internal storage, and anything else is an SD card or other external storage
		// Example for internal path /storage/emulated/0/DCIM, and for external path /storage/1B0B-0B1C/DCIM
		var volumePath = pathSplit[0].Equals(AndroidStorageConstants.PrimaryStorage, StringComparison.OrdinalIgnoreCase)
			? Environment.ExternalStorageDirectory?.Path
			: $"/{AndroidStorageConstants.Storage}/{pathSplit[0]}";

		// The root of a volume (primary:) is the folder of the volume itself, without a trailing slash
		return pathSplit[1].Length is 0 ? volumePath : $"{volumePath}/{pathSplit[1]}";
	}

	/// <summary>
	/// Get External Directory Path
	/// </summary>
	public static string GetExternalDirectory()
	{
		return Environment.ExternalStorageDirectory?.Path ?? "/storage/emulated/0";
	}

	/// <summary>
	/// Convert an absolute path (<see cref="string"/>) to the <see cref="Uri"/> of its document in the documents provider of the device's
	/// storage volumes, which a document picker can start in (<see cref="DocumentsContract.ExtraInitialUri"/>). A relative path is a path in
	/// the primary storage, and the root of the file system is the root of the primary storage.
	/// It returns <see langword="null"/> if no storage volume holds the path, such as a path in the app's private storage.
	/// </summary>
	internal static Uri? ToExternalStorageDocumentUri(string path)
	{
		var documentId = GetExternalStorageDocumentId(path);
		return documentId is null ? null : DocumentsContract.BuildDocumentUri(AndroidStorageConstants.ExternalStorageDocumentsAuthority, documentId);
	}

	/// <summary>
	/// Whether the <see cref="Uri"/> is a document of the Downloads provider that is named by the number of its download in the
	/// DownloadManager, an id that holds no path.
	/// </summary>
	internal static bool IsDownloadManagerDocument(this Uri uri) =>
		AndroidStorageConstants.DownloadsDocumentsAuthority.Equals(uri.Authority, StringComparison.Ordinal)
		&& uri.PathSegments is ["document", { Length: > 0 } documentId]
		&& documentId.All(char.IsAsciiDigit);

	/// <summary>
	/// Convert a document that the Downloads provider created in its root for a document picker (<c>ACTION_CREATE_DOCUMENT</c>) to the
	/// absolute path of its file, from the display name and the summary that the provider reports for the document.
	/// It returns <see langword="null"/> for any other document.
	/// </summary>
	/// <remarks>
	/// The provider creates the file in the public Download folder and registers it with the DownloadManager under the file's name, as both the
	/// title (the display name) and the description (the summary) of the download, whose number is the id of the document. That name can differ
	/// from the one that the picker was given: the provider makes it unique ("report (1).txt") and fits it to the file system. Any other download
	/// has a title, a description and a folder of its own, and the provider does not report the folder.
	/// </remarks>
	internal static string? ToPathOfDocumentCreatedInDownloads(this Uri uri, string? displayName, string? summary)
	{
		if (!uri.IsDownloadManagerDocument()
			|| string.IsNullOrEmpty(displayName)
			|| !displayName.Equals(summary, StringComparison.Ordinal)
			|| displayName is "." or ".."
			|| displayName.Contains('/', StringComparison.Ordinal)
			|| Environment.DirectoryDownloads is not string downloadFolder)
		{
			return null;
		}

		return $"{GetExternalDirectory()}/{downloadFolder}/{displayName}";
	}

	// The document id is the volume and the path in it, the reverse of ToPhysicalPath: /storage/emulated/0/DCIM is primary:DCIM, and
	// /storage/1B0B-0B1C/DCIM is 1B0B-0B1C:DCIM.
	static string? GetExternalStorageDocumentId(string path)
	{
		if (!path.StartsWith("/", StringComparison.Ordinal))
		{
			return $"{AndroidStorageConstants.PrimaryStorage}:{path.Trim('/')}";
		}

		// The root of the file system holds the storage volumes without being on one of them: like an empty path, it names the root of the
		// primary storage
		if (path.TrimEnd('/').Length is 0)
		{
			return $"{AndroidStorageConstants.PrimaryStorage}:";
		}

		// The primary storage, under its own path or under one of its aliases
		foreach (var primaryStoragePath in new[] { GetExternalDirectory(), "/sdcard", "/storage/self/primary" })
		{
			if (GetPathInFolder(path, primaryStoragePath) is string pathInPrimaryStorage)
			{
				return $"{AndroidStorageConstants.PrimaryStorage}:{pathInPrimaryStorage}";
			}
		}

		// Any other volume, such as an SD card, is mounted at /storage/<volume>; /storage/emulated and /storage/self hold the primary storage of each user
		var storagePath = $"/{AndroidStorageConstants.Storage}/";
		if (path.StartsWith(storagePath, StringComparison.Ordinal))
		{
			var volume = path[storagePath.Length..].Split('/')[0];
			if (volume is not ("" or "emulated" or "self"))
			{
				return $"{volume}:{GetPathInFolder(path, storagePath + volume)}";
			}
		}

		// The app's private storage, for example
		return null;
	}

	// The path of a path in a folder without its separators (an empty string for the folder itself), or null if the path is not in the folder
	static string? GetPathInFolder(string path, string folder)
	{
		if (!path.StartsWith(folder, StringComparison.Ordinal))
		{
			return null;
		}

		var pathInFolder = path[folder.Length..];
		return pathInFolder.Length is 0 || pathInFolder[0] is '/' ? pathInFolder.Trim('/') : null;
	}
}