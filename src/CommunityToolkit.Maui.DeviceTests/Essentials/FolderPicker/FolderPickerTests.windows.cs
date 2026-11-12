using System.Reflection;
using CommunityToolkit.Maui.Core.Primitives;
using CommunityToolkit.Maui.Storage;

namespace CommunityToolkit.Maui.DeviceTests;

// A test cannot answer the folder dialog, so everything from the dialog on needs a person (RequiresUserInteraction); the checks before it (the
// token, a main window to attach to) run headless.
public partial class FolderPickerTests
{
	const string noWindowMessage = "Cannot present folder picker: No active window found. Ensure the app is active with a visible window.";

	[Theory]
	[InlineData(0, false)]
	[InlineData(1, false)]
	[InlineData(0, true)]
	[InlineData(1, true)]
	public async Task PickAsyncFailsWithAFolderPickerExceptionWhenTheProcessHasNoVisibleWindow(int overload, bool useStaticApi)
	{
		Skip.IfNot(await HideTheAppWindowsAsync(), "Windows still reports a main window for the process after its windows were hidden, so the toolkit would show the real folder dialog.");

		var result = await InvokeOnMainThreadAsync(() => PickAsync(overload, useStaticApi, CancellationToken.None, Path.GetTempPath())).WaitAsync(operationTimeout);

		Assert.False(result.IsSuccessful);
		Assert.False(result.IsCancelled);
		Assert.Null(result.Folder);
		var exception = Assert.IsType<FolderPickerException>(result.Exception);
		Assert.Equal(noWindowMessage, exception.Message);
		Assert.Equal(0, CountOpenDialogs());
	}

	[Fact]
	public async Task InternalPickAsyncStopsBeforeCreatingThePickerWhenTheTokenIsAlreadyCanceled()
	{
		// PickAsync checks the token before it calls the platform code, so the check at the top of the platform method can only be reached
		// by calling that method itself.
		using var cancellation = new CancellationTokenSource();
		await cancellation.CancelAsync();
		var picker = new FolderPickerImplementation();
		var withInitialPath = typeof(FolderPickerImplementation).GetMethod("InternalPickAsync", BindingFlags.NonPublic | BindingFlags.Instance, [typeof(string), typeof(CancellationToken)])
			?? throw new InvalidOperationException("FolderPickerImplementation.InternalPickAsync(string, CancellationToken) was not found.");
		var withoutInitialPath = typeof(FolderPickerImplementation).GetMethod("InternalPickAsync", BindingFlags.NonPublic | BindingFlags.Instance, [typeof(CancellationToken)])
			?? throw new InvalidOperationException("FolderPickerImplementation.InternalPickAsync(CancellationToken) was not found.");

		await Assert.ThrowsAsync<OperationCanceledException>(() => InvokeOnMainThreadAsync(() => (Task<Folder>)withInitialPath.Invoke(picker, [Path.GetTempPath(), cancellation.Token])!));
		await Assert.ThrowsAsync<OperationCanceledException>(() => InvokeOnMainThreadAsync(() => (Task<Folder>)withoutInitialPath.Invoke(picker, [cancellation.Token])!));
		Assert.Equal(0, CountOpenDialogs());
	}

