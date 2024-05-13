namespace CommunityToolkit.Maui.Views;

/// <summary>
/// Event args containing all contextual information related to media capture event.
/// </summary>
/// <param name="stream">The <see cref="Stream"/> pointing to the newly captured media.</param>
public class MediaCapturedEventArgs(Stream stream) : EventArgs
{
	/// <summary>
	/// Gets the <see cref="Stream"/> pointing to the newly captured media.
	/// </summary>
	public Stream Media { get; } = stream;
}