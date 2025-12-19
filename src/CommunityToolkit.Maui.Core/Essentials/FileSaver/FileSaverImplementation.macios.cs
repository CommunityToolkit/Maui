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
		try
		{
			await WriteStream(stream, fileUrl.Path ?? throw new FileSaveException("Path cannot be null."), progress, cancellationToken);

			cancellationToken.ThrowIfCancellationRequested();
			
			var currentViewController = Platform.GetCurrentUIViewController();
			if (currentViewController is null)
			{
				throw new FileSaveException("Cannot present file picker: No active view controller found. Ensure the app is active with a visible window.");
			}

			taskCompetedSource?.TrySetCanceled(CancellationToken.None);
			var tcs = taskCompetedSource = new(cancellationToken);

			documentPickerViewController = new([fileUrl], true)
			{
				DirectoryUrl = NSUrl.FromString(initialPath)
			};
			documentPickerViewController.DidPickDocumentAtUrls += DocumentPickerViewControllerOnDidPickDocumentAtUrls;
			documentPickerViewController.WasCancelled += DocumentPickerViewControllerOnWasCancelled;

			currentViewController.PresentViewController(documentPickerViewController, true, null);

			return await tcs.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
		}
		catch
		{
			InternalDispose();
			throw;
		}
		finally
		{
			fileManager.Remove(tempDirectoryPath, out _);
		}
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