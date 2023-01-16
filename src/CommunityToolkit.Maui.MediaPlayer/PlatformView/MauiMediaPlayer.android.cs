using Android.Content;
using Android.Views;
using Android.Widget;
using AndroidX.CoordinatorLayout.Widget;
using Com.Google.Android.Exoplayer2.UI;

namespace CommunityToolkit.Maui.MediaPlayer.PlatformView;

/// <summary>
/// The user-interface element that represents the <see cref="MediaPlayer"/> on Android.
/// </summary>
public class MauiMediaPlayer : CoordinatorLayout
{
	readonly StyledPlayerView playerView;

	/// <summary>
	/// Initializes a new instance of the <see cref="MauiMediaPlayer"/> class.
	/// </summary>
	/// <param name="context">The application's <see cref="Context"/>.</param>
	/// <param name="playerView">The <see cref="StyledPlayerView"/> that acts as the platform media player.</param>
	public MauiMediaPlayer(Context context, StyledPlayerView playerView) : base(context)
	{
		this.playerView = playerView;

		// Create a RelativeLayout for sizing the video
		RelativeLayout relativeLayout = new(context)
		{
			LayoutParameters = new CoordinatorLayout.LayoutParams(LayoutParams.MatchParent, LayoutParams.MatchParent)
			{
				Gravity = (int)GravityFlags.Center
			}
		};

		relativeLayout.AddView(playerView);
		AddView(relativeLayout);
	}

	/// <summary>
	/// Releases the unmanaged resources used by the <see cref="MediaPlayer"/> and optionally releases the managed resources.
	/// </summary>
	/// <param name="disposing"><see langword="true"/> to release both managed and unmanaged resources; <see langword="false"/> to release only unmanaged resources.</param>
	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			if (playerView is not null)
			{
				// https://github.com/google/ExoPlayer/issues/1855#issuecomment-251041500
				playerView.Player?.Release();
				playerView.Player?.Dispose();
				playerView.Dispose();
			}
		}

		base.Dispose(disposing);
	}
}