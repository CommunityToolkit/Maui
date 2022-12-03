using CommunityToolkit.Maui.Core;
using Foundation;
using UIKit;

namespace CommunityToolkit.Maui.Storage;

/// <inheritdoc />
public partial class SaveFileDialogImplementation : ISaveFileDialog
{
	/// <inheritdoc/>
	public async Task<string> SaveAsync(string initialPath, string fileName, Stream stream, CancellationToken cancellationToken)
	{
		var fileManager = NSFileManager.DefaultManager;
		var fileUrl = fileManager.GetTemporaryDirectory().Append($"{Guid.NewGuid()}{GetExtension(fileName)}", false);
		await WriteStream(stream, fileUrl.Path ?? throw new Exception("Path cannot be null."), cancellationToken); 
		
		using var documentPickerViewController = new UIDocumentPickerViewController(new[] { fileUrl });
		var taskCompetedSource = new TaskCompletionSource<string>();

		documentPickerViewController.DidPickDocumentAtUrls += (s, e) =>
		{
			taskCompetedSource.SetResult(e.Urls[0].Path ?? throw new FileSaveException("Unable to retrieve the path of the saved file."));
		};

		documentPickerViewController.WasCancelled += (s, e) =>
		{
			taskCompetedSource.SetException(new FileSaveException("Operation cancelled."));
		};

		var currentViewController = Microsoft.Maui.Platform.UIApplicationExtensions.GetKeyWindow(UIApplication.SharedApplication)?.RootViewController;
		currentViewController?.PresentViewController(documentPickerViewController, true, null);

		return await taskCompetedSource.Task;
	}

	/// <inheritdoc/>
	public Task<string> SaveAsync(string fileName, Stream stream, CancellationToken cancellationToken)
	{
		return SaveAsync("/", fileName, stream, cancellationToken);
	}
}