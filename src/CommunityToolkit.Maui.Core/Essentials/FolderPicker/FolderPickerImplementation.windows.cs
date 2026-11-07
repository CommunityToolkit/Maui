using System.Diagnostics;
using CommunityToolkit.Maui.Core.Primitives;
#if NET11_0_OR_GREATER
using Microsoft.UI;
using Microsoft.Windows.Storage.Pickers;
#else
using Windows.Storage.Pickers;
#endif

namespace CommunityToolkit.Maui.Storage;

/// <inheritdoc />
public sealed partial class FolderPickerImplementation : IFolderPicker
{
	async Task<Folder> InternalPickAsync(string initialPath, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();

		// Process.MainWindowHandle, as on .NET 10, rather than Application.Windows[0]: it is zero only when no window of the app is visible,
		// and it still finds the host's window when .NET MAUI is embedded in a WinUI app, whose window is not a MauiWinUIWindow.
		var hwnd = Process.GetCurrentProcess().MainWindowHandle;
		if (hwnd == IntPtr.Zero)
		{
			throw new FolderPickerException(
				"Cannot present folder picker: No active window found. Ensure the app is active with a visible window.");
		}

#if NET11_0_OR_GREATER
		var folderPicker = new Microsoft.Windows.Storage.Pickers.FolderPicker(Win32Interop.GetWindowIdFromWindow(hwnd))
		{
			SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
			SuggestedFolder = initialPath
		};
#else
		var folderPicker = new Windows.Storage.Pickers.FolderPicker
		{
			SuggestedStartLocation = PickerLocationId.DocumentsLibrary
		};

		WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, hwnd);
		folderPicker.FileTypeFilter.Add("*");
#endif

#if NET11_0_OR_GREATER
		var folder = await PickerCancellation.PickAsync(hwnd, folderPicker.PickSingleFolderAsync, cancellationToken);
#else
		var folderPickerOperation = folderPicker.PickSingleFolderAsync();

		void CancelFolderPickerOperation()
		{
			folderPickerOperation.Cancel();
		}

		await using var _ = cancellationToken.Register(CancelFolderPickerOperation);
		var folder = await folderPickerOperation;
#endif
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