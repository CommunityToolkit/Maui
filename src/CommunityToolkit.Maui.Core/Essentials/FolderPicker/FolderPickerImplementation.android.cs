using System.Web;
using Android.Content;
using Android.Provider;
using CommunityToolkit.Maui.Core.Primitives;
using Microsoft.Maui.ApplicationModel;
using AndroidUri = Android.Net.Uri;

namespace CommunityToolkit.Maui.Storage;

/// <inheritdoc />
public sealed partial class FolderPickerImplementation : IFolderPicker
{
	async Task<Folder> InternalPickAsync(string initialPath, CancellationToken cancellationToken)
	{
		if (OperatingSystem.IsAndroidVersionAtLeast(32))
		{
			var statusRead = await Permissions.RequestAsync<Permissions.StorageRead>().WaitAsync(cancellationToken).ConfigureAwait(false);
			if (statusRead is not PermissionStatus.Granted)
			{
				throw new PermissionException("Storage permission is not granted.");
			}
		}

		Folder? folder = null;
		const string baseUrl = "content://com.android.externalstorage.documents/document/primary%3A";
		if (Android.OS.Environment.ExternalStorageDirectory is not null)
		{
			initialPath = initialPath.Replace(Android.OS.Environment.ExternalStorageDirectory.AbsolutePath, string.Empty, StringComparison.InvariantCulture);
		}

		var initialFolderUri = AndroidUri.Parse(baseUrl + HttpUtility.UrlEncode(initialPath));

		var intent = new Intent(Intent.ActionOpenDocumentTree);
		intent.PutExtra(DocumentsContract.ExtraInitialUri, initialFolderUri);

		await IntermediateActivity.StartAsync(intent, (int)AndroidRequestCode.RequestCodeFolderPicker, onResult: OnResult).WaitAsync(cancellationToken);

		return folder ?? throw new FolderPickerException("Unable to get folder.");

		void OnResult(Intent resultIntent)
		{
			var path = EnsurePhysicalPath(resultIntent.Data);
			folder = new Folder(path, Path.GetFileName(path));
		}
	}

	Task<Folder> InternalPickAsync(CancellationToken cancellationToken)
	{
		return InternalPickAsync(GetExternalDirectory(), cancellationToken);
	}

	static string GetExternalDirectory()
	{
		return Android.OS.Environment.ExternalStorageDirectory?.Path ?? "/storage/emulated/0";
	}

	static string EnsurePhysicalPath(AndroidUri? uri)
	{
		if (uri is null)
		{
			throw new FolderPickerException("Path is not selected.");
		}

		const string uriSchemeFolder = "content";
		if (uri.Scheme is not null && uri.Scheme.Equals(uriSchemeFolder, StringComparison.OrdinalIgnoreCase))
		{
			// Example path would be /tree/primary:DCIM, or /tree/SDCare:DCIM
			var path = uri.PathSegments?[1] ?? throw new FolderPickerException("Unable to resolve path.");

			var split = path.Split(':');

			// Primary is the device's internal storage, and anything else is an SD card or other external storage
			if (split[0].Equals("primary", StringComparison.OrdinalIgnoreCase))
			{
				// Example for internal path /storage/emulated/0/DCIM
				return $"{Android.OS.Environment.ExternalStorageDirectory?.Path}/{split[1]}";
			}
			else
			{
				// Example for external path /storage/1B0B-0B1C/DCIM
				return $"/storage/{split[0]}/{split[1]}";
			}
		}

		throw new FolderPickerException($"Unable to resolve absolute path or retrieve contents of URI '{uri}'.");
	}
}