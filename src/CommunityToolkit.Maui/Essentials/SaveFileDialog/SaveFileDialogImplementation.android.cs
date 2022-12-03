using CommunityToolkit.Maui.Core;

namespace CommunityToolkit.Maui.Storage;

/// <summary>
/// 
/// </summary>
public partial class SaveFileDialogImplementation : ISaveFileDialog
{
	/// <inheritdoc/>
	public async Task SaveAsync(string initialPath, string fileName, Stream stream, CancellationToken cancellationToken)
	{
		var status = await Permissions.RequestAsync<Permissions.StorageWrite>();
		if (status is not PermissionStatus.Granted)
		{
			throw new PermissionException("Storage permission is not granted");
		}

		var currentActivity = Platform.CurrentActivity ?? throw new InvalidOperationException($"{nameof(Platform.CurrentActivity)} cannot be null");
		var dialog = new FileFolderDialog(currentActivity, FileSelectionMode.FileSave, fileName);
		var path = await dialog.GetFileOrDirectoryAsync(GetExternalDirectory(), cancellationToken: cancellationToken);

		if (string.IsNullOrEmpty(path))
		{
			throw new FileSaveException("Path doesn't exist");
		}

		await WriteStream(stream, path, cancellationToken).ConfigureAwait(false);
	}

	/// <inheritdoc/>
	public Task SaveAsync(string fileName, Stream stream, CancellationToken cancellationToken)
	{
		return SaveAsync(GetExternalDirectory(), fileName, stream, cancellationToken);
	}

	static string GetExternalDirectory()
	{
		return Platform.CurrentActivity?.GetExternalFilesDir(null)
			?.ParentFile?.ParentFile?.ParentFile?.ParentFile?.AbsolutePath ?? "/storage/emulated/0";
	}
}