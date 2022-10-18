using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Extensions;
using Foundation;
using UIKit;

namespace CommunityToolkit.Maui.Essentials;

/// <inheritdoc />
public partial class SaveFileDialogImplementation : ISaveFileDialog
{
	/// <inheritdoc/>
	public async Task<bool> SaveAsync(string initialPath, Stream stream, CancellationToken cancellationToken)
	{
		var fileManager = NSFileManager.DefaultManager;
		var fileUrl = fileManager.GetTemporaryDirectory().Append(Guid.NewGuid().ToString(), false);
		await WriteStream(stream, fileUrl.Path, cancellationToken);
		var documentPickerViewController = new UIDocumentPickerViewController(new[] { fileUrl });
		var currentViewController = UIViewExtensions.GetCurrentUIViewController();
		var taskCompetedSource = new TaskCompletionSource<bool>();
		documentPickerViewController.DidPickDocumentAtUrls += (s, e) =>
		{
			taskCompetedSource.SetResult(true);
		};
		documentPickerViewController.WasCancelled += (s, e) =>
		{
			taskCompetedSource.SetResult(false);
		};
		currentViewController?.PresentViewController(documentPickerViewController, true, null);
		return await taskCompetedSource.Task;
	}

	/// <inheritdoc/>
	public Task<bool> SaveAsync(Stream stream, CancellationToken cancellationToken)
	{
		return SaveAsync("/", stream, cancellationToken);
	}
}