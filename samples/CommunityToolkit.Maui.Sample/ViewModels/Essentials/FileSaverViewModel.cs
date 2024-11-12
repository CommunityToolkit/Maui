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

	[RelayCommand]
	async Task SaveFile(CancellationToken cancellationToken)
	{
		using var stream = new MemoryStream(Encoding.Default.GetBytes("Hello from the Community Toolkit!"));
		try
		{
			var fileName = Application.Current?.Windows[0].Page?.DisplayPromptAsync("FileSaver", "Choose filename") ?? Task.FromResult("test.txt");
			var fileLocationResult = await fileSaver.SaveAsync(await fileName, stream, cancellationToken);
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
		using var client = new HttpClient();

		const string communityToolkitNuGetUrl = "https://www.nuget.org/api/v2/package/CommunityToolkit.Maui/5.0.0";
		await using var stream = await client.GetStreamAsync(communityToolkitNuGetUrl, cancellationToken);
		try
		{
			var fileSaverInstance = new FileSaverImplementation();
			var fileSaverResult = await fileSaverInstance.SaveAsync("communitytoolkit.maui.5.0.0.nupkg", stream, new Progress<double>(p => Progress = p), cancellationToken);
			fileSaverResult.EnsureSuccess();

			await Toast.Make($"File is saved: {fileSaverResult.FilePath}").Show(cancellationToken);
#if IOS || MACCATALYST
			fileSaverInstance.Dispose();
#endif
		}
		catch (Exception ex)
		{
			await Toast.Make($"File is not saved, {ex.Message}").Show(cancellationToken);
		}
	}
}