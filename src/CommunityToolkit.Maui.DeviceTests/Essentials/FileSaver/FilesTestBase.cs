using System.Runtime.ExceptionServices;
#if IOS || MACCATALYST
using UIKit;
#endif

namespace CommunityToolkit.Maui.DeviceTests;

public abstract partial class FilesTestBase : CommunityToolkitHandlerTestBase
{
	protected static readonly TimeSpan operationTimeout = TimeSpan.FromSeconds(15);

	readonly Stack<Func<Task>> cleanups = new();

	// An app calls FileSaver and FolderPicker from its UI thread, and a picker is only answered later by the test, so the operation is
	// returned still running instead of awaited.
	protected static async Task<Task<T>> StartOnMainThreadAsync<T>(Func<Task<T>> start)
	{
		Task<T> operation = null!;
		await InvokeOnMainThreadAsync(() => { operation = start(); });
		return operation;
	}

	// The last registered runs first, because a cleanup may still need what was set up before it was registered.
	protected void AddCleanup(Func<Task> cleanup) => cleanups.Push(cleanup);

	protected void AddCleanup(Action cleanup) => cleanups.Push(() =>
	{
		cleanup();
		return Task.CompletedTask;
	});

	// For an operation that the test may not get to answer (a failed test).
	protected CancellationToken CanceledWhenTheTestEnds()
	{
		var cancellation = new CancellationTokenSource();
		AddCleanup(() =>
		{
			cancellation.Cancel();
			cancellation.Dispose();
		});

		return cancellation.Token;
	}

	protected override async ValueTask DisposeAsyncCore()
	{
		var failures = new List<Exception>();

		try
		{
			await base.DisposeAsyncCore();
		}
		catch (Exception ex)
		{
			failures.Add(ex);
		}

		while (cleanups.Count > 0)
		{
			try
			{
				await cleanups.Pop()();
			}
			catch (Exception ex)
			{
				failures.Add(ex);
			}
		}

#if IOS || MACCATALYST
		// A picker that is still on screen would cover the next test (a test that fails before it answers the picker leaves it there).
		try
		{
			await InvokeOnMainThreadAsync(FilesApplePicker.DismissAnyPickerAsync);
		}
		catch (Exception ex)
		{
			failures.Add(ex);
		}
#endif

		if (failures.Count is 1)
		{
			ExceptionDispatchInfo.Capture(failures[0]).Throw();
		}
		else if (failures.Count > 1)
		{
			throw new AggregateException(failures);
		}
	}
}