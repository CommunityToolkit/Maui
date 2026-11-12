namespace CommunityToolkit.Maui.Core;

/// <summary>
/// Event args containing all contextual information related to the error occurred event.
/// </summary>
public class ErrorOccurredEventArgs : EventArgs
{
	/// <summary>
	/// Initializes a new instance of the <see cref="ErrorOccurredEventArgs"/> class.
	/// </summary>
	/// <param name="ex">The exception that caused the event.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="ex"/> is <see langword="null"/>.</exception>
	public ErrorOccurredEventArgs(Exception ex)
	{
		ArgumentNullException.ThrowIfNull(ex);

		Exception = ex;
	}

	/// <summary>
	/// Gets the <see cref="Exception"/> of the error that occurred.
	/// </summary>
	public Exception Exception { get; }
}