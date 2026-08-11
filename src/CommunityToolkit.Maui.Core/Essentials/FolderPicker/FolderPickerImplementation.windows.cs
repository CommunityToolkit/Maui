#if NET11_0_OR_GREATER
using CommunityToolkit.Maui.Core.Primitives;
using Microsoft.Maui.ApplicationModel;
using Microsoft.UI;
using Microsoft.Windows.AppLifecycle;
using Microsoft.Windows.Storage.Pickers;
#else
using System.Diagnostics;
using CommunityToolkit.Maui.Core.Primitives;
using Windows.Storage.Pickers;
#endif

namespace CommunityToolkit.Maui.Storage;

/// <inheritdoc />
public sealed partial class FolderPickerImplementation : IFolderPicker
{
	async Task<Folder> InternalPickAsync(string initialPath, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
#if NET11_0_OR_GREATER
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
#else
		var folderPicker = new Windows.Storage.Pickers.FolderPicker
		{
			SuggestedStartLocation = PickerLocationId.DocumentsLibrary
		};
		WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, Process.GetCurrentProcess().MainWindowHandle);
		folderPicker.FileTypeFilter.Add("*");
#endif

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