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
			var fileLocation = await fileSaver.SaveAsync("test.txt", stream, cancellationToken);
			await Toast.Make($"File is saved: {fileLocation}").Show(cancellationToken);
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
		try
		{
			var fileLocation = await FileSaver.SaveAsync("test.txt", stream, cancellationToken);
			await Toast.Make($"File is saved: {fileLocation}").Show(cancellationToken);
		}
		catch (Exception ex)
		{
			await Toast.Make($"File is not saved, {ex.Message}").Show(cancellationToken);
		}
	}

	[RelayCommand]
	async Task SaveFileInstance(CancellationToken cancellationToken)
	{
		using var stream = new MemoryStream(Encoding.Default.GetBytes("Hello from the Community Toolkit!"));
		try
		{
			var fileSaverInstance = new FileSaverImplementation();
			var fileLocation = await fileSaverInstance.SaveAsync("test.txt", stream, cancellationToken);
			await Toast.Make($"File is saved: {fileLocation}").Show(cancellationToken);
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