namespace CommunityToolkit.Maui.Core;

/// <summary>
/// Event args containing all contextual information related to the error occurred event.
/// </summary>
/// <param name="ex">The exception that caused the event.</param>
/// <exception cref="ArgumentNullException">Thrown when <paramref name="ex"/> is <see langword="null"/>.</exception>
public class ErrorOccurredEventArgs(Exception ex) : EventArgs
{
	/// <summary>
	/// Gets the <see cref="Exception"/> of the error that occurred.
	/// </summary>
	public Exception Exception { get; } = ex ?? throw new ArgumentNullException(nameof(ex));
}