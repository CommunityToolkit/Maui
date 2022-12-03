using CommunityToolkit.Maui.Core;

namespace CommunityToolkit.Maui.Storage;

/// <inheritdoc />
public partial class SaveFileDialogImplementation : ISaveFileDialog
{
	/// <inheritdoc />
	public async Task SaveAsync(string initialPath, string fileName, Stream stream, CancellationToken cancellationToken)
	{
		var status = await Permissions.RequestAsync<Permissions.StorageRead>();
		if (status is not PermissionStatus.Granted)
		{
			throw new PermissionException("Storage permission is not granted");
		}

		var dialog = new FileFolderDialog(FileSelectionMode.FileSave, initialPath, fileName: fileName, cancellationToken: cancellationToken);
		var path = await dialog.Open();

		if (string.IsNullOrEmpty(path))
		{
			throw new FileSaveException("Path doesn't exist");
		}

		await WriteStream(stream, path, cancellationToken).ConfigureAwait(false);
	}

	/// <inheritdoc />
	public Task SaveAsync(string fileName, Stream stream, CancellationToken cancellationToken)
	{
		return SaveAsync(GetExternalDirectory(), fileName, stream, cancellationToken);
	}

	static string GetExternalDirectory()
	{
		string? externalDirectory = null;
		foreach (var storage in Tizen.System.StorageManager.Storages)
		{
			if (storage.StorageType == Tizen.System.StorageArea.External)
			{
				externalDirectory = storage.RootDirectory;
			}
		}
		return externalDirectory ?? "/home/owner/media/";
	}
}