using CommunityToolkit.Maui.Storage;

namespace CommunityToolkit.Maui.DeviceTests;

[Category(TestCategory.FolderPicker)]
[Collection(TestCollections.DisableParallelization)]
public partial class FolderPickerTests : FilesTestBase
{
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

	// A null initial path is a mistake of the caller (no initial path is the overload without one). Android used to fail with a
	// NullReferenceException deep in the path conversion, while iOS, Mac Catalyst and Windows took it for no initial path and showed their picker.
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task PickAsyncWithANullInitialPathReturnsAnArgumentNullExceptionAndNeverStartsThePicker(bool useStaticApi)
	{
		await PrepareHostAsync();
		// A pick that did start would wait for a person, so a test that fails ends it.
		var token = CanceledWhenTheTestEnds();

		// From the UI thread, as an app calls it.
		var result = await InvokeOnMainThreadAsync(() => PickAsync(0, useStaticApi, token, null!)).WaitAsync(operationTimeout);

		Assert.False(result.IsSuccessful);
		Assert.False(result.IsCancelled);
		Assert.Null(result.Folder);
		var exception = Assert.IsType<ArgumentNullException>(result.Exception);
		Assert.Equal("initialPath", exception.ParamName);
		Assert.Throws<ArgumentNullException>(result.EnsureSuccess);
		await AssertNoPickerWasStartedAsync();
	}

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