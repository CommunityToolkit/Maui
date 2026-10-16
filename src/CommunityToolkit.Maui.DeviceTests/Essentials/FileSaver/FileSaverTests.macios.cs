using System.Reflection;
using System.Text.RegularExpressions;
using CommunityToolkit.Maui.Storage;
using Foundation;
using UIKit;

namespace CommunityToolkit.Maui.DeviceTests;

// iOS and Mac Catalyst: FileSaver writes a temporary copy of the stream in a new folder of the temporary directory, presents a
// UIDocumentPickerViewController that exports it, and reports what the delegate says. The picker is the real one, and the tests answer it
// through its delegate, as UIKit does for a person. The native state that they read back is the picker (mode, directory, event handlers),
// the temporary folder on disk, and whether the picker is still on screen.
public partial class FileSaverTests
{
	static readonly byte[] sample = Enumerable.Range(0, 10_000).Select(i => (byte)(i % 251)).ToArray();

	[Fact]
	public async Task SaveAsyncPresentsTheDocumentPickerToExportATemporaryCopyOfTheFile()
	{
		var fileName = UniqueName("report", ".txt");
		var folders = TrackTemporaryFolders(fileName);

		var (operation, picker, pickerDelegate) = await StartSaveAsync(fileName, new FilesScriptedStream(sample));

		var temporaryFolder = Assert.IsType<string>(folders.FolderWithFile());
		Assert.Equal(sample, await File.ReadAllBytesAsync(Path.Combine(temporaryFolder, fileName)));
		await InvokeOnMainThreadAsync(() =>
		{
			// No initial path: no directory, so the picker opens where the system chooses
			Assert.Equal(UIDocumentPickerMode.ExportToService, picker.DocumentPickerMode);
			Assert.Null(picker.DirectoryUrl);
			Assert.False(picker.AllowsMultipleSelection);
		});
		Assert.Equal((1, 1), FilesApplePicker.CountSubscribers(pickerDelegate));

		const string chosenPath = "/tmp/files-tests-chosen.txt";
		await FilesApplePicker.SimulateUserPicksAsync(picker, NSUrl.CreateFileUrl(chosenPath, false));
		var result = await operation.WaitAsync(operationTimeout);

		Assert.True(result.IsSuccessful, result.Exception?.ToString());
		Assert.Equal(chosenPath, result.FilePath);
		Assert.False(Directory.Exists(temporaryFolder), "The temporary folder was not removed.");
		Assert.Equal((0, 0), FilesApplePicker.CountSubscribers(pickerDelegate));
	}

	[Theory]
	[InlineData("report (final) copy.txt")]
	[InlineData("résumé.pdf")]
	[InlineData("名前.txt")]
	[InlineData("no-extension")]
	public async Task SaveAsyncKeepsTheFileNameOfTheTemporaryCopyWhateverItContains(string fileName)
	{
		var folders = TrackTemporaryFolders(fileName);

		var (operation, picker, _) = await StartSaveAsync(fileName, new MemoryStream([1, 2, 3]));

		var temporaryFolder = Assert.IsType<string>(folders.FolderWithFile());
		Assert.Equal([1, 2, 3], await File.ReadAllBytesAsync(Path.Combine(temporaryFolder, fileName)));
		await FilesApplePicker.SimulateUserCancelsAsync(picker);
		Assert.True((await operation.WaitAsync(operationTimeout)).IsCancelled);
	}

	// An empty initial path ("" expected) gives the picker no directory, so it opens where the system chooses. A file URL string ({temp-url} is
	// "file:///.../tmp/") names the folder of its URL, as it did before an initial path was taken for a path.
	[Theory]
	[InlineData("{temp}", "{temp}")]
	[InlineData("{temp}/My Folder", "{temp}/My Folder")]
	[InlineData("", "")]
	[InlineData("{temp-url}", "{temp}")]
	[InlineData("{temp-url}My%20Folder/", "{temp}/My Folder")]
	public async Task SaveAsyncStartsThePickerInTheFolderOfTheInitialPath(string initialPath, string expectedDirectory)
	{
		var fileName = UniqueName("report", ".txt");
		TrackTemporaryFolders(fileName);
		var temp = TemporaryRoot;
		var tempUrl = NSUrl.CreateFileUrl(temp, true).AbsoluteString!;

		var (operation, picker, _) = await StartSaveAsync(fileName, new MemoryStream([1]), initialPath: initialPath.Replace("{temp-url}", tempUrl, StringComparison.Ordinal).Replace("{temp}", temp, StringComparison.Ordinal));

		var directory = await InvokeOnMainThreadAsync(() => picker.DirectoryUrl?.Path);
		await FilesApplePicker.SimulateUserCancelsAsync(picker);
		Assert.True((await operation.WaitAsync(operationTimeout)).IsCancelled);

		Assert.Equal(expectedDirectory.Length is 0 ? null : expectedDirectory.Replace("{temp}", temp, StringComparison.Ordinal), directory);
	}

