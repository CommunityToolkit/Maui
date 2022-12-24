using Android.Content;
using CommunityToolkit.Maui.Core.Primitives;
using Microsoft.Maui.ApplicationModel;
using AndroidUri = Android.Net.Uri;

namespace CommunityToolkit.Maui.Storage;

/// <inheritdoc />
public class FolderPickerImplementation : IFolderPicker
{
	const int requestCodeFolderPicker = 12345;

	/// <inheritdoc />
	public async Task<Folder> PickAsync(string initialPath, CancellationToken cancellationToken)
	{
		var status = await Permissions.RequestAsync<Permissions.StorageRead>().WaitAsync(cancellationToken).ConfigureAwait(false);
		if (status is not PermissionStatus.Granted)
		{
			throw new PermissionException("Storage permission is not granted.");
		}

		Folder? folder = null;

		var intent = new Intent(Intent.ActionOpenDocumentTree);
		var pickerIntent = Intent.CreateChooser(intent, string.Empty) ?? throw new InvalidOperationException("Unable to create intent.");

		await IntermediateActivity.StartAsync(pickerIntent, requestCodeFolderPicker, onResult: OnResult).WaitAsync(cancellationToken);

		return folder ?? throw new FolderPickerException("Unable to get folder.");

		void OnResult(Intent resultIntent)
		{
			var path = EnsurePhysicalPath(resultIntent.Data);
			folder = new Folder(path, Path.GetFileName(path));
		}
	}

	/// <inheritdoc />
	public Task<Folder> PickAsync(CancellationToken cancellationToken)
	{
		return PickAsync(GetExternalDirectory(), cancellationToken);
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
		if (uri.Scheme != null && uri.Scheme.Equals(uriSchemeFolder, StringComparison.OrdinalIgnoreCase))
		{
			var split = uri.Path? .Split(":") ?? throw new FolderPickerException("Unable to resolve path.");
			return Android.OS.Environment.ExternalStorageDirectory + "/" + split[1];
		}

		throw new FolderPickerException($"Unable to resolve absolute path or retrieve contents of URI '{uri}'.");
	}
}