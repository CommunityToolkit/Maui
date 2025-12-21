using System.Runtime.Versioning;
using CommunityToolkit.Maui.Core.Primitives;
using Microsoft.Maui.ApplicationModel;
using UniformTypeIdentifiers;

namespace CommunityToolkit.Maui.Storage;

/// <inheritdoc cref="IFolderPicker" />
[SupportedOSPlatform("iOS14.0")]
[SupportedOSPlatform("MacCatalyst14.0")]
public sealed partial class FolderPickerImplementation : IFolderPicker
{
	TaskCompletionSource<Folder>? taskCompetedSource;

	async Task<Folder> InternalPickAsync(string initialPath, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		var currentViewController = Platform.GetCurrentUIViewController();

		taskCompetedSource?.TrySetCanceled(CancellationToken.None);
		var tcs = taskCompetedSource = new();
		if (currentViewController is null)
		{
			throw new FolderPickerException("Unable to get a window where to present the folder picker UI.");
		}

		UIDocumentPickerViewController documentPickerViewController = new([UTTypes.Folder])
		{
			AllowsMultipleSelection = false,
			DirectoryUrl = NSUrl.FromString(initialPath)
		};
		documentPickerViewController.DidPickDocumentAtUrls += DocumentPickerViewControllerOnDidPickDocumentAtUrls;
		documentPickerViewController.WasCancelled += DocumentPickerViewControllerOnWasCancelled;
		try
		{
			currentViewController.PresentViewController(documentPickerViewController, true, null);
			return await tcs.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
		}
		finally
		{
			documentPickerViewController.DidPickDocumentAtUrls -= DocumentPickerViewControllerOnDidPickDocumentAtUrls;
			documentPickerViewController.WasCancelled -= DocumentPickerViewControllerOnWasCancelled;
			documentPickerViewController.Dispose();
		}
	}

	Task<Folder> InternalPickAsync(CancellationToken cancellationToken)
	{
		return InternalPickAsync("/", cancellationToken);
	}

	void DocumentPickerViewControllerOnWasCancelled(object? sender, EventArgs e)
	{
		taskCompetedSource?.TrySetException(new FolderPickerException("Operation cancelled."));
	}

	void DocumentPickerViewControllerOnDidPickDocumentAtUrls(object? sender, UIDocumentPickedAtUrlsEventArgs e)
	{
		var path = e.Urls[0].Path ?? throw new FolderPickerException("Path cannot be null.");
		taskCompetedSource?.TrySetResult(new Folder(path, new DirectoryInfo(path).Name));
	}
}