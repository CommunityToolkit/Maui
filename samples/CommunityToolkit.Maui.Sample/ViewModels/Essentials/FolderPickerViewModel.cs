using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Storage;
using CommunityToolkit.Mvvm.Input;

namespace CommunityToolkit.Maui.Sample.ViewModels.Essentials;

public partial class FolderPickerViewModel(IFolderPicker folderPicker) : BaseViewModel
{
	readonly IFolderPicker folderPicker = folderPicker;

	static async Task<bool> ArePermissionsGranted()
	{
		var readPermissionStatus = await Permissions.RequestAsync<Permissions.StorageRead>();
		var writePermissionStatus = await Permissions.RequestAsync<Permissions.StorageWrite>();

		if (readPermissionStatus is PermissionStatus.Granted
			&& writePermissionStatus is PermissionStatus.Granted)
		{
			return true;
		}

		await Shell.Current.CurrentPage.DisplayAlertAsync("Storage permission is not granted.", "Please grant the permission to use this feature.", "OK");

		return false;
	}

	[RelayCommand]
	async Task PickFolder(CancellationToken cancellationToken)
	{
		if (!await ArePermissionsGranted())
		{
			return;
		}

		var folderPickerResult = await folderPicker.PickAsync(cancellationToken);
		if (folderPickerResult.IsSuccessful)
		{
			await Toast.Make($"Folder picked: Name - {folderPickerResult.Folder.Name}, Path - {folderPickerResult.Folder.Path}", ToastDuration.Long).Show(cancellationToken);
		}
		else
		{
			await Toast.Make($"Folder is not picked, {folderPickerResult.Exception.Message}").Show(cancellationToken);
		}
	}

	[RelayCommand]
	async Task PickFolderStatic(CancellationToken cancellationToken)
	{
		if (!await ArePermissionsGranted())
		{
			return;
		}

		var folderResult = await FolderPicker.PickAsync("DCIM", cancellationToken);
		if (folderResult.IsSuccessful)
		{
			var filesCount = Directory.EnumerateFiles(folderResult.Folder.Path).Count();
			await Toast.Make($"Folder picked: Name - {folderResult.Folder.Name}, Path - {folderResult.Folder.Path}, Files count - {filesCount}", ToastDuration.Long).Show(cancellationToken);
		}
		else
		{
			await Toast.Make($"Folder is not picked, {folderResult.Exception.Message}").Show(cancellationToken);
		}
	}

	[RelayCommand]
	async Task PickFolderInstance(CancellationToken cancellationToken)
	{
		if (!await ArePermissionsGranted())
		{
			return;
		}

		var folderPickerInstance = new FolderPickerImplementation();
		try
		{
			var folderPickerResult = await folderPickerInstance.PickAsync(cancellationToken);
			folderPickerResult.EnsureSuccess();

			await Toast.Make($"Folder picked: Name - {folderPickerResult.Folder.Name}, Path - {folderPickerResult.Folder.Path}", ToastDuration.Long).Show(cancellationToken);
#if IOS || MACCATALYST
			folderPickerInstance.Dispose();
#endif
		}
		catch (Exception e)
		{
			await Toast.Make($"Folder is not picked, {e.Message}").Show(cancellationToken);
		}
	}
}