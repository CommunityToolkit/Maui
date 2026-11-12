using CommunityToolkit.Maui.Storage;

namespace CommunityToolkit.Maui.DeviceTests;

// A test cannot answer the save dialog, so everything from the dialog on needs a person (RequiresUserInteraction); the check before it, that the
// process has a main window to attach the dialog to, runs headless with the app's windows hidden.
public partial class FileSaverTests
{
	const string noWindowMessage = "Cannot present file picker: No active window found. Ensure the app is active with a visible window.";

	static readonly byte[] content = Enumerable.Range(0, 10_000).Select(i => (byte)(i % 251)).ToArray();

	[Theory]
	[InlineData(0, false)]
	[InlineData(1, false)]
	[InlineData(2, false)]
	[InlineData(3, false)]
	[InlineData(0, true)]
	[InlineData(1, true)]
	[InlineData(2, true)]
	[InlineData(3, true)]
	public async Task SaveAsyncFailsWithAFileSaveExceptionWhenTheProcessHasNoVisibleWindow(int overload, bool useStaticApi)
	{
		Skip.IfNot(await HideTheAppWindowsAsync(), "Windows still reports a main window for the process after its windows were hidden, so the toolkit would show the real save dialog.");
		var stream = new FilesScriptedStream([1, 2, 3]);
		var progress = new FilesRecordingProgress();

		var result = await InvokeOnMainThreadAsync(() => SaveAsync(overload, useStaticApi, "report.txt", stream, progress, CancellationToken.None, Path.GetTempPath())).WaitAsync(operationTimeout);

		Assert.False(result.IsSuccessful);
		Assert.False(result.IsCancelled);
		Assert.Null(result.FilePath);
		var exception = Assert.IsType<FileSaveException>(result.Exception);
		Assert.Equal(noWindowMessage, exception.Message);
		Assert.Equal(0, stream.ReadCalls);
		Assert.Empty(progress.Values);
		Assert.Equal(0, CountOpenDialogs());
	}

	[Fact]
	[Category(TestCategory.RequiresUserInteraction)]
	public async Task SaveAsyncWritesTheStreamToTheFileThatThePersonPicksInTheSaveDialog()
	{
		// Person: save "files-test-saved.txt" in any folder of the dialog that opens (the "*.txt" choice is preselected).
		var progress = new FilesRecordingProgress();

		var result = await InvokeOnMainThreadAsync(() => FileSaver.Default.SaveAsync("files-test-saved.txt", new MemoryStream(content), progress)).WaitAsync(personTimeout);

		Assert.True(result.IsSuccessful, result.Exception?.ToString());
		var path = Assert.IsType<string>(result.FilePath);
		AddCleanup(() => File.Delete(path));
		Assert.Equal(".txt", Path.GetExtension(path));
		Assert.Equal(content, await File.ReadAllBytesAsync(path));
		Assert.Equal(100, progress.Values[^1]);
	}

	// SuggestedFolder only picks the folder that the dialog starts in, so the person may save anywhere.
	[Theory]
	[Category(TestCategory.RequiresUserInteraction)]
	[InlineData(0)]
	[InlineData(2)]
	public async Task SaveAsyncWithAnInitialPathWritesTheStreamToTheFileThatThePersonPicks(int overload)
	{
		// Person: save "files-test-initial-path.txt" in any folder of the dialog that opens.
		var progress = new FilesRecordingProgress();

		var result = await InvokeOnMainThreadAsync(() => SaveAsync(overload, false, "files-test-initial-path.txt", new MemoryStream(content), progress, CancellationToken.None, Path.GetTempPath())).WaitAsync(personTimeout);

		Assert.True(result.IsSuccessful, result.Exception?.ToString());
		var path = Assert.IsType<string>(result.FilePath);
		AddCleanup(() => File.Delete(path));
		Assert.Equal(content, await File.ReadAllBytesAsync(path));
		if (overload is 2)
		{
			Assert.Equal(100, progress.Values[^1]);
		}
		else
		{
			Assert.Empty(progress.Values);
		}
	}

