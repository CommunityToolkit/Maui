using CommunityToolkit.Maui.Core.Primitives;
using Microsoft.Maui.ApplicationModel;

namespace CommunityToolkit.Maui.Storage;

/// <inheritdoc />
public class FolderPickerImplementation : IFolderPicker
{
	/// <inheritdoc />
	public async Task<Folder> PickAsync(string initialPath, CancellationToken cancellationToken)
	{
		var status = await Permissions.RequestAsync<Permissions.StorageRead>().WaitAsync(cancellationToken);
		if (status is not PermissionStatus.Granted)
		{
			throw new PermissionException("Storage permission is not granted.");
		}

		var dialog = new FileFolderDialog(false, initialPath, cancellationToken: cancellationToken);
		var path = await dialog.Open().WaitAsync(cancellationToken).ConfigureAwait(false);

		return new Folder(path, Path.GetFileName(path));
	}

	/// <inheritdoc />
	public Task<Folder> PickAsync(CancellationToken cancellationToken)
	{
		return PickAsync(FileFolderDialog.TryGetExternalDirectory(), cancellationToken);
	}
}