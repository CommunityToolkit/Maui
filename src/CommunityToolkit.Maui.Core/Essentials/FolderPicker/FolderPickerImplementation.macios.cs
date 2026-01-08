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
	Task<Folder> InternalPickAsync(CancellationToken cancellationToken)
	{
		return InternalPickAsync("/", cancellationToken);
	}
	
	async Task<Folder> InternalPickAsync(string initialPath, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();

		var currentViewController = Platform.GetCurrentUIViewController()
		                            ?? throw new FolderPickerException("Unable to get a window where to present the folder picker UI.");

		var tcs = new TaskCompletionSource<Folder>(
			TaskCreationOptions.RunContinuationsAsynchronously);

		await using var registration = cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken));

		using var picker = new UIDocumentPickerViewController([UTTypes.Folder]);
		picker.AllowsMultipleSelection = false;
		picker.DirectoryUrl = NSUrl.FromString(initialPath);

		picker.DidPickDocumentAtUrls += OnPicked;
		picker.WasCancelled += OnCancelled;

		try
		{
			currentViewController.PresentViewController(picker, true, null);
			return await tcs.Task.WaitAsync(cancellationToken);
		}
		finally
		{
			picker.DidPickDocumentAtUrls -= OnPicked;
			picker.WasCancelled -= OnCancelled;
		}
		
		void OnPicked(object? sender, UIDocumentPickedAtUrlsEventArgs e)
		{
			if (e.Urls.Length is 0)
			{
				tcs.TrySetException(new FolderPickerException("No folder was selected."));
				return;
			}

			var path = e.Urls[0].Path;
			if (path is null)
			{
				tcs.TrySetException(new FileSaveException("File path cannot be null."));
				return;
			}

			tcs.TrySetResult(new Folder(path, new DirectoryInfo(path).Name));
		}

		void OnCancelled(object? sender, EventArgs e)
		{
			tcs.TrySetCanceled(cancellationToken);
		}
	}
}