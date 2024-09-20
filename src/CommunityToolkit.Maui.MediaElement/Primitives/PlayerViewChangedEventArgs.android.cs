using AndroidX.Media3.UI;

namespace CommunityToolkit.Maui.Primitives;

public sealed class PlayerViewChangedEventArgs(PlayerView playerView) : EventArgs
{
	public PlayerView UpdatedPlayerView { get; } = playerView;
}
