using CommunityToolkit.Maui.Core;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Views;

public class VideoRecordingStateTests
{
	[Fact]
	public async Task StartedCompletesAfterNativeStart()
	{
		var recordingState = new VideoRecordingState();

		Assert.False(recordingState.Started.IsCompleted);

		recordingState.OnStarted();

		await recordingState.Started;
	}

	[Fact]
	public async Task StartedFailsWhenNativeRecordingFinishesFirst()
	{
		var recordingState = new VideoRecordingState();
		var exception = new CameraException("Recording failed.");

		recordingState.OnFinalized(exception);

		var thrownException = await Assert.ThrowsAsync<CameraException>(() => recordingState.Started);
		Assert.Same(exception, thrownException);
		await recordingState.Finalized;
	}

	[Fact]
	public async Task FinalizeAfterStartDoesNotFailStarted()
	{
		var recordingState = new VideoRecordingState();

		recordingState.OnStarted();
		recordingState.OnFinalized(new CameraException("Ignored after start."));
		recordingState.CancelPendingTasks();

		await recordingState.Started;
		await recordingState.Finalized;
	}

	[Fact]
	public async Task RepeatedNativeCallbacksCompleteExactlyOnce()
	{
		var recordingState = new VideoRecordingState();

		recordingState.OnStarted();
		recordingState.OnStarted();
		recordingState.OnFinalized(new CameraException("Ignored after start."));
		recordingState.OnFinalized(new CameraException("Ignored duplicate."));

		await recordingState.Started;
		await recordingState.Finalized;
	}

	[Fact]
	public async Task StopLockCanBeReacquired()
	{
		var recordingState = new VideoRecordingState();

		Assert.True(await recordingState.StopSemaphore.WaitAsync(0, TestContext.Current.CancellationToken));
		Assert.False(await recordingState.StopSemaphore.WaitAsync(0, TestContext.Current.CancellationToken));
		recordingState.StopSemaphore.Release();
		Assert.True(await recordingState.StopSemaphore.WaitAsync(0, TestContext.Current.CancellationToken));
		recordingState.StopSemaphore.Release();
	}

	[Fact]
	public async Task CancelledWaitDoesNotCancelNativeState()
	{
		var recordingState = new VideoRecordingState();
		using var cancellationTokenSource = new CancellationTokenSource();
		cancellationTokenSource.Cancel();

		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => recordingState.Started.WaitAsync(cancellationTokenSource.Token));

		recordingState.OnStarted();
		recordingState.OnFinalized(new CameraException("Ignored after start."));

		await recordingState.Started;
		await recordingState.Finalized;
	}

	[Fact]
	public async Task DuplicateStartIsIgnoredUntilCleanup()
	{
		VideoRecordingState? currentState = null;
		var recordingState = Assert.IsType<VideoRecordingState>(VideoRecordingState.TryStart(ref currentState));

		Assert.Null(VideoRecordingState.TryStart(ref currentState));
		recordingState.OnStarted();
		Assert.Null(VideoRecordingState.TryStart(ref currentState));
		recordingState.OnFinalized(new CameraException("Ignored after start."));
		Assert.Null(VideoRecordingState.TryStart(ref currentState));
		Assert.Same(recordingState, currentState);

		await recordingState.Started;
		await recordingState.Finalized;
	}

	[Fact]
	public async Task TeardownCancelsPendingNativeWaits()
	{
		var recordingState = new VideoRecordingState();

		recordingState.CancelPendingTasks();
		recordingState.OnStarted();
		recordingState.OnFinalized(new CameraException("Ignored after teardown."));

		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => recordingState.Started);
		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => recordingState.Finalized);
	}
}