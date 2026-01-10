using System.Runtime.Versioning;
using Microsoft.Maui.ApplicationModel;

namespace CommunityToolkit.Maui.Storage;

/// <inheritdoc cref="IFileSaver" />
[SupportedOSPlatform("iOS14.0")]
[SupportedOSPlatform("MacCatalyst14.0")]
public sealed partial class FileSaverImplementation : IFileSaver
{
	Task<string> InternalSaveAsync(string fileName, Stream stream, IProgress<double>? progress, CancellationToken cancellationToken)
	{
		return InternalSaveAsync("/", fileName, stream, progress, cancellationToken);
	}
	
	async Task<string> InternalSaveAsync(
		string initialPath,
		string fileName,
		Stream stream,
		IProgress<double>? progress,
		CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();

		var currentViewController = Platform.GetCurrentUIViewController()
		                            ?? throw new FileSaveException(
			                            "Cannot present file picker: No active view controller found. Ensure the app is active with a visible window.");

		var fileManager = NSFileManager.DefaultManager;

		var tempDirectoryPath = fileManager
			.GetTemporaryDirectory()
			.Append(Guid.NewGuid().ToString(), true);

		if (!fileManager.CreateDirectory(tempDirectoryPath, true, null, out var error))
		{
			throw new FileSaveException(error?.LocalizedDescription ?? "Unable to create temp directory.");
		}

		var fileUrl = tempDirectoryPath.Append(fileName, false);

		await WriteStream(
			stream,
			fileUrl.Path ?? throw new FileSaveException("Path cannot be null."),
			progress,
			cancellationToken);

		var tcs = new TaskCompletionSource<string>(
			TaskCreationOptions.RunContinuationsAsynchronously);

		await using var registration = cancellationToken.Register(() =>
			tcs.TrySetCanceled(cancellationToken));

		using var picker = new UIDocumentPickerViewController([fileUrl], true);
		picker.DirectoryUrl = NSUrl.FromString(initialPath);

		picker.DidPickDocumentAtUrls += OnPicked;
		picker.WasCancelled += OnCancelled;

		try
		{
			cancellationToken.ThrowIfCancellationRequested();
			currentViewController.PresentViewController(picker, true, null);

			return await tcs.Task.WaitAsync(cancellationToken);
		}
		finally
		{
			fileManager.Remove(tempDirectoryPath, out _);

			picker.DidPickDocumentAtUrls -= OnPicked;
			picker.WasCancelled -= OnCancelled;
		}
		
		void OnPicked(object? sender, UIDocumentPickedAtUrlsEventArgs e)
		{
			if (e.Urls.Length is 0)
			{
				tcs.TrySetException(new FileSaveException("No file was selected."));
				return;
			}
			
			var path = e.Urls[0].Path;
			if (path is null)
			{
				tcs.TrySetException(new FileSaveException("File path cannot be null."));
				return;
			}
			
			tcs.TrySetResult(path);
		}

		void OnCancelled(object? sender, EventArgs e)
		{
			tcs.TrySetCanceled(cancellationToken);
		}
	}
}