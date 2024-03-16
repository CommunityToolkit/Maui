namespace CommunityToolkit.Maui.Core;

/// <summary>
/// <see cref="LongPressCompletedEventArgs"/>
/// </summary>
public class LongPressCompletedEventArgs(object? parameter) : EventArgs
{
	/// <summary>
	/// Parameter
	/// </summary>
	public object? Parameter { get; } = parameter;
}