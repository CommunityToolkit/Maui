using Android.App;
using Android.Content;
using Android.Provider;
using CommunityToolkit.Maui.Storage;
using AndroidEnvironment = Android.OS.Environment;
using AndroidUri = Android.Net.Uri;

namespace CommunityToolkit.Maui.DeviceTests;

public partial class FileSaverTests
{
	const string externalStorageProviderAuthority = "com.android.externalstorage.documents";

	static string ExternalStorage => AndroidEnvironment.ExternalStorageDirectory!.Path;

	[Theory]
	[InlineData("report.txt", "text/plain")]
	[InlineData("photo.png", "image/png")]
	[InlineData("archive.zip", "application/zip")]
	[InlineData("Report.TXT", "text/plain")]
	[InlineData("noextension", "*/*")]
	[InlineData("data.unknownext123", "*/*")]
	public async Task SaveAsyncAsksForANewOpenableDocumentWithTheFileNameAndItsMimeType(string fileName, string expectedMimeType)
	{
		var session = await StartSessionAsync();

		var (operation, launch) = await StartSaveAsync(session, fileName, new MemoryStream([1]));

		Assert.Equal(Intent.ActionCreateDocument, launch.Intent.Action);
		Assert.True(launch.Intent.HasCategory(Intent.CategoryOpenable));
		Assert.Equal(expectedMimeType, launch.Intent.Type);
		Assert.Equal(fileName, launch.Intent.GetStringExtra(Intent.ExtraTitle));
		await CancelPickerAsync(session, launch, operation);
	}

	[Fact]
	public async Task SaveAsyncUsesTheMimeTypeOfTheExtensionWhateverTheFileNameContains()
	{
		var session = await StartSessionAsync();

		foreach (var (fileName, expectedMimeType) in new[] { ("my report.txt", "text/plain"), ("résumé.pdf", "application/pdf") })
		{
			var (operation, launch) = await StartSaveAsync(session, fileName, new MemoryStream([1]));

			Assert.Equal(expectedMimeType, launch.Intent.Type);
			await CancelPickerAsync(session, launch, operation);
			FilesFakePicker.Reset();
		}
	}

	[Fact]
	public async Task SaveAsyncStartsThePickerAtTheRootOfTheExternalStorageWhenThereIsNoInitialPath()
	{
		var session = await StartSessionAsync();

		var (operation, launch) = await StartSaveAsync(session, "report.txt", new MemoryStream([1]));

		var initialUri = InitialUri(launch.Intent);
		Assert.Equal(externalStorageProviderAuthority, initialUri.Authority);
		Assert.Equal("primary:", DocumentsContract.GetDocumentId(initialUri));
		await CancelPickerAsync(session, launch, operation);
	}

	// The root of the file system ("/"), which no storage volume is, starts the picker at the root of the primary storage.
	[Theory]
	[InlineData("{external}", "primary:")]
	[InlineData("{external}/Documents", "primary:Documents")]
	[InlineData("{external}/Documents/Reports", "primary:Documents/Reports")]
	[InlineData("Documents/Reports", "primary:Documents/Reports")]
	[InlineData("/", "primary:")]
	public async Task SaveAsyncStartsThePickerInTheFolderOfTheInitialPath(string initialPath, string expectedDocumentId)
	{
		var session = await StartSessionAsync();

		var (operation, launch) = await StartSaveAsync(session, initialPath.Replace("{external}", ExternalStorage, StringComparison.Ordinal), "report.txt", new MemoryStream([1]));

		var initialUri = InitialUri(launch.Intent);
		Assert.Equal(externalStorageProviderAuthority, initialUri.Authority);
		// The folder may or may not carry a leading slash after the volume; what matters is which folder it names.
		Assert.Equal(expectedDocumentId, DocumentsContract.GetDocumentId(initialUri)!.Replace("primary:/", "primary:", StringComparison.Ordinal));
		await CancelPickerAsync(session, launch, operation);
	}

	[Fact]
	public async Task SaveAsyncStartsThePickerInAFolderWhoseNameHasASpace()
	{
		var session = await StartSessionAsync();

		var (operation, launch) = await StartSaveAsync(session, $"{ExternalStorage}/Documents/My Folder", "report.txt", new MemoryStream([1]));

		Assert.Equal("primary:Documents/My Folder", DocumentsContract.GetDocumentId(InitialUri(launch.Intent))!.Replace("primary:/", "primary:", StringComparison.Ordinal));
		await CancelPickerAsync(session, launch, operation);
	}

	[Theory]
	[InlineData("/storage/1B0B-0B1C/DCIM", "1B0B-0B1C:DCIM")]
	[InlineData("/sdcard/Download", "primary:Download")]
	[InlineData("{appdata}", "")]
	[InlineData("/sdcard2/Download", "")]
	public async Task SaveAsyncStartsThePickerOnTheVolumeOfTheInitialPath(string initialPath, string expectedDocumentId)
	{
		var session = await StartSessionAsync();

		var (operation, launch) = await StartSaveAsync(session, initialPath.Replace("{appdata}", FileSystem.AppDataDirectory, StringComparison.Ordinal), "report.txt", new MemoryStream([1]));

		if (expectedDocumentId.Length is 0)
		{
			// The app's private storage, and a path that only starts like a storage folder (/sdcard2 is not in /sdcard), are on no volume
			// that the document picker shows, so no document id can name them and the picker is left to choose where it starts
			// (DocumentsContract.EXTRA_INITIAL_URI is absent).
			Assert.Null(launch.Intent.GetParcelableExtra(DocumentsContract.ExtraInitialUri));
		}
		else
		{
			// As for the other initial paths, a slash after the volume names the same folder.
			Assert.Equal(expectedDocumentId, DocumentsContract.GetDocumentId(InitialUri(launch.Intent))!.Replace(":/", ":", StringComparison.Ordinal));
		}

		await CancelPickerAsync(session, launch, operation);
	}