	[Fact]
	[Category(TestCategory.RequiresUserInteraction)]
	public async Task PickAsyncReturnsThePathAndTheNameOfTheFolderThatThePersonPicks()
	{
		// Person: pick the folder "files-test-folder" that this test creates in the temporary folder (the dialog may open elsewhere).
		var folder = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "files-test-folder")).FullName;
		AddCleanup(() => Directory.Delete(folder, recursive: true));

		var result = await InvokeOnMainThreadAsync(() => FolderPicker.Default.PickAsync(Path.GetTempPath())).WaitAsync(personTimeout);

		Assert.True(result.IsSuccessful, result.Exception?.ToString());
		Assert.Equal(Path.TrimEndingDirectorySeparator(folder), Path.TrimEndingDirectorySeparator(result.Folder.Path), ignoreCase: true);
		Assert.Equal("files-test-folder", result.Folder.Name);
	}

	[Fact]
	[Category(TestCategory.RequiresUserInteraction)]
	public async Task PickAsyncReturnsACanceledResultWhenThePersonCancelsTheFolderDialog()
	{
		// Person: press Cancel in the dialog that opens.
		var result = await InvokeOnMainThreadAsync(() => FolderPicker.Default.PickAsync()).WaitAsync(personTimeout);

		Assert.True(result.IsCancelled);
		Assert.Null(result.Folder);
		Assert.IsType<OperationCanceledException>(result.Exception, exactMatch: false);
	}

	[Fact]
	[Category(TestCategory.RequiresUserInteraction)]
	public async Task PickAsyncFailsWhenThePersonPicksAFolderWithoutAFileSystemPath()
	{
		// Person: pick a folder that has no file system path, such as a library or a folder of a phone connected over MTP.
		var result = await InvokeOnMainThreadAsync(() => FolderPicker.Default.PickAsync()).WaitAsync(personTimeout);

		var exception = Assert.IsType<FolderPickerException>(result.Exception);
		Assert.Equal("Folder doesn't exist.", exception.Message);
	}

	[Fact]
	[Category(TestCategory.RequiresUserInteraction)]
	public async Task PickAsyncClosesTheFolderDialogAndReturnsACanceledResultWhenTheTokenIsCanceled()
	{
		// Person: only watch; the token closes the real dialog.
		using var cancellation = new CancellationTokenSource();
		var operation = await StartOnMainThreadAsync(() => FolderPicker.Default.PickAsync(cancellation.Token));
		await AssertionExtensions.AssertEventually(() => CountOpenDialogs() > 0, timeout: 15000, message: "The folder dialog did not open.");

		await cancellation.CancelAsync();
		var result = await operation.WaitAsync(operationTimeout);

		Assert.True(result.IsCancelled);
		await AssertionExtensions.AssertEventually(() => CountOpenDialogs() is 0, timeout: 5000, message: "The folder dialog is still open after the token was canceled.");
	}

	// A Cancel button runs the callbacks of the token inline inside Cancel(), on the UI thread, which must not wait for the dialog's modal loop.
	[Fact]
	[Category(TestCategory.RequiresUserInteraction)]
	public async Task PickAsyncClosesTheFolderDialogAndReturnsACanceledResultWhenTheTokenIsCanceledOnTheMainThread()
	{
		// Person: only watch; the token closes the real dialog.
		using var cancellation = new CancellationTokenSource();
		var operation = await StartOnMainThreadAsync(() => FolderPicker.Default.PickAsync(cancellation.Token));
		await AssertionExtensions.AssertEventually(() => CountOpenDialogs() > 0, timeout: 15000, message: "The folder dialog did not open.");

		await InvokeOnMainThreadAsync(cancellation.Cancel);
		var result = await operation.WaitAsync(operationTimeout);

		Assert.True(result.IsCancelled);
		await AssertionExtensions.AssertEventually(() => CountOpenDialogs() is 0, timeout: 5000, message: "The folder dialog is still open after the token was canceled.");
	}

	// The picker may be before its dialog, between its token check and its dialog, or inside its modal loop when the token is canceled.
	[Theory]
	[Category(TestCategory.RequiresUserInteraction)]
	[InlineData(0)]
	[InlineData(10)]
	[InlineData(50)]
	[InlineData(100)]
	[InlineData(200)]
	[InlineData(400)]
	public async Task PickAsyncReturnsACanceledResultAndLeavesNoDialogWhenTheTokenIsCanceledAtAnyTimeAfterTheStart(int cancelAfterMilliseconds)
	{
		// Person: only watch; the token cancels without waiting for the dialog.
		using var cancellation = new CancellationTokenSource();
		var operation = await StartOnMainThreadAsync(() => FolderPicker.Default.PickAsync(cancellation.Token));

		await Task.Delay(cancelAfterMilliseconds);
		await cancellation.CancelAsync();
		var result = await operation.WaitAsync(operationTimeout);

		Assert.True(result.IsCancelled);
		// A fixed wait, not AssertEventually: a dialog that opens after the canceled result must fail the test too.
		await Task.Delay(TimeSpan.FromSeconds(2));
		Assert.Equal(0, CountOpenDialogs());
	}

	// The picker only closes the dialog that it opened: a dialog that the app had already opened on the same window stays.
	[Fact]
	[Category(TestCategory.RequiresUserInteraction)]
	public async Task PickAsyncKeepsAMessageBoxOfTheAppThatWasOpenBeforeTheStartOpenWhenTheTokenIsCanceled()
	{
		// Person: only watch; the message box stays and the test closes it.
		var messageBox = await ShowAMessageBoxOnTheAppWindowAsync();
		using var cancellation = new CancellationTokenSource();
		var operation = await StartOnMainThreadAsync(() => FolderPicker.Default.PickAsync(cancellation.Token));
		await AssertionExtensions.AssertEventually(() => CountOpenDialogs() is 2, timeout: 15000, message: "The folder dialog did not open next to the message box.");

		await cancellation.CancelAsync();
		var result = await operation.WaitAsync(operationTimeout);

		Assert.True(result.IsCancelled);
		await AssertionExtensions.AssertEventually(() => CountOpenDialogs() is 1, timeout: 5000, message: "The folder dialog is still open after the token was canceled.");
		// A fixed wait, not AssertEventually: a late close of the message box must fail the test too.
		await Task.Delay(TimeSpan.FromSeconds(2));
		Assert.True(IsWindowOpen(messageBox), "The message box of the app was closed.");
		Assert.Equal(1, CountOpenDialogs());
	}

	// A save that starts while the folder dialog is open waits for it, and the folder pick's cancel leaves the save running.
	[Fact]
	[Category(TestCategory.RequiresUserInteraction)]
	public async Task PickAsyncCanceledByItsTokenLeavesASaveStartedWhileItsDialogWasOpenRunning()
	{
		// Person: only watch; the folder dialog closes, a save dialog opens, and the test closes it.
		var pickCancellation = CancellationEndedWithTheTest();
		var saveCancellation = CancellationEndedWithTheTest();
		var stream = new FilesScriptedStream([1, 2, 3]);
		var pick = await StartOnMainThreadAsync(() => FolderPicker.Default.PickAsync(pickCancellation.Token));
		await AssertionExtensions.AssertEventually(() => CountOpenDialogs() > 0, timeout: 15000, message: "The folder dialog did not open.");
		var save = await StartOnMainThreadAsync(() => FileSaver.Default.SaveAsync("files-test-overlap.txt", stream, saveCancellation.Token));
		await AssertKeepsRunningAsTheOnlyOpenDialogAsync(pick, "The folder pick ended before its token was canceled.");
		Assert.False(save.IsCompleted, "The save ended while it waited for the folder dialog.");

		var pickDialogs = OpenDialogs();
		await pickCancellation.CancelAsync();
		var pickResult = await pick.WaitAsync(operationTimeout);

		Assert.True(pickResult.IsCancelled);
		await AssertionExtensions.AssertEventually(() => OpenDialogs().Except(pickDialogs).Any(), timeout: 15000, message: "The save dialog did not open after the folder pick ended.");
		await AssertKeepsRunningAsTheOnlyOpenDialogAsync(save, "The save ended when the token of the folder pick was canceled.");

		await saveCancellation.CancelAsync();
		var saveResult = await save.WaitAsync(operationTimeout);

		Assert.True(saveResult.IsCancelled);
		Assert.Equal(0, stream.ReadCalls);
		await AssertionExtensions.AssertEventually(() => CountOpenDialogs() is 0, timeout: 5000, message: "The save dialog is still open after its token was canceled.");
	}

	// Two folder picks that start together on one window: the second waits for the first dialog, and the first one's cancel leaves it running.
	[Fact]
	[Category(TestCategory.RequiresUserInteraction)]
	public async Task PickAsyncCanceledByItsTokenLeavesAFolderPickStartedAtTheSameTimeRunning()
	{
		// Person: only watch; a folder dialog closes, a second one opens, and the test closes it.
		var firstCancellation = CancellationEndedWithTheTest();
		var secondCancellation = CancellationEndedWithTheTest();
		var (first, second) = await StartTogetherOnMainThreadAsync(
			() => FolderPicker.Default.PickAsync(firstCancellation.Token),
			() => FolderPicker.Default.PickAsync(secondCancellation.Token));
		await AssertionExtensions.AssertEventually(() => CountOpenDialogs() > 0, timeout: 15000, message: "The first folder dialog did not open.");
		await AssertKeepsRunningAsTheOnlyOpenDialogAsync(first, "The first folder pick ended before its token was canceled.");
		Assert.False(second.IsCompleted, "The second folder pick ended while it waited for the first dialog.");

		var firstDialogs = OpenDialogs();
		await firstCancellation.CancelAsync();
		var firstResult = await first.WaitAsync(operationTimeout);

		Assert.True(firstResult.IsCancelled);
		await AssertionExtensions.AssertEventually(() => OpenDialogs().Except(firstDialogs).Any(), timeout: 15000, message: "The second folder dialog did not open after the first pick ended.");
		await AssertKeepsRunningAsTheOnlyOpenDialogAsync(second, "The second folder pick ended when the token of the first was canceled.");

		await secondCancellation.CancelAsync();
		var secondResult = await second.WaitAsync(operationTimeout);

		Assert.True(secondResult.IsCancelled);
		await AssertionExtensions.AssertEventually(() => CountOpenDialogs() is 0, timeout: 5000, message: "The second folder dialog is still open after its token was canceled.");
	}

	Task PrepareHostAsync() => Task.CompletedTask;

	static Task AssertNoPickerWasStartedAsync()
	{
		Assert.Equal(0, CountOpenDialogs());
		return Task.CompletedTask;
	}
}