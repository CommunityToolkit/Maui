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
			dispatcher.Dispatch(action);
			return;
		}

		action();
	}

	public static Task DispatchIfRequiredAsync(this IDispatcher? dispatcher, Action action)
	{
		ArgumentNullException.ThrowIfNull(action);

		dispatcher = EnsureDispatcher(dispatcher);
		if (!dispatcher.IsDispatchRequired)
		{
			action();
			return Task.CompletedTask;
		}

		var taskCompletionSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		if (!dispatcher.Dispatch(() =>
		{
			try
			{
				action();
				taskCompletionSource.SetResult();
			}
			catch (Exception ex)
			{
				taskCompletionSource.SetException(ex);
			}
		}))
		{
			taskCompletionSource.SetException(new InvalidOperationException("The dispatcher was unable to queue the requested action."));
		}

		return taskCompletionSource.Task;
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