	[Fact]
	public async Task SaveAsyncDoesNotTraceTheInitialPathLimitOnAndroid26AndLater()
	{
		Skip.IfNot(OperatingSystem.IsAndroidVersionAtLeast(26), "Below Android 26 the limit is traced instead (SaveAsyncTracesThatAnInitialPathNeedsAndroid26BeforeThatVersion).");
		var session = await StartSessionAsync();
		using var trace = new FilesTraceCapture();

		var (operation, launch) = await StartSaveAsync(session, $"{ExternalStorage}/Documents", "report.txt", new MemoryStream([1]));

		Assert.False(trace.Contains("only supported on Android 26"), string.Join(Environment.NewLine, trace.Lines));
		await CancelPickerAsync(session, launch, operation);
	}

	[Fact]
	public async Task SaveAsyncTracesThatAnInitialPathNeedsAndroid26BeforeThatVersion()
	{
		Skip.If(OperatingSystem.IsAndroidVersionAtLeast(26), "The branch only runs below Android 26 (the emulators of the pool are API 28 and 36).");
		var session = await StartSessionAsync();
		using var trace = new FilesTraceCapture();

		var (withPath, launchWithPath) = await StartSaveAsync(session, $"{ExternalStorage}/Documents", "report.txt", new MemoryStream([1]));
		Assert.True(trace.Contains("Specifying an initial path is only supported on Android 26 and later."), string.Join(Environment.NewLine, trace.Lines));
		await CancelPickerAsync(session, launchWithPath, withPath);

		var traced = trace.Lines.Count;
		var (withoutPath, launchWithoutPath) = await StartSaveAsync(session, string.Empty, "report.txt", new MemoryStream([1]));
		Assert.Equal(traced, trace.Lines.Count);
		await CancelPickerAsync(session, launchWithoutPath, withoutPath);
	}

	[Fact]
	public async Task SaveAsyncWritesTheStreamToTheDocumentThatThePickerReturned()
	{
		var session = await StartSessionAsync();
		var content = Enumerable.Range(0, 10_000).Select(i => (byte)(i % 251)).ToArray();
		var progress = new FilesRecordingProgress();
		const string documentId = "primary:Documents/Reports/saved.bin";

		var (operation, launch) = await StartSaveAsync(session, "saved.bin", new FilesScriptedStream(content), progress);
		await session.DeliverAsync(launch, Result.Ok, Answer(documentId));
		var result = await operation.WaitAsync(operationTimeout);

		Assert.True(result.IsSuccessful, result.Exception?.ToString());
		Assert.Equal($"{ExternalStorage}/Documents/Reports/saved.bin", result.FilePath);
		Assert.Equal(content, await File.ReadAllBytesAsync(FilesTestContentProvider.GetBackingFile(documentId)));
		Assert.Equal(["wt"], FilesTestContentProvider.OpenModes);
		var values = progress.Values;
		Assert.True(values.Count >= 2, $"Expected the progress of each chunk and the final report, got {values.Count} values.");
		Assert.Equal(values.Order(), values);
		Assert.Equal(values.Max(), values[^1]);
	}

	[Fact]
	public async Task SaveAsyncReplacesTheContentThatTheDocumentHad()
	{
		var session = await StartSessionAsync();
		const string documentId = "primary:Documents/existing.txt";
		WriteBackingFile(documentId, Enumerable.Repeat((byte)0xFF, 20_000).ToArray());
		var content = Enumerable.Range(0, 100).Select(i => (byte)i).ToArray();

		var (operation, launch) = await StartSaveAsync(session, "existing.txt", new FilesScriptedStream(content));
		await session.DeliverAsync(launch, Result.Ok, Answer(documentId));
		var result = await operation.WaitAsync(operationTimeout);

		Assert.True(result.IsSuccessful, result.Exception?.ToString());
		Assert.Equal(content, await File.ReadAllBytesAsync(FilesTestContentProvider.GetBackingFile(documentId)));
	}

	[Fact]
	public async Task SaveAsyncTruncatesTheDocumentItselfWhenTheProviderIgnoresTheTruncateMode()
	{
		var session = await StartSessionAsync();
		const string documentId = "primary:Documents/existing.txt";
		WriteBackingFile(documentId, Enumerable.Repeat((byte)0xFF, 20_000).ToArray());
		FilesTestContentProvider.IgnoreTruncateFlag = true;
		var content = Enumerable.Range(0, 100).Select(i => (byte)i).ToArray();

		var (operation, launch) = await StartSaveAsync(session, "existing.txt", new FilesScriptedStream(content));
		await session.DeliverAsync(launch, Result.Ok, Answer(documentId));
		var result = await operation.WaitAsync(operationTimeout);

		Assert.True(result.IsSuccessful, result.Exception?.ToString());
		Assert.Equal(content, await File.ReadAllBytesAsync(FilesTestContentProvider.GetBackingFile(documentId)));
	}

	[Fact]
	public async Task SaveAsyncSavesAnEmptyStreamAsAnEmptyDocument()
	{
		var session = await StartSessionAsync();
		const string documentId = "primary:Documents/empty.txt";
		WriteBackingFile(documentId, Enumerable.Repeat((byte)0xFF, 50).ToArray());
		FilesTestContentProvider.IgnoreTruncateFlag = true;
		var progress = new FilesRecordingProgress();

		var (operation, launch) = await StartSaveAsync(session, "empty.txt", new FilesScriptedStream([]), progress);
		await session.DeliverAsync(launch, Result.Ok, Answer(documentId));
		var result = await operation.WaitAsync(operationTimeout);

		Assert.True(result.IsSuccessful, result.Exception?.ToString());
		Assert.Empty(await File.ReadAllBytesAsync(FilesTestContentProvider.GetBackingFile(documentId)));
		Assert.Single(progress.Values);
	}

