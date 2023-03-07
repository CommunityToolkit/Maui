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
		var folderPickerResult = await folderPicker.PickAsync(cancellationToken);
		if (folderPickerResult.IsSuccessful)
		{
			await Toast.Make($"Folder picked: Name - {folderPickerResult.Folder.Name}, Path - {folderPickerResult.Folder.Path}", ToastDuration.Long).Show(cancellationToken);
		}
		else
		{
			await Toast.Make($"Folder is not picked, {folderPickerResult.Exception.Message}").Show(cancellationToken);
		}
	}

	[RelayCommand]
	async Task PickFolderStatic(CancellationToken cancellationToken)
	{
		var folderResult = await FolderPicker.PickAsync("DCIM", cancellationToken);
		if (folderResult.IsSuccessful)
		{
			var filesCount = Directory.EnumerateFiles(folderResult.Folder.Path).Count();
			await Toast.Make($"Folder picked: Name - {folderResult.Folder.Name}, Path - {folderResult.Folder.Path}, Files count - {filesCount}", ToastDuration.Long).Show(cancellationToken);
		}
		else
		{
			await Toast.Make($"Folder is not picked, {folderResult.Exception.Message}").Show(cancellationToken);
		}
	}

	[RelayCommand]
	async Task PickFolderInstance(CancellationToken cancellationToken)
	{
		var folderPickerInstance = new FolderPickerImplementation();
		try
		{
			var folderPickerResult = await folderPickerInstance.PickAsync(cancellationToken);
			folderPickerResult.EnsureSuccess();

			await Toast.Make($"Folder picked: Name - {folderPickerResult.Folder.Name}, Path - {folderPickerResult.Folder.Path}", ToastDuration.Long).Show(cancellationToken);
#if IOS || MACCATALYST
			folderPickerInstance.Dispose();
#endif
		}
		catch (Exception e)
		{
			await Toast.Make($"Folder is not picked, {e.Message}").Show(cancellationToken);
		}
	}
}