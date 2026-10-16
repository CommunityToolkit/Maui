using System.Text;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CommunityToolkit.Maui.Sample.ViewModels.Essentials;

public partial class FileSaverViewModel(IFileSaver fileSaver) : BaseViewModel
{
	[ObservableProperty]
	public partial double Progress { get; set; }

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
	async Task SaveFile(CancellationToken cancellationToken)
	{
		if (!await ArePermissionsGranted())
		{
			return;
		}

		using var stream = new MemoryStream(Encoding.Default.GetBytes("Hello from the Community Toolkit!"));
		try
		{
			var fileName = await (Application.Current?.Windows[0].Page?.DisplayPromptAsync("FileSaver", "Choose filename") ?? Task.FromResult("test.txt"));

			// DisplayPromptAsync returns null when the prompt is canceled.
			if (fileName is null)
			{
				return;
			}

			// An empty name names no file: on iOS and Mac Catalyst the save would fail with a misleading "Access to the path ... is denied"
			if (string.IsNullOrWhiteSpace(fileName))
			{
				await Toast.Make("File is not saved, the file name is empty").Show(cancellationToken);
				return;
			}

			var initialFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
			var fileLocationResult = await fileSaver.SaveAsync(initialFolder, fileName, stream, cancellationToken);
			fileLocationResult.EnsureSuccess();

			await Toast.Make($"File is saved: {fileLocationResult.FilePath}").Show(cancellationToken);
		}
		catch (Exception ex)
		{
			await Toast.Make($"File is not saved, {ex.Message}").Show(cancellationToken);
		}
	}

	[RelayCommand]
	async Task SaveFileStatic(CancellationToken cancellationToken)
	{
		if (!await ArePermissionsGranted())
		{
			return;
		}

		using var stream = new MemoryStream(Encoding.Default.GetBytes("Hello from the Community Toolkit!"));
		var fileSaveResult = await FileSaver.SaveAsync("DCIM", "test.txt", stream, cancellationToken);
		if (fileSaveResult.IsSuccessful)
		{
			await Toast.Make($"File is saved: {fileSaveResult.FilePath}").Show(cancellationToken);
		}
		else
		{
			await Toast.Make($"File is not saved, {fileSaveResult.Exception.Message}").Show(cancellationToken);
		}
	}

	[RelayCommand]
	async Task SaveFileInstance(CancellationToken cancellationToken)
	{
		if (!await ArePermissionsGranted())
		{
			return;
		}

		using var client = new HttpClient();

		const string communityToolkitNuGetUrl = "https://www.nuget.org/api/v2/package/CommunityToolkit.Maui/5.0.0";
		await using var stream = await client.GetStreamAsync(communityToolkitNuGetUrl, cancellationToken);
		try
		{
			var fileSaverInstance = new FileSaverImplementation();
			var fileSaverResult = await fileSaverInstance.SaveAsync("communitytoolkit.maui.5.0.0.nupkg", stream, new Progress<double>(percent => Progress = percent / 100), cancellationToken);
			fileSaverResult.EnsureSuccess();

			await Toast.Make($"File is saved: {fileSaverResult.FilePath}").Show(cancellationToken);
		}
		catch (Exception ex)
		{
			await Toast.Make($"File is not saved, {ex.Message}").Show(cancellationToken);
		}
	}
}