	[Fact]
	public async Task SaveAsyncRewindsASeekableStreamBeforeWritingIt()
	{
		var session = await StartSessionAsync();
		const string documentId = "primary:Documents/rewound.bin";
		var content = Enumerable.Range(0, 3000).Select(i => (byte)(i % 199)).ToArray();
		var stream = new FilesScriptedStream(content);
		stream.Position = stream.Length;

		var (operation, launch) = await StartSaveAsync(session, "rewound.bin", stream);
		await session.DeliverAsync(launch, Result.Ok, Answer(documentId));
		var result = await operation.WaitAsync(operationTimeout);

		Assert.True(result.IsSuccessful, result.Exception?.ToString());
		Assert.Equal(content, await File.ReadAllBytesAsync(FilesTestContentProvider.GetBackingFile(documentId)));
		Assert.Equal(1, stream.SeekCalls);
	}

	[Fact]
	public async Task SaveAsyncWritesANonSeekableStreamFromTheStart()
	{
		var session = await StartSessionAsync();
		const string documentId = "primary:Documents/streamed.bin";
		var content = Enumerable.Range(0, 5000).Select(i => (byte)(i % 211)).ToArray();
		var stream = new FilesScriptedStream(content, canSeek: false);

		var (operation, launch) = await StartSaveAsync(session, "streamed.bin", stream);
		await session.DeliverAsync(launch, Result.Ok, Answer(documentId));
		var result = await operation.WaitAsync(operationTimeout);

		Assert.True(result.IsSuccessful, result.Exception?.ToString());
		Assert.Equal(content, await File.ReadAllBytesAsync(FilesTestContentProvider.GetBackingFile(documentId)));
		Assert.Equal(0, stream.SeekCalls);
	}

	[Fact]
	public async Task SaveAsyncReturnsACanceledResultWhenThePickerIsCanceled()
	{
		var session = await StartSessionAsync();
		var stream = new FilesScriptedStream([1, 2, 3]);

		var (operation, launch) = await StartSaveAsync(session, "report.txt", stream);
		await session.DeliverAsync(launch, Result.Canceled);
		var result = await operation.WaitAsync(operationTimeout);

		Assert.True(result.IsCancelled);
		Assert.Null(result.FilePath);
		Assert.IsType<OperationCanceledException>(result.Exception, exactMatch: false);
		Assert.Equal(0, stream.ReadCalls);
		Assert.Empty(FilesTestContentProvider.OpenModes);
	}

	[Fact]
	public async Task SaveAsyncReturnsACanceledResultWhenTheTokenIsCanceledWhileThePickerIsOpenAndIgnoresTheLateAnswer()
	{
		var session = await StartSessionAsync();
		using var cancellation = new CancellationTokenSource();
		const string documentId = "primary:Documents/late.txt";
		var stream = new FilesScriptedStream([1, 2, 3]);

		var (operation, launch) = await StartSaveAsync(session, "late.txt", stream, token: cancellation.Token);
		await cancellation.CancelAsync();
		var result = await operation.WaitAsync(operationTimeout);
		await session.DeliverAsync(launch, Result.Ok, Answer(documentId));
		await AssertionExtensions.AssertEventually(() => FilesActivityResultManagerState.PendingCount is 0, timeout: 5000, message: "The late answer was not drained.");

		Assert.True(result.IsCancelled);
		Assert.Equal(0, stream.ReadCalls);
		Assert.Empty(FilesTestContentProvider.OpenModes);
		Assert.False(File.Exists(FilesTestContentProvider.GetBackingFile(documentId)));
	}

	[Fact]
	public async Task SaveAsyncReturnsACanceledResultWhenTheActivityFinishesWhileThePickerIsOpen()
	{
		var session = await StartSessionAsync();

		var (operation, _) = await StartSaveAsync(session, "report.txt", new MemoryStream([1]));
		await session.FinishAsync(session.Activity);
		var result = await operation.WaitAsync(operationTimeout);

		Assert.True(result.IsCancelled);
		Assert.Equal(0, FilesActivityResultManagerState.PendingCount);
	}

	[Fact]
	public async Task SaveAsyncFailsWhenThePickerReturnsNoDocument()
	{
		var session = await StartSessionAsync();

		var (operation, launch) = await StartSaveAsync(session, "report.txt", new MemoryStream([1]));
		await session.DeliverAsync(launch, Result.Ok);
		var result = await operation.WaitAsync(operationTimeout);

		Assert.False(result.IsSuccessful);
		Assert.False(result.IsCancelled);
		Assert.Null(result.FilePath);
		var exception = Assert.IsType<FileSaveException>(result.Exception);
		Assert.Equal("Path is not selected.", exception.Message);
		Assert.Throws<FileSaveException>(result.EnsureSuccess);
	}

	[Theory]
	[InlineData("file:///storage/emulated/0/Documents/report.txt")]
	[InlineData("http://example.com/report.txt")]
	[InlineData("relative/report.txt")]
	public async Task SaveAsyncFailsWhenTheDocumentIsNotAContentUri(string uri)
	{
		var session = await StartSessionAsync();

		var (operation, launch) = await StartSaveAsync(session, "report.txt", new MemoryStream([1]));
		await session.DeliverAsync(launch, Result.Ok, new Intent().SetData(AndroidUri.Parse(uri)));
		var result = await operation.WaitAsync(operationTimeout);

		var exception = Assert.IsType<FileSaveException>(result.Exception);
		Assert.StartsWith("Unable to resolve absolute path or retrieve contents of URI", exception.Message, StringComparison.Ordinal);
		Assert.Contains(uri, exception.Message, StringComparison.Ordinal);
		Assert.Empty(FilesTestContentProvider.OpenModes);
	}

