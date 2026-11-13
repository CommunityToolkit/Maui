namespace CommunityToolkit.Maui.Core;

/// <summary>
/// <see cref="LongPressCompletedEventArgs"/>
/// </summary>
public class LongPressCompletedEventArgs(object? longPressCommandParameter) : EventArgs
{
	/// <summary>
	/// Parameter
	/// </summary>
	public object? LongPressCommandParameter { get; } = longPressCommandParameter;
}