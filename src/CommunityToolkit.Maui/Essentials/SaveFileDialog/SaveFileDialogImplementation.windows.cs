using System.Diagnostics;
using CommunityToolkit.Maui.Core;
using Windows.Storage.Pickers;

namespace CommunityToolkit.Maui.Essentials;

/// <inheritdoc />
public partial class SaveFileDialogImplementation : ISaveFileDialog
{
	List<string> allFilesExtension = new List<string> { "." };
	/// <inheritdoc />
	public async Task<bool> SaveAsync(string initialPath, string fileName, Stream stream, CancellationToken cancellationToken)
	{
		var savePicker = new FileSavePicker
		{
			SuggestedStartLocation = PickerLocationId.DocumentsLibrary
		};
		WinRT.Interop.InitializeWithWindow.Initialize(savePicker, Process.GetCurrentProcess().MainWindowHandle);
		savePicker.FileTypeChoices.Add(GetExtension(fileName), new List<string> { GetExtension(fileName) });
		savePicker.FileTypeChoices.Add("All files", allFilesExtension);
		savePicker.SuggestedFileName = GetFileName(fileName);
		var file = await savePicker.PickSaveFileAsync();
		if (string.IsNullOrEmpty(file?.Path))
		{
			return false;
		}

		await WriteStream(stream, file.Path, cancellationToken);
		return true;
	}

	/// <inheritdoc />
	public Task<bool> SaveAsync(string fileName, Stream stream, CancellationToken cancellationToken)
	{
		return SaveAsync(string.Empty, fileName, stream, cancellationToken);
	}
}