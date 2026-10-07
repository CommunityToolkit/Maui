namespace CommunityToolkit.Maui.Core;

/// <summary>
/// Event args containing all contextual information related to the error occurred event.
/// </summary>
/// <param name="ex">The <see cref="Exception"/>exception.</param>
public class ErrorOccurredEventArgs(Exception ex) : EventArgs
{
	/// <summary>
	/// Gets the <see cref="Exception"/> of the error that occurred.
	/// </summary>
	public Exception Exception { get; } = ex;
}