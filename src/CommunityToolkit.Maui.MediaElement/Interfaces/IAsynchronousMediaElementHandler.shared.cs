namespace CommunityToolkit.Maui.Core;

/// <summary>
/// Interface that allows asynchronous completion of .NET MAUI Handlers
/// </summary>
public interface IAsynchronousMediaElementHandler
{
	/// <summary>
	/// A <see cref="TaskCompletionSource"/> to provide Handlers an asynchronous way to complete
	/// </summary>
	/// <remarks>
	/// <see cref="IMediaElement.SeekTo(TimeSpan, CancellationToken)"/> waits for this source. When the token of that call ends the wait first, the
	/// source is cancelled, so a seek that ends or fails later finds it already complete: complete it with
	/// <see cref="TaskCompletionSource.TrySetResult()"/> or <see cref="TaskCompletionSource.TrySetException(Exception)"/>, because
	/// <see cref="TaskCompletionSource.SetResult()"/> and <see cref="TaskCompletionSource.SetException(Exception)"/> throw
	/// <see cref="InvalidOperationException"/> for a source that is already complete.
	/// </remarks>
	TaskCompletionSource SeekCompletedTCS { get; }
}