	[Fact]
	[Category(TestCategory.RequiresUserInteraction)]
	public async Task SaveAsyncOffersOnlyAllFilesForAFileNameWithoutAnExtensionAndWritesTheStream()
	{
		// Person: the "Save as type" list of the dialog that opens has the single choice "All files"; save the file in any folder.
		var result = await InvokeOnMainThreadAsync(() => FileSaver.Default.SaveAsync("files-test-no-extension", new MemoryStream(content))).WaitAsync(personTimeout);

		Assert.True(result.IsSuccessful, result.Exception?.ToString());
		var path = Assert.IsType<string>(result.FilePath);
		AddCleanup(() => File.Delete(path));
		Assert.StartsWith("files-test-no-extension", Path.GetFileName(path), StringComparison.Ordinal);
		Assert.Equal(content, await File.ReadAllBytesAsync(path));
	}

	[Fact]
	[Category(TestCategory.RequiresUserInteraction)]
	public async Task SaveAsyncReturnsACanceledResultWhenThePersonCancelsTheSaveDialog()
	{
		// Person: press Cancel in the dialog that opens.
		var stream = new FilesScriptedStream([1, 2, 3]);

		var result = await InvokeOnMainThreadAsync(() => FileSaver.Default.SaveAsync("files-test-canceled.txt", stream)).WaitAsync(personTimeout);

		Assert.True(result.IsCancelled);
		Assert.Null(result.FilePath);
		Assert.IsType<OperationCanceledException>(result.Exception, exactMatch: false);
		Assert.Equal(0, stream.ReadCalls);
	}

	[Fact]
	[Category(TestCategory.RequiresUserInteraction)]
	public async Task SaveAsyncFailsWhenThePersonPicksALocationWithoutAFileSystemPath()
	{
		// Person: save into a location that has no file system path, such as a library or a phone connected over MTP.
		var stream = new FilesScriptedStream([1, 2, 3]);

		var result = await InvokeOnMainThreadAsync(() => FileSaver.Default.SaveAsync("files-test-virtual.txt", stream)).WaitAsync(personTimeout);

		var exception = Assert.IsType<FileSaveException>(result.Exception);
		Assert.Equal("Path doesn't exist.", exception.Message);
		Assert.Equal(0, stream.ReadCalls);
	}

	[Fact]
	[Category(TestCategory.RequiresUserInteraction)]
	public async Task SaveAsyncClosesTheSaveDialogAndReturnsACanceledResultWhenTheTokenIsCanceled()
	{
		// Person: only watch; the token closes the real dialog.
		using var cancellation = new CancellationTokenSource();
		var stream = new FilesScriptedStream([1, 2, 3]);
		var operation = await StartOnMainThreadAsync(() => FileSaver.Default.SaveAsync("files-test-token.txt", stream, cancellation.Token));
		await AssertionExtensions.AssertEventually(() => CountOpenDialogs() > 0, timeout: 15000, message: "The save dialog did not open.");

		await cancellation.CancelAsync();
		var result = await operation.WaitAsync(operationTimeout);

		Assert.True(result.IsCancelled);
		Assert.Equal(0, stream.ReadCalls);
		await AssertionExtensions.AssertEventually(() => CountOpenDialogs() is 0, timeout: 5000, message: "The save dialog is still open after the token was canceled.");
	}

