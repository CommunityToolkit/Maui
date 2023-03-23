using System.Text;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Storage;
using CommunityToolkit.Mvvm.Input;

namespace CommunityToolkit.Maui.Sample.ViewModels.Essentials;

public partial class FileSaverViewModel : BaseViewModel
{
	readonly IFileSaver fileSaver;

	public FileSaverViewModel(IFileSaver fileSaver)
	{
		this.fileSaver = fileSaver;
	}

	[RelayCommand]
	async Task SaveFile(CancellationToken cancellationToken)
	{
		using var stream = new MemoryStream(Encoding.Default.GetBytes("Hello from the Community Toolkit!"));
		try
		{
			var fileName = Application.Current?.MainPage?.DisplayPromptAsync("FileSaver", "Choose filename") ?? Task.FromResult("test.txt");
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
		await using var stream = await client.GetStreamAsync("https://www.nuget.org/api/v2/package/CommunityToolkit.Maui/5.0.0", cancellationToken);
		try
		{
			var fileSaverInstance = new FileSaverImplementation();
			var fileSaverResult = await fileSaverInstance.SaveAsync("communitytoolkit.maui.5.0.0.nupkg", stream, cancellationToken);
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