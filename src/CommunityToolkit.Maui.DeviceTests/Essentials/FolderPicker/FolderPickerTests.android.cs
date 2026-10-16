using Android.App;
using Android.Content;
using Android.Provider;
using CommunityToolkit.Maui.Storage;
using AndroidEnvironment = Android.OS.Environment;
using AndroidUri = Android.Net.Uri;

namespace CommunityToolkit.Maui.DeviceTests;

// Android: FolderPicker launches ACTION_OPEN_DOCUMENT_TREE through ActivityResultManager and turns the tree URI that the picker returns
// into a physical path. The picker is a FilesFakePickerActivity (see ActivityResultManagerTests); the path conversion is the real one.
public partial class FolderPickerTests
{
	const string externalStorageProviderAuthority = "com.android.externalstorage.documents";
	const string tree = "content://com.android.externalstorage.documents/tree/";

	static string ExternalStorage => AndroidEnvironment.ExternalStorageDirectory!.Path;

	[Fact]
	public async Task PickAsyncAsksForADocumentTreeStartingAtTheRootOfTheExternalStorage()
	{
		var session = await StartSessionAsync();

		var (operation, launch) = await StartPickAsync(session);

		Assert.Equal(Intent.ActionOpenDocumentTree, launch.Intent.Action);
		var initialUri = InitialUri(launch.Intent);
		Assert.Equal(externalStorageProviderAuthority, initialUri.Authority);
		Assert.Equal("primary:", DocumentsContract.GetDocumentId(initialUri));
		await CancelPickerAsync(session, launch, operation);
	}

	// The root of the file system ("/"), which no storage volume is, starts the picker at the root of the primary storage, and so does an
	// empty path: only null is rejected (see PickAsyncWithANullInitialPathReturnsAnArgumentNullExceptionAndNeverStartsThePicker).
	[Theory]
	[InlineData("{external}", "primary:")]
	[InlineData("{external}/Documents", "primary:Documents")]
	[InlineData("{external}/Documents/Reports", "primary:Documents/Reports")]
	[InlineData("Documents/Reports", "primary:Documents/Reports")]
	[InlineData("/", "primary:")]
	[InlineData("", "primary:")]
	public async Task PickAsyncStartsThePickerInTheFolderOfTheInitialPath(string initialPath, string expectedDocumentId)
	{
		var session = await StartSessionAsync();

		var (operation, launch) = await StartPickAsync(session, initialPath.Replace("{external}", ExternalStorage, StringComparison.Ordinal));

		Assert.Equal(Intent.ActionOpenDocumentTree, launch.Intent.Action);
		var initialUri = InitialUri(launch.Intent);
		Assert.Equal(externalStorageProviderAuthority, initialUri.Authority);
		// The folder may or may not carry a leading slash after the volume; what matters is which folder it names.
		Assert.Equal(expectedDocumentId, DocumentsContract.GetDocumentId(initialUri)!.Replace("primary:/", "primary:", StringComparison.Ordinal));
		await CancelPickerAsync(session, launch, operation);
	}

	[Fact]
	public async Task PickAsyncStartsThePickerInAFolderWhoseNameHasASpace()
	{
		var session = await StartSessionAsync();

		var (operation, launch) = await StartPickAsync(session, $"{ExternalStorage}/Documents/My Folder");

		Assert.Equal("primary:Documents/My Folder", DocumentsContract.GetDocumentId(InitialUri(launch.Intent))!.Replace("primary:/", "primary:", StringComparison.Ordinal));
		await CancelPickerAsync(session, launch, operation);
	}

