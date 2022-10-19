using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;

namespace CommunityToolkit.Maui.Essentials;

/// <summary>
/// 
/// </summary>
public partial class SaveFileDialogImplementation : ISaveFileDialog
{
	/// <inheritdoc/>
	public async Task<bool> SaveAsync(string initialPath, string fileName, Stream stream, CancellationToken cancellationToken)
	{
		var status = await Permissions.RequestAsync<Permissions.StorageWrite>();
		if (status == PermissionStatus.Granted)
		{
			var dialog = new FileFolderDialog(Platform.CurrentActivity, FileFolderDialog.FileSelectionMode.FileSave, fileName);
			var path = await dialog.GetFileOrDirectoryAsync(GetExternalDirectory());
			if (string.IsNullOrEmpty(path))
			{
				return false;
			}

			try
			{
				await WriteStream(stream, path, cancellationToken);

				return true;
			}
			catch
			{
				await Toast.Make("File is not stored").Show(cancellationToken);
				return false;
			}
		}

		await Toast.Make("Storage permission is not granted").Show(cancellationToken);
		return false;
	}

	/// <inheritdoc/>
	public Task<bool> SaveAsync(string fileName, Stream stream, CancellationToken cancellationToken)
	{
		return SaveAsync(GetExternalDirectory(), fileName, stream, cancellationToken);
	}

	static string GetExternalDirectory()
	{
		return Platform.CurrentActivity?.GetExternalFilesDir(null)
			?.ParentFile?.ParentFile?.ParentFile?.ParentFile?.AbsolutePath ?? "/storage/emulated/0";
	}
}