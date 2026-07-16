using Microsoft.Maui.Dispatching;

namespace CommunityToolkit.Maui.Extensions;

static class DispatcherExtensions
{
	public static void DispatchIfRequired(this IDispatcher? dispatcher, Action action)
	{
		ArgumentNullException.ThrowIfNull(action);

		dispatcher = EnsureDispatcher(dispatcher);
		if (dispatcher.IsDispatchRequired)
		{
			if (!dispatcher.Dispatch(action))
			{
				throw new InvalidOperationException("The dispatcher was unable to queue the requested action.");
			}

			return;
		}

		action();
	}

	public static async Task DispatchIfRequiredAsync(this IDispatcher? dispatcher, Action action, CancellationToken token = default)
	{
		ArgumentNullException.ThrowIfNull(action);
		token.ThrowIfCancellationRequested();

		dispatcher = EnsureDispatcher(dispatcher);
		if (!dispatcher.IsDispatchRequired)
		{
			action();
			return;
		}

		var taskCompletionSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		using var cancellationTokenRegistration = token.Register(static state =>
			((TaskCompletionSource)state!).TrySetCanceled(), taskCompletionSource);

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

	static IDispatcher EnsureDispatcher(IDispatcher? dispatcher)
	{
		if (dispatcher is not null)
		{
			return dispatcher;
		}

		if (Dispatcher.GetForCurrentThread() is IDispatcher currentThreadDispatcher)
		{
			return currentThreadDispatcher;
		}

		if (Application.Current?.Dispatcher is IDispatcher applicationDispatcher)
		{
			return applicationDispatcher;
		}

		throw new InvalidOperationException("The dispatcher was not found and the current application does not have a dispatcher.");
	}
}
