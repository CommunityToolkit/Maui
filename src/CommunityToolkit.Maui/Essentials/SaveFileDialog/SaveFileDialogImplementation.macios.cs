using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Extensions;
using Foundation;
using UIKit;

namespace CommunityToolkit.Maui.Essentials;

/// <inheritdoc />
public partial class SaveFileDialogImplementation : ISaveFileDialog
{
	/// <inheritdoc/>
	public async Task<bool> SaveAsync(string initialPath, string fileName, Stream stream, CancellationToken cancellationToken)
	{
		var fileManager = NSFileManager.DefaultManager;
		var fileUrl = fileManager.GetTemporaryDirectory().Append($"{Guid.NewGuid()}.{GetExtension(fileName)}", false);
		await WriteStream(stream, fileUrl.Path ?? throw new Exception("Path cannot be null"), cancellationToken);

		var documentPickerViewController = new UIDocumentPickerViewController(new[] { fileUrl });
		var taskCompetedSource = new TaskCompletionSource<bool>();

		documentPickerViewController.DidPickDocumentAtUrls += (s, e) =>
		{
			taskCompetedSource.SetResult(true);
		};

		documentPickerViewController.WasCancelled += (s, e) =>
		{
			taskCompetedSource.SetResult(false);
		};

		var currentViewController = Microsoft.Maui.Platform.UIApplicationExtensions.GetKeyWindow(UIApplication.SharedApplication)?.RootViewController;
		currentViewController?.PresentViewController(documentPickerViewController, true, null);

		return await taskCompetedSource.Task;
	}

	/// <inheritdoc/>
	public Task<bool> SaveAsync(string fileName, Stream stream, CancellationToken cancellationToken)
	{
		return SaveAsync("/", fileName, stream, cancellationToken);
	}
}