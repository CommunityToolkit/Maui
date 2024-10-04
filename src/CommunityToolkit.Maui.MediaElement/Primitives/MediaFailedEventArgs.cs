namespace CommunityToolkit.Maui.Core.Primitives;

/// <summary>
/// Represents event data for when media has failed loading or playing.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="MediaFailedEventArgs"/> class.
/// </remarks>
/// <param name="errorMessage">An error message providing more information for this event.</param>
public sealed class MediaFailedEventArgs(string errorMessage) : EventArgs
{

	/// <summary>
	/// Gets a description of why the media failed to load and/or play.
	/// </summary>
	public string ErrorMessage { get; } = errorMessage;
}