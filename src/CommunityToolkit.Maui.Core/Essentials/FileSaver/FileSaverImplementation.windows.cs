using System.Diagnostics;
#if NET11_0_OR_GREATER
using Microsoft.UI;
using Microsoft.Windows.Storage.Pickers;
#else
using Windows.Storage.Pickers;
#endif

namespace CommunityToolkit.Maui.Storage;

/// <inheritdoc />
public sealed partial class FileSaverImplementation : IFileSaver
{
	readonly List<string> allFilesExtension = ["."];

	async Task<string> InternalSaveAsync(string initialPath, string fileName, Stream stream, IProgress<double>? progress, CancellationToken cancellationToken)
	{
		// Process.MainWindowHandle, as on .NET 10, rather than Application.Windows[0]: it is zero only when no window of the app is visible,
		// and it still finds the host's window when .NET MAUI is embedded in a WinUI app, whose window is not a MauiWinUIWindow.
		var hwnd = Process.GetCurrentProcess().MainWindowHandle;
		if (hwnd == IntPtr.Zero)
		{
			throw new FileSaveException(
				"Cannot present file picker: No active window found. Ensure the app is active with a visible window.");
		}

#if NET11_0_OR_GREATER
		var savePicker = new FileSavePicker(Win32Interop.GetWindowIdFromWindow(hwnd))
		{
			SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
			SuggestedFolder = initialPath,
			SuggestedFileName = Path.GetFileNameWithoutExtension(fileName)
		};
#else
		var savePicker = new FileSavePicker
		{
			SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
			SuggestedFileName = Path.GetFileNameWithoutExtension(fileName)
		};

		WinRT.Interop.InitializeWithWindow.Initialize(savePicker, hwnd);
#endif

		var extension = Path.GetExtension(fileName);
		if (!string.IsNullOrEmpty(extension))
		{
			savePicker.FileTypeChoices.Add(extension, [extension]);
		}

		savePicker.FileTypeChoices.Add("All files", allFilesExtension);

#if NET11_0_OR_GREATER
		var file = await PickerCancellation.PickAsync(hwnd, savePicker.PickSaveFileAsync, cancellationToken);
#else
		var filePickerOperation = savePicker.PickSaveFileAsync();
		await using var _ = cancellationToken.Register(CancelFilePickerOperation);
		var file = await filePickerOperation;
#endif
		if (file is null)
		{
			throw new OperationCanceledException("Operation cancelled.");
		}

		if (string.IsNullOrEmpty(file.Path))
		{
			throw new FileSaveException("Path doesn't exist.");
		}

		await WriteStream(stream, file.Path, progress, cancellationToken).ConfigureAwait(false);
		return file.Path;

#if !NET11_0_OR_GREATER
		void CancelFilePickerOperation()
		{
			filePickerOperation.Cancel();
		}
#endif
	}

	Task<string> InternalSaveAsync(string fileName, Stream stream, IProgress<double>? progress, CancellationToken cancellationToken)
	{
		return InternalSaveAsync(string.Empty, fileName, stream, progress, cancellationToken);
	}
}