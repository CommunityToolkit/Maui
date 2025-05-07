namespace CommunityToolkit.Maui.Core;

/// <summary>
/// Event args containing all contextual information related to a media capture failure event.
/// </summary>
/// <remarks>
/// Creates a new instance of <see cref="MediaCaptureFailedEventArgs"/>.
/// </remarks>
/// <param name="failureReason">A string containing the reason why the capture attempt failed.</param>
public class MediaCaptureFailedEventArgs(string failureReason) : EventArgs
{
	/// <summary>
	/// Gets the reason why the capture attempt failed.
	/// </summary>
	public string FailureReason { get; } = failureReason;
}