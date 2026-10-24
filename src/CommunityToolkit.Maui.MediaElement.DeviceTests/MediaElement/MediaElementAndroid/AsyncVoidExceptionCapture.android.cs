using System.Runtime.ExceptionServices;

namespace CommunityToolkit.Maui.DeviceTests;

// An async void method posts its exception to the SynchronizationContext that was current when it started, and the app's context rethrows
// it on the UI thread, which ends the process.
public sealed class AsyncVoidExceptionCapture : SynchronizationContext, IDisposable
{
	readonly SynchronizationContext? previousContext = Current;

	public AsyncVoidExceptionCapture() => SetSynchronizationContext(this);

	public Exception? Exception { get; private set; }

	public override void Post(SendOrPostCallback d, object? state)
	{
		try
		{
			d(state);
		}
		catch (Exception ex)
		{
			// ThrowAsync wraps the original exception in an ExceptionDispatchInfo that rethrows it.
			Exception ??= ex;
		}
	}

	public override void Send(SendOrPostCallback d, object? state) => Post(d, state);

	public void Dispose() => SetSynchronizationContext(previousContext);
}