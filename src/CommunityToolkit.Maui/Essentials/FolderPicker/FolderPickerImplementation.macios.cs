using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Primitives;
using Foundation;
using UIKit;
using UniformTypeIdentifiers;

namespace CommunityToolkit.Maui.Storage;

/// <inheritdoc />
public class FolderPickerImplementation : IFolderPicker
{
	/// <inheritdoc />
	public async Task<Folder> PickAsync(string initialPath, CancellationToken cancellationToken)
	{
		var documentPickerViewController = new UIDocumentPickerViewController(new[] { UTTypes.Folder })
		{
			AllowsMultipleSelection = false,
			DirectoryUrl = NSUrl.FromString(initialPath)
		};

		var currentViewController = Microsoft.Maui.Platform.UIApplicationExtensions.GetKeyWindow(UIApplication.SharedApplication)?.RootViewController;
		var taskCompetedSource = new TaskCompletionSource<Folder>();

		documentPickerViewController.DidPickDocumentAtUrls += (s, e) =>
		{
			var path = e.Urls[0].AbsoluteString ?? throw new Exception("Path cannot be null");
			var folder = new Folder
			{
				Path = path,
				Name = new DirectoryInfo(path).Name
			};
			taskCompetedSource.SetResult(folder);
		};

		documentPickerViewController.WasCancelled += (s, e) =>
		{
			taskCompetedSource.SetException(new FolderPickerException("Operation cancelled."));
		};

		currentViewController?.PresentViewController(documentPickerViewController, true, null);

		return await taskCompetedSource.Task;
	}

	/// <inheritdoc />
	public Task<Folder> PickAsync(CancellationToken cancellationToken)
	{
		return PickAsync("/", cancellationToken);
	}
}