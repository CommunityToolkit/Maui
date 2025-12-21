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
		var fileManager = NSFileManager.DefaultManager;
		var tempDirectoryPath = fileManager.GetTemporaryDirectory().Append(Guid.NewGuid().ToString(), true);
		var isDirectoryCreated = fileManager.CreateDirectory(tempDirectoryPath, true, null, out var error);
		if (!isDirectoryCreated)
		{
			throw new FileSaveException(error?.LocalizedDescription ?? "Unable to create temp directory.");
		}

		var fileUrl = tempDirectoryPath.Append(fileName, false);
		UIDocumentPickerViewController? documentPickerViewController = null;
		try
		{
			await WriteStream(stream, fileUrl.Path ?? throw new FileSaveException("Path cannot be null."), progress, cancellationToken);

			cancellationToken.ThrowIfCancellationRequested();
			
			var currentViewController = Platform.GetCurrentUIViewController();
			if (currentViewController is null)
			{
				throw new FileSaveException("Cannot present file picker: No active view controller found. Ensure the app is active with a visible window.");
			}

			taskCompletedSource?.TrySetCanceled(CancellationToken.None);
			var tcs = taskCompletedSource = new(cancellationToken);

			documentPickerViewController = new([fileUrl], true)
			{
				DirectoryUrl = NSUrl.FromString(initialPath)
			};
			documentPickerViewController.DidPickDocumentAtUrls += DocumentPickerViewControllerOnDidPickDocumentAtUrls;
			documentPickerViewController.WasCancelled += DocumentPickerViewControllerOnWasCancelled;

			currentViewController.PresentViewController(documentPickerViewController, true, null);

			return await tcs.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
		}
		finally
		{
			fileManager.Remove(tempDirectoryPath, out _);
			if (documentPickerViewController is not null)
			{
				documentPickerViewController.DidPickDocumentAtUrls -= DocumentPickerViewControllerOnDidPickDocumentAtUrls;
				documentPickerViewController.WasCancelled -= DocumentPickerViewControllerOnWasCancelled;
				documentPickerViewController.Dispose();
			}
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
		taskCompletedSource?.TrySetResult(e.Urls[0].Path ?? e.Urls[0].ToString());
	}
}