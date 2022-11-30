using Android.Content;
using Android.Views;
using Android.Widget;
using AndroidX.CoordinatorLayout.Widget;
using Com.Google.Android.Exoplayer2.UI;

namespace CommunityToolkit.Maui.MediaElement.PlatformView;

public class MauiMediaElement : CoordinatorLayout
{
	StyledPlayerView? playerView;

	public MauiMediaElement(Context context, StyledPlayerView playerView)
		 : base(context)
	{
		// Create a RelativeLayout for sizing the video
		RelativeLayout relativeLayout = new(context)
		{
			LayoutParameters = new CoordinatorLayout.LayoutParams(LayoutParams.MatchParent, LayoutParams.MatchParent)
			{
				Gravity = (int)GravityFlags.Center
			}
		};

		this.playerView = playerView;

		relativeLayout.AddView(playerView);
		AddView(relativeLayout);
	}

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
				playerView = null;
			}
		}

		base.Dispose(disposing);
	}
}