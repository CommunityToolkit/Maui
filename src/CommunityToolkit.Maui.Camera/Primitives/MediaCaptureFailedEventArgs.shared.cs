namespace CommunityToolkit.Maui.Core;

/// <summary>
/// Event args containing all contextual information related to a media capture failure event.
/// </summary>
/// <remarks>
/// Creates a new instance of <see cref="MediaCaptureFailedEventArgs"/>.
/// </remarks>
/// <param name="failureReason">A string containing the reason why the capture attempt failed.</param>
/// <exception cref="ArgumentNullException">Thrown when <paramref name="failureReason"/> is <see langword="null"/>.</exception>
public class MediaCaptureFailedEventArgs(string failureReason) : EventArgs
{
	/// <summary>
	/// Gets the reason why the capture attempt failed.
	/// </summary>
	public string FailureReason { get; } = failureReason ?? throw new ArgumentNullException(nameof(failureReason));
}