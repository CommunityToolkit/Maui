using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Mvvm.Input;

namespace CommunityToolkit.Maui.Sample.ViewModels.Essentials;

public partial class FolderPickerViewModel:BaseViewModel
{
	readonly IFolderPicker folderPicker;

	public FolderPickerViewModel(IFolderPicker folderPicker)
	{
		this.folderPicker = folderPicker;
	}
	
	[RelayCommand]
	async Task PickFolder(CancellationToken cancellationToken)
	{
		var folder = await folderPicker.PickAsync(cancellationToken);
		if (folder != null)
		{
			await Toast.Make($"Folder picked: Name - {folder.Name}, Path - {folder.Path}").Show(cancellationToken);
		}
	}
}