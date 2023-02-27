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
			var fileLocationResult = await fileSaver.SaveAsync("test.txt", stream, cancellationToken);
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
		using var stream = new MemoryStream(Encoding.Default.GetBytes("Hello from the Community Toolkit!"));
		try
		{
			var fileSaverInstance = new FileSaverImplementation();
			var fileSaverResult = await fileSaverInstance.SaveAsync("test.txt", stream, cancellationToken);
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