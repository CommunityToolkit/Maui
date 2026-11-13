namespace CommunityToolkit.Maui.Behaviors;

/// <summary>
/// Container object for the event arguments that are provided when the <see cref="MaxLengthReachedBehavior.MaxLengthReached"/> event is triggered.
/// </summary>
public partial class MaxLengthReachedEventArgs : EventArgs
{
	/// <summary>
	/// Constructor to create a new instance of <see cref="MaxLengthReachedEventArgs"/>.
	/// </summary>
	/// <param name="text">The new text value as determined by the <see cref="MaxLengthReachedBehavior"/></param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="text"/> is <see langword="null"/>.</exception>
	public MaxLengthReachedEventArgs(string text)
	{
		ArgumentNullException.ThrowIfNull(text);

		Text = text;
	}

	/// <summary>
	/// The new text value as determined by the <see cref="MaxLengthReachedBehavior"/>
	/// </summary>
	public string Text { get; }
}