	[Fact]
	public async Task SaveAsyncGivesThePickerTheInitialFolderAsAFileUrl()
	{
		var fileName = UniqueName("report", ".txt");
		TrackTemporaryFolders(fileName);

		var (operation, picker, _) = await StartSaveAsync(fileName, new MemoryStream([1]), initialPath: TemporaryRoot);
		var (isFileUrl, path) = await InvokeOnMainThreadAsync(() => (picker.DirectoryUrl?.IsFileUrl, picker.DirectoryUrl?.Path));
		await FilesApplePicker.SimulateUserCancelsAsync(picker);
		await operation.WaitAsync(operationTimeout);

		Assert.True(isFileUrl is true, $"The picker's directory URL is not a file URL (path '{path}').");
		Assert.Equal(TemporaryRoot, path);
	}

	// Every overload reaches the same picker; the ones with an initial path set its directory (the others give it none), and the ones with a
	// progress report to it.
	[Theory]
	[InlineData(0, false)]
	[InlineData(1, false)]
	[InlineData(2, false)]
	[InlineData(3, false)]
	[InlineData(0, true)]
	[InlineData(1, true)]
	[InlineData(2, true)]
	[InlineData(3, true)]
	public async Task EverySaveAsyncOverloadExportsTheFileThroughTheDocumentPicker(int overload, bool useStaticApi)
	{
		var fileName = UniqueName("overload", ".txt");
		var folders = TrackTemporaryFolders(fileName);
		var progress = new FilesRecordingProgress();
		var initialPath = TemporaryRoot;

		var operation = await StartOnMainThreadAsync(() => SaveAsync(overload, useStaticApi, fileName, new FilesScriptedStream(sample), progress, CancellationToken.None, initialPath));
		var picker = await FilesApplePicker.WaitForPickerAsync();

		var hasInitialPath = overload is 0 or 2;
		var hasProgress = overload is 2 or 3;
		Assert.Equal(hasInitialPath ? initialPath : null, await InvokeOnMainThreadAsync(() => picker.DirectoryUrl?.Path));
		Assert.NotNull(folders.FolderWithFile());
		await FilesApplePicker.SimulateUserPicksAsync(picker, NSUrl.CreateFileUrl("/tmp/files-tests-overload.txt", false));
		var result = await operation.WaitAsync(operationTimeout);

		Assert.True(result.IsSuccessful, result.Exception?.ToString());
		Assert.Equal("/tmp/files-tests-overload.txt", result.FilePath);
		Assert.Equal(hasProgress, progress.Values.Count > 0);
	}

	[Fact]
	public async Task SaveAsyncReportsProgressWhileWritingTheTemporaryCopy()
	{
		var fileName = UniqueName("progress", ".bin");
		TrackTemporaryFolders(fileName);
		var progress = new FilesRecordingProgress();

		var (operation, picker, _) = await StartSaveAsync(fileName, new FilesScriptedStream(sample), progress);
		await FilesApplePicker.SimulateUserCancelsAsync(picker);
		await operation.WaitAsync(operationTimeout);

		var values = progress.Values;
		Assert.True(values.Count >= 2, $"Expected the progress of each chunk and the final report, got {values.Count} values.");
		Assert.Equal(values.Order(), values);
		Assert.Equal(values.Max(), values[^1]);
	}

	[Fact]
	public async Task SaveAsyncRewindsASeekableStreamAndCopiesANonSeekableOneFromItsPosition()
	{
		var seekableName = UniqueName("seekable", ".bin");
		var streamedName = UniqueName("streamed", ".bin");
		var seekableFolders = TrackTemporaryFolders(seekableName);
		var streamedFolders = TrackTemporaryFolders(streamedName);
		var seekable = new FilesScriptedStream(sample);
		seekable.Position = seekable.Length;
		var streamed = new FilesScriptedStream(sample, canSeek: false);

		var (seekableOperation, seekablePicker, _) = await StartSaveAsync(seekableName, seekable);
		Assert.Equal(sample, await File.ReadAllBytesAsync(Path.Combine(seekableFolders.FolderWithFile()!, seekableName)));
		await FilesApplePicker.SimulateUserCancelsAsync(seekablePicker);
		await seekableOperation.WaitAsync(operationTimeout);

		var (streamedOperation, streamedPicker, _) = await StartSaveAsync(streamedName, streamed);
		Assert.Equal(sample, await File.ReadAllBytesAsync(Path.Combine(streamedFolders.FolderWithFile()!, streamedName)));
		await FilesApplePicker.SimulateUserCancelsAsync(streamedPicker);
		await streamedOperation.WaitAsync(operationTimeout);

		Assert.Equal(1, seekable.SeekCalls);
		Assert.Equal(0, streamed.SeekCalls);
	}

