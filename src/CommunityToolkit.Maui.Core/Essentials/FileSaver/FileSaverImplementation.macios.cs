using System.Runtime.Versioning;
using Microsoft.Maui.ApplicationModel;

namespace CommunityToolkit.Maui.Storage;

/// <inheritdoc cref="IFileSaver" />
[SupportedOSPlatform("iOS14.0")]
[SupportedOSPlatform("MacCatalyst14.0")]
public sealed partial class FileSaverImplementation : IFileSaver, IDisposable
{
	UIDocumentPickerViewController? documentPickerViewController;
	TaskCompletionSource<string>? taskCompetedSource;

	/// <inheritdoc />
	public void Dispose()
	{
		InternalDispose();
	}

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
		await WriteStream(stream, fileUrl.Path ?? throw new Exception("Path cannot be null."), progress, cancellationToken);

		cancellationToken.ThrowIfCancellationRequested();
		taskCompetedSource?.TrySetCanceled(CancellationToken.None);
		var tcs = taskCompetedSource = new(cancellationToken);

		documentPickerViewController = new([fileUrl])
		{
			DirectoryUrl = NSUrl.FromString(initialPath)
		};
		documentPickerViewController.DidPickDocumentAtUrls += DocumentPickerViewControllerOnDidPickDocumentAtUrls;
		documentPickerViewController.WasCancelled += DocumentPickerViewControllerOnWasCancelled;

		var currentViewController = Platform.GetCurrentUIViewController();
		if (currentViewController is not null)
		{
			currentViewController.PresentViewController(documentPickerViewController, true, null);
		}
		else
		{
			throw new FileSaveException("Unable to get a window where to present the file saver UI.");
		}

		return await tcs.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
	}

	Task<string> InternalSaveAsync(string fileName, Stream stream, IProgress<double>? progress, CancellationToken cancellationToken)
	{
		return InternalSaveAsync("/", fileName, stream, progress, cancellationToken);
	}

	void DocumentPickerViewControllerOnWasCancelled(object? sender, EventArgs e)
	{
		taskCompetedSource?.TrySetException(new FileSaveException("Operation cancelled."));
		InternalDispose();
	}

	void DocumentPickerViewControllerOnDidPickDocumentAtUrls(object? sender, UIDocumentPickedAtUrlsEventArgs e)
	{
		try
		{
			taskCompetedSource?.TrySetResult(e.Urls[0].Path ?? throw new FileSaveException("Unable to retrieve the path of the saved file."));
		}
		finally
		{
			InternalDispose();
		}
	}

	void InternalDispose()
	{
		if (documentPickerViewController is not null)
		{
			documentPickerViewController.DidPickDocumentAtUrls -= DocumentPickerViewControllerOnDidPickDocumentAtUrls;
			documentPickerViewController.WasCancelled -= DocumentPickerViewControllerOnWasCancelled;
			documentPickerViewController.Dispose();
		}
	}
}