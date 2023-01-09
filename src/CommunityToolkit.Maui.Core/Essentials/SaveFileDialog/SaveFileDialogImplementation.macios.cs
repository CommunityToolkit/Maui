namespace CommunityToolkit.Maui.Storage;

/// <inheritdoc cref="ISaveFileDialog" />
sealed partial class SaveFileDialogImplementation : ISaveFileDialog, IDisposable
{
	UIDocumentPickerViewController? documentPickerViewController;
	TaskCompletionSource<string>? taskCompetedSource;
	
	/// <inheritdoc/>
	public async Task<string> SaveAsync(string initialPath, string fileName, Stream stream, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		var fileManager = NSFileManager.DefaultManager;
		var fileUrl = fileManager.GetTemporaryDirectory().Append($"{Guid.NewGuid()}{GetExtension(fileName)}", false);
		await WriteStream(stream, fileUrl.Path ?? throw new Exception("Path cannot be null."), cancellationToken);

		cancellationToken.ThrowIfCancellationRequested();
		taskCompetedSource = new TaskCompletionSource<string>();
		
		documentPickerViewController = new UIDocumentPickerViewController(new[] { fileUrl });
		documentPickerViewController.DidPickDocumentAtUrls += DocumentPickerViewControllerOnDidPickDocumentAtUrls;
		documentPickerViewController.WasCancelled += DocumentPickerViewControllerOnWasCancelled;
		
		var currentViewController = Microsoft.Maui.Platform.UIApplicationExtensions.GetKeyWindow(UIApplication.SharedApplication)?.RootViewController;
		currentViewController?.PresentViewController(documentPickerViewController, true, null);

		return await taskCompetedSource.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
	}

	/// <inheritdoc/>
	public Task<string> SaveAsync(string fileName, Stream stream, CancellationToken cancellationToken)
	{
		return SaveAsync("/", fileName, stream, cancellationToken);
	}

	/// <inheritdoc />
	public void Dispose()
	{
		if (documentPickerViewController is not null)
		{
			documentPickerViewController.DidPickDocumentAtUrls -= DocumentPickerViewControllerOnDidPickDocumentAtUrls;
			documentPickerViewController.WasCancelled -= DocumentPickerViewControllerOnWasCancelled;
			documentPickerViewController.Dispose();
		}
	}

	void DocumentPickerViewControllerOnWasCancelled(object? sender, EventArgs e)
	{
		taskCompetedSource?.SetException(new FolderPickerException("Operation cancelled."));
	}

	void DocumentPickerViewControllerOnDidPickDocumentAtUrls(object? sender, UIDocumentPickedAtUrlsEventArgs e)
	{
		taskCompetedSource?.SetResult(e.Urls[0].Path ?? throw new FileSaveException("Unable to retrieve the path of the saved file."));
	}
}