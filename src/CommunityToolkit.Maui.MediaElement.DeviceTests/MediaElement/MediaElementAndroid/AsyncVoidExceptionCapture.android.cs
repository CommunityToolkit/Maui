using System.Runtime.ExceptionServices;

namespace CommunityToolkit.Maui.DeviceTests;

/// <summary>
/// Captures the exception that an <c>async void</c> method raises. An <c>async void</c> method posts its exception to the
/// <see cref="SynchronizationContext"/> that was current when it started, and the app's context rethrows it on the UI thread,
/// which ends the process. Install this context on the calling thread around the call, then inspect <see cref="Exception"/>.
/// </summary>
public sealed class AsyncVoidExceptionCapture : SynchronizationContext, IDisposable
{
	readonly SynchronizationContext? previousContext = Current;

	public AsyncVoidExceptionCapture() => SetSynchronizationContext(this);

	/// <summary>The first exception that reached the context, or null.</summary>
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