	[Fact]
	public async Task SaveAsyncReturnsACanceledResultWhenTheUserCancelsThePicker()
	{
		var fileName = UniqueName("report", ".txt");
		var folders = TrackTemporaryFolders(fileName);

		var (operation, picker, pickerDelegate) = await StartSaveAsync(fileName, new MemoryStream([1]));
		var temporaryFolder = folders.FolderWithFile();
		await FilesApplePicker.SimulateUserCancelsAsync(picker);
		var result = await operation.WaitAsync(operationTimeout);

		Assert.True(result.IsCancelled);
		Assert.Null(result.FilePath);
		Assert.IsType<OperationCanceledException>(result.Exception, exactMatch: false);
		Assert.NotNull(temporaryFolder);
		Assert.False(Directory.Exists(temporaryFolder), "The temporary folder was not removed.");
		Assert.Equal((0, 0), FilesApplePicker.CountSubscribers(pickerDelegate));
	}

	[Fact]
	public async Task SaveAsyncReturnsACanceledResultWhenTheTokenIsCanceledWhileThePickerIsPresented()
	{
		var fileName = UniqueName("report", ".txt");
		var folders = TrackTemporaryFolders(fileName);
		using var cancellation = new CancellationTokenSource();

		var (operation, _, pickerDelegate) = await StartSaveAsync(fileName, new MemoryStream([1]), token: cancellation.Token);
		var temporaryFolder = folders.FolderWithFile();
		await cancellation.CancelAsync();
		var result = await operation.WaitAsync(operationTimeout);

		Assert.True(result.IsCancelled);
		Assert.Null(result.FilePath);
		Assert.NotNull(temporaryFolder);
		Assert.False(Directory.Exists(temporaryFolder), "The temporary folder was not removed.");
		Assert.Equal((0, 0), FilesApplePicker.CountSubscribers(pickerDelegate));
	}

	[Fact]
	public async Task SaveAsyncDismissesThePickerWhenTheTokenIsCanceled()
	{
		using var cancellation = new CancellationTokenSource();
		var (operation, _, _) = await StartSaveAsync(UniqueName("report", ".txt"), new MemoryStream([1]), token: cancellation.Token);

		await cancellation.CancelAsync();
		await operation.WaitAsync(operationTimeout);

		await AssertionExtensions.AssertEventuallyAsync(async () => !await FilesApplePicker.IsPickerPresentedAsync(), timeout: 3000, message: "The document picker is still on screen after the save was canceled.");
	}

	[Fact]
	public async Task SaveAsyncDismissesThePickerWhenTheTokenIsCanceledOnceThePickerIsShown()
	{
		using var cancellation = new CancellationTokenSource();
		var fileName = UniqueName("report", ".txt");
		TrackTemporaryFolders(fileName);
		var (operation, picker, _) = await StartSaveAsync(fileName, new MemoryStream([1]), token: cancellation.Token);
		await AssertionExtensions.AssertEventuallyAsync(() => InvokeOnMainThreadAsync(() => picker.PresentingViewController is not null && !picker.IsBeingPresented), timeout: 15000, message: "The document picker was not shown.");

		await cancellation.CancelAsync();
		Assert.True((await operation.WaitAsync(operationTimeout)).IsCancelled);

		await AssertionExtensions.AssertEventuallyAsync(async () => !await FilesApplePicker.IsPickerPresentedAsync(), timeout: 3000, message: "The document picker is still on screen after the save was canceled.");
	}

