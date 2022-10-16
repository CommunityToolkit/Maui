using Android.Content;
using Android.Views;
using Android.Widget;
using AndroidX.ConstraintLayout.Helper.Widget;
using AndroidX.CoordinatorLayout.Widget;
using Com.Google.Android.Exoplayer2;
using Com.Google.Android.Exoplayer2.Source.Dash;
using Com.Google.Android.Exoplayer2.UI;
using Com.Google.Android.Exoplayer2.Upstream;
using Microsoft.Maui.Controls;
using static Android.Provider.MediaStore;
using Color = Android.Graphics.Color;
using Uri = Android.Net.Uri;

namespace CommunityToolkit.Maui.MediaElement.PlatformView;

public class MauiMediaElement : CoordinatorLayout
{
	readonly PlayerView playerView;
	SimpleExoPlayer player;
	readonly Context context;
	bool isPrepared;
	MediaElement video;

	public MauiMediaElement(Context context, MediaElement mediaElement)
		 : base(context)
	{
		this.context = context;
		this.video = mediaElement;

		// Create a RelativeLayout for sizing the video
		RelativeLayout relativeLayout = new RelativeLayout(context)
		{
			LayoutParameters = new CoordinatorLayout.LayoutParams(LayoutParams.MatchParent, LayoutParams.MatchParent)
			{
				Gravity = (int)GravityFlags.Center
			}
		};

		player = new SimpleExoPlayer.Builder(context).Build() ?? throw new NullReferenceException();
		
		// TODO: should we use this over the check in SetSource?
		//player.PlayWhenReady = mediaElement.AutoPlay;

		playerView = new PlayerView(context)
		{
			Player = player,
			UseController = false,
			ControllerAutoShow = false,
			LayoutParameters = new RelativeLayout.LayoutParams(LayoutParams.MatchParent, LayoutParams.MatchParent),
		};

		relativeLayout.AddView(playerView);
		AddView(relativeLayout);
	}

	public void UpdateShowsPlaybackControls()
	{
		// TODO
	}

	public void UpdateSource()
	{
		isPrepared = false;
		bool hasSetSource = false;

		if (video.Source is UriMediaSource)
		{
			string uri = (video.Source as UriMediaSource)!.Uri!.AbsoluteUri;
			if (!string.IsNullOrWhiteSpace(uri))
			{
				var httpDataSourceFactory = new DefaultHttpDataSource.Factory();
				var mediaSource = new DashMediaSource.Factory(httpDataSourceFactory)
					.CreateMediaSource(MediaItem.FromUri(uri));

				player.SetMediaSource(mediaSource);
				player.Prepare();

				hasSetSource = true;
			}
		}
		else if (video.Source is FileMediaSource)
		{
			//string filename = (video.Source as FileMediaSource)!.File!;
			//if (!string.IsNullOrWhiteSpace(filename))
			//{
			//	player.SetVideoPath(filename);
			//	hasSetSource = true;
			//}
		}
		//else if (video.Source is ResourceVideoSource)
		//{
		//	string package = Context.PackageName;
		//	string path = (_video.Source as ResourceVideoSource).Path;
		//	if (!string.IsNullOrWhiteSpace(path))
		//	{
		//		string assetFilePath = "content://" + package + "/" + path;
		//		_videoView.SetVideoPath(assetFilePath);
		//		hasSetSource = true;
		//	}
		//}

		if (hasSetSource && video.AutoPlay)
		{
			player.Play();
		}
	}

	public void UpdateSpeed()
	{
		// TODO
	}

	public void UpdateVolume()
	{
		// TODO
	}
}

