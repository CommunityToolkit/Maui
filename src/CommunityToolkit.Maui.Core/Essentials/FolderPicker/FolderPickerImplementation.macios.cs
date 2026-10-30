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
	// The dismissal takes under a second: the timeout only bounds a dismissal that UIKit never reports done
	static readonly TimeSpan dismissalTimeout = TimeSpan.FromSeconds(3);

	Task<Folder> InternalPickAsync(CancellationToken cancellationToken)
	{
		return InternalPickAsync(string.Empty, cancellationToken);
	}

	async Task<Folder> InternalPickAsync(string initialPath, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();

		// Not Platform.GetCurrentUIViewController(): it throws a NullReferenceException instead of returning null
		var currentViewController = WindowStateManager.Default.GetCurrentUIViewController()
									?? throw new FolderPickerException("Unable to get a window where to present the folder picker UI.");

		var tcs = new TaskCompletionSource<Folder>(
			TaskCreationOptions.RunContinuationsAsynchronously);

		await using var registration = cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken));

		using var picker = new UIDocumentPickerViewController([UTTypes.Folder]);
		picker.AllowsMultipleSelection = false;
		// NSUrl.FromString would give a URL without a scheme, which the picker ignores, so a path is made a file URL; a file URL string
		// ("file:///..."), the form that worked before paths did, is still taken as the URL it is.
		picker.DirectoryUrl = string.IsNullOrEmpty(initialPath)
			? null
			: initialPath.StartsWith("file:", StringComparison.OrdinalIgnoreCase) ? NSUrl.FromString(initialPath) : NSUrl.CreateFileUrl(initialPath, true);

		picker.DidPickDocumentAtUrls += OnPicked;
		picker.WasCancelled += OnCancelled;

		var isPresented = false;
		var dismissWhenPresented = false;

		try
		{
			currentViewController.PresentViewController(picker, true, OnPresented);
			return await tcs.Task.WaitAsync(cancellationToken);
		}
		catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
		{
			// The token, not the person, canceled the pick: UIKit only dismisses the picker when the person picks or cancels
			if (!isPresented)
			{
				// UIKit presents the picker some time after PresentViewController, and a picker can only be dismissed once it is presented
				dismissWhenPresented = true;
			}
			else if (picker.PresentedViewController is null && picker.PresentingViewController is UIViewController presentingViewController)
			{
				// Only a picker that nothing covers: dismissing one would also take what is presented on top of it off the screen, such as
				// the picker of another call, which would then never complete. A covered picker stays until the person closes it.
				// The pick returns once the picker is off the screen: until then the picker is the view controller on top, which a call made as
				// soon as this one returns presents its picker from, and UIKit drops a presentation from a view controller that is being
				// dismissed. The wait is bounded, so that a dismissal that UIKit never reports done cannot hold the cancellation.
				await Task.WhenAny(presentingViewController.DismissViewControllerAsync(true), Task.Delay(dismissalTimeout, CancellationToken.None));
			}

			throw;
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
				tcs.TrySetException(new FolderPickerException("File path cannot be null."));
				return;
			}

			tcs.TrySetResult(new Folder(path, new DirectoryInfo(path).Name));
		}

		void OnCancelled(object? sender, EventArgs e)
		{
			tcs.TrySetCanceled(cancellationToken);
		}

		void OnPresented()
		{
			isPresented = true;

			// The pick is over and its picker disposed: the picker is reached through the view controller that presented it, and only
			// dismissed when nothing covers it
			if (dismissWhenPresented && currentViewController.PresentedViewController is UIDocumentPickerViewController { PresentedViewController: null } presentedPicker)
			{
				presentedPicker.DismissViewController(true, null);
			}
		}
	}
}