	// The token is canceled on the main thread right after the call. An empty stream is copied without waiting for anything, so the call has
	// already asked UIKit to present the picker when it returns (the copy reports its end before that); UIKit shows a document picker some
	// time after it was asked to, so the picker is not on screen yet, and can only be dismissed once it is.
	[Fact]
	public async Task SaveAsyncDismissesThePickerWhenTheTokenIsCanceledBeforeTheSystemShowsIt()
	{
		using var cancellation = new CancellationTokenSource();
		var fileName = UniqueName("empty", ".txt");
		TrackTemporaryFolders(fileName);
		var progress = new FilesRecordingProgress();

		var (operation, copiedBeforeReturning) = await InvokeOnMainThreadAsync(() =>
		{
			var save = FileSaver.Default.SaveAsync(fileName, new MemoryStream(), progress, cancellation.Token);
			var copied = progress.Values.Count > 0;
			cancellation.Cancel();
			return (save, copied);
		});

		Assert.True(copiedBeforeReturning, "The copy was not done when SaveAsync returned, so the token was not canceled after the picker was asked for.");
		Assert.True((await operation.WaitAsync(operationTimeout)).IsCancelled);
		await FilesApplePicker.WaitForPickerAsync();
		await AssertionExtensions.AssertEventuallyAsync(async () => !await FilesApplePicker.IsPickerPresentedAsync(), timeout: 3000, message: "The document picker is still on screen after the save was canceled.");
	}

	// Two saves at once: the picker of the second one is presented on top of the picker of the first. Dismissing the first picker would
	// also take the second one off the screen, and the second save would then wait for an answer that never comes.
	[Fact]
	public async Task SaveAsyncLeavesThePickerOfAnotherSaveOnScreenWhenTheTokenIsCanceled()
	{
		using var cancellation = new CancellationTokenSource();
		var firstName = UniqueName("first", ".txt");
		var secondName = UniqueName("second", ".txt");
		TrackTemporaryFolders(firstName);
		TrackTemporaryFolders(secondName);
		var (first, firstPicker, _) = await StartSaveAsync(firstName, new MemoryStream([1]), token: cancellation.Token);
		await FilesApplePicker.WaitUntilShownAsync(firstPicker);
		var secondToken = CanceledWhenTheTestEnds();
		var second = await StartOnMainThreadAsync(() => FileSaver.Default.SaveAsync(secondName, new MemoryStream([2]), secondToken));
		var secondPicker = await FilesApplePicker.WaitForPickerOnTopOfAsync(firstPicker);

		await cancellation.CancelAsync();
		Assert.True((await first.WaitAsync(operationTimeout)).IsCancelled);

		Assert.True(await InvokeOnMainThreadAsync(() => secondPicker.PresentingViewController is not null && !secondPicker.IsBeingDismissed), "The picker of the second save was taken off the screen when the first save was canceled.");
		Assert.False(second.IsCompleted);
		const string chosenPath = "/tmp/files-tests-second.txt";
		await FilesApplePicker.SimulateUserPicksAsync(secondPicker, NSUrl.CreateFileUrl(chosenPath, false));
		var result = await second.WaitAsync(operationTimeout);
		Assert.True(result.IsSuccessful, result.Exception?.ToString());
		Assert.Equal(chosenPath, result.FilePath);
	}

	// A save that the app starts as soon as a canceled one returns, on the main thread as app code does, presents its picker from the view
	// controller on top: that must no longer be the canceled save's picker, since UIKit drops a presentation from a view controller that is
	// being dismissed (the new save would then have no picker, and never complete).
	[Fact]
	public async Task SaveAsyncPresentsThePickerOfASaveStartedAsSoonAsACanceledSaveReturns()
	{
		using var cancellation = new CancellationTokenSource();
		var firstName = UniqueName("first", ".txt");
		var nextName = UniqueName("next", ".txt");
		TrackTemporaryFolders(firstName);
		TrackTemporaryFolders(nextName);
		var (first, firstPicker, _) = await StartSaveAsync(firstName, new MemoryStream([1]), token: cancellation.Token);
		await FilesApplePicker.WaitUntilShownAsync(firstPicker);
		var nextToken = CanceledWhenTheTestEnds();

		var (firstResult, pickerOnScreen, next) = await InvokeOnMainThreadAsync(async () =>
		{
			cancellation.Cancel();
			var result = await first;
			var onScreen = await FilesApplePicker.IsPickerPresentedAsync();
			return (result, onScreen, FileSaver.Default.SaveAsync(nextName, new MemoryStream([2]), nextToken));
		});

		Assert.True(firstResult.IsCancelled);
		Assert.False(pickerOnScreen, "The canceled save returned while its picker was still on screen.");
		var nextPicker = await FilesApplePicker.WaitForPickerAsync();
		await FilesApplePicker.WaitUntilShownAsync(nextPicker);
		const string chosenPath = "/tmp/files-tests-next.txt";
		await FilesApplePicker.SimulateUserPicksAsync(nextPicker, NSUrl.CreateFileUrl(chosenPath, false));
		var nextResult = await next.WaitAsync(operationTimeout);
		Assert.True(nextResult.IsSuccessful, nextResult.Exception?.ToString());
		Assert.Equal(chosenPath, nextResult.FilePath);
	}

