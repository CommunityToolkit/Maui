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
}