using System.Diagnostics;
using System.Runtime.Versioning;
using Android.Content;
using Android.Provider;
using CommunityToolkit.Maui.Core.Essentials;
using CommunityToolkit.Maui.Core.Extensions;
using CommunityToolkit.Maui.Core.Primitives;
using Microsoft.Maui.ApplicationModel;
using AndroidUri = Android.Net.Uri;

namespace CommunityToolkit.Maui.Storage;

/// <inheritdoc />
[SupportedOSPlatform("Android26.0")]
public sealed partial class FolderPickerImplementation : IFolderPicker
{
	static async Task<Folder> InternalPickAsync(string initialPath, CancellationToken cancellationToken)
	{
		if (!OperatingSystem.IsAndroidVersionAtLeast(26) && !string.IsNullOrEmpty(initialPath))
		{
			Trace.WriteLine("Specifying an initial path is only supported on Android 26 and later.");
		}

		Folder? folder = null;

		var intent = new Intent(Intent.ActionOpenDocumentTree);

		// No storage volume holds a folder of the app's private storage: the picker then starts where it chooses.
		if (AndroidPathExtensions.ToExternalStorageDocumentUri(initialPath) is AndroidUri initialFolderUri)
		{
			intent.PutExtra(DocumentsContract.ExtraInitialUri, initialFolderUri);
		}

		await ActivityResultManager.StartAsync(intent, AndroidRequestCode.RequestCodeFolderPicker, OnResult, cancellationToken);

		return folder ?? throw new FolderPickerException("Unable to get folder.");

		void OnResult(Intent resultIntent)
		{
			var path = EnsurePhysicalPath(resultIntent.Data);
			folder = new Folder(path, Path.GetFileName(path));
		}
	}

	static Task<Folder> InternalPickAsync(CancellationToken cancellationToken)
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