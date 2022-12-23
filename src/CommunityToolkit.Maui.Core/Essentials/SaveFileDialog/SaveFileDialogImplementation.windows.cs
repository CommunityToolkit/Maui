using System.Diagnostics;
using Windows.Storage.Pickers;

namespace CommunityToolkit.Maui.Storage;

/// <inheritdoc />
public partial class SaveFileDialogImplementation : ISaveFileDialog
{
	readonly List<string> allFilesExtension = new() { "." };

	/// <inheritdoc />
	public async ValueTask<string> SaveAsync(string initialPath, string fileName, Stream stream, CancellationToken cancellationToken)
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

		var file = await savePicker.PickSaveFileAsync();

		if (string.IsNullOrEmpty(file?.Path))
		{
			throw new FileSaveException("Path doesn't exist.");
		}

		await WriteStream(stream, file.Path, cancellationToken).ConfigureAwait(false);
		return file.Path;
	}

	/// <inheritdoc />
	public ValueTask<string> SaveAsync(string fileName, Stream stream, CancellationToken cancellationToken)
	{
		return SaveAsync(string.Empty, fileName, stream, cancellationToken);
	}
}