	// A Cancel button runs the callbacks of the token inline inside Cancel(), on the UI thread, which must not wait for the dialog's modal loop.
	[Fact]
	[Category(TestCategory.RequiresUserInteraction)]
	public async Task SaveAsyncClosesTheSaveDialogAndReturnsACanceledResultWhenTheTokenIsCanceledOnTheMainThread()
	{
		// Person: only watch; the token closes the real dialog.
		using var cancellation = new CancellationTokenSource();
		var stream = new FilesScriptedStream([1, 2, 3]);
		var operation = await StartOnMainThreadAsync(() => FileSaver.Default.SaveAsync("files-test-token-main-thread.txt", stream, cancellation.Token));
		await AssertionExtensions.AssertEventually(() => CountOpenDialogs() > 0, timeout: 15000, message: "The save dialog did not open.");

		await InvokeOnMainThreadAsync(cancellation.Cancel);
		var result = await operation.WaitAsync(operationTimeout);

		Assert.True(result.IsCancelled);
		Assert.Equal(0, stream.ReadCalls);
		await AssertionExtensions.AssertEventually(() => CountOpenDialogs() is 0, timeout: 5000, message: "The save dialog is still open after the token was canceled.");
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
	public async Task SaveAsyncReturnsACanceledResultAndLeavesNoDialogWhenTheTokenIsCanceledAtAnyTimeAfterTheStart(int cancelAfterMilliseconds)
	{
		// Person: only watch; the token cancels without waiting for the dialog.
		using var cancellation = new CancellationTokenSource();
		var stream = new FilesScriptedStream([1, 2, 3]);
		var operation = await StartOnMainThreadAsync(() => FileSaver.Default.SaveAsync("files-test-token-timing.txt", stream, cancellation.Token));

		await Task.Delay(cancelAfterMilliseconds);
		await cancellation.CancelAsync();
		var result = await operation.WaitAsync(operationTimeout);

		Assert.True(result.IsCancelled);
		Assert.Equal(0, stream.ReadCalls);
		// A fixed wait, not AssertEventually: a dialog that opens after the canceled result must fail the test too.
		await Task.Delay(TimeSpan.FromSeconds(2));
		Assert.Equal(0, CountOpenDialogs());
	}

	// The picker only closes the dialog that it opened: a dialog that the app had already opened on the same window stays.
	[Fact]
	[Category(TestCategory.RequiresUserInteraction)]
	public async Task SaveAsyncKeepsAMessageBoxOfTheAppThatWasOpenBeforeTheStartOpenWhenTheTokenIsCanceled()
	{
		// Person: only watch; the message box stays and the test closes it.
		var messageBox = await ShowAMessageBoxOnTheAppWindowAsync();
		using var cancellation = new CancellationTokenSource();
		var stream = new FilesScriptedStream([1, 2, 3]);
		var operation = await StartOnMainThreadAsync(() => FileSaver.Default.SaveAsync("files-test-token-snapshot.txt", stream, cancellation.Token));
		await AssertionExtensions.AssertEventually(() => CountOpenDialogs() is 2, timeout: 15000, message: "The save dialog did not open next to the message box.");

		await cancellation.CancelAsync();
		var result = await operation.WaitAsync(operationTimeout);

		Assert.True(result.IsCancelled);
		Assert.Equal(0, stream.ReadCalls);
		await AssertionExtensions.AssertEventually(() => CountOpenDialogs() is 1, timeout: 5000, message: "The save dialog is still open after the token was canceled.");
		// A fixed wait, not AssertEventually: a late close of the message box must fail the test too.
		await Task.Delay(TimeSpan.FromSeconds(2));
		Assert.True(IsWindowOpen(messageBox), "The message box of the app was closed.");
		Assert.Equal(1, CountOpenDialogs());
	}

	// Two picks that start together on one window: the folder pick waits for the save dialog, and the save's cancel leaves it running.
	[Fact]
	[Category(TestCategory.RequiresUserInteraction)]
	public async Task SaveAsyncCanceledByItsTokenLeavesAFolderPickStartedAtTheSameTimeRunning()
	{
		// Person: only watch; the save dialog closes, a folder dialog opens, and the test closes it.
		var saveCancellation = CancellationEndedWithTheTest();
		var pickCancellation = CancellationEndedWithTheTest();
		var stream = new FilesScriptedStream([1, 2, 3]);
		var (save, pick) = await StartTogetherOnMainThreadAsync(
			() => FileSaver.Default.SaveAsync("files-test-overlap.txt", stream, saveCancellation.Token),
			() => FolderPicker.Default.PickAsync(pickCancellation.Token));
		await AssertionExtensions.AssertEventually(() => CountOpenDialogs() > 0, timeout: 15000, message: "The save dialog did not open.");
		await AssertKeepsRunningAsTheOnlyOpenDialogAsync(save, "The save ended before its token was canceled.");
		Assert.False(pick.IsCompleted, "The folder pick ended while it waited for the save dialog.");

		var saveDialogs = OpenDialogs();
		await saveCancellation.CancelAsync();
		var saveResult = await save.WaitAsync(operationTimeout);

		Assert.True(saveResult.IsCancelled);
		Assert.Equal(0, stream.ReadCalls);
		await AssertionExtensions.AssertEventually(() => OpenDialogs().Except(saveDialogs).Any(), timeout: 15000, message: "The folder dialog did not open after the save ended.");
		await AssertKeepsRunningAsTheOnlyOpenDialogAsync(pick, "The folder pick ended when the token of the save was canceled.");

		await pickCancellation.CancelAsync();
		var pickResult = await pick.WaitAsync(operationTimeout);

		Assert.True(pickResult.IsCancelled);
		await AssertionExtensions.AssertEventually(() => CountOpenDialogs() is 0, timeout: 5000, message: "The folder dialog is still open after its token was canceled.");
	}

	// A pick that waits for an earlier pick on the same window ends at once when its token is canceled, and the earlier dialog stays.
	[Fact]
	[Category(TestCategory.RequiresUserInteraction)]
	public async Task SaveAsyncCanceledByItsTokenWhileItWaitsForAnEarlierSaveLeavesTheEarlierSaveRunning()
	{
		// Person: only watch; one save dialog opens and the test closes it.
		var firstCancellation = CancellationEndedWithTheTest();
		var secondCancellation = CancellationEndedWithTheTest();
		var firstStream = new FilesScriptedStream([1, 2, 3]);
		var secondStream = new FilesScriptedStream([1, 2, 3]);
		var (first, second) = await StartTogetherOnMainThreadAsync(
			() => FileSaver.Default.SaveAsync("files-test-overlap-first.txt", firstStream, firstCancellation.Token),
			() => FileSaver.Default.SaveAsync("files-test-overlap-second.txt", secondStream, secondCancellation.Token));
		await AssertionExtensions.AssertEventually(() => CountOpenDialogs() > 0, timeout: 15000, message: "The first save dialog did not open.");

		// Without turns the two saves open their dialogs in either order, and a first dialog that opened after the second save ended would
		// escape that save's cancel; waiting as long as the one-dialog check of the other tests lets both be on screen first.
		await Task.Delay(TimeSpan.FromSeconds(2));
		Assert.False(second.IsCompleted, "The second save ended while it waited for the first save dialog.");

		await secondCancellation.CancelAsync();
		var secondResult = await second.WaitAsync(operationTimeout);

		Assert.True(secondResult.IsCancelled);
		Assert.Equal(0, secondStream.ReadCalls);
		await AssertKeepsRunningAsTheOnlyOpenDialogAsync(first, "The first save ended when the token of the second was canceled.");

		await firstCancellation.CancelAsync();
		var firstResult = await first.WaitAsync(operationTimeout);

		Assert.True(firstResult.IsCancelled);
		Assert.Equal(0, firstStream.ReadCalls);
		await AssertionExtensions.AssertEventually(() => CountOpenDialogs() is 0, timeout: 5000, message: "The first save dialog is still open after its token was canceled.");
	}

	Task PrepareHostAsync() => Task.CompletedTask;

	static Task AssertNoPickerWasStartedAsync()
	{
		Assert.Equal(0, CountOpenDialogs());
		return Task.CompletedTask;
	}
}