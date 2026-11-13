using AndroidX.Media3.UI;

namespace CommunityToolkit.Maui.Core;

/// <summary>
/// Event arguments that carry the <see cref="PlayerView"/> that a MediaElement uses after it changes.
/// </summary>
public sealed class PlayerViewChangedEventArgs : EventArgs
{
	/// <summary>
	/// Initializes a new instance of the <see cref="PlayerViewChangedEventArgs"/> class.
	/// </summary>
	/// <param name="playerView">The updated <see cref="PlayerView"/>.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="playerView"/> is <see langword="null"/>.</exception>
	public PlayerViewChangedEventArgs(PlayerView playerView)
	{
		ArgumentNullException.ThrowIfNull(playerView);

		UpdatedPlayerView = playerView;
	}

	/// <summary>
	/// Gets the updated <see cref="PlayerView"/>.
	/// </summary>
	public PlayerView UpdatedPlayerView { get; }
}