	[Fact]
	public async Task SaveAsyncFailsWhenThePickerReturnsNoUrl()
	{
		var fileName = UniqueName("report", ".txt");
		var folders = TrackTemporaryFolders(fileName);

		var (operation, picker, _) = await StartSaveAsync(fileName, new MemoryStream([1]));
		var temporaryFolder = folders.FolderWithFile();
		await FilesApplePicker.SimulateUserPicksAsync(picker);
		var result = await operation.WaitAsync(operationTimeout);

		Assert.False(result.IsSuccessful);
		Assert.False(result.IsCancelled);
		Assert.Null(result.FilePath);
		var exception = Assert.IsType<FileSaveException>(result.Exception);
		Assert.Equal("No file was selected.", exception.Message);
		Assert.Throws<FileSaveException>(result.EnsureSuccess);
		Assert.False(Directory.Exists(temporaryFolder), "The temporary folder was not removed.");
	}

	[Fact]
	public async Task SaveAsyncFailsWhenThePickedUrlHasNoPath()
	{
		var (operation, picker, _) = await StartSaveAsync(UniqueName("report", ".txt"), new MemoryStream([1]));

		await FilesApplePicker.SimulateUserPicksAsync(picker, NSUrl.FromString("mailto:someone@example.com")!);
		var result = await operation.WaitAsync(operationTimeout);

		Assert.False(result.IsSuccessful);
		var exception = Assert.IsType<FileSaveException>(result.Exception);
		Assert.Equal("File path cannot be null.", exception.Message);
	}

	[Fact]
	public async Task SaveAsyncReturnsACanceledResultWhenTheTokenIsCanceledWhileTheLastBlockIsRead()
	{
		var fileName = UniqueName("report", ".txt");
		var folders = TrackTemporaryFolders(fileName);
		using var cancellation = new CancellationTokenSource();
		var stream = new FilesScriptedStream([1, 2, 3], canSeek: false) { CancelAtEndOfData = true, CancelSource = cancellation };

		var result = await InvokeOnMainThreadAsync(() => FileSaver.Default.SaveAsync(fileName, stream, cancellation.Token)).WaitAsync(operationTimeout);

		// The copy was complete when the token was canceled, so the picker is never presented and the temporary folder is cleaned up.
		Assert.True(result.IsCancelled);
		Assert.Equal(2, stream.ReadCalls);
		Assert.False(await FilesApplePicker.IsPickerPresentedAsync());
		Assert.Empty(folders.Created());
	}

	[Fact]
	public async Task SaveAsyncReturnsTheExceptionThatReadingTheStreamThrew()
	{
		var fileName = UniqueName("report", ".txt");
		TrackTemporaryFolders(fileName);
		var failure = new IOException("The source failed.");

		var result = await InvokeOnMainThreadAsync(() => FileSaver.Default.SaveAsync(fileName, new FilesScriptedStream([1, 2, 3], canSeek: false) { FailAtRead = 1, FailureOnRead = failure })).WaitAsync(operationTimeout);

		Assert.False(result.IsSuccessful);
		Assert.False(result.IsCancelled);
		Assert.Same(failure, result.Exception);
		Assert.False(await FilesApplePicker.IsPickerPresentedAsync());
	}

	[Fact]
	public async Task SaveAsyncReturnsACanceledResultWhenTheTokenIsCanceledWhileTheStreamIsRead()
	{
		var fileName = UniqueName("report", ".bin");
		TrackTemporaryFolders(fileName);
		using var cancellation = new CancellationTokenSource();

		var result = await InvokeOnMainThreadAsync(() => FileSaver.Default.SaveAsync(fileName, new FilesScriptedStream(sample) { CancelAtRead = 2, CancelSource = cancellation }, cancellation.Token)).WaitAsync(operationTimeout);

		Assert.True(result.IsCancelled);
		Assert.False(await FilesApplePicker.IsPickerPresentedAsync());
	}

