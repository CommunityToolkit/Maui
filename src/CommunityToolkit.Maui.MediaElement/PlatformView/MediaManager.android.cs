using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.Runtime;
using Com.Google.Android.Exoplayer2;
using Com.Google.Android.Exoplayer2.Audio;
using Com.Google.Android.Exoplayer2.Metadata;
using Com.Google.Android.Exoplayer2.Text;
using Com.Google.Android.Exoplayer2.Trackselection;
using Com.Google.Android.Exoplayer2.Video;
using Java.Interop;
using static Com.Google.Android.Exoplayer2.IPlayer;
using DeviceInfo = Com.Google.Android.Exoplayer2.DeviceInfo;
using SkipSilenceEnabledChangedEventArgs = Com.Google.Android.Exoplayer2.IPlayer.SkipSilenceEnabledChangedEventArgs;
using VideoSizeChangedEventArgs = Com.Google.Android.Exoplayer2.IPlayer.VideoSizeChangedEventArgs;

namespace CommunityToolkit.Maui.MediaElement;

sealed class IListenerImplementor : Java.Lang.Object, IListener, IJavaObject, IDisposable, IJavaPeerable
{
	object sender;

	public EventHandler<AudioAttributesChangedEventArgs>? OnAudioAttributesChangedHandler;

	public EventHandler<AudioSessionIdChangedEventArgs>? OnAudioSessionIdChangedHandler;

	public EventHandler<AvailableCommandsChangedEventArgs>? OnAvailableCommandsChangedHandler;

	public EventHandler<CuesEventArgs>? OnCuesHandler;

	public EventHandler<DeviceInfoChangedEventArgs>? OnDeviceInfoChangedHandler;

	public EventHandler<DeviceVolumeChangedEventArgs>? OnDeviceVolumeChangedHandler;

	public EventHandler<EventsEventArgs>? OnEventsHandler;

	public EventHandler<IsLoadingChangedEventArgs>? OnIsLoadingChangedHandler;

	public EventHandler<IsPlayingChangedEventArgs>? OnIsPlayingChangedHandler;

	public EventHandler<LoadingChangedEventArgs>? OnLoadingChangedHandler;

	public EventHandler<MaxSeekToPreviousPositionChangedEventArgs>? OnMaxSeekToPreviousPositionChangedHandler;

	public EventHandler<MediaItemTransitionEventArgs>? OnMediaItemTransitionHandler;

	public EventHandler<MediaMetadataChangedEventArgs>? OnMediaMetadataChangedHandler;

	public EventHandler<MetadataEventArgs>? OnMetadataHandler;

	public EventHandler<PlayWhenReadyChangedEventArgs>? OnPlayWhenReadyChangedHandler;

	public EventHandler<PlaybackParametersChangedEventArgs>? OnPlaybackParametersChangedHandler;

	public EventHandler<PlaybackStateChangedEventArgs>? OnPlaybackStateChangedHandler;

	public EventHandler<PlaybackSuppressionReasonChangedEventArgs>? OnPlaybackSuppressionReasonChangedHandler;

	public EventHandler<PlayerErrorEventArgs>? OnPlayerErrorHandler;

	public EventHandler<PlayerErrorChangedEventArgs>? OnPlayerErrorChangedHandler;

	public EventHandler<PlayerStateChangedEventArgs>? OnPlayerStateChangedHandler;

	public EventHandler<PlaylistMetadataChangedEventArgs>? OnPlaylistMetadataChangedHandler;

	public EventHandler<PositionDiscontinuityEventArgs>? OnPositionDiscontinuityHandler;

	public EventHandler? OnRenderedFirstFrameHandler;

	public EventHandler<RepeatModeChangedEventArgs>? OnRepeatModeChangedHandler;

	public EventHandler<SeekBackIncrementChangedEventArgs>? OnSeekBackIncrementChangedHandler;

	public EventHandler<SeekForwardIncrementChangedEventArgs>? OnSeekForwardIncrementChangedHandler;

	public EventHandler? OnSeekProcessedHandler;

	public EventHandler<ShuffleModeEnabledChangedEventArgs>? OnShuffleModeEnabledChangedHandler;

	public EventHandler<SkipSilenceEnabledChangedEventArgs>? OnSkipSilenceEnabledChangedHandler;

	public EventHandler<SurfaceSizeChangedEventArgs>? OnSurfaceSizeChangedHandler;

	public EventHandler<TimelineChangedEventArgs>? OnTimelineChangedHandler;

	public EventHandler<TrackSelectionParametersChangedEventArgs>? OnTrackSelectionParametersChangedHandler;

	public EventHandler<TracksChangedEventArgs>? OnTracksChangedHandler;

	public EventHandler<VideoSizeChangedEventArgs>? OnVideoSizeChangedHandler;

	public EventHandler<VolumeChangedEventArgs>? OnVolumeChangedHandler;

	public IListenerImplementor(object sender)
		: base(JNIEnv.StartCreateInstance("mono/com/google/android/exoplayer2/Player_ListenerImplementor", "()V"), JniHandleOwnership.TransferLocalRef)
	{
		JNIEnv.FinishCreateInstance(base.Handle, "()V");
		this.sender = sender;
	}

