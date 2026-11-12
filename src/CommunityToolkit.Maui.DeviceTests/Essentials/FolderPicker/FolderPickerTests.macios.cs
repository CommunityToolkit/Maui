using System.Text;
using CommunityToolkit.Maui.Core.Primitives;
using CommunityToolkit.Maui.Storage;
using Foundation;
using UIKit;

namespace CommunityToolkit.Maui.DeviceTests;

public partial class FolderPickerTests
{
	[Fact]
	public async Task PickAsyncPresentsAPickerThatOpensASingleFolderStartingWhereTheSystemChooses()
	{
		var (operation, picker, pickerDelegate) = await StartPickAsync();

		await InvokeOnMainThreadAsync(() =>
		{
			Assert.Equal(UIDocumentPickerMode.Open, picker.DocumentPickerMode);
			Assert.Null(picker.DirectoryUrl);
			Assert.False(picker.AllowsMultipleSelection);
		});
		Assert.Equal((1, 1), FilesApplePicker.CountSubscribers(pickerDelegate));

		await FilesApplePicker.SimulateUserCancelsAsync(picker);
		Assert.True((await operation.WaitAsync(operationTimeout)).IsCancelled);
		Assert.Equal((0, 0), FilesApplePicker.CountSubscribers(pickerDelegate));
	}

	// An empty initial path ("" expected) gives the picker no directory, so it opens where the system chooses. A file URL string ({temp-url} is
	// "file:///.../tmp/") names the folder of its URL, as it did before an initial path was taken for a path.
	[Theory]
	[InlineData("{temp}", "{temp}")]
	[InlineData("{temp}/My Folder", "{temp}/My Folder")]
	[InlineData("", "")]
	[InlineData("{temp-url}", "{temp}")]
	[InlineData("{temp-url}My%20Folder/", "{temp}/My Folder")]
	public async Task PickAsyncStartsThePickerInTheFolderOfTheInitialPath(string initialPath, string expectedDirectory)
	{
		var temp = TemporaryRoot;
		var tempUrl = NSUrl.CreateFileUrl(temp, true).AbsoluteString!;

		var (operation, picker, _) = await StartPickAsync(initialPath.Replace("{temp-url}", tempUrl, StringComparison.Ordinal).Replace("{temp}", temp, StringComparison.Ordinal));

		var directory = await InvokeOnMainThreadAsync(() => picker.DirectoryUrl?.Path);
		await FilesApplePicker.SimulateUserCancelsAsync(picker);
		Assert.True((await operation.WaitAsync(operationTimeout)).IsCancelled);

		Assert.Equal(expectedDirectory.Length is 0 ? null : expectedDirectory.Replace("{temp}", temp, StringComparison.Ordinal), directory);
	}

	[Fact]
	public async Task PickAsyncGivesThePickerTheInitialFolderAsAFileUrl()
	{
		var (operation, picker, _) = await StartPickAsync(TemporaryRoot);
		var (isFileUrl, path) = await InvokeOnMainThreadAsync(() => (picker.DirectoryUrl?.IsFileUrl, picker.DirectoryUrl?.Path));
		await FilesApplePicker.SimulateUserCancelsAsync(picker);
		await operation.WaitAsync(operationTimeout);

		Assert.True(isFileUrl is true, $"The picker's directory URL is not a file URL (path '{path}').");
		Assert.Equal(TemporaryRoot, path);
	}

