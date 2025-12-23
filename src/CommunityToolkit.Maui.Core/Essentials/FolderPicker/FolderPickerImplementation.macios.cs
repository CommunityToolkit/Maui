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

		var picker = new UIDocumentPickerViewController([UTTypes.Folder])
		{
			AllowsMultipleSelection = false,
			DirectoryUrl = NSUrl.FromString(initialPath)
		};

		void OnPicked(object? sender, UIDocumentPickedAtUrlsEventArgs e)
		{
			if (e.Urls.Length == 0)
			{
				tcs.TrySetException(new FolderPickerException("No folder was selected."));
				return;
			}

			var path = e.Urls[0].Path;
			if (path is null)
			{
				tcs.TrySetException(new FileSaveException("Path cannot be null."));
				return;
			}

			tcs.TrySetResult(new Folder(path, new DirectoryInfo(path).Name));
		}

		void OnCancelled(object? sender, EventArgs e)
		{
			tcs.TrySetCanceled(CancellationToken.None);
		}

		picker.DidPickDocumentAtUrls += OnPicked;
		picker.WasCancelled += OnCancelled;

		try
		{
			currentViewController.PresentViewController(picker, true, null);
			return await tcs.Task.ConfigureAwait(false);
		}
		finally
		{
			picker.DidPickDocumentAtUrls -= OnPicked;
			picker.WasCancelled -= OnCancelled;
			picker.Dispose();
		}
	}
}