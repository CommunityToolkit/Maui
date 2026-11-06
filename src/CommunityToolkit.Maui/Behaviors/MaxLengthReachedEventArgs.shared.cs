namespace CommunityToolkit.Maui.Behaviors;

/// <summary>
/// Container object for the event arguments that are provided when the <see cref="MaxLengthReachedBehavior.MaxLengthReached"/> event is triggered.
/// </summary>
/// <remarks>
/// Constructor to create a new instance of <see cref="MaxLengthReachedEventArgs"/>.
/// </remarks>
/// <param name="text">The new text value as determined by the <see cref="MaxLengthReachedBehavior"/></param>
/// <exception cref="ArgumentNullException">Thrown when <paramref name="text"/> is <see langword="null"/>.</exception>
public partial class MaxLengthReachedEventArgs(string text) : EventArgs
{
	/// <summary>
	/// The new text value as determined by the <see cref="MaxLengthReachedBehavior"/>
	/// </summary>
	public string Text { get; } = text ?? throw new ArgumentNullException(nameof(text));
}