	public IListenerImplementor()
	: base(JNIEnv.StartCreateInstance("mono/com/google/android/exoplayer2/Player_ListenerImplementor", "()V"), JniHandleOwnership.TransferLocalRef)
	{
		JNIEnv.FinishCreateInstance(base.Handle, "()V");
		this.sender = this;
	}

	public void OnAudioAttributesChanged(AudioAttributes? audioAttributes)
	{
		OnAudioAttributesChangedHandler?.Invoke(sender, new AudioAttributesChangedEventArgs(audioAttributes));
	}

	public void OnAudioSessionIdChanged(int audioSessionId)
	{
		OnAudioSessionIdChangedHandler?.Invoke(sender, new AudioSessionIdChangedEventArgs(audioSessionId));
	}

	public void OnAvailableCommandsChanged(Commands? availableCommands)
	{
		OnAvailableCommandsChangedHandler?.Invoke(sender, new AvailableCommandsChangedEventArgs(availableCommands));
	}

	public void OnCues(CueGroup? cueGroup)
	{
		OnCuesHandler?.Invoke(sender, new CuesEventArgs(cueGroup));
	}

	public void OnDeviceInfoChanged(DeviceInfo? deviceInfo)
	{
		OnDeviceInfoChangedHandler?.Invoke(sender, new DeviceInfoChangedEventArgs(deviceInfo));
	}

	public void OnDeviceVolumeChanged(int volume, bool muted)
	{
		OnDeviceVolumeChangedHandler?.Invoke(sender, new DeviceVolumeChangedEventArgs(volume, muted));
	}

	public void OnEvents(IPlayer? player, Events? events)
	{
		OnEventsHandler?.Invoke(sender, new EventsEventArgs(player, events));
	}

	public void OnIsLoadingChanged(bool isLoading)
	{
		OnIsLoadingChangedHandler?.Invoke(sender, new IsLoadingChangedEventArgs(isLoading));
	}

	public void OnIsPlayingChanged(bool isPlaying)
	{
		OnIsPlayingChangedHandler?.Invoke(sender, new IsPlayingChangedEventArgs(isPlaying));
	}

	public void OnLoadingChanged(bool isLoading)
	{
		OnLoadingChangedHandler?.Invoke(sender, new LoadingChangedEventArgs(isLoading));
	}

	public void OnMaxSeekToPreviousPositionChanged(long maxSeekToPreviousPositionMs)
	{
		OnMaxSeekToPreviousPositionChangedHandler?.Invoke(sender, new MaxSeekToPreviousPositionChangedEventArgs(maxSeekToPreviousPositionMs));
	}

	public void OnMediaItemTransition(MediaItem? mediaItem, int reason)
	{
		OnMediaItemTransitionHandler?.Invoke(sender, new MediaItemTransitionEventArgs(mediaItem, reason));
	}

	public void OnMediaMetadataChanged(MediaMetadata? mediaMetadata)
	{
		OnMediaMetadataChangedHandler?.Invoke(sender, new MediaMetadataChangedEventArgs(mediaMetadata));
	}

	public void OnMetadata(Com.Google.Android.Exoplayer2.Metadata.Metadata? metadata)
	{
		OnMetadataHandler?.Invoke(sender, new MetadataEventArgs(metadata));
	}

	public void OnPlayWhenReadyChanged(bool playWhenReady, int reason)
	{
		OnPlayWhenReadyChangedHandler?.Invoke(sender, new PlayWhenReadyChangedEventArgs(playWhenReady, reason));
	}

	public void OnPlaybackParametersChanged(PlaybackParameters? playbackParameters)
	{
		OnPlaybackParametersChangedHandler?.Invoke(sender, new PlaybackParametersChangedEventArgs(playbackParameters));
	}

	public void OnPlaybackStateChanged(int playbackState)
	{
		OnPlaybackStateChangedHandler?.Invoke(sender, new PlaybackStateChangedEventArgs(playbackState));
	}

	public void OnPlaybackSuppressionReasonChanged(int playbackSuppressionReason)
	{
		OnPlaybackSuppressionReasonChangedHandler?.Invoke(sender, new PlaybackSuppressionReasonChangedEventArgs(playbackSuppressionReason));
	}

	public void OnPlayerError(PlaybackException? error)
	{
		OnPlayerErrorHandler?.Invoke(sender, new PlayerErrorEventArgs(error));
	}

	public void OnPlayerErrorChanged(PlaybackException? error)
	{
		OnPlayerErrorChangedHandler?.Invoke(sender, new PlayerErrorChangedEventArgs(error));
	}

	public void OnPlayerStateChanged(bool playWhenReady, int playbackState)
	{
		OnPlayerStateChangedHandler?.Invoke(sender, new PlayerStateChangedEventArgs(playWhenReady, playbackState));
	}

	public void OnPlaylistMetadataChanged(MediaMetadata? mediaMetadata)
	{
		OnPlaylistMetadataChangedHandler?.Invoke(sender, new PlaylistMetadataChangedEventArgs(mediaMetadata));
	}

