namespace CommunityToolkit.Maui.Core.Primitives;

/// <summary>
/// Represents event data for when media has failed loading or playing.
/// </summary>
public sealed class MediaFailedEventArgs : EventArgs
{
	/// <summary>
	/// Initializes a new instance of the <see cref="MediaFailedEventArgs"/> class.
	/// </summary>
	/// <param name="errorMessage">An error message providing more information for this event.</param>
	public MediaFailedEventArgs(string errorMessage)
	{
		ErrorMessage = errorMessage;
	}

	/// <summary>
	/// Gets a description of why the media failed to load and/or play.
	/// </summary>
	public string ErrorMessage { get; }
}