	[Fact]
	public async Task SaveAsyncAcceptsTheContentSchemeInAnyCaseAndLeavesTheRestToTheContentResolver()
	{
		var session = await StartSessionAsync();
		var uri = FilesTestContentProvider.BuildDocumentUri("primary:Documents/upper.txt").ToString()!.Replace("content://", "CONTENT://", StringComparison.Ordinal);

		var (operation, launch) = await StartSaveAsync(session, "upper.txt", new MemoryStream([7, 8, 9]));
		await session.DeliverAsync(launch, Result.Ok, new Intent().SetData(AndroidUri.Parse(uri)));
		var result = await operation.WaitAsync(operationTimeout);

		// FileSaver's own check ignores the case of the scheme, so this is not the FileSaveException of a URI that is not a content URI. The
		// content resolver only serves the lowercase scheme, and that is where the save ends (a picker always returns the lowercase one).
		Assert.False(result.IsSuccessful);
		Assert.IsType<Java.IO.FileNotFoundException>(result.Exception);
		Assert.Empty(FilesTestContentProvider.OpenModes);
	}

	[Fact]
	public async Task SaveAsyncFailsWhenNoProviderServesTheDocument()
	{
		var session = await StartSessionAsync();

		var (operation, launch) = await StartSaveAsync(session, "report.txt", new MemoryStream([1]));
		await session.DeliverAsync(launch, Result.Ok, new Intent().SetData(AndroidUri.Parse("content://com.communitytoolkit.maui.devicetests.nobody/document/primary%3Areport.txt")));
		var result = await operation.WaitAsync(operationTimeout);

		Assert.False(result.IsSuccessful);
		Assert.IsType<Java.IO.FileNotFoundException>(result.Exception);
	}

	[Fact]
	public async Task SaveAsyncFailsWhenTheProviderHasNoFileForTheDocument()
	{
		var session = await StartSessionAsync();

		var (operation, launch) = await StartSaveAsync(session, "report.txt", new MemoryStream([1]));
		await session.DeliverAsync(launch, Result.Ok, Answer($"{FilesTestContentProvider.NullDescriptorPrefix}primary:report.txt"));
		var result = await operation.WaitAsync(operationTimeout);

		Assert.False(result.IsSuccessful);
		Assert.False(result.IsCancelled);
		Assert.NotNull(result.Exception);
		Assert.Equal(["wt"], FilesTestContentProvider.OpenModes);
	}

	[Fact]
	public async Task SaveAsyncExplainsWithAFileSaveExceptionWhenTheProviderHasNoFileForTheDocument()
	{
		var session = await StartSessionAsync();

		var (operation, launch) = await StartSaveAsync(session, "report.txt", new MemoryStream([1]));
		await session.DeliverAsync(launch, Result.Ok, Answer($"{FilesTestContentProvider.NullDescriptorPrefix}primary:report.txt"));
		var result = await operation.WaitAsync(operationTimeout);

		Assert.IsType<FileSaveException>(result.Exception);
	}

	// The path is resolved after the document was written, so a provider whose document ids have no volume prefix (a cloud provider, for
	// example) still gets the whole file, and the app gets the document's content URI, which the ContentResolver opens, instead of a failure.
	[Fact]
	public async Task SaveAsyncReportsTheContentUriOfASavedDocumentWhosePathCannotBeResolved()
	{
		var session = await StartSessionAsync();
		var progress = new FilesRecordingProgress();
		var documentUri = FilesTestContentProvider.BuildDocumentUri("plain-document-id");

		var (operation, launch) = await StartSaveAsync(session, "report.txt", new MemoryStream([1]), progress);
		await session.DeliverAsync(launch, Result.Ok, new Intent().SetData(documentUri));
		var result = await operation.WaitAsync(operationTimeout);

		Assert.True(result.IsSuccessful, result.Exception?.ToString());
		Assert.Equal(documentUri.ToString(), result.FilePath);
		Assert.Equal([1], await File.ReadAllBytesAsync(FilesTestContentProvider.GetBackingFile("plain-document-id")));
		Assert.Equal([100d], progress.Values);
	}

	[Fact]
	public async Task SaveAsyncReturnsTheExceptionOfTheWriteWhenTheDocumentCannotBeWritten()
	{
		var session = await StartSessionAsync();
		var documentId = $"{FilesTestContentProvider.ReadOnlyDescriptorPrefix}primary:Documents/locked.txt";
		WriteBackingFile(documentId, [9, 9, 9]);
		var stream = new FilesScriptedStream(Enumerable.Range(0, 10_000).Select(i => (byte)(i % 251)).ToArray());

		var (operation, launch) = await StartSaveAsync(session, "locked.txt", stream);
		await session.DeliverAsync(launch, Result.Ok, Answer(documentId));
		var result = await operation.WaitAsync(operationTimeout);

		Assert.False(result.IsSuccessful);
		Assert.False(result.IsCancelled);
		Assert.Null(result.FilePath);
		var exception = Assert.IsType<Java.IO.IOException>(result.Exception, exactMatch: false);
		Assert.Contains("EBADF", exception.Message, StringComparison.Ordinal);
		Assert.Equal(1, stream.ReadsWithData);
		Assert.Equal(["wt"], FilesTestContentProvider.OpenModes);
		Assert.Equal([9, 9, 9], await File.ReadAllBytesAsync(FilesTestContentProvider.GetBackingFile(documentId)));
	}

	[Fact]
	public async Task SaveAsyncReturnsTheExceptionThatReadingTheStreamThrew()
	{
		var session = await StartSessionAsync();
		var failure = new IOException("The source failed.");

		var (operation, launch) = await StartSaveAsync(session, "report.txt", new FilesScriptedStream([1, 2, 3], canSeek: false) { FailAtRead = 1, FailureOnRead = failure });
		await session.DeliverAsync(launch, Result.Ok, Answer("primary:Documents/failed.txt"));
		var result = await operation.WaitAsync(operationTimeout);

		Assert.False(result.IsSuccessful);
		Assert.False(result.IsCancelled);
		Assert.Same(failure, result.Exception);
	}