	public void OnPositionDiscontinuity(int reason)
	{
		OnPositionDiscontinuityHandler?.Invoke(sender, new PositionDiscontinuityEventArgs(reason));
	}

	public void OnRenderedFirstFrame()
	{
		OnRenderedFirstFrameHandler?.Invoke(sender, new EventArgs());
	}

	public void OnRepeatModeChanged(int repeatMode)
	{
		OnRepeatModeChangedHandler?.Invoke(sender, new RepeatModeChangedEventArgs(repeatMode));
	}

	public void OnSeekBackIncrementChanged(long seekBackIncrementMs)
	{
		OnSeekBackIncrementChangedHandler?.Invoke(sender, new SeekBackIncrementChangedEventArgs(seekBackIncrementMs));
	}

	public void OnSeekForwardIncrementChanged(long seekForwardIncrementMs)
	{
		OnSeekForwardIncrementChangedHandler?.Invoke(sender, new SeekForwardIncrementChangedEventArgs(seekForwardIncrementMs));
	}

	public void OnSeekProcessed()
	{
		OnSeekProcessedHandler?.Invoke(sender, new EventArgs());
	}

	public void OnShuffleModeEnabledChanged(bool shuffleModeEnabled)
	{
		OnShuffleModeEnabledChangedHandler?.Invoke(sender, new ShuffleModeEnabledChangedEventArgs(shuffleModeEnabled));
	}

	public void OnSkipSilenceEnabledChanged(bool skipSilenceEnabled)
	{
		OnSkipSilenceEnabledChangedHandler?.Invoke(sender, new SkipSilenceEnabledChangedEventArgs(skipSilenceEnabled));
	}

	public void OnSurfaceSizeChanged(int width, int height)
	{
		OnSurfaceSizeChangedHandler?.Invoke(sender, new SurfaceSizeChangedEventArgs(width, height));
	}

	public void OnTimelineChanged(Timeline? timeline, int reason)
	{
		OnTimelineChangedHandler?.Invoke(sender, new TimelineChangedEventArgs(timeline, reason));
	}

	public void OnTrackSelectionParametersChanged(TrackSelectionParameters? parameters)
	{
		OnTrackSelectionParametersChangedHandler?.Invoke(sender, new TrackSelectionParametersChangedEventArgs(parameters));
	}

	public void OnTracksChanged(Tracks? tracks)
	{
		OnTracksChangedHandler?.Invoke(sender, new TracksChangedEventArgs(tracks));
	}

	public void OnVideoSizeChanged(VideoSize? videoSize)
	{
		OnVideoSizeChangedHandler?.Invoke(sender, new VideoSizeChangedEventArgs(videoSize));
	}

	public void OnVolumeChanged(float volume)
	{
		OnVolumeChangedHandler?.Invoke(sender, new VolumeChangedEventArgs(volume));
	}

	internal static bool __IsEmpty(IListenerImplementor value)
	{
		if (value.OnAudioAttributesChangedHandler == null && value.OnAudioSessionIdChangedHandler == null && value.OnAvailableCommandsChangedHandler == null && value.OnCuesHandler == null && value.OnDeviceInfoChangedHandler == null && value.OnDeviceVolumeChangedHandler == null && value.OnEventsHandler == null && value.OnIsLoadingChangedHandler == null && value.OnIsPlayingChangedHandler == null && value.OnLoadingChangedHandler == null && value.OnMaxSeekToPreviousPositionChangedHandler == null && value.OnMediaItemTransitionHandler == null && value.OnMediaMetadataChangedHandler == null && value.OnMetadataHandler == null && value.OnPlayWhenReadyChangedHandler == null && value.OnPlaybackParametersChangedHandler == null && value.OnPlaybackStateChangedHandler == null && value.OnPlaybackSuppressionReasonChangedHandler == null && value.OnPlayerErrorHandler == null && value.OnPlayerErrorChangedHandler == null && value.OnPlayerStateChangedHandler == null && value.OnPlaylistMetadataChangedHandler == null && value.OnPositionDiscontinuityHandler == null && value.OnRenderedFirstFrameHandler == null && value.OnRepeatModeChangedHandler == null && value.OnSeekBackIncrementChangedHandler == null && value.OnSeekForwardIncrementChangedHandler == null && value.OnSeekProcessedHandler == null && value.OnShuffleModeEnabledChangedHandler == null && value.OnSkipSilenceEnabledChangedHandler == null && value.OnSurfaceSizeChangedHandler == null && value.OnTimelineChangedHandler == null && value.OnTrackSelectionParametersChangedHandler == null && value.OnTracksChangedHandler == null && value.OnVideoSizeChangedHandler == null)
		{
			return value.OnVolumeChangedHandler == null;
		}

		return false;
	}
}


public partial class MediaManager : Java.Lang.Object, IPlayer.IListener
{
	public PlatformMediaView CreatePlatformView()
	{
		player = new IExoPlayer.Builder(mauiContext.Context).Build() ?? throw new NullReferenceException();
		player.AddListener(this);
		//player.AddListener(new IListenerImplementor());
		return player;
	}


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
