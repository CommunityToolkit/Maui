using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Extensions;
using CommunityToolkit.Maui.Core.Primitives;
using Foundation;
using UIKit;
using UniformTypeIdentifiers;

namespace CommunityToolkit.Maui.Essentials;

/// <inheritdoc />
public class FolderPickerImplementation : IFolderPicker
{
	/// <inheritdoc />
	public async Task<Folder?> PickAsync(string initialPath, CancellationToken cancellationToken)
	{
		var documentPickerViewController = new UIDocumentPickerViewController(new[]{ UTTypes.Folder });
		documentPickerViewController.AllowsMultipleSelection = false;
		documentPickerViewController.DirectoryUrl = NSUrl.FromString(initialPath);
		var currentViewController = UIViewExtensions.GetCurrentUIViewController();
		var taskCompetedSource = new TaskCompletionSource<Folder?>();
		documentPickerViewController.DidPickDocumentAtUrls += (s, e) =>
		{
			var folder = new Folder
			{
				Path = e.Urls[0].AbsoluteString,
				Name = Path.GetDirectoryName(e.Urls[0].AbsoluteString)
			};
			taskCompetedSource.SetResult(folder);
		};
		documentPickerViewController.WasCancelled += (s, e) =>
		{
			taskCompetedSource.SetResult(null);
		};
		currentViewController?.PresentViewController(documentPickerViewController, true, null);
		return await taskCompetedSource.Task;
	}
	
	/// <inheritdoc />
	public Task<Folder?> PickAsync(CancellationToken cancellationToken)
	{
		return PickAsync("/", cancellationToken);
	}
}