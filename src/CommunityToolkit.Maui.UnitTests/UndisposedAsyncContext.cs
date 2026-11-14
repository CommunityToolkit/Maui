using Nito.AsyncEx;
using Nito.AsyncEx.Synchronous;

namespace CommunityToolkit.Maui.UnitTests;

// AsyncContext.Run disposes its queue as soon as its thread sees the last operation end. An async void that awaits with ConfigureAwait(false),
// like AnimateCommand, ends its operation on a thread-pool thread, whose CompleteAdding can then hit the disposed queue and crash the test host.
static class UndisposedAsyncContext
{
	public static void Run(Action action)
	{
		var context = new AsyncContext();
		var task = context.Factory.Run(action);
		context.Execute();
		task.WaitAndUnwrapException();
	}

	public static void Run(Func<Task> test)
	{
		var context = new AsyncContext();
		context.SynchronizationContext.OperationStarted();
		var testTask = context.Factory.Run(test).ContinueWith(task =>
		{
			context.SynchronizationContext.OperationCompleted();
			task.WaitAndUnwrapException();
		}, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, context.Scheduler);
		context.Execute();
		testTask.WaitAndUnwrapException();
	}
}
