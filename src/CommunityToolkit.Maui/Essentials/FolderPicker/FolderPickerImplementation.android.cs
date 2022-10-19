using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Primitives;

namespace CommunityToolkit.Maui.Essentials;

/// <inheritdoc />
public class FolderPickerImplementation : IFolderPicker
{
	/// <inheritdoc />
	public async Task<Folder?> PickAsync(string initialPath, CancellationToken cancellationToken)
	{
		var status = await Permissions.RequestAsync<Permissions.StorageRead>();
		if (status == PermissionStatus.Granted)
		{
			var dialog = new FileFolderDialog(Platform.CurrentActivity, FileFolderDialog.FileSelectionMode.FolderChoose);
			var path = await dialog.GetFileOrDirectoryAsync(initialPath);
			if (string.IsNullOrEmpty(path))
			{
				return null;
			}

			return new Folder
			{
				Path = path,
				Name = new DirectoryInfo(path).Name
			};
		}

		await Toast.Make("Storage permission is not granted").Show(cancellationToken);
		return null;
	}

	/// <inheritdoc />
	public Task<Folder?> PickAsync(CancellationToken cancellationToken)
	{
		return PickAsync(GetExternalDirectory(), cancellationToken);
	}
	
	static string GetExternalDirectory()
	{
		return Platform.CurrentActivity?.GetExternalFilesDir(null)
			?.ParentFile?.ParentFile?.ParentFile?.ParentFile?.AbsolutePath ?? "/storage/emulated/0";
	}
}