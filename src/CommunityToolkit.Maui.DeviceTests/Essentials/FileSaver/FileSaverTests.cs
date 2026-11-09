using CommunityToolkit.Maui.Storage;

namespace CommunityToolkit.Maui.DeviceTests;

[Category(TestCategory.FileSaver)]
[Collection(TestCollections.DisableParallelization)]
public partial class FileSaverTests : FilesTestBase
{
	[Theory]
	[InlineData(0, false)]
	[InlineData(1, false)]
	[InlineData(2, false)]
	[InlineData(3, false)]
	[InlineData(0, true)]
	[InlineData(1, true)]
	[InlineData(2, true)]
	[InlineData(3, true)]
	public async Task SaveAsyncWithACanceledTokenReturnsACanceledResultAndNeverStartsThePicker(int overload, bool useStaticApi)
	{
		await PrepareHostAsync();
		using var cancellation = new CancellationTokenSource();
		await cancellation.CancelAsync();
		var stream = new FilesScriptedStream([1, 2, 3]);
		var progress = new FilesRecordingProgress();

		var result = await SaveAsync(overload, useStaticApi, "report.txt", stream, progress, cancellation.Token).WaitAsync(operationTimeout);

		Assert.True(result.IsCancelled);
		Assert.False(result.IsSuccessful);
		Assert.Null(result.FilePath);
		Assert.IsType<OperationCanceledException>(result.Exception);
		Assert.Throws<OperationCanceledException>(result.EnsureSuccess);
		Assert.Equal(0, stream.ReadCalls);
		Assert.Empty(progress.Values);
		await AssertNoPickerWasStartedAsync();
	}

	// An app passes a null file name on when the person cancels the prompt that asked for it. Android used to fail with a NullReferenceException,
	// and iOS and Mac Catalyst with the ArgumentNullException of NSUrl.Append, once they had created a temporary folder that they left behind.
	[Theory]
	[InlineData(0, false)]
	[InlineData(1, false)]
	[InlineData(2, false)]
	[InlineData(3, false)]
	[InlineData(0, true)]
	[InlineData(1, true)]
	[InlineData(2, true)]
	[InlineData(3, true)]
	public async Task SaveAsyncWithANullFileNameReturnsAnArgumentNullExceptionAndNeverStartsThePicker(int overload, bool useStaticApi)
	{
		await PrepareHostAsync();
		var stream = new FilesScriptedStream([1, 2, 3]);
		var progress = new FilesRecordingProgress();

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		var result = await SaveAsync(overload, useStaticApi, null, stream, progress, CancellationToken.None).WaitAsync(operationTimeout);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

		AssertArgumentNullException("fileName", result);
		Assert.Equal(0, stream.ReadCalls);
		Assert.Empty(progress.Values);
		await AssertNoPickerWasStartedAsync();
	}

	// A null stream used to fail only once the picker had returned the document to write it to.
	[Theory]
	[InlineData(0, false)]
	[InlineData(1, false)]
	[InlineData(2, false)]
	[InlineData(3, false)]
	[InlineData(0, true)]
	[InlineData(1, true)]
	[InlineData(2, true)]
	[InlineData(3, true)]
	public async Task SaveAsyncWithANullStreamReturnsAnArgumentNullExceptionAndNeverStartsThePicker(int overload, bool useStaticApi)
	{
		await PrepareHostAsync();
		var progress = new FilesRecordingProgress();

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		var result = await SaveAsync(overload, useStaticApi, "report.txt", null, progress, CancellationToken.None).WaitAsync(operationTimeout);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

		AssertArgumentNullException("stream", result);
		Assert.Empty(progress.Values);
		await AssertNoPickerWasStartedAsync();
	}

	static void AssertArgumentNullException(string parameterName, FileSaverResult result)
	{
		Assert.False(result.IsSuccessful);
		Assert.False(result.IsCancelled);
		Assert.Null(result.FilePath);
		Assert.Equal(parameterName, Assert.IsType<ArgumentNullException>(result.Exception).ParamName);
		Assert.Throws<ArgumentNullException>(result.EnsureSuccess);
	}

	static Task<FileSaverResult> SaveAsync(int overload, bool useStaticApi, string fileName, Stream stream, IProgress<double> progress, CancellationToken token, string initialPath = "") =>
		useStaticApi
			? overload switch
			{
				0 => FileSaver.SaveAsync(initialPath, fileName, stream, token),
				1 => FileSaver.SaveAsync(fileName, stream, token),
				2 => FileSaver.SaveAsync(initialPath, fileName, stream, progress, token),
				3 => FileSaver.SaveAsync(fileName, stream, progress, token),
				_ => throw new ArgumentOutOfRangeException(nameof(overload)),
			}
			: overload switch
			{
				0 => new FileSaverImplementation().SaveAsync(initialPath, fileName, stream, token),
				1 => new FileSaverImplementation().SaveAsync(fileName, stream, token),
				2 => new FileSaverImplementation().SaveAsync(initialPath, fileName, stream, progress, token),
				3 => new FileSaverImplementation().SaveAsync(fileName, stream, progress, token),
				_ => throw new ArgumentOutOfRangeException(nameof(overload)),
			};
}