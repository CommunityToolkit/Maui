using System.Diagnostics;
using CommunityToolkit.Maui.Core.Primitives;
using Windows.Storage.Pickers;

namespace CommunityToolkit.Maui.Storage;

/// <inheritdoc />
public sealed partial class FolderPickerImplementation : IFolderPicker
{
	async Task<Folder> InternalPickAsync(string initialPath, CancellationToken cancellationToken)
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

		await using var _ = cancellationToken.Register(CancelFolderPickerOperation);
		var folder = await folderPickerOperation;
		if (folder is null)
		{
			throw new FolderPickerException("Operation cancelled or Folder doesn't exist.");
		}

		return new Folder(folder.Path, folder.Name);
	}

	Task<Folder> InternalPickAsync(CancellationToken cancellationToken)
	{
		return InternalPickAsync(string.Empty, cancellationToken);
	}
}