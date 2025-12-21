using System.Runtime.Versioning;
using Microsoft.Maui.ApplicationModel;

namespace CommunityToolkit.Maui.Storage;

/// <inheritdoc cref="IFileSaver" />
[SupportedOSPlatform("iOS14.0")]
[SupportedOSPlatform("MacCatalyst14.0")]
public sealed partial class FileSaverImplementation : IFileSaver
{
	TaskCompletionSource<string>? taskCompletedSource;

	async Task<string> InternalSaveAsync(string initialPath, string fileName, Stream stream, IProgress<double>? progress, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		var currentViewController = Platform.GetCurrentUIViewController();
		if (currentViewController is null)
		{
			throw new FileSaveException("Cannot present file picker: No active view controller found. Ensure the app is active with a visible window.");
		}
		
		var fileManager = NSFileManager.DefaultManager;
		var tempDirectoryPath = fileManager.GetTemporaryDirectory().Append(Guid.NewGuid().ToString(), true);
		var isDirectoryCreated = fileManager.CreateDirectory(tempDirectoryPath, true, null, out var error);
		if (!isDirectoryCreated)
		{
			throw new FileSaveException(error?.LocalizedDescription ?? "Unable to create temp directory.");
		}

		taskCompletedSource?.TrySetCanceled(CancellationToken.None);
		var tcs = taskCompletedSource = new(cancellationToken);
		
		var fileUrl = tempDirectoryPath.Append(fileName, false);
		await WriteStream(stream, fileUrl.Path ?? throw new FileSaveException("Path cannot be null."), progress, cancellationToken);
		UIDocumentPickerViewController? documentPickerViewController = new([fileUrl], true)
		{
			DirectoryUrl = NSUrl.FromString(initialPath)
		};
		documentPickerViewController.DidPickDocumentAtUrls += DocumentPickerViewControllerOnDidPickDocumentAtUrls;
		documentPickerViewController.WasCancelled += DocumentPickerViewControllerOnWasCancelled;
		try
		{
			cancellationToken.ThrowIfCancellationRequested();
			currentViewController.PresentViewController(documentPickerViewController, true, null);

			return await tcs.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
		}
		finally
		{
			fileManager.Remove(tempDirectoryPath, out _);
			documentPickerViewController.DidPickDocumentAtUrls -= DocumentPickerViewControllerOnDidPickDocumentAtUrls;
			documentPickerViewController.WasCancelled -= DocumentPickerViewControllerOnWasCancelled;
			documentPickerViewController.Dispose();
		}
	}

	Task<string> InternalSaveAsync(string fileName, Stream stream, IProgress<double>? progress, CancellationToken cancellationToken)
	{
		return InternalSaveAsync("/", fileName, stream, progress, cancellationToken);
	}

	void DocumentPickerViewControllerOnWasCancelled(object? sender, EventArgs e)
	{
		taskCompletedSource?.TrySetException(new FileSaveException("Operation cancelled."));
	}

	void DocumentPickerViewControllerOnDidPickDocumentAtUrls(object? sender, UIDocumentPickedAtUrlsEventArgs e)
	{
		if (e.Urls.Length == 0)
		{
			taskCompletedSource?.TrySetException(new FileSaveException("No file was selected."));
			return;
		}
		
		taskCompletedSource?.TrySetResult(e.Urls[0].Path ?? e.Urls[0].ToString());
	}
}