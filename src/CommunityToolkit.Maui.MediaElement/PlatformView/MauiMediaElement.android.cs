using Android.Content;
using Android.Views;
using Android.Widget;
using AndroidX.ConstraintLayout.Helper.Widget;
using AndroidX.CoordinatorLayout.Widget;
using Com.Google.Android.Exoplayer2.Source.Dash;
using Com.Google.Android.Exoplayer2.UI;
using Com.Google.Android.Exoplayer2;
using Com.Google.Android.Exoplayer2.Upstream;
using Microsoft.Maui.Controls;
using static Android.Provider.MediaStore;
using Color = Android.Graphics.Color;
using Uri = Android.Net.Uri;
using Android.Media.Session;

namespace CommunityToolkit.Maui.MediaElement.PlatformView;

public class MauiMediaElement : CoordinatorLayout
{
	PlayerView? playerView;
	SimpleExoPlayer? player;
	readonly Context context;
	bool isPrepared;
	MediaElement mediaElement;

	public MauiMediaElement(Context context, MediaElement mediaElement)
		 : base(context)
	{
		this.context = context;
		this.mediaElement = mediaElement;

		// Create a RelativeLayout for sizing the video
		RelativeLayout relativeLayout = new RelativeLayout(context)
		{
			LayoutParameters = new CoordinatorLayout.LayoutParams(LayoutParams.MatchParent, LayoutParams.MatchParent)
			{
				Gravity = (int)GravityFlags.Center
			}
		};

		player = new SimpleExoPlayer.Builder(context).Build() ?? throw new NullReferenceException();

		//TODO: should we use this over the check in SetSource?
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

		player.PlaybackStateChanged += Player_PlaybackStateChanged;
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			if (player is not null)
			{
				player.PlaybackStateChanged -= Player_PlaybackStateChanged;
			}

			if (playerView is not null)
			{
				playerView.Dispose();
				playerView = null;
			}

			player = null;
		}

		base.Dispose(disposing);
	}

	public void PlayRequested(TimeSpan position)
	{
		// TODO do something with position
		if (player is null)
		{
			return;
		}

		player.Play();
	}

	public void PauseRequested(TimeSpan position)
	{
		// TODO do something with position
		if (player is null)
		{
			return;
		}

		player.Pause();
	}

	public void StopRequested(TimeSpan position)
	{
		// TODO do something with position
		if (player is null)
		{
			return;
		}

		// Stops and releases the media player, do a reset so that the media can be played again
		player.Stop(true);
	}

	public void UpdateIsLooping()
	{
		if (mediaElement is null || player is null)
		{
			return;
		}

		player.RepeatMode = mediaElement.IsLooping ? Player.RepeatModeOne : Player.RepeatModeOff;
	}

	public void UpdatePosition()
	{
		if (mediaElement is null || player is null)
		{
			return;
		}

		if (Math.Abs(player.CurrentPosition - mediaElement.Position.TotalMilliseconds) > 1000)
		{
			player.SeekTo((long)mediaElement.Position.TotalMilliseconds);
		}
	}

	public void UpdateShowsPlaybackControls()
	{
		if (mediaElement is null || playerView is null)
		{
			return;
		}

		playerView.UseController = mediaElement.ShowsPlaybackControls;
	}

	public void UpdateSource()
	{
		isPrepared = false;
		bool hasSetSource = false;

		if (player is null)
		{
			return;
		}

		if (mediaElement.Source is UriMediaSource)
		{
			string uri = (mediaElement.Source as UriMediaSource)!.Uri!.AbsoluteUri;
			if (!string.IsNullOrWhiteSpace(uri))
			{
				//var httpDataSourceFactory = new DefaultHttpDataSource.Factory();
				//var mediaSource = new DashMediaSource.Factory(httpDataSourceFactory)
				//.CreateMediaSource(MediaItem.FromUri(uri));

				//player.SetMediaSource(mediaSource);


				player.SetMediaItem(MediaItem.FromUri(Uri.Parse(uri)));
				player.Prepare();

				hasSetSource = true;
			}
		}
		else if (mediaElement.Source is FileMediaSource)
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

		if (hasSetSource && mediaElement.AutoPlay)
		{
			player.Play();
		}
	}

	public void UpdateSpeed()
	{
		if (mediaElement is null || player is null)
		{
			return;
		}

		if (mediaElement.Speed > 0)
		{
			player.SetPlaybackSpeed((float)mediaElement.Speed);
			player.Play();
		}
		else
		{
			player.Pause();
		}
	}

	public void UpdateStatus()
	{
		if (mediaElement is null || player is null)
		{
			return;
		}

		var videoStatus = MediaElementState.Closed;

		switch ((PlaybackStateCode)player.PlaybackState)
		{
			case PlaybackStateCode.Playing:
				videoStatus = MediaElementState.Playing;
				break;

			case PlaybackStateCode.Paused:
				videoStatus = MediaElementState.Paused;
				break;

			case PlaybackStateCode.Stopped:
				videoStatus = MediaElementState.Stopped;
				break;
		}

		mediaElement.CurrentState = videoStatus;

		mediaElement.Position = TimeSpan.FromMilliseconds(player.CurrentPosition);
	}

	public void UpdateVolume()
	{
		if (mediaElement is null || player is null)
		{
			return;
		}

		player.Volume = (float)mediaElement.Volume;
	}

	void Player_PlaybackStateChanged(object? sender, Com.Google.Android.Exoplayer2.Analytics.PlaybackStateChangedEventArgs e)
	{
		if (player is null)
		{
			return;
		}

		if (e.State == Player.StateReady)
		{
			isPrepared = true;
			mediaElement.Duration = TimeSpan.FromMilliseconds(player.Duration);
		}
	}
}

