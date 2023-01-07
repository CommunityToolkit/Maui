using Android.Support.V4.Media.Session;
using Android.Widget;
using Com.Google.Android.Exoplayer2;
using Com.Google.Android.Exoplayer2.Audio;
using Com.Google.Android.Exoplayer2.Metadata;
using Com.Google.Android.Exoplayer2.Text;
using Com.Google.Android.Exoplayer2.Trackselection;
using Com.Google.Android.Exoplayer2.UI;
using Com.Google.Android.Exoplayer2.Video;
using Microsoft.Extensions.Logging;

namespace CommunityToolkit.Maui.MediaView;

public partial class MediaManager : Java.Lang.Object, IPlayer.IListener
{
	protected StyledPlayerView? playerView;

	/// <summary>
	/// Creates the corresponding platform view of <see cref="MediaView"/> on Android.
	/// </summary>
	/// <returns>The platform native counterpart of <see cref="MediaView"/>.</returns>
	/// <exception cref="NullReferenceException">Thrown when <see cref="Android.Content.Context"/> is <see langword="null"/> or when the platform view could not be created.</exception>
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

	/// <summary>
	/// Occurs when ExoPlayer changes the playback parameters.
	/// </summary>
	/// <paramref name="playbackParameters">Object containing the new playback parameter values.</paramref>
	/// <remarks>
	/// This is part of the <see cref="IPlayer.IListener"/> implementation.
	/// While this method does not seem to have any references, it's invoked at runtime.
	/// </remarks>
	public void OnPlaybackParametersChanged(PlaybackParameters? playbackParameters)
	{
		if (playbackParameters is null || mediaView is null)
		{
			return;
		}

		if ((double)playbackParameters.Speed != mediaView.Speed)
		{
			mediaView.Speed = (double)playbackParameters.Speed;
		}
	}

	/// <summary>
	/// Occurs when ExoPlayer changes the player state.
	/// </summary>
	/// <paramref name="playWhenReady">Indicates whether the player should start playing the media whenever the media is ready.</paramref>
	/// <paramref name="playbackState">The state that the player has transitioned to.</paramref>
	/// <remarks>
	/// This is part of the <see cref="IPlayer.IListener"/> implementation.
	/// While this method does not seem to have any references, it's invoked at runtime.
	/// </remarks>
	public void OnPlayerStateChanged(bool playWhenReady, int playbackState)
	{
		if (player is null || mediaView.Source is null)
		{
			return;
		}

		var newState = playbackState switch
		{
			PlaybackStateCompat.StateFastForwarding
				or PlaybackStateCompat.StateRewinding
				or PlaybackStateCompat.StateSkippingToNext
				or PlaybackStateCompat.StateSkippingToPrevious
				or PlaybackStateCompat.StateSkippingToQueueItem
				or PlaybackStateCompat.StatePlaying => playWhenReady
														? MediaViewState.Playing
														: MediaViewState.Paused,

			PlaybackStateCompat.StatePaused => MediaViewState.Paused,

			PlaybackStateCompat.StateConnecting
				or PlaybackStateCompat.StateBuffering => MediaViewState.Buffering,

			PlaybackStateCompat.StateNone => MediaViewState.None,
			PlaybackStateCompat.StateStopped => mediaView.CurrentState is not MediaViewState.Failed
												? MediaViewState.Stopped
												: MediaViewState.Failed,

			PlaybackStateCompat.StateError => MediaViewState.Failed,
			_ => MediaViewState.None,
		};

		mediaView.CurrentStateChanged(newState);

		if (playbackState is IPlayer.StateReady)
		{
			mediaView.Duration = TimeSpan.FromMilliseconds(player.Duration < 0 ? 0 : player.Duration);
		}
	}

