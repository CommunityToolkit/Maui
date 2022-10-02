namespace CommunityToolkit.Maui.Behaviors;

public class LongPressCompletedEventArgs : EventArgs
{
	internal LongPressCompletedEventArgs(object? parameter)
		=> Parameter = parameter;

	public object? Parameter { get; }
}