using System.Runtime.Versioning;
using CommunityToolkit.Maui.Core.Primitives;
using UniformTypeIdentifiers;

namespace CommunityToolkit.Maui.Storage;

/// <inheritdoc cref="IFolderPicker" />
[SupportedOSPlatform("iOS14.0")]
[SupportedOSPlatform("MacCatalyst14.0")]
public sealed partial class FolderPickerImplementation : IFolderPicker, IDisposable
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
	public void Dispose()
	{
		documentPickerViewController.DidPickDocumentAtUrls -= DocumentPickerViewControllerOnDidPickDocumentAtUrls;
		documentPickerViewController.WasCancelled -= DocumentPickerViewControllerOnWasCancelled;
		documentPickerViewController.Dispose();
	}

	async Task<Folder> InternalPickAsync(string initialPath, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		documentPickerViewController.DirectoryUrl = NSUrl.FromString(initialPath);
		var currentViewController = Microsoft.Maui.Platform.UIApplicationExtensions.GetKeyWindow(UIApplication.SharedApplication)?.RootViewController;

		taskCompetedSource = new TaskCompletionSource<Folder>();
		currentViewController?.PresentViewController(documentPickerViewController, true, null);

		return await taskCompetedSource.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
	}

	Task<Folder> InternalPickAsync(CancellationToken cancellationToken)
	{
		return InternalPickAsync("/", cancellationToken);
	}

	void DocumentPickerViewControllerOnWasCancelled(object? sender, EventArgs e)
	{
		taskCompetedSource?.SetException(new FolderPickerException("Operation cancelled."));
	}

	void DocumentPickerViewControllerOnDidPickDocumentAtUrls(object? sender, UIDocumentPickedAtUrlsEventArgs e)
	{
		var path = e.Urls[0].Path ?? throw new FolderPickerException("Path cannot be null.");
		taskCompetedSource?.SetResult(new Folder(path, new DirectoryInfo(path).Name));
	}
}