namespace CommunityToolkit.Maui.Views;

/// <summary>
/// Event args containing all contextual information related to a media capture failure event.
/// </summary>
public class MediaCaptureFailedEventArgs : EventArgs
{
	/// <summary>
	/// Creates a new instance of <see cref="MediaCaptureFailedEventArgs"/>.
	/// </summary>
	/// <param name="failureReason">A string containing the reason why the capture attempt failed.</param>
	public MediaCaptureFailedEventArgs(string failureReason)
	{
		FailureReason = failureReason;
	}

	/// <summary>
	/// Gets the reason why the capture attempt failed.
	/// </summary>
	public string FailureReason { get; }
}