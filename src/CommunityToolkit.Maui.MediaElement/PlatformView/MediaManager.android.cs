using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.Content;
using Android.Runtime;
using Android.Widget;
using Com.Google.Android.Exoplayer2;
using Com.Google.Android.Exoplayer2.Audio;
using Com.Google.Android.Exoplayer2.Metadata;
using Com.Google.Android.Exoplayer2.Text;
using Com.Google.Android.Exoplayer2.Trackselection;
using Com.Google.Android.Exoplayer2.UI;
using Com.Google.Android.Exoplayer2.Video;
using Java.Interop;
using static Com.Google.Android.Exoplayer2.IPlayer;
using DeviceInfo = Com.Google.Android.Exoplayer2.DeviceInfo;
using SkipSilenceEnabledChangedEventArgs = Com.Google.Android.Exoplayer2.IPlayer.SkipSilenceEnabledChangedEventArgs;
using VideoSizeChangedEventArgs = Com.Google.Android.Exoplayer2.IPlayer.VideoSizeChangedEventArgs;

namespace CommunityToolkit.Maui.MediaElement;

public partial class MediaManager : Java.Lang.Object, IPlayer.IListener
{
	StyledPlayerView? playerView;

	public (PlatformMediaView platformView, StyledPlayerView playerView) CreatePlatformView()
	{
		ArgumentNullException.ThrowIfNull(mauiContext.Context);
		player = new IExoPlayer.Builder(mauiContext.Context).Build() ?? throw new NullReferenceException();
		player.AddListener(this);
		playerView = new StyledPlayerView(mauiContext.Context)
		{
			Player = player,
			UseController = false,
			ControllerAutoShow = false,
			LayoutParameters = new RelativeLayout.LayoutParams(Android.Views.ViewGroup.LayoutParams.MatchParent, Android.Views.ViewGroup.LayoutParams.MatchParent),
		};
		return (player, playerView);
	}

	protected virtual partial void PlatformPlay(TimeSpan timeSpan)
	{
		// TODO do something with position
		if (player is null)
		{
			return;
		}

		player.Play();
	}

	protected virtual partial void PlatformPause(TimeSpan timeSpan)
	{
		// TODO do something with position
		if (player is null)
		{
			return;
		}

		player.Pause();
	}
	protected virtual partial void PlatformStop(TimeSpan timeSpan)
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
	protected virtual partial void PlatformUpdateSource() { }
	protected virtual partial void PlatformUpdateSpeed() { }
	protected virtual partial void PlatformUpdateShowsPlaybackControls()
	{
		if (mediaElement is null || playerView is null)
		{
			return;
		}

		playerView.UseController = mediaElement.ShowsPlaybackControls;
	}
	protected virtual partial void PlatformUpdatePosition()
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
	protected virtual partial void PlatformUpdateStatus() { }
	protected virtual partial void PlatformUpdateVolume() { }

	#region IPlayer.IListener implementation method stubs
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
	public void OnPlayerError(PlaybackException? error) { }
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
