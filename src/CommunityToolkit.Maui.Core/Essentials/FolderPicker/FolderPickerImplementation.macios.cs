using CommunityToolkit.Maui.Core.Primitives;
using UniformTypeIdentifiers;

namespace CommunityToolkit.Maui.Storage;

/// <inheritdoc cref="IFolderPicker" />
public sealed class FolderPickerImplementation : IFolderPicker, IDisposable
{
	readonly UIDocumentPickerViewController documentPickerViewController;
	TaskCompletionSource<Folder>? taskCompetedSource;
	
	/// <summary>
	/// Initializes a new instance of the <see cref="FolderPickerImplementation"/> class.
	/// </summary>
	public FolderPickerImplementation()
	{
		documentPickerViewController = new UIDocumentPickerViewController(new []{ UTTypes.Folder })
		{
			AllowsMultipleSelection = false
		};
	}
	
	/// <inheritdoc />
	public async ValueTask<Folder> PickAsync(string initialPath, CancellationToken cancellationToken)
	{
		documentPickerViewController.DirectoryUrl = NSUrl.FromString(initialPath);
		var currentViewController = Microsoft.Maui.Platform.UIApplicationExtensions.GetKeyWindow(UIApplication.SharedApplication)?.RootViewController;
		taskCompetedSource = new TaskCompletionSource<Folder>();
		documentPickerViewController.DidPickDocumentAtUrls += DocumentPickerViewControllerOnDidPickDocumentAtUrls;
		documentPickerViewController.WasCancelled += DocumentPickerViewControllerOnWasCancelled;
		currentViewController?.PresentViewController(documentPickerViewController, true, null);
		return await taskCompetedSource.Task;
	}

	void DocumentPickerViewControllerOnWasCancelled(object? sender, EventArgs e)
	{
		taskCompetedSource?.SetException(new FolderPickerException("Operation cancelled."));
	}

	void DocumentPickerViewControllerOnDidPickDocumentAtUrls(object? sender, UIDocumentPickedAtUrlsEventArgs e)
	{
		var path = e.Urls[0].AbsoluteString ?? throw new Exception("Path cannot be null.");
		taskCompetedSource?.SetResult(new Folder(path, new DirectoryInfo(path).Name));
	}

	/// <inheritdoc />
	public ValueTask<Folder> PickAsync(CancellationToken cancellationToken)
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
}