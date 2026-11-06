namespace CommunityToolkit.Maui.Core;

/// <summary>
/// Event args containing all contextual information related to a media capture failure event.
/// </summary>
public class MediaCaptureFailedEventArgs : EventArgs
{
	/// <summary>
	/// Creates a new instance of <see cref="MediaCaptureFailedEventArgs"/>.
	/// </summary>
	/// <param name="failureReason">A string containing the reason why the capture attempt failed.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="failureReason"/> is <see langword="null"/>.</exception>
	public MediaCaptureFailedEventArgs(string failureReason)
	{
		ArgumentNullException.ThrowIfNull(failureReason);

		FailureReason = failureReason;
	}

	/// <summary>
	/// Gets the reason why the capture attempt failed.
	/// </summary>
	public string FailureReason { get; }
}