	[Fact]
	public async Task SaveAsyncReturnsACanceledResultWithoutPresentingThePickerWhenTheTokenIsCanceledBetweenTwoBlocks()
	{
		var fileName = UniqueName("report", ".bin");
		TrackTemporaryFolders(fileName);
		using var cancellation = new CancellationTokenSource();
		// The token is canceled inside the second read, which still returns its block: only the toolkit's own use of the token stops the
		// copy. WriteStream hands the token to FileStream.WriteAsync, which checks it before it writes, so no third read is made.
		var stream = new FilesScriptedStream(sample) { CancelDuringRead = 2, CancelSource = cancellation };

		var result = await InvokeOnMainThreadAsync(() => FileSaver.Default.SaveAsync(fileName, stream, cancellation.Token)).WaitAsync(operationTimeout);

		Assert.True(result.IsCancelled, result.Exception?.ToString());
		Assert.Null(result.FilePath);
		Assert.Equal(2, stream.ReadCalls);
		Assert.Equal(2, stream.ReadsWithData);
		Assert.False(await FilesApplePicker.IsPickerPresentedAsync());
	}

	[Fact]
	public async Task SaveAsyncReturnsACanceledResultWithoutReadingAgainWhenTheTokenIsCanceledAfterABlockWasWritten()
	{
		var fileName = UniqueName("report", ".bin");
		TrackTemporaryFolders(fileName);
		using var cancellation = new CancellationTokenSource();
		var stream = new FilesScriptedStream(sample);
		// The report of the first block cancels the token, between the write of that block and the next read: the token that WriteStream
		// hands to that read stops the copy (the stream honors it) before a second block is read.
		var progress = new FilesRecordingProgress { OnReport = _ => cancellation.Cancel() };

		var result = await InvokeOnMainThreadAsync(() => FileSaver.Default.SaveAsync(fileName, stream, progress, cancellation.Token)).WaitAsync(operationTimeout);

		Assert.True(result.IsCancelled, result.Exception?.ToString());
		Assert.Null(result.FilePath);
		Assert.Equal(2, stream.ReadCalls);
		Assert.Equal(1, stream.ReadsWithData);
		Assert.False(await FilesApplePicker.IsPickerPresentedAsync());
	}

	[Fact]
	public async Task SaveAsyncRemovesTheTemporaryFolderWhenCopyingTheStreamFails()
	{
		var fileName = UniqueName("report", ".txt");
		var folders = TrackTemporaryFolders(fileName);

		var result = await InvokeOnMainThreadAsync(() => FileSaver.Default.SaveAsync(fileName, new FilesScriptedStream([1, 2, 3], canSeek: false) { FailAtRead = 2, FailureOnRead = new IOException("The source failed.") })).WaitAsync(operationTimeout);

		Assert.False(result.IsSuccessful);
		Assert.Empty(folders.Created());
	}

	[Fact]
	public async Task SaveAsyncRemovesTheTemporaryFolderWhenCopyingTheStreamIsCanceled()
	{
		var fileName = UniqueName("report", ".bin");
		var folders = TrackTemporaryFolders(fileName);
		using var cancellation = new CancellationTokenSource();

		var result = await InvokeOnMainThreadAsync(() => FileSaver.Default.SaveAsync(fileName, new FilesScriptedStream(sample) { CancelAtRead = 2, CancelSource = cancellation }, cancellation.Token)).WaitAsync(operationTimeout);

		Assert.True(result.IsCancelled);
		Assert.Empty(folders.Created());
	}

	[Theory]
	[InlineData("")]
	[InlineData("sub/folder.txt")]
	public async Task SaveAsyncFailsWithoutPresentingThePickerWhenTheFileNameCannotBeWritten(string fileName)
	{
		TrackTemporaryFolders("folder.txt");

		var result = await InvokeOnMainThreadAsync(() => FileSaver.Default.SaveAsync(fileName, new MemoryStream([1, 2, 3]))).WaitAsync(operationTimeout);

		Assert.False(result.IsSuccessful);
		Assert.False(result.IsCancelled);
		Assert.NotNull(result.Exception);
		Assert.False(await FilesApplePicker.IsPickerPresentedAsync());
	}