	[Theory]
	[InlineData(0, false)]
	[InlineData(1, false)]
	[InlineData(0, true)]
	[InlineData(1, true)]
	public async Task EveryPickAsyncOverloadPresentsThePickerAndReturnsThePickedFolder(int overload, bool useStaticApi)
	{
		var initialPath = TemporaryRoot;
		var folder = NewFolder("Reports");

		var operation = await StartOnMainThreadAsync(() => PickAsync(overload, useStaticApi, CancellationToken.None, initialPath));
		var picker = await FilesApplePicker.WaitForPickerAsync();
		Assert.Equal(overload is 0 ? initialPath : null, await InvokeOnMainThreadAsync(() => picker.DirectoryUrl?.Path));
		await FilesApplePicker.SimulateUserPicksAsync(picker, NSUrl.CreateFileUrl(folder, true));
		var result = await operation.WaitAsync(operationTimeout);

		Assert.True(result.IsSuccessful, result.Exception?.ToString());
		Assert.Equal(new Folder(folder, "Reports"), result.Folder);
	}

	[Theory]
	[InlineData("Reports")]
	[InlineData("My Reports (2026)")]
	[InlineData("résumés")]
	public async Task PickAsyncReturnsThePathAndTheNameOfThePickedFolder(string name)
	{
		var folder = NewFolder(name);
		var (operation, picker, pickerDelegate) = await StartPickAsync();

		await FilesApplePicker.SimulateUserPicksAsync(picker, NSUrl.CreateFileUrl(folder, true));
		var result = await operation.WaitAsync(operationTimeout);

		// NSUrl.Path gives accented letters decomposed (NFD), so "résumés" comes back as "re\u0301sume\u0301s": the same folder for the file
		// system, a different string for an ordinal comparison.
		Assert.True(result.IsSuccessful, result.Exception?.ToString());
		Assert.False(result.IsCancelled);
		Assert.Equal(folder.Normalize(NormalizationForm.FormC), result.Folder.Path.Normalize(NormalizationForm.FormC));
		Assert.Equal(name.Normalize(NormalizationForm.FormC), result.Folder.Name.Normalize(NormalizationForm.FormC));
		Assert.True(Directory.Exists(result.Folder.Path), $"The returned path '{result.Folder.Path}' does not name a folder.");
		Assert.Equal((0, 0), FilesApplePicker.CountSubscribers(pickerDelegate));
	}

	[Fact]
	public async Task PickAsyncNamesTheRootFolderWithItsPath()
	{
		var (operation, picker, _) = await StartPickAsync();

		await FilesApplePicker.SimulateUserPicksAsync(picker, NSUrl.CreateFileUrl("/", true));
		var result = await operation.WaitAsync(operationTimeout);

		Assert.True(result.IsSuccessful, result.Exception?.ToString());
		Assert.Equal(new Folder("/", "/"), result.Folder);
	}

	[Fact]
	public async Task PickAsyncReturnsACanceledResultWhenTheUserCancelsThePicker()
	{
		var (operation, picker, _) = await StartPickAsync();

		await FilesApplePicker.SimulateUserCancelsAsync(picker);
		var result = await operation.WaitAsync(operationTimeout);

		Assert.True(result.IsCancelled);
		Assert.False(result.IsSuccessful);
		Assert.Null(result.Folder);
		Assert.IsType<OperationCanceledException>(result.Exception, exactMatch: false);
		Assert.ThrowsAny<OperationCanceledException>(result.EnsureSuccess);
	}

	[Fact]
	public async Task PickAsyncReturnsACanceledResultWhenTheTokenIsCanceledWhileThePickerIsPresented()
	{
		using var cancellation = new CancellationTokenSource();
		var (operation, _, pickerDelegate) = await StartPickAsync(token: cancellation.Token);

		await cancellation.CancelAsync();
		var result = await operation.WaitAsync(operationTimeout);

		Assert.True(result.IsCancelled);
		Assert.Null(result.Folder);
		Assert.Equal((0, 0), FilesApplePicker.CountSubscribers(pickerDelegate));
	}

	[Fact]
	public async Task PickAsyncDismissesThePickerWhenTheTokenIsCanceled()
	{
		using var cancellation = new CancellationTokenSource();
		var (operation, _, _) = await StartPickAsync(token: cancellation.Token);

		await cancellation.CancelAsync();
		await operation.WaitAsync(operationTimeout);

		await AssertionExtensions.AssertEventuallyAsync(async () => !await FilesApplePicker.IsPickerPresentedAsync(), timeout: 3000, message: "The document picker is still on screen after the pick was canceled.");
	}

