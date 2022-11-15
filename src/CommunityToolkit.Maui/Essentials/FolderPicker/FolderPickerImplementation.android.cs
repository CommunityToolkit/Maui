using CommunityToolkit.Maui.Alerts;
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

		var currentActivity = Platform.CurrentActivity ?? throw new InvalidOperationException($"{nameof(Platform.CurrentActivity)} cannot be null");
		var dialog = new FileFolderDialog(currentActivity, FileSelectionMode.FolderChoose);
		var path = await dialog.GetFileOrDirectoryAsync(initialPath);

		return new Folder
		{
			Path = path,
			Name = new DirectoryInfo(path).Name
		};
	}

	/// <inheritdoc />
	public Task<Folder> PickAsync(CancellationToken cancellationToken)
	{
		return PickAsync(GetExternalDirectory(), cancellationToken);
	}

	static string GetExternalDirectory()
	{
		return Platform.CurrentActivity?.GetExternalFilesDir(null)?.ParentFile?.ParentFile?.ParentFile?.ParentFile?.AbsolutePath
				?? "/storage/emulated/0";
	}
}