	[Fact]
	public async Task SaveAsyncFailsWithTheErrorOfTheFileSystemWhenTheTemporaryFolderCannotBeCreated()
	{
		Skip.If(OperatingSystem.IsMacCatalyst(), "The temporary folder of a Mac Catalyst app is the user's shared temporary folder; making it read-only would break every process of the machine.");
		var fileManager = NSFileManager.DefaultManager;
		var temporaryRoot = TemporaryRoot;
		var original = fileManager.GetAttributes(temporaryRoot, out _)?.PosixPermissions ?? Convert.ToInt16("755", 8);
		AddCleanup(() => fileManager.SetAttributes(new NSFileAttributes { PosixPermissions = original }, temporaryRoot, out _));
		Assert.True(fileManager.SetAttributes(new NSFileAttributes { PosixPermissions = Convert.ToInt16("555", 8) }, temporaryRoot, out var error), error?.LocalizedDescription);

		// What the file system answers for a new folder there: the toolkit creates a folder named by a GUID, so the name in the message
		// is matched as a GUID.
		var probeName = $"files-probe-{Guid.NewGuid():N}";
		Assert.False(fileManager.CreateDirectory(fileManager.GetTemporaryDirectory().Append(probeName, true), true, null, out var systemError), "The temporary folder accepts new folders although it is read-only.");
		Assert.NotNull(systemError);
		var expectedMessage = "^" + Regex.Escape(systemError.LocalizedDescription).Replace(Regex.Escape(probeName), "[0-9a-fA-F-]{36}", StringComparison.Ordinal) + "$";

		var result = await InvokeOnMainThreadAsync(() => FileSaver.Default.SaveAsync(UniqueName("report", ".txt"), new MemoryStream([1]))).WaitAsync(operationTimeout);

		Assert.False(result.IsSuccessful);
		var exception = Assert.IsType<FileSaveException>(result.Exception);
		Assert.NotEqual("Unable to create temp directory.", exception.Message);
		Assert.Matches(expectedMessage, exception.Message);
		Assert.False(await FilesApplePicker.IsPickerPresentedAsync());
	}

	[Fact]
	public async Task SaveAsyncFailsWithoutPresentingAnythingWhenNoWindowHasAViewController()
	{
		var fileName = UniqueName("report", ".txt");
		var folders = TrackTemporaryFolders(fileName);
		await RemoveRootViewControllersAsync();

		var result = await InvokeOnMainThreadAsync(() => FileSaver.Default.SaveAsync(fileName, new MemoryStream([1]))).WaitAsync(operationTimeout);

		Assert.False(result.IsSuccessful);
		Assert.False(result.IsCancelled);
		Assert.NotNull(result.Exception);
		Assert.Empty(folders.Created());
	}

	[Fact]
	public async Task SaveAsyncExplainsWithAFileSaveExceptionWhenNoWindowHasAViewController()
	{
		await RemoveRootViewControllersAsync();

		var result = await InvokeOnMainThreadAsync(() => FileSaver.Default.SaveAsync(UniqueName("report", ".txt"), new MemoryStream([1]))).WaitAsync(operationTimeout);

		var exception = Assert.IsType<FileSaveException>(result.Exception);
		Assert.Contains("No active view controller found", exception.Message, StringComparison.Ordinal);
	}

	[Fact]
	public async Task SaveAsyncFromABackgroundThreadFailsBecauseUIKitMustBeUsedOnTheMainThread()
	{
		Skip.IfNot(UIApplication.CheckForIllegalCrossThreadCalls, "UIKit's cross-thread check is off in this build (Release), so the call would reach UIKit from a background thread.");

		var result = await Task.Run(() => FileSaver.Default.SaveAsync(UniqueName("report", ".txt"), new MemoryStream([1]))).WaitAsync(operationTimeout);

		Assert.False(result.IsSuccessful);
		Assert.IsType<UIKitThreadAccessException>(result.Exception);
		Assert.False(await FilesApplePicker.IsPickerPresentedAsync());
	}

	[Fact]
	public async Task InternalSaveAsyncStopsBeforeTouchingUIKitWhenTheTokenIsAlreadyCanceled()
	{
		// SaveAsync checks the token before it calls the platform code, so the check at the top of the platform method can only be reached
		// by calling that method itself.
		using var cancellation = new CancellationTokenSource();
		await cancellation.CancelAsync();
		var saver = new FileSaverImplementation();
		var withInitialPath = typeof(FileSaverImplementation).GetMethod("InternalSaveAsync", BindingFlags.NonPublic | BindingFlags.Instance, [typeof(string), typeof(string), typeof(Stream), typeof(IProgress<double>), typeof(CancellationToken)])!;
		var withoutInitialPath = typeof(FileSaverImplementation).GetMethod("InternalSaveAsync", BindingFlags.NonPublic | BindingFlags.Instance, [typeof(string), typeof(Stream), typeof(IProgress<double>), typeof(CancellationToken)])!;

		await Assert.ThrowsAsync<OperationCanceledException>(() => (Task<string>)withInitialPath.Invoke(saver, ["/", "report.txt", new MemoryStream(), null, cancellation.Token])!);
		await Assert.ThrowsAsync<OperationCanceledException>(() => (Task<string>)withoutInitialPath.Invoke(saver, ["report.txt", new MemoryStream(), null, cancellation.Token])!);
		Assert.False(await FilesApplePicker.IsPickerPresentedAsync());
	}

