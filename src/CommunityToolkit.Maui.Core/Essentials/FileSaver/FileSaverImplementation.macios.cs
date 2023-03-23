using System.Runtime.Versioning;

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

	async Task<string> InternalSaveAsync(string initialPath, string fileName, Stream stream, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		var fileManager = NSFileManager.DefaultManager;
		var tempDirectoryPath = fileManager.GetTemporaryDirectory().Append(Guid.NewGuid().ToString(), true);
		var isDirectoryCreated = fileManager.CreateDirectory(tempDirectoryPath, true, null, out var error);
		if (!isDirectoryCreated)
		{
			throw new Exception(error?.LocalizedDescription ?? "Unable to create temp directory.");
		}

		var fileUrl = tempDirectoryPath.Append(fileName, false);
		await WriteStream(stream, fileUrl.Path ?? throw new Exception("Path cannot be null."), cancellationToken);

		cancellationToken.ThrowIfCancellationRequested();
		taskCompetedSource = new TaskCompletionSource<string>();

		documentPickerViewController = new UIDocumentPickerViewController(new[] { fileUrl });
		documentPickerViewController.DirectoryUrl = NSUrl.FromString(initialPath);
		documentPickerViewController.DidPickDocumentAtUrls += DocumentPickerViewControllerOnDidPickDocumentAtUrls;
		documentPickerViewController.WasCancelled += DocumentPickerViewControllerOnWasCancelled;

		var currentViewController = Microsoft.Maui.Platform.UIApplicationExtensions.GetKeyWindow(UIApplication.SharedApplication)?.RootViewController;
		currentViewController?.PresentViewController(documentPickerViewController, true, null);

		return await taskCompetedSource.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
	}

	Task<string> InternalSaveAsync(string fileName, Stream stream, CancellationToken cancellationToken)
	{
		return InternalSaveAsync("/", fileName, stream, cancellationToken);
	}

	void DocumentPickerViewControllerOnWasCancelled(object? sender, EventArgs e)
	{
		taskCompetedSource?.SetException(new FolderPickerException("Operation cancelled."));
		InternalDispose();
	}

	void DocumentPickerViewControllerOnDidPickDocumentAtUrls(object? sender, UIDocumentPickedAtUrlsEventArgs e)
	{
		try
		{
			taskCompetedSource?.SetResult(e.Urls[0].Path ?? throw new FileSaveException("Unable to retrieve the path of the saved file."));
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