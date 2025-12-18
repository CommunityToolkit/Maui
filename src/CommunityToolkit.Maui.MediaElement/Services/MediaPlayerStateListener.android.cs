using AndroidX.Media3.Common;
using AndroidX.Media3.Common.Text;

namespace CommunityToolkit.Maui.Media.Services;

sealed class MediaPlayerStateListener(string sessionId, MediaControlsService parent) : Java.Lang.Object, IPlayerListener
{
	readonly string sessionId = sessionId;
	readonly MediaControlsService parent = parent;

	public void OnIsPlayingChanged(bool isPlaying)
	{
		if (isPlaying)
		{
			parent.PromoteToForeground(sessionId);
		}
	}

	// Other IPlayerListener members - no-op (match signatures used elsewhere)
	public void OnPlaybackParametersChanged(PlaybackParameters? playbackParameters) { }
	public void OnPlayerStateChanged(bool playWhenReady, int playbackState) { }
	public void OnTracksChanged(Tracks? tracks) { }
	public void OnPlaybackStateChanged(int playbackState) { }
	public void OnPlayerError(PlaybackException? error) { }
	public void OnVideoSizeChanged(VideoSize? videoSize) { }
	public void OnVolumeChanged(float volume) { }
	public void OnAudioAttributesChanged(AudioAttributes? audioAttributes) { }
	public void OnAvailableCommandsChanged(PlayerCommands? player) { }
	public void OnCues(CueGroup? cues) { }
	public void OnDeviceInfoChanged(AndroidX.Media3.Common.DeviceInfo? deviceInfo) { }
	public void OnDeviceVolumeChanged(int volume, bool muted) { }
	public void OnEvents(IPlayer? player, PlayerEvents? playerEvents) { }
	public void OnIsLoadingChanged(bool isLoading) { }
	public void OnLoadingChanged(bool isLoading) { }
	public void OnMaxSeekToPreviousPositionChanged(long maxSeekToPreviousPositionMs) { }
	public void OnMediaItemTransition(MediaItem? mediaItem, int reason){ }
	public void OnMediaMetadataChanged(MediaMetadata? mediaMetadata) { }
	public void OnMetadata(Metadata? metadata) { }
	public void OnPlayWhenReadyChanged(bool playWhenReady, int reason) { }
	public void OnPositionDiscontinuity(PlayerPositionInfo? oldPosition, PlayerPositionInfo? newPosition, int reason) { }
	public void OnPlaybackSuppressionReasonChanged(int playbackSuppressionReason) { }
	public void OnPlayerErrorChanged(PlaybackException? error) { }
	public void OnPlaylistMetadataChanged(MediaMetadata? mediaMetadata) { }
	public void OnRenderedFirstFrame() { }
	public void OnRepeatModeChanged(int repeatMode) { }
	public void OnSeekBackIncrementChanged(long seekBackIncrementMs) { }
	public void OnSeekForwardIncrementChanged(long seekForwardIncrementMs) { }
	public void OnShuffleModeEnabledChanged(bool shuffleModeEnabled) { }
	public void OnSkipSilenceEnabledChanged(bool skipSilenceEnabled) { }
	public void OnSurfaceSizeChanged(int width, int height) { }
	public void OnTimelineChanged(Timeline? timeline, int reason) { }
	public void OnTrackSelectionParametersChanged(TrackSelectionParameters? trackSelectionParameters) { }
}