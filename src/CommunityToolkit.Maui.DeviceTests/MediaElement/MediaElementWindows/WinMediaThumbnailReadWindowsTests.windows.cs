using System.Runtime.InteropServices;

namespace CommunityToolkit.Maui.DeviceTests;

[Category(TestCategory.MediaMetadataWindows)]
[Collection(TestCollections.DisableParallelization)]
public class WinMediaThumbnailReadWindowsTests : WinMediaTestBase
{
	[Theory]
	[InlineData(unchecked((int)0x80070002))]
	[InlineData(unchecked((int)0x80010108))]
	public async Task ThumbnailReadRetriesATransientFailureAndReturnsTheNextRead(int hresult)
	{
		var transientFailure = TransientFailure(hresult);
		var reads = 0;
		var retriedFailures = new List<(int Attempt, Exception Failure)>();

		var bytes = await WinMediaAssets.ReadThumbnailAsync(
			() => ++reads is 1 ? throw transientFailure : Task.FromResult<byte[]>([1, 2, 3]),
			(attempt, failure) => retriedFailures.Add((attempt, failure)));

		AotSafeAssert.SequenceEqual(new byte[] { 1, 2, 3 }, bytes);
		Assert.Equal(2, reads);
		var retried = Assert.Single(retriedFailures);
		Assert.Equal(1, retried.Attempt);
		Assert.Same(transientFailure, retried.Failure);
	}

	[Fact]
	public async Task ThumbnailReadGivesUpAfterTheLastAttemptWithItsFailure()
	{
		var reads = 0;

		await Assert.ThrowsAsync<FileNotFoundException>(() => WinMediaAssets.ReadThumbnailAsync(() =>
		{
			reads++;
			throw TransientFailure(unchecked((int)0x80070002));
		}));

		Assert.Equal(WinMediaAssets.ThumbnailReadAttempts, reads);
	}

	[Fact]
	public async Task ThumbnailReadDoesNotRetryAFailureThatIsNotTransient()
	{
		var reads = 0;

		await Assert.ThrowsAsync<TimeoutException>(() => WinMediaAssets.ReadThumbnailAsync(() =>
		{
			reads++;
			throw new TimeoutException();
		}));

		Assert.Equal(1, reads);
	}

	[Fact]
	public async Task DisplayUpdaterProbeFailsInsteadOfSkippingWhenItsOwnArtworkCannotBeReadBack()
	{
		await RequireDisplayUpdaterAsync();
		var reads = 0;

		var failure = await Assert.ThrowsAsync<InvalidOperationException>(() => WinMediaProbe.ProbeDisplayUpdaterAsync(_ =>
		{
			reads++;
			throw TransientFailure(unchecked((int)0x80070002));
		}));

		Assert.IsType<FileNotFoundException>(failure.InnerException);
		Assert.Equal(WinMediaAssets.ThumbnailReadAttempts, reads);
		Assert.Contains("artwork server received", failure.Message, StringComparison.Ordinal);
	}

	[Fact]
	public async Task DisplayUpdaterProbeRecoversWhenTheFirstReadBackFailsTransiently()
	{
		await RequireDisplayUpdaterAsync();
		var reads = 0;

		var reason = await WinMediaProbe.ProbeDisplayUpdaterAsync(thumbnail =>
		{
			reads++;
			return reads is 1
				? throw TransientFailure(unchecked((int)0x80070002))
				: Task.FromResult(WinMediaAssets.ReadArtworkBytes());
		});

		Assert.Null(reason);
		Assert.Equal(2, reads);
	}

	[Fact]
	public async Task DisplayUpdaterProbeStillSkipsWhenTheThumbnailDeliversOtherBytes()
	{
		await RequireDisplayUpdaterAsync();

		var reason = await WinMediaProbe.ProbeDisplayUpdaterAsync(_ => Task.FromResult<byte[]>([1, 2, 3]));

		Assert.StartsWith("the thumbnail read back from the DisplayUpdater delivers 3 bytes", reason, StringComparison.Ordinal);
	}

	static Exception TransientFailure(int hresult) =>
		hresult == unchecked((int)0x80070002) ? new FileNotFoundException("Unable to find the specified file.") : new COMException("The server died.", hresult);
}