using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Primitives;

namespace CommunityToolkit.Maui.Storage;

/// <inheritdoc />
public class FolderPickerImplementation : IFolderPicker
{
	/// <inheritdoc />
	public async Task<Folder> PickAsync(string initialPath, CancellationToken cancellationToken)
	{
		var status = await Permissions.RequestAsync<Permissions.StorageRead>();
		if (status is not PermissionStatus.Granted)
		{
			throw new PermissionException("Storage permission is not granted");
		}

		var dialog = new FileFolderDialog(FileSelectionMode.FolderChoose, initialPath, cancellationToken: cancellationToken);
		var path = await dialog.Open();

		return new Folder
		{
			Path = path,
			Name = Path.GetFileName(path)
		};
	}

	/// <inheritdoc />
	public Task<Folder> PickAsync(CancellationToken cancellationToken)
	{
		return PickAsync(GetExternalDirectory(), cancellationToken);
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