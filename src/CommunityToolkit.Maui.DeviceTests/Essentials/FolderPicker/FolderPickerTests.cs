using CommunityToolkit.Maui.Storage;

namespace CommunityToolkit.Maui.DeviceTests;

// FolderPicker on a device. FolderPickerImplementation (Core, Essentials/FolderPicker) is one class in several files: the shared file maps
// what the platform does to a FolderPickerResult, and the Android, iOS/Mac Catalyst and Windows files talk to the system's picker. This
// file holds what is the same everywhere; each platform partial drives its own picker and reads the native state back. See
// FileSaverTests.cs for how the pickers are answered without a person.
[Category(TestCategory.FolderPicker)]
[Collection(TestCollections.DisableParallelization)]
public partial class FolderPickerTests : FilesTestBase
{
	// The two PickAsync overloads, once through a FolderPickerImplementation and once through the static FolderPicker facade.
	[Theory]
	[InlineData(0, false)]
	[InlineData(1, false)]
	[InlineData(0, true)]
	[InlineData(1, true)]
	public async Task PickAsyncWithACanceledTokenReturnsACanceledResultAndNeverStartsThePicker(int overload, bool useStaticApi)
	{
		await PrepareHostAsync();
		using var cancellation = new CancellationTokenSource();
		await cancellation.CancelAsync();

		var result = await PickAsync(overload, useStaticApi, cancellation.Token).WaitAsync(operationTimeout);

		Assert.True(result.IsCancelled);
		Assert.False(result.IsSuccessful);
		Assert.Null(result.Folder);
		Assert.IsType<OperationCanceledException>(result.Exception);
		Assert.Throws<OperationCanceledException>(result.EnsureSuccess);
		await AssertNoPickerWasStartedAsync();
	}

	// A null initial path is a mistake of the caller (no initial path is the overload without one), and every platform answers it the same
	// way: an ArgumentNullException in the result, before anything reaches a picker. Android used to fail with a NullReferenceException deep
	// in the path conversion, while iOS, Mac Catalyst and Windows took it for no initial path and showed their picker.
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task PickAsyncWithANullInitialPathReturnsAnArgumentNullExceptionAndNeverStartsThePicker(bool useStaticApi)
	{
		await PrepareHostAsync();
		// A pick that did start would wait for a person, so a test that fails ends it
		var token = CanceledWhenTheTestEnds();

		// From the UI thread, as an app calls it
		var result = await InvokeOnMainThreadAsync(() => PickAsync(0, useStaticApi, token, null!)).WaitAsync(operationTimeout);

		Assert.False(result.IsSuccessful);
		Assert.False(result.IsCancelled);
		Assert.Null(result.Folder);
		var exception = Assert.IsType<ArgumentNullException>(result.Exception);
		Assert.Equal("initialPath", exception.ParamName);
		Assert.Throws<ArgumentNullException>(result.EnsureSuccess);
		await AssertNoPickerWasStartedAsync();
	}

	/// <summary>Calls one of the two overloads: 0 passes <paramref name="initialPath"/>, 1 has no initial path.</summary>
	static Task<FolderPickerResult> PickAsync(int overload, bool useStaticApi, CancellationToken token, string initialPath = "") =>
		useStaticApi
			? overload switch
			{
				0 => FolderPicker.PickAsync(initialPath, token),
				1 => FolderPicker.PickAsync(token),
				_ => throw new ArgumentOutOfRangeException(nameof(overload)),
			}
			: overload switch
			{
				0 => new FolderPickerImplementation().PickAsync(initialPath, token),
				1 => new FolderPickerImplementation().PickAsync(token),
				_ => throw new ArgumentOutOfRangeException(nameof(overload)),
			};
}