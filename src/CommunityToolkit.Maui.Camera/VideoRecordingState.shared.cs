namespace CommunityToolkit.Maui.Core;

sealed class VideoRecordingState
{
	readonly TaskCompletionSource finalizedTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
	readonly TaskCompletionSource startedTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
	int hasStopOwner;

	public Task Finalized => finalizedTcs.Task;

	public Task Started => startedTcs.Task;

	public void OnFinalized(Exception startException)
	{
		startedTcs.TrySetException(startException);
		finalizedTcs.TrySetResult();
	}

	public void OnStarted()
	{
		startedTcs.TrySetResult();
	}

	public bool TryOwnStop()
	{
		return Interlocked.CompareExchange(ref hasStopOwner, 1, 0) is 0;
	}
}