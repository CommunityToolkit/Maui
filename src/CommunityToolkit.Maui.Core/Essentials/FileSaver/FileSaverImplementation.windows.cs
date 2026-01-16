using System;
using System.Diagnostics;
using Microsoft.Windows.Storage.Pickers;

namespace CommunityToolkit.Maui.Storage;

/// <inheritdoc />
public sealed partial class FileSaverImplementation : IFileSaver
{
	readonly List<string> allFilesExtension = ["."];

	async Task<string> InternalSaveAsync(string initialPath, string fileName, Stream stream, IProgress<double>? progress, CancellationToken cancellationToken)
	{
		var window = IPlatformApplication.Current?.Application.Windows[0].Handler?.PlatformView as MauiWinUIWindow;
		if (window is null)
		{
			throw new FileSaveException(
				"Cannot present file picker: No active window found. Ensure the app is active with a visible window.");
		}
		
		var savePicker = new FileSavePicker(window.AppWindow.Id)
		{
			SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
			SuggestedFolder = initialPath,
			SuggestedFileName = Path.GetFileNameWithoutExtension(fileName)
		};

		var extension = Path.GetExtension(fileName);
		if (!string.IsNullOrEmpty(extension))
		{
			savePicker.FileTypeChoices.Add(extension, [extension]);
		}

		savePicker.FileTypeChoices.Add("All files", allFilesExtension);

		var filePickerOperation = savePicker.PickSaveFileAsync();
		await using var taskCompetedSource = cancellationToken.Register(CancelFilePickerOperation);
		var file = await filePickerOperation;
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

		void CancelFilePickerOperation()
		{
			filePickerOperation.Cancel();
		}
	}

	Task<string> InternalSaveAsync(string fileName, Stream stream, IProgress<double>? progress, CancellationToken cancellationToken)
	{
		return InternalSaveAsync(string.Empty, fileName, stream, progress, cancellationToken);
	}
}