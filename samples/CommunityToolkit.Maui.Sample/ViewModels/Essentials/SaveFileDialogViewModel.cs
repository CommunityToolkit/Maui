using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Essentials;
using CommunityToolkit.Mvvm.Input;

namespace CommunityToolkit.Maui.Sample.ViewModels.Essentials;

public partial class SaveFileDialogViewModel : BaseViewModel
{
	[RelayCommand]
	async Task SaveFile(CancellationToken cancellationToken)
	{
		using var stream = new MemoryStream();
		var isSaved = await SaveFileDialog.SaveAsync("test.txt", stream, cancellationToken);
		if (isSaved)
		{
			await Toast.Make("File is saved").Show(cancellationToken);
		}
		else
		{
			await Toast.Make("File is not saved").Show(cancellationToken);
		}
	}
}