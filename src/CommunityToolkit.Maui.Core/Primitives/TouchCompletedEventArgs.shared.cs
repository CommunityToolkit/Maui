namespace CommunityToolkit.Maui.Core;

/// <summary>
/// <see cref="TouchCompletedEventArgs"/>
/// </summary>
public class TouchCompletedEventArgs : EventArgs

{
	/// <summary>
	/// Initializes a new instance of the <see cref="TouchCompletedEventArgs"/> class.
	/// </summary>
	public TouchCompletedEventArgs(object? parameter)
		=> Parameter = parameter;

	/// <summary>
	/// Gets the parameter associated with the touch event.
	/// </summary>
	public object? Parameter { get; }
}