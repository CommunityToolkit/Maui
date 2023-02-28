using System.Diagnostics;
using Windows.Storage.Pickers;

namespace CommunityToolkit.Maui.Storage;

/// <inheritdoc />
public sealed partial class FileSaverImplementation : IFileSaver
{
	readonly List<string> allFilesExtension = new() { "." };

	async Task<string> InternalSaveAsync(string initialPath, string fileName, Stream stream, CancellationToken cancellationToken)
	{
		var savePicker = new FileSavePicker
		{
			SuggestedStartLocation = PickerLocationId.DocumentsLibrary
		};
		WinRT.Interop.InitializeWithWindow.Initialize(savePicker, Process.GetCurrentProcess().MainWindowHandle);

		var extension = GetExtension(fileName);
		savePicker.FileTypeChoices.Add(extension, new List<string> { extension });
		savePicker.FileTypeChoices.Add("All files", allFilesExtension);
		savePicker.SuggestedFileName = GetFileName(fileName);

		var filePickerOperation = savePicker.PickSaveFileAsync();

		await using var _ = cancellationToken.Register(CancelFilePickerOperation);
		var file = await filePickerOperation;
		if (string.IsNullOrEmpty(file?.Path))
		{
			throw new FileSaveException("Path doesn't exist.");
		}

		await WriteStream(stream, file.Path, cancellationToken).ConfigureAwait(false);
		return file.Path;

		void CancelFilePickerOperation()
		{
			filePickerOperation.Cancel();
		}
	}

	Task<string> InternalSaveAsync(string fileName, Stream stream, CancellationToken cancellationToken)
	{
		return InternalSaveAsync(string.Empty, fileName, stream, cancellationToken);
	}
}