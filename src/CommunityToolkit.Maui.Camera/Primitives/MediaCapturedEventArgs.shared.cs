namespace CommunityToolkit.Maui.Core;

/// <summary>
/// Event args containing all contextual information related to media capture event.
/// </summary>
/// <param name="stream">The <see cref="Stream"/> pointing to the newly captured media.</param>
/// <exception cref="ArgumentNullException">Thrown when <paramref name="stream"/> is <see langword="null"/>.</exception>
public class MediaCapturedEventArgs(Stream stream) : EventArgs
{
	/// <summary>
	/// Gets the <see cref="Stream"/> pointing to the newly captured media.
	/// </summary>
	public Stream Media { get; } = stream ?? throw new ArgumentNullException(nameof(stream));
}