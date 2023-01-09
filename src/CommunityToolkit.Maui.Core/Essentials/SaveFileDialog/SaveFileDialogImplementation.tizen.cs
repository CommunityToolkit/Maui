using Microsoft.Maui.ApplicationModel;

namespace CommunityToolkit.Maui.Storage;

/// <inheritdoc />
partial class SaveFileDialogImplementation : ISaveFileDialog
{
	/// <inheritdoc />
	public async Task<string> SaveAsync(string initialPath, string fileName, Stream stream, CancellationToken cancellationToken)
	{
		var status = await Permissions.RequestAsync<Permissions.StorageRead>().WaitAsync(cancellationToken);
		if (status is not PermissionStatus.Granted)
		{
			throw new PermissionException("Storage permission is not granted.");
		}

		var dialog = new FileFolderDialog(true, initialPath, fileName: fileName, cancellationToken: cancellationToken);
		var path = await dialog.Open().WaitAsync(cancellationToken).ConfigureAwait(false);

		if (string.IsNullOrEmpty(path))
		{
			throw new FileSaveException("Path doesn't exist.");
		}

		await WriteStream(stream, path, cancellationToken).ConfigureAwait(false);
		return path;
	}

	/// <inheritdoc />
	public Task<string> SaveAsync(string fileName, Stream stream, CancellationToken cancellationToken)
	{
		return SaveAsync(FileFolderDialog.TryGetExternalDirectory(), fileName, stream, cancellationToken);
	}
}