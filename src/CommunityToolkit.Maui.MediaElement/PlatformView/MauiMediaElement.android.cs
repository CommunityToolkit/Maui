using Android.Content;
using Android.Media.Session;
using Android.Views;
using Android.Widget;
using AndroidX.CoordinatorLayout.Widget;
using Com.Google.Android.Exoplayer2;
using Com.Google.Android.Exoplayer2.Audio;
using Com.Google.Android.Exoplayer2.Metadata;
using Com.Google.Android.Exoplayer2.Text;
using Com.Google.Android.Exoplayer2.Trackselection;
using Com.Google.Android.Exoplayer2.UI;
using Com.Google.Android.Exoplayer2.Video;
using static Com.Google.Android.Exoplayer2.IPlayer;

namespace CommunityToolkit.Maui.MediaElement.PlatformView;

public class MauiMediaElement : CoordinatorLayout, IPlayer.IListener
{
	StyledPlayerView? playerView;
	IExoPlayer? player;
	readonly MediaElement mediaElement;

	public void OnPlayerStateChanged(bool playWhenReady, int playbackState)
	{
		if (player is null)
		{
			return;
		}

		if (playbackState == IPlayer.StateReady)
		{
			mediaElement.Duration = TimeSpan.FromMilliseconds(player.Duration);
		}
	}

	public MauiMediaElement(Context context, MediaElement mediaElement)
		 : base(context)
	{
		this.mediaElement = mediaElement;

		// Create a RelativeLayout for sizing the video
		RelativeLayout relativeLayout = new(context)
		{
			LayoutParameters = new CoordinatorLayout.LayoutParams(LayoutParams.MatchParent, LayoutParams.MatchParent)
			{
				Gravity = (int)GravityFlags.Center
			}
		};

		player = new IExoPlayer.Builder(context).Build() ?? throw new NullReferenceException();
		player.AddListener(this);

		playerView = new StyledPlayerView(context)
		{
			Player = player,
			UseController = false,
			ControllerAutoShow = false,
			LayoutParameters = new RelativeLayout.LayoutParams(LayoutParams.MatchParent, LayoutParams.MatchParent),
		};

		relativeLayout.AddView(playerView);
		AddView(relativeLayout);
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			if (player is not null)
			{
				player.RemoveListener(this);
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

		// Stops and resets the media player
		player.Stop();
		player.SeekTo(0);
		player.PlayWhenReady = false;
		player.Prepare();
	}

	public void UpdateIsLooping()
	{
		if (mediaElement is null || player is null)
		{
			return;
		}

		player.RepeatMode = mediaElement.IsLooping ? IPlayer.RepeatModeOne : IPlayer.RepeatModeOff;
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
				//Com.Google.Android.Exoplayer2.Util.Util.InferContentType()
				//var httpDataSourceFactory = new DefaultHttpDataSource.Factory();
				//var mediaSource = new DashMediaSource.Factory(httpDataSourceFactory)
				//.CreateMediaSource(MediaItem.FromUri(uri));

				//player.SetMediaSource(mediaSource);

				player.SetMediaItem(MediaItem.FromUri(uri));
				player.Prepare();

				hasSetSource = true;
			}
		}
		else if (mediaElement.Source is FileMediaSource)
		{
			string filePath = (mediaElement.Source as FileMediaSource)!.File!;
			if (!string.IsNullOrWhiteSpace(filePath))
			{
				player.SetMediaItem(MediaItem.FromUri(filePath));
				player.Prepare();

				hasSetSource = true;
			}
		}

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

	#region IPlayer.IListener implementation method stubs
	public void OnAudioAttributesChanged(AudioAttributes? audioAttributes) { }
	public void OnAudioSessionIdChanged(int audioSessionId) { }
	public void OnAvailableCommandsChanged(IPlayer.Commands? availableCommands) { }
	public void OnCues(CueGroup? cueGroup) { }
	public void OnCues(List<Cue> cues) { }
	public void OnDeviceInfoChanged(Com.Google.Android.Exoplayer2.DeviceInfo? deviceInfo) { }
	public void OnDeviceVolumeChanged(int volume, bool muted) { }
	public void OnEvents(IPlayer? player, IPlayer.Events? events) { }
	public void OnIsLoadingChanged(bool isLoading) { }
	public void OnIsPlayingChanged(bool isPlaying) { }
	public void OnLoadingChanged(bool isLoading) { }
	public void OnMaxSeekToPreviousPositionChanged(long maxSeekToPreviousPositionMs) { }
	public void OnMediaItemTransition(MediaItem? mediaItem, int transition) { }
	public void OnMediaMetadataChanged(MediaMetadata? mediaMetadata) { }
	public void OnMetadata(Metadata? metadata) { }
	public void OnPlaybackParametersChanged(PlaybackParameters? playbackParameters) { }
	public void OnPlaybackStateChanged(int playbackState) { }
	public void OnPlaybackSuppressionReasonChanged(int playbackSuppressionReason) { }
	public void OnPlayerError(ExoPlaybackException error) { }
	public void OnPlayerErrorChanged(PlaybackException? error) { }
	public void OnPlaylistMetadataChanged(MediaMetadata? mediaMetadata) { }
	public void OnPlayWhenReadyChanged(bool playWhenReady, int reason) { }
	public void OnPositionDiscontinuity(int reason) { }
	public void OnPositionDiscontinuity(IPlayer.PositionInfo oldPosition, IPlayer.PositionInfo newPosition, int reason) { }
	public void OnRenderedFirstFrame() { }
	public void OnRepeatModeChanged(int repeatMode) { }
	public void OnSeekBackIncrementChanged(long seekBackIncrementMs) { }
	public void OnSeekForwardIncrementChanged(long seekForwardIncrementMs) { }
	public void OnSeekProcessed() { }
	public void OnShuffleModeEnabledChanged(bool shuffleModeEnabled) { }
	public void OnSkipSilenceEnabledChanged(bool skipSilenceEnabled) { }
	public void OnSurfaceSizeChanged(int width, int height) { }
	public void OnTimelineChanged(Timeline? timeline, int reason) { }
	public void OnTracksChanged(Tracks? tracks) { }
	public void OnTrackSelectionParametersChanged(TrackSelectionParameters? trackSelectionParameters) { }
	public void OnVideoSizeChanged(VideoSize? videoSize) { }
	public void OnVolumeChanged(VideoSize? videoSize) { }
	#endregion
}