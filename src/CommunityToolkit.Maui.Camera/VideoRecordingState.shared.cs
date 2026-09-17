namespace CommunityToolkit.Maui.Core;

sealed class VideoRecordingState
{
	readonly TaskCompletionSource finalizedTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
	readonly TaskCompletionSource startedTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

	public Task Finalized => finalizedTcs.Task;

	public Task Started => startedTcs.Task;

	public SemaphoreSlim StopSemaphore { get; } = new(1, 1);

	public static VideoRecordingState? TryStart(ref VideoRecordingState? currentState)
	{
		var recordingState = new VideoRecordingState();
		return Interlocked.CompareExchange(ref currentState, recordingState, null) is null ? recordingState : null;
	}

	public void OnStarted()
	{
		startedTcs.TrySetResult();
	}

	public void OnFinalized(Exception startException)
	{
		startedTcs.TrySetException(startException);
		finalizedTcs.TrySetResult();
	}

	public void CancelPendingTasks()
	{
		startedTcs.TrySetCanceled();
		finalizedTcs.TrySetCanceled();
	}
}