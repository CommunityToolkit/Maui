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
		if (status is not PermissionStatus.Granted)
		{
			await Toast.Make("Storage permission is not granted").Show(cancellationToken);
			return null;
		}

		try
		{
			var currentActivity = Platform.CurrentActivity ?? throw new InvalidOperationException($"{nameof(Platform.CurrentActivity)} cannot be null");
			var dialog = new FileFolderDialog(Platform.CurrentActivity, FileSelectionMode.FolderChoose);
			var path = await dialog.GetFileOrDirectoryAsync(initialPath);

			return new Folder
			{
				Path = path,
				Name = new DirectoryInfo(path).Name
			};
		}
		catch (IOException)
		{
			return null;
		}
	}

	/// <inheritdoc />
	public Task<Folder?> PickAsync(CancellationToken cancellationToken)
	{
		return PickAsync(GetExternalDirectory(), cancellationToken);
	}

	static string GetExternalDirectory()
	{
		return Platform.CurrentActivity?.GetExternalFilesDir(null)?.ParentFile?.ParentFile?.ParentFile?.ParentFile?.AbsolutePath
				?? "/storage/emulated/0";
	}
}