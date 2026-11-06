using AndroidX.Media3.UI;

namespace CommunityToolkit.Maui.Core;

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

	public PlayerView UpdatedPlayerView { get; }
}