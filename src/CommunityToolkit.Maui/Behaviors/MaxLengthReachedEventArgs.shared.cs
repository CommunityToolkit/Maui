namespace CommunityToolkit.Maui.Behaviors;

/// <summary>
/// Container object for the event arguments that are provided when the <see cref="MaxLengthReachedBehavior.MaxLengthReached"/> event is triggered.
/// </summary>
public class MaxLengthReachedEventArgs : EventArgs
{
	/// <summary>
	/// The new text value as determined by the <see cref="MaxLengthReachedBehavior"/>
	/// </summary>
	public string Text { get; }

	/// <summary>
	/// Constructor to create a new instance of <see cref="MaxLengthReachedEventArgs"/>.
	/// </summary>
	/// <param name="text">The new text value as determined by the <see cref="MaxLengthReachedBehavior"/></param>
	public MaxLengthReachedEventArgs(string text)
		=> Text = text;
}