	[Fact]
	public async Task SaveAsyncReturnsACanceledResultWhenTheTokenIsCanceledWhileTheStreamIsRead()
	{
		var session = await StartSessionAsync();
		using var cancellation = new CancellationTokenSource();
		var content = Enumerable.Range(0, 10_000).Select(i => (byte)(i % 251)).ToArray();
		var stream = new FilesScriptedStream(content) { CancelAtRead = 2, CancelSource = cancellation };

		var (operation, launch) = await StartSaveAsync(session, "report.bin", stream, token: cancellation.Token);
		await session.DeliverAsync(launch, Result.Ok, Answer("primary:Documents/cancelled.bin"));
		var result = await operation.WaitAsync(operationTimeout);

		Assert.True(result.IsCancelled);
		Assert.Null(result.FilePath);
		Assert.Equal(2, stream.ReadCalls);
	}

	// The token is canceled inside the second read, which still returns its block, and the stream never looks at the token (as many
	// streams do not): only FileSaver's own check of the token between two blocks can stop the copy before the end of the data. Waiting
	// for each write with WaitAsync(token) instead saw the cancellation late or never whenever the write had already finished (2 to 5 of
	// 150 repetitions on API 28 reported success with the whole stream written), and a wait that did see it left the write running on the
	// thread pool while SaveDocument disposed the output stream (on API 36 the app then aborted with "JNI DETECTED ERROR ... invalid
	// global reference ... in call to CallVoidMethodA"). Both were races: repeat this test ([Repeat(150)]) on API 28 and 36 to verify a
	// change of SaveDocument. The block that was read while the token was canceled is never written, so the document holds the first block.
	[Fact]
	public async Task SaveAsyncStopsWritingWhenTheTokenIsCanceledBetweenTwoBlocks()
	{
		var session = await StartSessionAsync();
		using var cancellation = new CancellationTokenSource();
		const string documentId = "primary:Documents/stopped.bin";
		var content = Enumerable.Range(0, 20_000).Select(i => (byte)(i % 251)).ToArray();
		var stream = new FilesScriptedStream(content) { CancelDuringRead = 2, CancelSource = cancellation, IgnoresToken = true };

		var (operation, launch) = await StartSaveAsync(session, "stopped.bin", stream, token: cancellation.Token);
		await session.DeliverAsync(launch, Result.Ok, Answer(documentId));
		var result = await operation.WaitAsync(operationTimeout);

		Assert.True(result.IsCancelled, result.Exception?.ToString());
		Assert.Null(result.FilePath);
		Assert.Equal(2, stream.ReadsWithData);
		Assert.Equal(content[..4096], await File.ReadAllBytesAsync(FilesTestContentProvider.GetBackingFile(documentId)));
	}

	[Fact]
	public async Task SaveAsyncDoesNotReadAgainWhenTheTokenIsCanceledAfterABlockWasWritten()
	{
		var session = await StartSessionAsync();
		using var cancellation = new CancellationTokenSource();
		const string documentId = "primary:Documents/one-block.bin";
		var content = Enumerable.Range(0, 20_000).Select(i => (byte)(i % 251)).ToArray();
		var stream = new FilesScriptedStream(content);
		// The report of the first block cancels the token, after that block was written and before the next read: the token that
		// SaveDocument hands to that read stops the copy (the stream honors it) before a second block is read.
		var progress = new FilesRecordingProgress { OnReport = _ => cancellation.Cancel() };

		var (operation, launch) = await StartSaveAsync(session, "one-block.bin", stream, progress, cancellation.Token);
		await session.DeliverAsync(launch, Result.Ok, Answer(documentId));
		var result = await operation.WaitAsync(operationTimeout);

		Assert.True(result.IsCancelled, result.Exception?.ToString());
		Assert.Null(result.FilePath);
		Assert.Equal(2, stream.ReadCalls);
		Assert.Equal(1, stream.ReadsWithData);
		Assert.Equal(content[..4096], await File.ReadAllBytesAsync(FilesTestContentProvider.GetBackingFile(documentId)));
	}

	[Fact]
	public async Task SaveAsyncReturnsACanceledResultWhenTheTokenIsCanceledWhileTheLastBlockIsRead()
	{
		var session = await StartSessionAsync();
		using var cancellation = new CancellationTokenSource();
		var content = Enumerable.Range(0, 5000).Select(i => (byte)(i % 211)).ToArray();
		// The copy loop ends normally, but SaveDocument then checks the token once more, so the save is canceled.
		var stream = new FilesScriptedStream(content) { CancelAtEndOfData = true, CancelSource = cancellation };
		const string documentId = "primary:Documents/last.bin";

		var (operation, launch) = await StartSaveAsync(session, "last.bin", stream, token: cancellation.Token);
		await session.DeliverAsync(launch, Result.Ok, Answer(documentId));
		var result = await operation.WaitAsync(operationTimeout);

		Assert.True(result.IsCancelled, result.Exception?.ToString());
		Assert.Null(result.FilePath);
		Assert.Equal(3, stream.ReadCalls);
		Assert.Equal(2, stream.ReadsWithData);
		// The token was canceled after the last block was written, and the provider emptied the document when it was opened ("wt"), so the
		// document already holds the whole stream although the save reports Canceled.
		Assert.Equal(content, await File.ReadAllBytesAsync(FilesTestContentProvider.GetBackingFile(documentId)));
	}

	[Fact]
	public async Task SaveAsyncWritesANonSeekableStreamWhileReportingProgress()
	{
		var session = await StartSessionAsync();
		const string documentId = "primary:Documents/streamed-with-progress.bin";
		var content = Enumerable.Range(0, 10_000).Select(i => (byte)(i % 211)).ToArray();
		var progress = new FilesRecordingProgress();

		var (operation, launch) = await StartSaveAsync(session, "streamed-with-progress.bin", new FilesScriptedStream(content, canSeek: false), progress);
		await session.DeliverAsync(launch, Result.Ok, Answer(documentId));
		var result = await operation.WaitAsync(operationTimeout);

		Assert.True(result.IsSuccessful, result.Exception?.ToString());
		Assert.Equal(content, await File.ReadAllBytesAsync(FilesTestContentProvider.GetBackingFile(documentId)));
		Assert.Equal(100, progress.Values[^1]);
	}

