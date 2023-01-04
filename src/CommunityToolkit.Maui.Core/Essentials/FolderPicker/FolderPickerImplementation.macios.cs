using CommunityToolkit.Maui.Core.Primitives;
using UniformTypeIdentifiers;

namespace CommunityToolkit.Maui.Storage;

/// <inheritdoc cref="IFolderPicker" />
public sealed class FolderPickerImplementation : IFolderPicker, IDisposable
{
	readonly UIDocumentPickerViewController documentPickerViewController = new(new[] { UTTypes.Folder })
	{
		AllowsMultipleSelection = false
	};

	TaskCompletionSource<Folder>? taskCompetedSource;

	/// <summary>
	/// Initializes a new instance of the <see cref="FolderPickerImplementation"/> class.
	/// </summary>
	public FolderPickerImplementation()
	{
		documentPickerViewController.DidPickDocumentAtUrls += DocumentPickerViewControllerOnDidPickDocumentAtUrls;
		documentPickerViewController.WasCancelled += DocumentPickerViewControllerOnWasCancelled;
	}

	/// <inheritdoc />
	public async Task<Folder> PickAsync(string initialPath, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		documentPickerViewController.DirectoryUrl = NSUrl.FromString(initialPath);
		var currentViewController = Microsoft.Maui.Platform.UIApplicationExtensions.GetKeyWindow(UIApplication.SharedApplication)?.RootViewController;

		taskCompetedSource = new TaskCompletionSource<Folder>();
		currentViewController?.PresentViewController(documentPickerViewController, true, null);

		return await taskCompetedSource.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
	}

	/// <inheritdoc />
	public Task<Folder> PickAsync(CancellationToken cancellationToken)
	{
		return PickAsync("/", cancellationToken);
	}

	/// <inheritdoc />
	public void Dispose()
	{
		documentPickerViewController.DidPickDocumentAtUrls -= DocumentPickerViewControllerOnDidPickDocumentAtUrls;
		documentPickerViewController.WasCancelled -= DocumentPickerViewControllerOnWasCancelled;
		documentPickerViewController.Dispose();
	}

	void DocumentPickerViewControllerOnWasCancelled(object? sender, EventArgs e)
	{
		taskCompetedSource?.SetException(new FolderPickerException("Operation cancelled."));
	}

	void DocumentPickerViewControllerOnDidPickDocumentAtUrls(object? sender, UIDocumentPickedAtUrlsEventArgs e)
	{
		var path = e.Urls[0].AbsoluteString ?? throw new FolderPickerException("Path cannot be null.");
		taskCompetedSource?.SetResult(new Folder(path, new DirectoryInfo(path).Name));
	}
}