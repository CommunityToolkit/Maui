namespace CommunityToolkit.Maui.Core;

/// <summary>
/// <see cref="LongPressCompletedEventArgs"/>
/// </summary>
public class LongPressCompletedEventArgs : EventArgs
{
	/// <summary>
	/// Constructor for <see cref="LongPressCompletedEventArgs"/>
	/// </summary>
	/// <param name="parameter"></param>
	public LongPressCompletedEventArgs(object? parameter)
		=> Parameter = parameter;

	/// <summary>
	/// Parameter
	/// </summary>
	public object? Parameter { get; }
}