using System.Diagnostics;
using System.Web;
using Android.Content;
using Android.Provider;
using CommunityToolkit.Maui.Core.Essentials;
using CommunityToolkit.Maui.Core.Extensions;
using CommunityToolkit.Maui.Core.Primitives;
using Microsoft.Maui.ApplicationModel;
using AndroidUri = Android.Net.Uri;

namespace CommunityToolkit.Maui.Storage;

/// <inheritdoc />
public sealed partial class FolderPickerImplementation : IFolderPicker
{
	async Task<Folder> InternalPickAsync(string initialPath, CancellationToken cancellationToken)
	{
		if (!OperatingSystem.IsAndroidVersionAtLeast(26) && !string.IsNullOrEmpty(initialPath))
		{
			Trace.WriteLine("Specifying an initial path is only supported on Android 26 and later.");
		}

		Folder? folder = null;

		if (!OperatingSystem.IsAndroidVersionAtLeast(33))
		{
			var statusRead = await Permissions.RequestAsync<Permissions.StorageRead>().WaitAsync(cancellationToken).ConfigureAwait(false);
			if (statusRead is not PermissionStatus.Granted)
			{
				throw new PermissionException("Storage permission is not granted.");
			}
		}

		if (Android.OS.Environment.ExternalStorageDirectory is not null)
		{
			initialPath = initialPath.Replace(Android.OS.Environment.ExternalStorageDirectory.AbsolutePath, string.Empty, StringComparison.InvariantCulture);
		}

		var initialFolderUri = AndroidUri.Parse(AndroidStorageConstants.ExternalStorageBaseUrl + HttpUtility.UrlEncode(initialPath));

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
		return InternalPickAsync(AndroidPathExtensions.GetExternalDirectory(), cancellationToken);
	}

	static string EnsurePhysicalPath(AndroidUri? uri)
	{
		if (uri is null)
		{
			throw new FolderPickerException("Path is not selected.");
		}

		return uri.ToPhysicalPath() ?? throw new FolderPickerException($"Unable to resolve absolute path or retrieve contents of URI '{uri}'.");
	}
}