	[Fact]
	public async Task ThePickerAndItsDelegateAreNotKeptAliveOnceTheSaveIsDone()
	{
		var fileName = UniqueName("report", ".txt");
		TrackTemporaryFolders(fileName);
		var (operation, picker, pickerDelegate) = await StartSaveAsync(fileName, new MemoryStream([1]));
		var references = new[] { new WeakReference(picker), new WeakReference(pickerDelegate) };
		await FilesApplePicker.SimulateUserCancelsAsync(picker);
		await operation.WaitAsync(operationTimeout);
		await InvokeOnMainThreadAsync(FilesApplePicker.DismissAnyPickerAsync);
		picker = null!;
		pickerDelegate = null!;
		operation = null!;

		await AssertionExtensions.WaitForGC(references);
	}

	// A save that is not asked about must never leave a picker on screen.
	Task PrepareHostAsync() => Task.CompletedTask;

	static async Task AssertNoPickerWasStartedAsync() =>
		Assert.False(await FilesApplePicker.IsPickerPresentedAsync(), "A document picker is on screen.");

	static string TemporaryRoot => NSFileManager.DefaultManager.GetTemporaryDirectory().Path!;

	static string UniqueName(string stem, string extension) => $"{stem}-{Guid.NewGuid():N}{extension}";

	TemporaryFolders TrackTemporaryFolders(string fileName)
	{
		var folders = new TemporaryFolders(fileName);
		AddCleanup(folders.DeleteCreated);
		return folders;
	}

	// Starts FileSaver.Default.SaveAsync on the UI thread (the overload follows from the arguments) and waits for the picker it presents.
	async Task<(Task<FileSaverResult> Operation, UIDocumentPickerViewController Picker, object PickerDelegate)> StartSaveAsync(string fileName, Stream stream, IProgress<double>? progress = null, string? initialPath = null, CancellationToken token = default)
	{
		var operation = await StartOnMainThreadAsync(() => (initialPath, progress) switch
		{
			(null, null) => FileSaver.Default.SaveAsync(fileName, stream, token),
			(null, not null) => FileSaver.Default.SaveAsync(fileName, stream, progress, token),
			(not null, null) => FileSaver.Default.SaveAsync(initialPath, fileName, stream, token),
			(not null, not null) => FileSaver.Default.SaveAsync(initialPath, fileName, stream, progress, token),
		});

		var picker = await FilesApplePicker.WaitForPickerAsync();
		return (operation, picker, await FilesApplePicker.GetDelegateAsync(picker));
	}

	// Takes the root view controller away from every window, so that MAUI has no view controller to present from. The cleanup puts them back.
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

	/// <summary>
	/// The folders that FileSaver creates in the temporary directory: a folder named by a GUID that holds the file. On Mac Catalyst the
	/// temporary directory is shared with the rest of the machine, so only folders that did not exist before, and hold nothing but this
	/// test's file, count.
	/// </summary>
	sealed class TemporaryFolders
	{
		readonly string fileName;
		readonly HashSet<string> existing;

		public TemporaryFolders(string fileName)
		{
			this.fileName = fileName;
			existing = Snapshot();
		}

		/// <summary>The folder that holds this test's file, or null.</summary>
		public string? FolderWithFile() => Snapshot().FirstOrDefault(folder => File.Exists(Path.Combine(folder, fileName)));

		/// <summary>New folders that are empty or hold only this test's file.</summary>
		public IReadOnlyList<string> Created() => [.. Snapshot().Where(folder => !existing.Contains(folder) && HoldsOnlyTheFile(folder))];

		public void DeleteCreated()
		{
			foreach (var folder in Created())
			{
				try
				{
					Directory.Delete(folder, recursive: true);
				}
				catch (IOException)
				{
				}
			}
		}

		static HashSet<string> Snapshot() => [.. Directory.EnumerateDirectories(TemporaryRoot).Where(folder => Guid.TryParse(Path.GetFileName(folder), out _))];

		bool HoldsOnlyTheFile(string folder)
		{
			try
			{
				return Directory.EnumerateFileSystemEntries(folder).All(entry => Path.GetFileName(entry) == fileName);
			}
			catch (UnauthorizedAccessException)
			{
				return false;
			}
		}
	}
}