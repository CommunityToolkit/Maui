namespace CommunityToolkit.Maui.Core;

/// <summary>
/// <see cref="TouchCompletedEventArgs"/>
/// </summary>
public class TouchCompletedEventArgs(object? parameter) : EventArgs
{
	/// <summary>
	/// Gets the parameter associated with the touch event.
	/// </summary>
	public object? Parameter { get; } = parameter;
}