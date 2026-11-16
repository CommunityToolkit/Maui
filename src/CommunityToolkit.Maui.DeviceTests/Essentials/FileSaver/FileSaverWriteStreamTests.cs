#if !ANDROID
using System.Reflection;
using System.Runtime.ExceptionServices;
using CommunityToolkit.Maui.Storage;

namespace CommunityToolkit.Maui.DeviceTests;

// WriteStream is private, so these tests call it by reflection. Android writes through its content resolver instead, so they are not
// built for it.
[Category(TestCategory.FileSaver)]
[Collection(TestCollections.DisableParallelization)]
public partial class FileSaverWriteStreamTests : FilesTestBase
{
	static readonly byte[] sample = Enumerable.Range(0, 10_000).Select(i => (byte)(i % 251)).ToArray();

	[Fact]
	public async Task WriteStreamCreatesTheFileWithTheContentOfTheStream()
	{
		var path = NewFilePath();

		await WriteStream(new FilesScriptedStream(sample), path);

		Assert.Equal(sample, await File.ReadAllBytesAsync(path));
	}

	[Fact]
	public async Task WriteStreamTruncatesAnExistingLongerFile()
	{
		var path = NewFilePath();
		await File.WriteAllBytesAsync(path, Enumerable.Repeat((byte)0xFF, 20_000).ToArray());
		var content = sample[..100];

		await WriteStream(new FilesScriptedStream(content), path);

		Assert.Equal(content, await File.ReadAllBytesAsync(path));
	}

	[Fact]
	public async Task WriteStreamOverwritesAnExistingShorterFile()
	{
		var path = NewFilePath();
		await File.WriteAllBytesAsync(path, [1, 2, 3]);

		await WriteStream(new FilesScriptedStream(sample), path);

		Assert.Equal(sample, await File.ReadAllBytesAsync(path));
	}

	[Fact]
	public async Task WriteStreamEmptiesAnExistingFileWhenTheStreamIsEmpty()
	{
		var path = NewFilePath();
		await File.WriteAllBytesAsync(path, [1, 2, 3]);
		var progress = new FilesRecordingProgress();

		await WriteStream(new FilesScriptedStream([]), path, progress);

		Assert.Empty(await File.ReadAllBytesAsync(path));
		Assert.Single(progress.Values);
	}

	[Fact]
	public async Task WriteStreamRewindsASeekableStreamBeforeCopyingIt()
	{
		var path = NewFilePath();
		var stream = new FilesScriptedStream(sample);
		stream.Position = stream.Length;

		await WriteStream(stream, path);

		Assert.Equal(sample, await File.ReadAllBytesAsync(path));
		Assert.Equal(1, stream.SeekCalls);
	}

	[Fact]
	public async Task WriteStreamCopiesANonSeekableStreamFromItsCurrentPosition()
	{
		var path = NewFilePath();
		var stream = new FilesScriptedStream(sample, canSeek: false);
		Assert.Equal(10, stream.Read(new byte[10], 0, 10));

		await WriteStream(stream, path);

		Assert.Equal(sample[10..], await File.ReadAllBytesAsync(path));
		Assert.Equal(0, stream.SeekCalls);
	}

	[Fact]
	public async Task WriteStreamReportsGrowingProgressAndAFinalReport()
	{
		var path = NewFilePath();
		var progress = new FilesRecordingProgress();

		await WriteStream(new FilesScriptedStream(sample), path, progress);

		var values = progress.Values;
		Assert.True(values.Count >= 2, $"Expected the progress of each chunk and the final report, got {values.Count} values.");
		Assert.Equal(values.Order(), values);
		Assert.Equal(values.Max(), values[^1]);
		Assert.All(values, value => Assert.True(value > 0));
	}

	[Fact]
	public async Task WriteStreamThrowsTheExceptionThatReadingTheStreamThrew()
	{
		var failure = new IOException("The source failed.");

		var thrown = await Assert.ThrowsAsync<IOException>(() => WriteStream(new FilesScriptedStream(sample, canSeek: false) { FailAtRead = 1, FailureOnRead = failure }, NewFilePath()));

		Assert.Same(failure, thrown);
	}

	[Fact]
	public async Task WriteStreamStopsWhenTheTokenIsCanceledWhileTheStreamIsRead()
	{
		using var cancellation = new CancellationTokenSource();

		await Assert.ThrowsAsync<OperationCanceledException>(() => WriteStream(new FilesScriptedStream(sample) { CancelAtRead = 2, CancelSource = cancellation }, NewFilePath(), null, cancellation.Token));
	}

	[Fact]
	public async Task WriteStreamStopsWhenTheTokenIsCanceledBetweenTwoBlocks()
	{
		using var cancellation = new CancellationTokenSource();
		var path = NewFilePath();
		var content = Enumerable.Range(0, 20_000).Select(i => (byte)(i % 251)).ToArray();
		// The token is canceled inside the second read, which still returns its block: only WriteStream's own use of the token stops the
		// copy. The token is handed to FileStream.WriteAsync, which checks it before it writes anything, so the second block is never
		// written and no third read is made.
		var stream = new FilesScriptedStream(content) { CancelDuringRead = 2, CancelSource = cancellation };

		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => WriteStream(stream, path, null, cancellation.Token));

