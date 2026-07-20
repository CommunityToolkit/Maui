using Microsoft.Maui.Dispatching;

namespace CommunityToolkit.Maui.Core.Extensions;

static class DispatcherExtensions
{
	public static void DispatchIfRequired(this IDispatcher dispatcher, Action action)
	{
		ArgumentNullException.ThrowIfNull(dispatcher);
		ArgumentNullException.ThrowIfNull(action);

		if (!dispatcher.IsDispatchRequired)
		{
			action();
		}
		else
		{
			if (!dispatcher.Dispatch(action))
			{
				throw new InvalidOperationException("The dispatcher was unable to queue the requested action.");
			}
		}
	}

	public static async Task DispatchIfRequiredAsync(this IDispatcher dispatcher, Action action, CancellationToken token = default)
	{
		ArgumentNullException.ThrowIfNull(dispatcher);
		ArgumentNullException.ThrowIfNull(action);

		token.ThrowIfCancellationRequested();

		if (!dispatcher.IsDispatchRequired)
		{
			action();
			return;
		}

		var taskCompletionSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		await using var cancellationTokenRegistration = token.Register(static state =>
			((TaskCompletionSource?)state)?.TrySetCanceled(), taskCompletionSource);

		if (!dispatcher.Dispatch(() =>
		    {
			    if (token.IsCancellationRequested)
			    {
				    taskCompletionSource.TrySetCanceled(token);
				    return;
			    }

			    try
			    {
				    action();
				    taskCompletionSource.TrySetResult();
			    }
			    catch (Exception ex)
			    {
				    taskCompletionSource.TrySetException(ex);
			    }
		    }))
		{
			taskCompletionSource.TrySetException(new InvalidOperationException("The dispatcher was unable to queue the requested action."));
		}

		await taskCompletionSource.Task.ConfigureAwait(false);
	}
}