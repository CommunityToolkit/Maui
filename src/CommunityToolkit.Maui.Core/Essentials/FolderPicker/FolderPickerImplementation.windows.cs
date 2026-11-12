using System.Diagnostics;
using CommunityToolkit.Maui.Core.Primitives;
using Microsoft.UI;
using Microsoft.Windows.Storage.Pickers;

namespace CommunityToolkit.Maui.Storage;

/// <inheritdoc />
public sealed partial class FolderPickerImplementation : IFolderPicker
{
	async Task<Folder> InternalPickAsync(string initialPath, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();

		// Process.MainWindowHandle rather than Application.Windows[0]: it is zero only when no window of the app is visible,
		// and it still finds the host's window when .NET MAUI is embedded in a WinUI app, whose window is not a MauiWinUIWindow.
		var hwnd = Process.GetCurrentProcess().MainWindowHandle;
		if (hwnd == IntPtr.Zero)
		{
			throw new FolderPickerException(
				"Cannot present folder picker: No active window found. Ensure the app is active with a visible window.");
		}

		var folderPicker = new Microsoft.Windows.Storage.Pickers.FolderPicker(Win32Interop.GetWindowIdFromWindow(hwnd))
		{
			SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
			SuggestedFolder = initialPath
		};

		var folder = await PickerCancellation.PickAsync(hwnd, folderPicker.PickSingleFolderAsync, cancellationToken);
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