using System.Text;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Storage;
using CommunityToolkit.Mvvm.Input;

namespace CommunityToolkit.Maui.Sample.ViewModels.Essentials;

public partial class FolderPickerViewModel : BaseViewModel
{
	readonly IFolderPicker folderPicker;

	public FolderPickerViewModel(IFolderPicker folderPicker)
	{
		this.folderPicker = folderPicker;
	}

	[RelayCommand]
	async Task PickFolder(CancellationToken cancellationToken)
	{
		try
		{
			var folder = await folderPicker.PickAsync(cancellationToken);
			await Toast.Make($"Folder picked: Name - {folder.Name}, Path - {folder.Path}", ToastDuration.Long).Show(cancellationToken);
		}
		catch (Exception ex)
		{
			await Toast.Make($"Folder is not picked, {ex.Message}").Show(cancellationToken);
		}
	}

	[RelayCommand]
	async Task PickFolderStatic(CancellationToken cancellationToken)
	{
		try
		{
			var folder = await FolderPicker.PickAsync(cancellationToken);
			await Toast.Make($"Folder picked: Name - {folder.Name}, Path - {folder.Path}", ToastDuration.Long).Show(cancellationToken);
		}
		catch (Exception ex)
		{
			await Toast.Make($"Folder is not picked, {ex.Message}").Show(cancellationToken);
		}
	}

	[RelayCommand]
	async Task PickFolderInstance(CancellationToken cancellationToken)
	{
		using var stream = new MemoryStream(Encoding.Default.GetBytes("Hello from the Community Toolkit!"));
		try
		{
			var folderPickerInstance = new FolderPickerImplementation();
			var folder = await folderPickerInstance.PickAsync(cancellationToken);
			await Toast.Make($"Folder picked: Name - {folder.Name}, Path - {folder.Path}", ToastDuration.Long).Show(cancellationToken);
#if IOS || MACCATALYST
			folderPickerInstance.Dispose();
#endif
		}
		catch (Exception ex)
		{
			await Toast.Make($"Folder is not picked, {ex.Message}").Show(cancellationToken);
		}
	}
}