	[Theory]
	[InlineData("/storage/1B0B-0B1C/DCIM", "1B0B-0B1C:DCIM")]
	[InlineData("/sdcard/Download", "primary:Download")]
	[InlineData("{appdata}", "")]
	[InlineData("/sdcard2/Download", "")]
	public async Task PickAsyncStartsThePickerOnTheVolumeOfTheInitialPath(string initialPath, string expectedDocumentId)
	{
		var session = await StartSessionAsync();

		var (operation, launch) = await StartPickAsync(session, initialPath.Replace("{appdata}", FileSystem.AppDataDirectory, StringComparison.Ordinal));

		if (expectedDocumentId.Length is 0)
		{
			// An empty expected id: the app's private storage, like a path that only starts like a storage folder (/sdcard2 is not in
			// /sdcard), is on no volume that the document picker shows, so no document id can name it, and the picker is left to choose
			// where it starts (DocumentsContract.EXTRA_INITIAL_URI is absent), as the system does when it cannot find the initial location.
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
	public async Task PickAsyncDoesNotTraceTheInitialPathLimitOnAndroid26AndLater()
	{
		Skip.IfNot(OperatingSystem.IsAndroidVersionAtLeast(26), "Below Android 26 the limit is traced instead (PickAsyncTracesThatAnInitialPathNeedsAndroid26BeforeThatVersion).");
		var session = await StartSessionAsync();
		using var trace = new FilesTraceCapture();

		var (operation, launch) = await StartPickAsync(session, $"{ExternalStorage}/Documents");

		Assert.False(trace.Contains("only supported on Android 26"), string.Join(Environment.NewLine, trace.Lines));
		await CancelPickerAsync(session, launch, operation);
	}

	[Fact]
	public async Task PickAsyncTracesThatAnInitialPathNeedsAndroid26BeforeThatVersion()
	{
		Skip.If(OperatingSystem.IsAndroidVersionAtLeast(26), "The branch only runs below Android 26 (the emulators of the pool are API 28 and 36).");
		var session = await StartSessionAsync();
		using var trace = new FilesTraceCapture();

		var (withPath, launchWithPath) = await StartPickAsync(session, $"{ExternalStorage}/Documents");
		Assert.True(trace.Contains("Specifying an initial path is only supported on Android 26 and later."), string.Join(Environment.NewLine, trace.Lines));
		await CancelPickerAsync(session, launchWithPath, withPath);

		var traced = trace.Lines.Count;
		var (withoutPath, launchWithoutPath) = await StartPickAsync(session, string.Empty);
		Assert.Equal(traced, trace.Lines.Count);
		await CancelPickerAsync(session, launchWithoutPath, withoutPath);
	}

	[Theory]
	[InlineData(tree + "primary%3ADCIM", "{external}/DCIM", "DCIM")]
	[InlineData(tree + "primary%3ADCIM%2FCamera", "{external}/DCIM/Camera", "Camera")]
	[InlineData(tree + "primary%3AMy%20Pictures", "{external}/My Pictures", "My Pictures")]
	[InlineData(tree + "1B0B-0B1C%3ADCIM%2FCamera", "/storage/1B0B-0B1C/DCIM/Camera", "Camera")]
	public async Task PickAsyncReturnsThePhysicalPathAndTheNameOfThePickedFolder(string treeUri, string expectedPath, string expectedName)
	{
		var session = await StartSessionAsync();

		var (operation, launch) = await StartPickAsync(session);
		await session.DeliverAsync(launch, Result.Ok, new Intent().SetData(AndroidUri.Parse(treeUri)));
		var result = await operation.WaitAsync(operationTimeout);

		Assert.True(result.IsSuccessful, result.Exception?.ToString());
		Assert.False(result.IsCancelled);
		Assert.Equal(expectedPath.Replace("{external}", ExternalStorage, StringComparison.Ordinal), result.Folder.Path);
		Assert.Equal(expectedName, result.Folder.Name);
		Assert.Equal(0, FilesActivityResultManagerState.PendingCount);
	}

	[Fact]
	public async Task PickAsyncReturnsTheRootOfTheStorageWhenThePersonPicksIt()
	{
		var session = await StartSessionAsync();

		var (operation, launch) = await StartPickAsync(session);
		await session.DeliverAsync(launch, Result.Ok, new Intent().SetData(AndroidUri.Parse(tree + "primary%3A")));
		var result = await operation.WaitAsync(operationTimeout);

		Assert.True(result.IsSuccessful, result.Exception?.ToString());
		Assert.Equal(ExternalStorage, Path.TrimEndingDirectorySeparator(result.Folder.Path));
	}

	[Fact]
	public async Task PickAsyncReturnsTheRootOfTheStorageWithoutATrailingSlashAndWithAName()
	{
		var session = await StartSessionAsync();

		var (operation, launch) = await StartPickAsync(session);
		await session.DeliverAsync(launch, Result.Ok, new Intent().SetData(AndroidUri.Parse(tree + "primary%3A")));
		var result = await operation.WaitAsync(operationTimeout);

		Assert.True(result.IsSuccessful, result.Exception?.ToString());
		Assert.Equal(ExternalStorage, result.Folder.Path);
		Assert.False(string.IsNullOrEmpty(result.Folder.Name), $"The root folder '{result.Folder.Path}' has no name.");
	}

	[Fact]
	public async Task PickAsyncReturnsACanceledResultWhenThePickerIsCanceled()
	{
		var session = await StartSessionAsync();

		var (operation, launch) = await StartPickAsync(session);
		await session.DeliverAsync(launch, Result.Canceled, new Intent().SetData(AndroidUri.Parse(tree + "primary%3ADCIM")));
		var result = await operation.WaitAsync(operationTimeout);

		Assert.True(result.IsCancelled);
		Assert.Null(result.Folder);
		Assert.IsType<OperationCanceledException>(result.Exception, exactMatch: false);
	}

	[Fact]
	public async Task PickAsyncReturnsACanceledResultWhenTheTokenIsCanceledWhileThePickerIsOpenAndIgnoresTheLateAnswer()
	{
		var session = await StartSessionAsync();
		using var cancellation = new CancellationTokenSource();

		var (operation, launch) = await StartPickAsync(session, token: cancellation.Token);
		await cancellation.CancelAsync();
		var result = await operation.WaitAsync(operationTimeout);
		await session.DeliverAsync(launch, Result.Ok, new Intent().SetData(AndroidUri.Parse(tree + "primary%3ADCIM")));
		await AssertionExtensions.AssertEventually(() => FilesActivityResultManagerState.PendingCount is 0, timeout: 5000, message: "The late answer was not drained.");

		Assert.True(result.IsCancelled);
		Assert.Null(result.Folder);
	}

	[Fact]
	public async Task PickAsyncReturnsACanceledResultWhenTheActivityFinishesWhileThePickerIsOpen()
	{
		var session = await StartSessionAsync();

		var (operation, _) = await StartPickAsync(session);
		await session.FinishAsync(session.Activity);
		var result = await operation.WaitAsync(operationTimeout);

		Assert.True(result.IsCancelled);
		Assert.Equal(0, FilesActivityResultManagerState.PendingCount);
	}

	[Fact]
	public async Task PickAsyncFailsWhenThePickerReturnsNoUri()
	{
		var session = await StartSessionAsync();

		var (operation, launch) = await StartPickAsync(session);
		await session.DeliverAsync(launch, Result.Ok);
		var result = await operation.WaitAsync(operationTimeout);

		Assert.False(result.IsSuccessful);
		Assert.False(result.IsCancelled);
		Assert.Null(result.Folder);
		var exception = Assert.IsType<FolderPickerException>(result.Exception);
		Assert.Equal("Path is not selected.", exception.Message);
		Assert.Throws<FolderPickerException>(result.EnsureSuccess);
	}

	[Theory]
	[InlineData("content://media/external/file/123")]
	[InlineData("content://authority/onlyone")]
	[InlineData("file:///storage/emulated/0/DCIM")]
	[InlineData("http://example.com/tree/primary%3ADCIM")]
	[InlineData("relative/tree/primary%3ADCIM")]
	public async Task PickAsyncFailsWhenTheUriCannotBeResolvedToAPath(string uri)
	{
		var session = await StartSessionAsync();

		var (operation, launch) = await StartPickAsync(session);
		await session.DeliverAsync(launch, Result.Ok, new Intent().SetData(AndroidUri.Parse(uri)));
		var result = await operation.WaitAsync(operationTimeout);

		Assert.False(result.IsSuccessful);
		var exception = Assert.IsType<FolderPickerException>(result.Exception);
		Assert.StartsWith("Unable to resolve absolute path or retrieve contents of URI", exception.Message, StringComparison.Ordinal);
		Assert.Contains(uri, exception.Message, StringComparison.Ordinal);
	}

	// Every overload reaches the same picker; the one with an initial path puts it in the intent.
	[Theory]
	[InlineData(0, false)]
	[InlineData(1, false)]
	[InlineData(0, true)]
	[InlineData(1, true)]
	public async Task EveryPickAsyncOverloadReturnsTheFolderThatThePickerReturned(int overload, bool useStaticApi)
	{
		var session = await StartSessionAsync();

		var operation = await StartOnMainThreadAsync(() => PickAsync(overload, useStaticApi, CancellationToken.None, $"{ExternalStorage}/Music"));
		var launch = await session.WaitForLaunchAsync();
		await session.DeliverAsync(launch, Result.Ok, new Intent().SetData(AndroidUri.Parse(tree + "primary%3ADCIM")));
		var result = await operation.WaitAsync(operationTimeout);

		var initialDocumentId = DocumentsContract.GetDocumentId(InitialUri(launch.Intent))!.Replace("primary:/", "primary:", StringComparison.Ordinal);
		Assert.True(result.IsSuccessful, result.Exception?.ToString());
		Assert.Equal($"{ExternalStorage}/DCIM", result.Folder.Path);
		Assert.Equal(overload is 0 ? "primary:Music" : "primary:", initialDocumentId);
	}

	[Fact]
	public async Task PickAsyncAndSaveAsyncInFlightTogetherEachGetTheirOwnAnswer()
	{
		var session = await StartSessionAsync();
		var pick = await StartOnMainThreadAsync(() => FolderPicker.Default.PickAsync());
		var save = await StartOnMainThreadAsync(() => FileSaver.Default.SaveAsync("together.txt", new MemoryStream([1, 2])));
		var pickLaunch = await session.WaitForLaunchAsync(0);
		var saveLaunch = await session.WaitForLaunchAsync(1);

		await session.DeliverAsync(saveLaunch, Result.Ok, new Intent().SetData(FilesTestContentProvider.BuildDocumentUri("primary:Documents/together.txt")));
		var saveResult = await save.WaitAsync(operationTimeout);
		Assert.False(pick.IsCompleted);
		await session.DeliverAsync(pickLaunch, Result.Ok, new Intent().SetData(AndroidUri.Parse(tree + "primary%3APictures")));
		var pickResult = await pick.WaitAsync(operationTimeout);

		Assert.Equal(Intent.ActionOpenDocumentTree, pickLaunch.Intent.Action);
		Assert.Equal(Intent.ActionCreateDocument, saveLaunch.Intent.Action);
		Assert.Equal($"{ExternalStorage}/Documents/together.txt", saveResult.FilePath);
		Assert.Equal($"{ExternalStorage}/Pictures", pickResult.Folder?.Path);
	}

	[Fact]
	public async Task PickAsyncFromABackgroundThreadReturnsTheFolder()
	{
		var session = await StartSessionAsync();
		Task<FolderPickerResult> operation = null!;
		await Task.Run(() => { operation = FolderPicker.Default.PickAsync(); });
		var launch = await session.WaitForLaunchAsync();

		await session.DeliverAsync(launch, Result.Ok, new Intent().SetData(AndroidUri.Parse(tree + "primary%3ADCIM")));
		var result = await operation.WaitAsync(operationTimeout);

		Assert.True(result.IsSuccessful, result.Exception?.ToString());
		Assert.Equal($"{ExternalStorage}/DCIM", result.Folder.Path);
	}

	[Fact]
	public async Task PickAsyncFailsWhenTheCurrentActivityCannotReceiveResults()
	{
		var session = await StartSessionAsync();
		await session.StartPlainAsync();

		var result = await FolderPicker.Default.PickAsync().WaitAsync(operationTimeout);

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

	// A pick that is not asked about must never reach the real document picker, so every test has the fake one in front.
	Task PrepareHostAsync() => StartSessionAsync();

	static Task AssertNoPickerWasStartedAsync()
	{
		Assert.Empty(FilesFakePicker.Launches);
		Assert.Equal(0, FilesActivityResultManagerState.PendingCount);
		return Task.CompletedTask;
	}

	// Starts FolderPicker.Default.PickAsync on the UI thread (with the initial path when there is one) and returns it with the launch it made.
	static async Task<(Task<FolderPickerResult> Operation, FilesFakePickerLaunch Launch)> StartPickAsync(FilesPickerSession session, string? initialPath = null, CancellationToken token = default)
	{
		var operation = await StartOnMainThreadAsync(() => initialPath is null
			? FolderPicker.Default.PickAsync(token)
			: FolderPicker.Default.PickAsync(initialPath, token));

		return (operation, await session.WaitForLaunchAsync());
	}

	// Answers the picker as a person who closes it, and waits for the pick to report it.
	static async Task CancelPickerAsync(FilesPickerSession session, FilesFakePickerLaunch launch, Task<FolderPickerResult> operation)
	{
		await session.DeliverAsync(launch, Result.Canceled);
		Assert.True((await operation.WaitAsync(operationTimeout)).IsCancelled);
	}

	static AndroidUri InitialUri(Intent intent) =>
		intent.GetParcelableExtra(DocumentsContract.ExtraInitialUri) as AndroidUri ?? throw new InvalidOperationException("The intent has no initial URI.");
}