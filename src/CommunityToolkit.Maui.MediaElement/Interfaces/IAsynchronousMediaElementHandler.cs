namespace CommunityToolkit.Maui.Core;

/// <summary>
/// Interface that allows asynchronous completion of .NET MAUI Handlers
/// </summary>
public interface IAsynchronousMediaElementHandler
{
	/// <summary>
	/// A <see cref="TaskCompletionSource"/> to provide Handlers an asynchronous way to complete
	/// </summary>
	TaskCompletionSource SeekCompletedTCS { get; }
}