	/// <summary>
	/// Occurs when ExoPlayer changes the playback state.
	/// </summary>
	/// <paramref name="playbackState">The state that the player has transitioned to.</paramref>
	/// <remarks>
	/// This is part of the <see cref="IPlayer.IListener"/> implementation.
	/// While this method does not seem to have any references, it's invoked at runtime.
	/// </remarks>
	public void OnPlaybackStateChanged(int playbackState)
	{
		if (mediaView.Source is null)
		{
			return;
		}

		MediaViewState newState = mediaView.CurrentState;

		switch (playbackState)
		{
			case IPlayer.StateBuffering:
				newState = MediaViewState.Buffering;
				break;
			case IPlayer.StateEnded:
				mediaView.MediaEnded();
				break;
		}

		mediaView.CurrentStateChanged(newState);
	}

	/// <summary>
	/// Occurs when ExoPlayer encounters an error.
	/// </summary>
	/// <paramref name="error">An instance of <seealso cref="PlaybackException"/> containing details of the error.</paramref>
	/// <remarks>
	/// This is part of the <see cref="IPlayer.IListener"/> implementation.
	/// While this method does not seem to have any references, it's invoked at runtime.
	/// </remarks>
	public void OnPlayerError(PlaybackException? error)
	{
		if (mediaView is null)
		{
			return;
		}

		var errorMessage = string.Empty;
		var errorCode = string.Empty;
		var errorCodeName = string.Empty;

		if (!string.IsNullOrWhiteSpace(error?.LocalizedMessage))
		{
			errorMessage = $"Error message: {error.LocalizedMessage}";
		}

		if (error?.ErrorCode is not null)
		{
			errorCode = $"Error code: {error?.ErrorCode}";
		}

		if (!string.IsNullOrWhiteSpace(error?.ErrorCodeName))
		{
			errorCode = $"Error codename: {error?.ErrorCodeName}";
		}

		var message = string.Join(", ", new[] { errorCodeName, errorCode, errorMessage }.Where(s => !string.IsNullOrEmpty(s)));

		mediaView.MediaFailed(new MediaFailedEventArgs(message));

		Logger?.LogError("{logMessage}", message);
	}

	/// <summary>
	/// Invoked when a seek operation has been processed.
	/// </summary>
	/// <remarks>
	/// This is part of the <see cref="IPlayer.IListener"/> implementation.
	/// While this method does not seem to have any references, it's invoked at runtime.
	/// </remarks>
	public void OnSeekProcessed()
	{
		mediaView?.SeekCompleted();
	}

	/// <summary>
	/// Occurs when ExoPlayer changes volume.
	/// </summary>
	/// <param name="volume">The new value for volume.</param>
	/// <remarks>
	/// This is part of the <see cref="IPlayer.IListener"/> implementation.
	/// While this method does not seem to have any references, it's invoked at runtime.
	/// </remarks>
	public void OnVolumeChanged(float volume)
	{
		if (player is null || mediaView is null)
		{
			return;
		}

		mediaView.Volume = volume;
	}

	protected virtual partial void PlatformPlay()
	{
		if (player is null || mediaView.Source is null)
		{
			return;
		}

		player.Prepare();
		player.Play();
	}

	protected virtual partial void PlatformPause()
	{
		if (player is null || mediaView.Source is null)
		{
			return;
		}

		player.Pause();
	}

	protected virtual partial void PlatformSeek(TimeSpan position)
	{
		player?.SeekTo((long)position.TotalMilliseconds);
	}

	protected virtual partial void PlatformStop()
	{
		if (player is null || mediaView is null
			 || mediaView.Source is null)
		{
			return;
		}

		// Stops and resets the media player
		player.SeekTo(0);
		player.Stop();

		mediaView.Position = TimeSpan.Zero;
	}