	[Fact]
	public async Task PickAsyncDismissesThePickerWhenTheTokenIsCanceledOnceThePickerIsShown()
	{
		using var cancellation = new CancellationTokenSource();
		var (operation, picker, _) = await StartPickAsync(token: cancellation.Token);
		await AssertionExtensions.AssertEventuallyAsync(() => InvokeOnMainThreadAsync(() => picker.PresentingViewController is not null && !picker.IsBeingPresented), timeout: 15000, message: "The document picker was not shown.");

		await cancellation.CancelAsync();
		Assert.True((await operation.WaitAsync(operationTimeout)).IsCancelled);

		await AssertionExtensions.AssertEventuallyAsync(async () => !await FilesApplePicker.IsPickerPresentedAsync(), timeout: 3000, message: "The document picker is still on screen after the pick was canceled.");
	}

	[Fact]
	public async Task PickAsyncDismissesThePickerWhenTheTokenIsCanceledBeforeTheSystemShowsIt()
	{
		using var cancellation = new CancellationTokenSource();

		// The token is canceled on the main thread right after the call: UIKit shows a document picker some time after it was asked to
		// present it, so the picker is not on screen yet, and can only be dismissed once it is.
		var operation = await StartOnMainThreadAsync(() =>
		{
			var pick = FolderPicker.Default.PickAsync(cancellation.Token);
			cancellation.Cancel();
			return pick;
		});

		Assert.True((await operation.WaitAsync(operationTimeout)).IsCancelled);
		await FilesApplePicker.WaitForPickerAsync();
		await AssertionExtensions.AssertEventuallyAsync(async () => !await FilesApplePicker.IsPickerPresentedAsync(), timeout: 3000, message: "The document picker is still on screen after the pick was canceled.");
	}

	// Two picks at once: the picker of the second one is presented on top of the picker of the first. Dismissing the first picker would
	// also take the second one off the screen, and the second pick would then wait for an answer that never comes.
	[Fact]
	public async Task PickAsyncLeavesThePickerOfAnotherPickOnScreenWhenTheTokenIsCanceled()
	{
		using var cancellation = new CancellationTokenSource();
		var folder = NewFolder("Second");
		var (first, firstPicker, _) = await StartPickAsync(token: cancellation.Token);
		await FilesApplePicker.WaitUntilShownAsync(firstPicker);
		var secondToken = CanceledWhenTheTestEnds();
		var second = await StartOnMainThreadAsync(() => FolderPicker.Default.PickAsync(secondToken));
		var secondPicker = await FilesApplePicker.WaitForPickerOnTopOfAsync(firstPicker);

		await cancellation.CancelAsync();
		Assert.True((await first.WaitAsync(operationTimeout)).IsCancelled);

		Assert.True(await InvokeOnMainThreadAsync(() => secondPicker.PresentingViewController is not null && !secondPicker.IsBeingDismissed), "The picker of the second pick was taken off the screen when the first pick was canceled.");
		Assert.False(second.IsCompleted);
		await FilesApplePicker.SimulateUserPicksAsync(secondPicker, NSUrl.CreateFileUrl(folder, true));
		var result = await second.WaitAsync(operationTimeout);
		Assert.True(result.IsSuccessful, result.Exception?.ToString());
		Assert.Equal(new Folder(folder, "Second"), result.Folder);
	}

