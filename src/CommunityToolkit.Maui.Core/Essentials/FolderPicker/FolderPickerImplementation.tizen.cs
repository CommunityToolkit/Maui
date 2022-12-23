using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Primitives;

namespace CommunityToolkit.Maui.Storage;

/// <inheritdoc />
public class FolderPickerImplementation : IFolderPicker
{
	/// <inheritdoc />
	public async ValueTask<Folder> PickAsync(string initialPath, CancellationToken cancellationToken)
	{
		var status = await Permissions.RequestAsync<Permissions.StorageRead>();
		if (status is not PermissionStatus.Granted)
		{
			throw new PermissionException("Storage permission is not granted.");
		}

		var dialog = new FileFolderDialog(false, initialPath, cancellationToken: cancellationToken);
		var path = await dialog.Open();

		return new Folder(path, Path.GetFileName(path));
	}

	/// <inheritdoc />
	public ValueTask<Folder> PickAsync(CancellationToken cancellationToken)
	{
		return PickAsync(FileFolderDialog.TryGetExternalDirectory(), cancellationToken);
	}
}