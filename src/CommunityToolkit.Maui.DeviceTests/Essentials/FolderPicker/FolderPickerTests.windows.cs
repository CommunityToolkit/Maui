using System.Reflection;
using CommunityToolkit.Maui.Core.Primitives;
using CommunityToolkit.Maui.Storage;

namespace CommunityToolkit.Maui.DeviceTests;

// Windows: FolderPicker shows the system's folder dialog (Windows.Storage.Pickers.FolderPicker attached to the process's main window, or the
// Windows App SDK picker from .NET 11) and turns the folder that the person picks into a Folder. A test cannot answer the dialog, so everything
// from the dialog on needs a person (RequiresUserInteraction); the checks before it (the token, a main window to attach to) run headless.
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
		// Shows the real dialog and closes it through the token; the person only watches.
		using var cancellation = new CancellationTokenSource();
		var operation = await StartOnMainThreadAsync(() => FolderPicker.Default.PickAsync(cancellation.Token));
		await AssertionExtensions.AssertEventually(() => CountOpenDialogs() > 0, timeout: 15000, message: "The folder dialog did not open.");

		await cancellation.CancelAsync();
		var result = await operation.WaitAsync(operationTimeout);

		Assert.True(result.IsCancelled);
		await AssertionExtensions.AssertEventually(() => CountOpenDialogs() is 0, timeout: 5000, message: "The folder dialog is still open after the token was canceled.");
	}

	// A pick that is not asked about must never show a dialog.
	Task PrepareHostAsync() => Task.CompletedTask;

	static Task AssertNoPickerWasStartedAsync()
	{
		Assert.Equal(0, CountOpenDialogs());
		return Task.CompletedTask;
	}
}