	// A pick that the app starts as soon as a canceled one returns, on the main thread as app code does, presents its picker from the view
	// controller on top: that must no longer be the canceled pick's picker, since UIKit drops a presentation from a view controller that is
	// being dismissed (the new pick would then have no picker, and never complete).
	[Fact]
	public async Task PickAsyncPresentsThePickerOfAPickStartedAsSoonAsACanceledPickReturns()
	{
		using var cancellation = new CancellationTokenSource();
		var folder = NewFolder("Next");
		var (first, firstPicker, _) = await StartPickAsync(token: cancellation.Token);
		await FilesApplePicker.WaitUntilShownAsync(firstPicker);
		var nextToken = CanceledWhenTheTestEnds();

		var (firstResult, pickerOnScreen, next) = await InvokeOnMainThreadAsync(async () =>
		{
			cancellation.Cancel();
			var result = await first;
			var onScreen = await FilesApplePicker.IsPickerPresentedAsync();
			return (result, onScreen, FolderPicker.Default.PickAsync(nextToken));
		});

		Assert.True(firstResult.IsCancelled);
		Assert.False(pickerOnScreen, "The canceled pick returned while its picker was still on screen.");
		var nextPicker = await FilesApplePicker.WaitForPickerAsync();
		await FilesApplePicker.WaitUntilShownAsync(nextPicker);
		await FilesApplePicker.SimulateUserPicksAsync(nextPicker, NSUrl.CreateFileUrl(folder, true));
		var nextResult = await next.WaitAsync(operationTimeout);
		Assert.True(nextResult.IsSuccessful, nextResult.Exception?.ToString());
		Assert.Equal(new Folder(folder, "Next"), nextResult.Folder);
	}

	[Fact]
	public async Task PickAsyncFailsWhenThePickerReturnsNoUrl()
	{
		var (operation, picker, _) = await StartPickAsync();

		await FilesApplePicker.SimulateUserPicksAsync(picker);
		var result = await operation.WaitAsync(operationTimeout);

		Assert.False(result.IsSuccessful);
		Assert.False(result.IsCancelled);
		Assert.Null(result.Folder);
		var exception = Assert.IsType<FolderPickerException>(result.Exception);
		Assert.Equal("No folder was selected.", exception.Message);
		Assert.Throws<FolderPickerException>(result.EnsureSuccess);
	}

	[Fact]
	public async Task PickAsyncFailsWhenThePickedUrlHasNoPath()
	{
		var (operation, picker, _) = await StartPickAsync();

		await FilesApplePicker.SimulateUserPicksAsync(picker, NSUrl.FromString("mailto:someone@example.com")!);
		var result = await operation.WaitAsync(operationTimeout);

		Assert.False(result.IsSuccessful);
		var exception = Assert.IsType<FolderPickerException>(result.Exception);
		Assert.Equal("File path cannot be null.", exception.Message);
	}

	[Fact]
	public async Task PickAsyncFailsWithoutPresentingAnythingWhenNoWindowHasAViewController()
	{
		await RemoveRootViewControllersAsync();

		var result = await InvokeOnMainThreadAsync(() => FolderPicker.Default.PickAsync()).WaitAsync(operationTimeout);

		Assert.False(result.IsSuccessful);
		Assert.False(result.IsCancelled);
		Assert.NotNull(result.Exception);
	}

	[Fact]
	public async Task PickAsyncExplainsWithAFolderPickerExceptionWhenNoWindowHasAViewController()
	{
		await RemoveRootViewControllersAsync();

		var result = await InvokeOnMainThreadAsync(() => FolderPicker.Default.PickAsync()).WaitAsync(operationTimeout);

		var exception = Assert.IsType<FolderPickerException>(result.Exception);
		Assert.Contains("Unable to get a window", exception.Message, StringComparison.Ordinal);
	}

	[Fact]
	public async Task PickAsyncFromABackgroundThreadFailsBecauseUIKitMustBeUsedOnTheMainThread()
	{
		Skip.IfNot(UIApplication.CheckForIllegalCrossThreadCalls, "UIKit's cross-thread check is off in this build (Release), so the call would reach UIKit from a background thread.");

		var result = await Task.Run(() => FolderPicker.Default.PickAsync()).WaitAsync(operationTimeout);

		Assert.False(result.IsSuccessful);
		Assert.IsType<UIKitThreadAccessException>(result.Exception);
		Assert.False(await FilesApplePicker.IsPickerPresentedAsync());
	}

