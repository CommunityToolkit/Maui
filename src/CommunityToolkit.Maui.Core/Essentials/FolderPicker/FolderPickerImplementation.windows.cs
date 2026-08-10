using CommunityToolkit.Maui.Core.Primitives;
using Microsoft.Maui.ApplicationModel;
using Microsoft.UI;
using Microsoft.Windows.AppLifecycle;
using Microsoft.Windows.Storage.Pickers;

namespace CommunityToolkit.Maui.Storage;

/// <inheritdoc />
public sealed partial class FolderPickerImplementation : IFolderPicker
{
	async Task<Folder> InternalPickAsync(string initialPath, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		if (IPlatformApplication.Current?.Application.Windows[0].Handler?.PlatformView is not MauiWinUIWindow window)
		{
			throw new FolderPickerException(
				"Cannot present folder picker: No active window found. Ensure the app is active with a visible window.");
		}

		var folderPicker = new Microsoft.Windows.Storage.Pickers.FolderPicker(window.AppWindow.Id)
		{
			SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
			SuggestedFolder = initialPath
		};

		var folderPickerOperation = folderPicker.PickSingleFolderAsync();

		void CancelFolderPickerOperation()
		{
			folderPickerOperation.Cancel();
		}

		await using var _ = cancellationToken.Register(CancelFolderPickerOperation);
		var folder = await folderPickerOperation;
		if (folder is null)
		{
			throw new OperationCanceledException("Operation cancelled.");
		}

		if (string.IsNullOrEmpty(folder.Path))
		{
			throw new FolderPickerException("Folder doesn't exist.");
		}

		return new Folder(folder.Path, new DirectoryInfo(folder.Path).Name);
	}

	Task<Folder> InternalPickAsync(CancellationToken cancellationToken)
	{
		return InternalPickAsync(string.Empty, cancellationToken);
	}
}