	[Fact]
	public async Task SaveAsyncReportsTheProgressOfEachBlockInPercent()
	{
		var session = await StartSessionAsync();
		var content = Enumerable.Range(0, 10_000).Select(i => (byte)(i % 251)).ToArray();
		var progress = new FilesRecordingProgress();

		var (operation, launch) = await StartSaveAsync(session, "percent.bin", new FilesScriptedStream(content), progress);
		await session.DeliverAsync(launch, Result.Ok, Answer("primary:Documents/percent.bin"));
		Assert.True((await operation.WaitAsync(operationTimeout)).IsSuccessful);

		// Blocks of 4096 bytes: 4096 of 10000 bytes is 40.96 %, 8192 is 81.92 %.
		var values = progress.Values;
		Assert.Equal(40.96, values[0], 2);
		Assert.Equal(81.92, values[1], 2);
		Assert.All(values, value => Assert.InRange(value, 0, 100));
		Assert.Equal(100, values[^1]);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task SaveAsyncDoesNotReportACompleteSaveWhenItFailsOrIsCanceled(bool canceled)
	{
		var session = await StartSessionAsync();
		using var cancellation = new CancellationTokenSource();
		var content = Enumerable.Range(0, 10_000).Select(i => (byte)(i % 251)).ToArray();
		var progress = new FilesRecordingProgress();
		var stream = canceled
			? new FilesScriptedStream(content) { CancelDuringRead = 2, CancelSource = cancellation }
			: new FilesScriptedStream(content) { FailAtRead = 2, FailureOnRead = new IOException("The source failed.") };

		var (operation, launch) = await StartSaveAsync(session, "failed-progress.bin", stream, progress, cancellation.Token);
		await session.DeliverAsync(launch, Result.Ok, Answer("primary:Documents/failed-progress.bin"));
		var result = await operation.WaitAsync(operationTimeout);

		Assert.False(result.IsSuccessful);
		Assert.Equal(canceled, result.IsCancelled);
		var values = progress.Values;
		Assert.True(values.Count is 0 || values[^1] < 100, $"A {(canceled ? "canceled" : "failed")} save reported {string.Join(", ", values)}.");
	}

	// The failing variant throws on the fourth read, which reports the end of the data (the 10,000 bytes are read in three blocks).
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task SaveAsyncDoesNotReportACompleteSaveWhenItFailsOrIsCanceledAfterTheLastBlock(bool canceled)
	{
		var session = await StartSessionAsync();
		using var cancellation = new CancellationTokenSource();
		var content = Enumerable.Range(0, 10_000).Select(i => (byte)(i % 251)).ToArray();
		var progress = new FilesRecordingProgress();
		var stream = canceled
			? new FilesScriptedStream(content) { CancelAtEndOfData = true, CancelSource = cancellation }
			: new FilesScriptedStream(content) { FailAtRead = 4, FailureOnRead = new IOException("The source failed at its end.") };
		const string documentId = "primary:Documents/last-block.bin";

		var (operation, launch) = await StartSaveAsync(session, "last-block.bin", stream, progress, cancellation.Token);
		await session.DeliverAsync(launch, Result.Ok, Answer(documentId));
		var result = await operation.WaitAsync(operationTimeout);

		Assert.False(result.IsSuccessful);
		Assert.Equal(canceled, result.IsCancelled);
		Assert.Equal(content, await File.ReadAllBytesAsync(FilesTestContentProvider.GetBackingFile(documentId)));
		var values = progress.Values;
		Assert.True(values.Count is 0 || values[^1] < 100, $"A {(canceled ? "canceled" : "failed")} save reported {string.Join(", ", values)}.");
	}

	[Fact]
	public async Task SaveAsyncReportsACompleteSaveOnlyOnceAtTheEnd()
	{
		var session = await StartSessionAsync();
		var content = Enumerable.Range(0, 10_000).Select(i => (byte)(i % 251)).ToArray();
		var progress = new FilesRecordingProgress();

		var (operation, launch) = await StartSaveAsync(session, "once.bin", new FilesScriptedStream(content), progress);
		await session.DeliverAsync(launch, Result.Ok, Answer("primary:Documents/once.bin"));
		Assert.True((await operation.WaitAsync(operationTimeout)).IsSuccessful);

		var values = progress.Values;
		Assert.Equal(100, values[^1]);
		Assert.All(values.SkipLast(1), value => Assert.True(value < 100, $"The save reported {string.Join(", ", values)}."));
	}

	[Fact]
	public async Task SaveAsyncClosesTheDocumentDescriptorWhenTheSaveIsDone()
	{
		var session = await StartSessionAsync();

		var (operation, launch) = await StartSaveAsync(session, "closed.txt", new FilesScriptedStream([1, 2, 3]));
		await session.DeliverAsync(launch, Result.Ok, Answer("primary:Documents/closed.txt"));
		Assert.True((await operation.WaitAsync(operationTimeout)).IsSuccessful);

		await AssertionExtensions.AssertEventually(
			() => FilesTestContentProvider.CleanCloseCount is 1,
			timeout: 5000,
			message: $"The document descriptor that FileSaver opened was not closed (closed: {FilesTestContentProvider.CleanCloseCount}, finalized: {FilesTestContentProvider.FailedCloseCount}).");
		Assert.Equal(0, FilesTestContentProvider.FailedCloseCount);
	}

	// Closing with an error (ParcelFileDescriptor.closeWithError: "communicate to the remote side that an error occurred") keeps a provider that
	// commits a document when it is closed from taking the part that was written for the whole document. The error carries the reason, which
	// tells this close apart from a descriptor that was leaked (also an error for the provider).
	[Fact]
	public async Task SaveAsyncClosesTheDocumentDescriptorWithAnErrorWhenReadingTheStreamFails()
	{
		var session = await StartSessionAsync();
		var failure = new IOException("The source failed.");

		var (operation, launch) = await StartSaveAsync(session, "failed.txt", new FilesScriptedStream([1, 2, 3], canSeek: false) { FailAtRead = 1, FailureOnRead = failure });
		await session.DeliverAsync(launch, Result.Ok, Answer("primary:Documents/failed.txt"));
		Assert.Same(failure, (await operation.WaitAsync(operationTimeout)).Exception);
		Assert.Equal(["wt"], FilesTestContentProvider.OpenModes);

		await AssertTheDescriptorWasClosedWithAnErrorAsync(failure.Message);
	}

	[Fact]
	public async Task SaveAsyncClosesTheDocumentDescriptorWithAnErrorWhenTheSaveIsCanceled()
	{
		var session = await StartSessionAsync();
		using var cancellation = new CancellationTokenSource();
		var content = Enumerable.Range(0, 10_000).Select(i => (byte)(i % 251)).ToArray();
		var stream = new FilesScriptedStream(content) { CancelDuringRead = 2, CancelSource = cancellation, IgnoresToken = true };

		var (operation, launch) = await StartSaveAsync(session, "canceled.bin", stream, token: cancellation.Token);
		await session.DeliverAsync(launch, Result.Ok, Answer("primary:Documents/canceled.bin"));
		var canceled = Assert.IsType<OperationCanceledException>((await operation.WaitAsync(operationTimeout)).Exception, exactMatch: false);

		await AssertTheDescriptorWasClosedWithAnErrorAsync(canceled.Message);
	}

	// Calibrates SaveAsyncClosesTheDocumentDescriptorWhenTheSaveIsDone: the test provider does see a descriptor that its client closes, so a
	// failure of that test is FileSaver's.
	[Fact]
	public async Task TheTestDocumentProviderSeesADescriptorThatItsClientClosesExplicitly()
	{
		await StartSessionAsync();
		const string documentId = "primary:Documents/calibration.txt";

		await Task.Run(() =>
		{
			var descriptor = Android.App.Application.Context.ContentResolver!.OpenFileDescriptor(FilesTestContentProvider.BuildDocumentUri(documentId), "wt")
				?? throw new InvalidOperationException("The test provider returned no descriptor.");
			using var output = new Android.OS.ParcelFileDescriptor.AutoCloseOutputStream(descriptor);
			output.Write([7, 8]);
			output.Close();
		});

		await AssertionExtensions.AssertEventually(() => FilesTestContentProvider.CleanCloseCount is 1, timeout: 5000, message: "The provider did not see the descriptor closed.");
		Assert.Equal([7, 8], await File.ReadAllBytesAsync(FilesTestContentProvider.GetBackingFile(documentId)));
	}

	// Calibrates the tests of a save that fails or is canceled: the test provider sees a descriptor that its client closes with an error as
	// such, with the client's message.
	[Fact]
	public async Task TheTestDocumentProviderSeesADescriptorThatItsClientClosesWithAnError()
	{
		await StartSessionAsync();
		const string documentId = "primary:Documents/calibration-error.txt";

		await Task.Run(() =>
		{
			var descriptor = Android.App.Application.Context.ContentResolver!.OpenFileDescriptor(FilesTestContentProvider.BuildDocumentUri(documentId), "wt")
				?? throw new InvalidOperationException("The test provider returned no descriptor.");
			using var output = new Android.OS.ParcelFileDescriptor.AutoCloseOutputStream(descriptor);
			output.Write([7, 8]);
			descriptor.CloseWithError("The client failed.");
		});

		await AssertTheDescriptorWasClosedWithAnErrorAsync("The client failed.");
		Assert.Equal([7, 8], await File.ReadAllBytesAsync(FilesTestContentProvider.GetBackingFile(documentId)));
	}

	[Theory]
	[InlineData(0, false)]
	[InlineData(1, false)]
	[InlineData(2, false)]
	[InlineData(3, false)]
	[InlineData(0, true)]
	[InlineData(1, true)]
	[InlineData(2, true)]
	[InlineData(3, true)]
	public async Task EverySaveAsyncOverloadSavesToTheDocumentThatThePickerReturned(int overload, bool useStaticApi)
	{
		var session = await StartSessionAsync();
		var progress = new FilesRecordingProgress();
		var documentId = $"primary:Documents/overload-{overload}-{useStaticApi}.txt";
		var initialPath = $"{ExternalStorage}/Music";

		var operation = await StartOnMainThreadAsync(() => SaveAsync(overload, useStaticApi, "overload.txt", new MemoryStream([4, 5, 6]), progress, CancellationToken.None, initialPath));
		var launch = await session.WaitForLaunchAsync();
		await session.DeliverAsync(launch, Result.Ok, Answer(documentId));
		var result = await operation.WaitAsync(operationTimeout);

		var hasInitialPath = overload is 0 or 2;
		var hasProgress = overload is 2 or 3;
		var initialDocumentId = DocumentsContract.GetDocumentId(InitialUri(launch.Intent))!.Replace("primary:/", "primary:", StringComparison.Ordinal);
		Assert.True(result.IsSuccessful, result.Exception?.ToString());
		Assert.Equal($"{ExternalStorage}/Documents/overload-{overload}-{useStaticApi}.txt", result.FilePath);
		Assert.Equal([4, 5, 6], await File.ReadAllBytesAsync(FilesTestContentProvider.GetBackingFile(documentId)));
		Assert.Equal(hasInitialPath ? "primary:Music" : "primary:", initialDocumentId);
		Assert.Equal(hasProgress, progress.Values.Count > 0);
	}

	[Fact]
	public async Task SaveAsyncKeepsTwoSavesInFlightApart()
	{
		var session = await StartSessionAsync();
		var first = await StartOnMainThreadAsync(() => FileSaver.Default.SaveAsync("first.txt", new MemoryStream([1, 1, 1])));
		var second = await StartOnMainThreadAsync(() => FileSaver.Default.SaveAsync("second.txt", new MemoryStream([2, 2])));
		var firstLaunch = await session.WaitForLaunchAsync(0);
		var secondLaunch = await session.WaitForLaunchAsync(1);

		await session.DeliverAsync(secondLaunch, Result.Ok, Answer("primary:Documents/second.txt"));
		var secondResult = await second.WaitAsync(operationTimeout);
		Assert.False(first.IsCompleted);
		await session.DeliverAsync(firstLaunch, Result.Ok, Answer("primary:Documents/first.txt"));
		var firstResult = await first.WaitAsync(operationTimeout);

		Assert.Equal($"{ExternalStorage}/Documents/first.txt", firstResult.FilePath);
		Assert.Equal($"{ExternalStorage}/Documents/second.txt", secondResult.FilePath);
		Assert.Equal([1, 1, 1], await File.ReadAllBytesAsync(FilesTestContentProvider.GetBackingFile("primary:Documents/first.txt")));
		Assert.Equal([2, 2], await File.ReadAllBytesAsync(FilesTestContentProvider.GetBackingFile("primary:Documents/second.txt")));
	}

	[Fact]
	public async Task SaveAsyncFromABackgroundThreadSavesTheFile()
	{
		var session = await StartSessionAsync();
		Task<FileSaverResult> operation = null!;
		await Task.Run(() => { operation = FileSaver.Default.SaveAsync("background.txt", new MemoryStream([3, 2, 1])); });
		var launch = await session.WaitForLaunchAsync();

		await session.DeliverAsync(launch, Result.Ok, Answer("primary:Documents/background.txt"));
		var result = await operation.WaitAsync(operationTimeout);

		Assert.True(result.IsSuccessful, result.Exception?.ToString());
		Assert.Equal([3, 2, 1], await File.ReadAllBytesAsync(FilesTestContentProvider.GetBackingFile("primary:Documents/background.txt")));
	}

	[Fact]
	public async Task SaveAsyncFailsWhenTheCurrentActivityCannotReceiveResults()
	{
		var session = await StartSessionAsync();
		await session.StartPlainAsync();

		var result = await FileSaver.Default.SaveAsync("report.txt", new MemoryStream([1])).WaitAsync(operationTimeout);

		Assert.False(result.IsSuccessful);
		Assert.IsType<InvalidOperationException>(result.Exception);
		Assert.Empty(session.Launches);
	}

	async Task<FilesPickerSession> StartSessionAsync()
	{
		var session = await FilesPickerSession.StartAsync();
		AddCleanup(() => session.DisposeAsync().AsTask());
		AddCleanup(() => FilesActivityResultManagerState.PendingKeys.ToList().ForEach(FilesActivityResultManagerState.Untrack));
		return session;
	}

	// A save that is not asked about must never reach the real document picker, so every test has the fake one in front.
	Task PrepareHostAsync() => StartSessionAsync();

	static Task AssertNoPickerWasStartedAsync()
	{
		Assert.Empty(FilesFakePicker.Launches);
		Assert.Equal(0, FilesActivityResultManagerState.PendingCount);
		return Task.CompletedTask;
	}

	static Task<(Task<FileSaverResult> Operation, FilesFakePickerLaunch Launch)> StartSaveAsync(FilesPickerSession session, string fileName, Stream stream, IProgress<double>? progress = null, CancellationToken token = default) =>
		StartSaveAsync(session, null, fileName, stream, progress, token);

	static Task<(Task<FileSaverResult> Operation, FilesFakePickerLaunch Launch)> StartSaveAsync(FilesPickerSession session, string initialPath, string fileName, Stream stream) =>
		StartSaveAsync(session, initialPath, fileName, stream, null, default);

	static async Task<(Task<FileSaverResult> Operation, FilesFakePickerLaunch Launch)> StartSaveAsync(FilesPickerSession session, string? initialPath, string fileName, Stream stream, IProgress<double>? progress, CancellationToken token)
	{
		var operation = await StartOnMainThreadAsync(() => (initialPath, progress) switch
		{
			(null, null) => FileSaver.Default.SaveAsync(fileName, stream, token),
			(null, not null) => FileSaver.Default.SaveAsync(fileName, stream, progress, token),
			(not null, null) => FileSaver.Default.SaveAsync(initialPath, fileName, stream, token),
			(not null, not null) => FileSaver.Default.SaveAsync(initialPath, fileName, stream, progress, token),
		});

		return (operation, await session.WaitForLaunchAsync());
	}

	static async Task CancelPickerAsync(FilesPickerSession session, FilesFakePickerLaunch launch, Task<FileSaverResult> operation)
	{
		await session.DeliverAsync(launch, Result.Canceled);
		Assert.True((await operation.WaitAsync(operationTimeout)).IsCancelled);
	}

	static Intent Answer(string documentId) => new Intent().SetData(FilesTestContentProvider.BuildDocumentUri(documentId));

	static async Task AssertTheDescriptorWasClosedWithAnErrorAsync(string reason)
	{
		await AssertionExtensions.AssertEventually(() => FilesTestContentProvider.CleanCloseCount + FilesTestContentProvider.FailedCloseCount > 0, timeout: 5000, message: "The document descriptor was not closed.");
		Assert.Equal(0, FilesTestContentProvider.CleanCloseCount);
		Assert.Equal(1, FilesTestContentProvider.FailedCloseCount);
		Assert.Contains(reason, Assert.Single(FilesTestContentProvider.CloseErrors), StringComparison.Ordinal);
	}

	static AndroidUri InitialUri(Intent intent) =>
		intent.GetParcelableExtra(DocumentsContract.ExtraInitialUri) as AndroidUri ?? throw new InvalidOperationException("The intent has no initial URI.");

	static void WriteBackingFile(string documentId, byte[] content)
	{
		var file = FilesTestContentProvider.GetBackingFile(documentId);
		Directory.CreateDirectory(Path.GetDirectoryName(file)!);
		File.WriteAllBytes(file, content);
	}
}