	[Fact]
	public async Task InternalPickAsyncStopsBeforeTouchingUIKitWhenTheTokenIsAlreadyCanceled()
	{
		// PickAsync checks the token before it calls the platform code, so the check at the top of the platform method can only be reached
		// by calling that method itself.
		using var cancellation = new CancellationTokenSource();
		await cancellation.CancelAsync();
		var picker = new FolderPickerImplementation();
		var withInitialPath = typeof(FolderPickerImplementation).GetMethod("InternalPickAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance, [typeof(string), typeof(CancellationToken)])!;
		var withoutInitialPath = typeof(FolderPickerImplementation).GetMethod("InternalPickAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance, [typeof(CancellationToken)])!;

		await Assert.ThrowsAsync<OperationCanceledException>(() => (Task<Folder>)withInitialPath.Invoke(picker, ["/", cancellation.Token])!);
		await Assert.ThrowsAsync<OperationCanceledException>(() => (Task<Folder>)withoutInitialPath.Invoke(picker, [cancellation.Token])!);
		Assert.False(await FilesApplePicker.IsPickerPresentedAsync());
	}

	[Fact]
	public async Task ThePickerAndItsDelegateAreNotKeptAliveOncePickingIsDone()
	{
		var (operation, picker, pickerDelegate) = await StartPickAsync();
		var references = new[] { new WeakReference(picker), new WeakReference(pickerDelegate) };
		await FilesApplePicker.SimulateUserCancelsAsync(picker);
		await operation.WaitAsync(operationTimeout);
		await InvokeOnMainThreadAsync(FilesApplePicker.DismissAnyPickerAsync);
		picker = null!;
		pickerDelegate = null!;
		operation = null!;

		await AssertionExtensions.WaitForGC(references);
	}

	Task PrepareHostAsync() => Task.CompletedTask;

	static async Task AssertNoPickerWasStartedAsync() =>
		Assert.False(await FilesApplePicker.IsPickerPresentedAsync(), "A document picker is on screen.");

	static string TemporaryRoot => NSFileManager.DefaultManager.GetTemporaryDirectory().Path!;

	async Task<(Task<FolderPickerResult> Operation, UIDocumentPickerViewController Picker, object PickerDelegate)> StartPickAsync(string? initialPath = null, CancellationToken token = default)
	{
		var operation = await StartOnMainThreadAsync(() => initialPath is null
			? FolderPicker.Default.PickAsync(token)
			: FolderPicker.Default.PickAsync(initialPath, token));

		var picker = await FilesApplePicker.WaitForPickerAsync();
		return (operation, picker, await FilesApplePicker.GetDelegateAsync(picker));
	}

	string NewFolder(string name)
	{
		var parent = Path.Combine(TemporaryRoot, $"files-folderpicker-{Guid.NewGuid():N}");
		var folder = Path.Combine(parent, name);
		Directory.CreateDirectory(folder);
		AddCleanup(() => Directory.Delete(parent, recursive: true));
		return folder;
	}

	async Task RemoveRootViewControllersAsync()
	{
		var removed = await InvokeOnMainThreadAsync(() =>
		{
			var windows = UIApplication.SharedApplication.ConnectedScenes.ToArray().OfType<UIWindowScene>().SelectMany(scene => scene.Windows).ToArray();
			var rootViewControllers = windows.Select(window => (Window: window, window.RootViewController)).ToArray();
			foreach (var window in windows)
			{
				window.RootViewController = null;
			}

			return rootViewControllers;
		});

		AddCleanup(() => InvokeOnMainThreadAsync(() =>
		{
			foreach (var (window, rootViewController) in removed)
			{
				window.RootViewController = rootViewController;
			}
		}));
	}
}