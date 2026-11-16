using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core.Primitives;
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

	// The picked folder is shown in an alert, not in a toast: Android 12 and later cut a toast after two lines, which a folder's path can fill on its own
	static string DescribeFolder(Folder folder) => $"Name: {folder.Name}\nPath: {folder.Path}";

	static string CountFiles(string folderPath)
	{
		// On Android 10 and later, scoped storage keeps System.IO from listing files that other apps put in a shared folder, so a count would be too low
		if (OperatingSystem.IsAndroidVersionAtLeast(29))
		{
			return "not available on Android 10 and later (scoped storage hides files of other apps from System.IO)";
		}

		try
		{
			return $"{Directory.EnumerateFiles(folderPath).Count()}";
		}
		catch (Exception e) when (e is IOException or UnauthorizedAccessException)
		{
			return $"not available, {e.Message}";
		}
	}

	[RelayCommand]
	async Task PickFolder(CancellationToken cancellationToken)
	{
		if (!await ArePermissionsGranted())
		{
			return;
		}

		var initialFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
		var folderPickerResult = await folderPicker.PickAsync(initialFolder, cancellationToken);
		if (folderPickerResult.IsSuccessful)
		{
			await Shell.Current.CurrentPage.DisplayAlertAsync("Folder picked", DescribeFolder(folderPickerResult.Folder), "OK");
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
			await Shell.Current.CurrentPage.DisplayAlertAsync("Folder picked", $"{DescribeFolder(folderResult.Folder)}\nFiles count: {CountFiles(folderResult.Folder.Path)}", "OK");
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

			await Shell.Current.CurrentPage.DisplayAlertAsync("Folder picked", DescribeFolder(folderPickerResult.Folder), "OK");
		}
		catch (Exception e)
		{
			await Toast.Make($"Folder is not picked, {e.Message}").Show(cancellationToken);
		}
	}
}