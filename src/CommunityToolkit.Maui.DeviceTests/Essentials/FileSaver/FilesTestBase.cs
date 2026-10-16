using System.Runtime.ExceptionServices;
#if IOS || MACCATALYST
using UIKit;
#endif

namespace CommunityToolkit.Maui.DeviceTests;

/// <summary>
/// Base class of the FileSaver, FolderPicker, Badge, ActivityResultManager and Android storage tests. On top of the app's base class
/// it starts operations on the UI thread without awaiting them (a picker only completes when the test answers it), bounds every wait,
/// and runs the cleanups that the tests register, last registered first, even when the test failed.
/// </summary>
public abstract partial class FilesTestBase : CommunityToolkitHandlerTestBase
{
	/// <summary>How long a test waits for something the toolkit is expected to do. A picker test never waits on a person.</summary>
	protected static readonly TimeSpan operationTimeout = TimeSpan.FromSeconds(15);

	readonly Stack<Func<Task>> cleanups = new();

	/// <summary>
	/// Calls <paramref name="start"/> on the UI thread and returns the operation it started, which is still running. An app calls
	/// FileSaver and FolderPicker from its UI thread, and a picker is only answered later by the test.
	/// </summary>
	protected static async Task<Task<T>> StartOnMainThreadAsync<T>(Func<Task<T>> start)
	{
		Task<T> operation = null!;
		await InvokeOnMainThreadAsync(() => { operation = start(); });
		return operation;
	}

	/// <summary>Registers work to undo when the test ends (the last registered runs first).</summary>
	protected void AddCleanup(Func<Task> cleanup) => cleanups.Push(cleanup);

	/// <summary>Registers work to undo when the test ends (the last registered runs first).</summary>
	protected void AddCleanup(Action cleanup) => cleanups.Push(() =>
	{
		cleanup();
		return Task.CompletedTask;
	});

	/// <summary>A token that is canceled when the test ends, for an operation that the test may not get to answer (a failed test).</summary>
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