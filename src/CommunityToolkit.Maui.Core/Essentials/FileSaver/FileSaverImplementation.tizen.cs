using Microsoft.Maui.ApplicationModel;

namespace CommunityToolkit.Maui.Storage;

/// <inheritdoc />
public sealed partial class FileSaverImplementation : IFileSaver
{
	async Task<string> InternalSaveAsync(string initialPath, string fileName, Stream stream, CancellationToken cancellationToken)
	{
		var status = await Permissions.RequestAsync<Permissions.StorageRead>().WaitAsync(cancellationToken);
		if (status is not PermissionStatus.Granted)
		{
			throw new PermissionException("Storage permission is not granted.");
		}

		using var dialog = new FileFolderDialog(true, initialPath, fileName: fileName);
		var path = await dialog.Open().WaitAsync(cancellationToken).ConfigureAwait(false);

		if (string.IsNullOrEmpty(path))
		{
			throw new FileSaveException("Path doesn't exist.");
		}

		await WriteStream(stream, path, cancellationToken).ConfigureAwait(false);
		return path;
	}

	Task<string> InternalSaveAsync(string fileName, Stream stream, CancellationToken cancellationToken)
	{
		return InternalSaveAsync(FileFolderDialog.GetExternalDirectory(), fileName, stream, cancellationToken);
	}
}