		Assert.Equal(2, stream.ReadCalls);
		Assert.Equal(2, stream.ReadsWithData);
		Assert.Equal(content[..4096], await File.ReadAllBytesAsync(path));
	}

	[Fact]
	public async Task WriteStreamDoesNotReadAgainWhenTheTokenIsCanceledAfterABlockWasWritten()
	{
		using var cancellation = new CancellationTokenSource();
		var path = NewFilePath();
		var content = Enumerable.Range(0, 20_000).Select(i => (byte)(i % 251)).ToArray();
		var stream = new FilesScriptedStream(content);
		// The report of the first block cancels the token, between the write of that block and the next read: the token that WriteStream
		// hands to that read stops the copy (the stream honors it), before a second block is read.
		var progress = new FilesRecordingProgress { OnReport = _ => cancellation.Cancel() };

		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => WriteStream(stream, path, progress, cancellation.Token));

		Assert.Equal(2, stream.ReadCalls);
		Assert.Equal(1, stream.ReadsWithData);
		Assert.Equal(content[..4096], await File.ReadAllBytesAsync(path));
	}

	[Fact]
	public async Task WriteStreamCopiesANonSeekableStreamWhileReportingProgress()
	{
		var path = NewFilePath();
		var progress = new FilesRecordingProgress();

		await WriteStream(new FilesScriptedStream(sample, canSeek: false), path, progress);

		Assert.Equal(sample, await File.ReadAllBytesAsync(path));
		Assert.Equal(100, progress.Values[^1]);
	}

	[Fact]
	public async Task WriteStreamReportsTheProgressOfEachBlockInPercent()
	{
		var progress = new FilesRecordingProgress();

		await WriteStream(new FilesScriptedStream(sample), NewFilePath(), progress);

		// Blocks of 4096 bytes: 4096 of 10000 bytes is 40.96 %, 8192 is 81.92 %.
		var values = progress.Values;
		Assert.Equal(40.96, values[0], 2);
		Assert.Equal(81.92, values[1], 2);
		Assert.All(values, value => Assert.InRange(value, 0, 100));
		Assert.Equal(100, values[^1]);
	}

	[Fact]
	public async Task WriteStreamReportsACompleteCopyOnlyOnceAtTheEnd()
	{
		var progress = new FilesRecordingProgress();

		await WriteStream(new FilesScriptedStream(sample), NewFilePath(), progress);

		var values = progress.Values;
		Assert.Equal(100, values[^1]);
		Assert.All(values.SkipLast(1), value => Assert.True(value < 100, $"The copy reported {string.Join(", ", values)}."));
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task WriteStreamDoesNotReportACompleteCopyWhenItFailsOrIsCanceled(bool canceled)
	{
		using var cancellation = new CancellationTokenSource();
		var progress = new FilesRecordingProgress();
		var stream = canceled
			? new FilesScriptedStream(sample) { CancelDuringRead = 2, CancelSource = cancellation }
			: new FilesScriptedStream(sample) { FailAtRead = 2, FailureOnRead = new IOException("The source failed.") };

		var thrown = await Record.ExceptionAsync(() => WriteStream(stream, NewFilePath(), progress, cancellation.Token));

		Assert.True(canceled ? thrown is OperationCanceledException : thrown is IOException, $"Unexpected outcome: {thrown}");
		var values = progress.Values;
		Assert.True(values.Count is 0 || values[^1] < 100, $"A {(canceled ? "canceled" : "failed")} copy reported {string.Join(", ", values)}.");
	}

	[Fact]
	public async Task WriteStreamFailsWhenThePathIsAFolder()
	{
		var folder = NewFilePath();
		Directory.CreateDirectory(folder);

		await Assert.ThrowsAsync<UnauthorizedAccessException>(() => WriteStream(new FilesScriptedStream(sample), folder));
	}

	string NewFilePath()
	{
		var folder = Path.Combine(FileSystem.CacheDirectory, $"files-writestream-{Guid.NewGuid():N}");
		Directory.CreateDirectory(folder);
		AddCleanup(() =>
		{
			if (Directory.Exists(folder))
			{
				Directory.Delete(folder, recursive: true);
			}
		});

		return Path.Combine(folder, "saved.bin");
	}

	static Task WriteStream(Stream stream, string path, IProgress<double>? progress = null, CancellationToken token = default)
	{
		var method = typeof(FileSaverImplementation).GetMethod("WriteStream", BindingFlags.NonPublic | BindingFlags.Static)
			?? throw new InvalidOperationException("FileSaverImplementation.WriteStream was not found.");

		try
		{
			return (Task)method.Invoke(null, [stream, path, progress, token])!;
		}
		catch (TargetInvocationException ex) when (ex.InnerException is not null)
		{
			ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
			throw;
		}
	}
}
#endif