	protected virtual partial void PlatformUpdateSource()
	{
		var hasSetSource = false;

		if (player is null)
		{
			return;
		}

		if (mediaView.Source is null)
		{
			player.ClearMediaItems();
			mediaView.Duration = TimeSpan.Zero;
			mediaView.CurrentStateChanged(MediaViewState.None);

			return;
		}

		mediaView.CurrentStateChanged(MediaViewState.Opening);

		player.PlayWhenReady = mediaView.ShouldAutoPlay;

		if (mediaView.Source is UriMediaSource uriMediaSource)
		{
			var uri = uriMediaSource.Uri;
			if (!string.IsNullOrWhiteSpace(uri?.AbsoluteUri))
			{
				player.SetMediaItem(MediaItem.FromUri(uri.AbsoluteUri));
				player.Prepare();

				hasSetSource = true;
			}
		}
		else if (mediaView.Source is FileMediaSource fileMediaSource)
		{
			var filePath = fileMediaSource.Path;
			if (!string.IsNullOrWhiteSpace(filePath))
			{
				player.SetMediaItem(MediaItem.FromUri(filePath));
				player.Prepare();

				hasSetSource = true;
			}
		}
		else if (mediaView.Source is ResourceMediaSource resourceMediaSource)
		{
			var package = playerView?.Context?.PackageName ?? "";
			var path = resourceMediaSource.Path;
			if (!string.IsNullOrWhiteSpace(path))
			{
				string assetFilePath = "asset://" + package + "/" + path;

				player.SetMediaItem(MediaItem.FromUri(assetFilePath));
				player.Prepare();

				hasSetSource = true;
			}
		}

		if (hasSetSource && player.PlayerError is null)
		{
			mediaView.MediaOpened();
		}
	}

	protected virtual partial void PlatformUpdateSpeed()
	{
		if (mediaView is null || player is null)
		{
			return;
		}

		if (mediaView.Speed > 0)
		{
			player.SetPlaybackSpeed((float)mediaView.Speed);
			player.Play();
		}
		else
		{
			player.Pause();
		}
	}

	protected virtual partial void PlatformUpdateShouldShowPlaybackControls()
	{
		if (mediaView is null || playerView is null)
		{
			return;
		}

		playerView.UseController = mediaView.ShouldShowPlaybackControls;
	}

	protected virtual partial void PlatformUpdatePosition()
	{
		if (mediaView is null || player is null)
		{
			return;
		}

		if (mediaView.Duration != TimeSpan.Zero)
		{
			mediaView.Position = TimeSpan.FromMilliseconds(player.CurrentPosition);
		}
	}

	protected virtual partial void PlatformUpdateVolume()
	{
		if (mediaView is null || player is null)
		{
			return;
		}

		player.Volume = (float)mediaView.Volume;
	}

	protected virtual partial void PlatformUpdateShouldKeepScreenOn()
	{
		if (playerView is null)
		{
			return;
		}

		playerView.KeepScreenOn = mediaView.ShouldKeepScreenOn;
	}

	protected virtual partial void PlatformUpdateShouldLoopPlayback()
	{
		if (mediaView is null || player is null)
		{
			return;
		}

		player.RepeatMode = mediaView.ShouldLoopPlayback ? IPlayer.RepeatModeOne : IPlayer.RepeatModeOff;
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
	public void OnPlaybackSuppressionReasonChanged(int playbackSuppressionReason) { }
	public void OnPlayerErrorChanged(PlaybackException? error) { }
	public void OnPlaylistMetadataChanged(MediaMetadata? mediaMetadata) { }
	public void OnPlayWhenReadyChanged(bool playWhenReady, int reason) { }
	public void OnPositionDiscontinuity(int reason) { }
	public void OnPositionDiscontinuity(IPlayer.PositionInfo oldPosition, IPlayer.PositionInfo newPosition, int reason) { }
	public void OnRenderedFirstFrame() { }
	public void OnRepeatModeChanged(int repeatMode) { }
	public void OnSeekBackIncrementChanged(long seekBackIncrementMs) { }
	public void OnSeekForwardIncrementChanged(long seekForwardIncrementMs) { }
	public void OnShuffleModeEnabledChanged(bool shuffleModeEnabled) { }
	public void OnSkipSilenceEnabledChanged(bool skipSilenceEnabled) { }
	public void OnSurfaceSizeChanged(int width, int height) { }
	public void OnTimelineChanged(Timeline? timeline, int reason) { }
	public void OnTracksChanged(Tracks? tracks) { }
	public void OnTrackSelectionParametersChanged(TrackSelectionParameters? trackSelectionParameters) { }
	public void OnVideoSizeChanged(VideoSize? videoSize) { }
	#endregion
}
