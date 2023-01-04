using System.Diagnostics;
using CommunityToolkit.Maui.Core.Primitives;
using Windows.Storage.Pickers;

namespace CommunityToolkit.Maui.Storage;

/// <inheritdoc />
public class FolderPickerImplementation : IFolderPicker
{
	/// <inheritdoc />
	public async Task<Folder> PickAsync(string initialPath, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		var folderPicker = new Windows.Storage.Pickers.FolderPicker()
		{
			SuggestedStartLocation = PickerLocationId.DocumentsLibrary
		};
		WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, Process.GetCurrentProcess().MainWindowHandle);
		folderPicker.FileTypeFilter.Add("*");
		var folderPickerOperation = folderPicker.PickSingleFolderAsync();

		void CancelFolderPickerOperation()
		{
			folderPickerOperation.Cancel();
		}

		cancellationToken.Register(CancelFolderPickerOperation);
		var folder = await folderPickerOperation;
		if (folder is null)
		{
			throw new FolderPickerException("Folder doesn't exist.");
		}

		return new Folder(folder.Path, folder.Name);
	}

	/// <inheritdoc />
	public Task<Folder> PickAsync(CancellationToken cancellationToken)
	{
		return PickAsync(string.Empty, cancellationToken);
	}
}