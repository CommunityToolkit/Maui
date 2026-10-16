using CommunityToolkit.Maui.Storage;

namespace CommunityToolkit.Maui.DeviceTests;

// Windows: FileSaver shows the system's save dialog (Windows.Storage.Pickers.FileSavePicker attached to the process's main window, or the
// Windows App SDK picker from .NET 11) and writes the stream to the file that the person picks with WriteStream (FileSaverWriteStreamTests).
// A test cannot answer the dialog, so everything from the dialog on needs a person (RequiresUserInteraction); the check before it, that the
// process has a main window to attach the dialog to, runs headless with the app's windows hidden.
public partial class FileSaverTests
{
	const string noWindowMessage = "Cannot present file picker: No active window found. Ensure the app is active with a visible window.";

	static readonly byte[] content = Enumerable.Range(0, 10_000).Select(i => (byte)(i % 251)).ToArray();

	// Every overload reaches the check of the main window before it shows anything, so nothing is read from the stream.
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

	// The overloads with an initial path (0 without, 2 with a progress report). The .NET 10 build hands the dialog no folder to start in
	// (only the .NET 11 arm sets SuggestedFolder), so the person may save anywhere.
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
		// Shows the real dialog and closes it through the token; the person only watches.
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

	// A save that is not asked about must never show a dialog.
	Task PrepareHostAsync() => Task.CompletedTask;

	static Task AssertNoPickerWasStartedAsync()
	{
		Assert.Equal(0, CountOpenDialogs());
		return Task.CompletedTask;
	}
}