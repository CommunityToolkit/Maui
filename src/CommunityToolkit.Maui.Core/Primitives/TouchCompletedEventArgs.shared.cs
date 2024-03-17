namespace CommunityToolkit.Maui.Core;

/// <summary>
/// <see cref="TouchGestureCompletedEventArgs"/>
/// </summary>
public class TouchGestureCompletedEventArgs(object? touchCommandParameter) : EventArgs
{
	/// <summary>
	/// Gets the parameter associated with the touch event.
	/// </summary>
	public object? TouchCommandParameter { get; } = touchCommandParameter;
}