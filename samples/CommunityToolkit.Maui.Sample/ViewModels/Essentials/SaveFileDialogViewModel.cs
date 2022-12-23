using System.Text;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Storage;
using CommunityToolkit.Mvvm.Input;

namespace CommunityToolkit.Maui.Sample.ViewModels.Essentials;

public partial class SaveFileDialogViewModel : BaseViewModel
{
	readonly ISaveFileDialog saveFileDialog;

	public SaveFileDialogViewModel(ISaveFileDialog saveFileDialog)
	{
		this.saveFileDialog = saveFileDialog;
	}

	[RelayCommand]
	async Task SaveFile(CancellationToken cancellationToken)
	{
		using var stream = new MemoryStream(Encoding.Default.GetBytes("Hello from the Community Toolkit!"));
		try
		{
			var fileLocation = await saveFileDialog.SaveAsync("test.txt", stream, cancellationToken);
			await Toast.Make($"File is saved: {fileLocation}").Show(cancellationToken);
		}
		catch (Exception ex)
		{
			await Toast.Make($"File is not saved, {ex.Message}").Show(cancellationToken);
		}
	}
}