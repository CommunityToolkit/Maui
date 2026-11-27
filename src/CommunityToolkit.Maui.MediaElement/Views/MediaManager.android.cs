using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Versioning;
using Android.Content;
using Android.Util;
using Android.Views;
using Android.Widget;
using AndroidX.Core.Content;
using AndroidX.Media3.Common;
using AndroidX.Media3.Common.Text;
using AndroidX.Media3.Common.Util;
using AndroidX.Media3.DataSource;
using AndroidX.Media3.ExoPlayer;
using AndroidX.Media3.ExoPlayer.Source;
using AndroidX.Media3.ExoPlayer.TrackSelection;
using AndroidX.Media3.Session;
using AndroidX.Media3.UI;
using CommunityToolkit.Maui.Media.Services;
using CommunityToolkit.Maui.Services;
using CommunityToolkit.Maui.Views;
using Java.Lang;
using Microsoft.Extensions.Logging;
using AudioAttributes = AndroidX.Media3.Common.AudioAttributes;
using DeviceInfo = AndroidX.Media3.Common.DeviceInfo;
using Exception = System.Exception;
using MediaController = AndroidX.Media3.Session.MediaController;
using MediaMetadata = AndroidX.Media3.Common.MediaMetadata;

namespace CommunityToolkit.Maui.Core.Views;

public partial class MediaManager : Java.Lang.Object, IPlayerListener
{
	const int stateIdle = 1;
	const int stateBuffering = 2;
	const int stateReady = 3;
	const int stateEnded = 4;

	// Media3's Player.EVENT_PLAYBACK_STATE_CHANGED and Player.EVENT_PLAY_WHEN_READY_CHANGED, which the binding does not expose as constants
	const int eventPlaybackStateChanged = 4;
	const int eventPlayWhenReadyChanged = 5;

	const string foregroundServiceRefusalFailure = $"Android refused to start {nameof(MediaControlsService)}, the foreground service of the media notification";

	// Media3 catches Android's exception and only logs it, so the report carries an equivalent one that points to that log
	const string foregroundServiceRefusalReason = "Media3 logged Android's reason under the logcat tag MSessionService";

	readonly HttpClient client = new();
	readonly SemaphoreSlim seekToSemaphoreSlim = new(1, 1);
	readonly CancellationTokenSource disconnectTokenSource = new();
	readonly string playerConnectionId = Guid.NewGuid().ToString("N");
	bool isAndroidForegroundServiceEnabled = false;
	bool hasPendingSourceUpdate;

	// Media3 tries the start again whenever the notification changes: each Source reports a refusal once
	bool hasReportedForegroundServiceRefusal;
	DefaultTrackSelector? localTrackSelector;

	double? previousSpeed;
	float volumeBeforeMute = 1;
	TaskCompletionSource? seekToTaskCompletionSource;
	CancellationTokenSource? cancellationTokenSource;
	StreamDataSourceFactory? currentStreamDataSourceFactory;

	/// <summary>
	/// The platform native counterpart of <see cref="MediaElement"/>.
	/// </summary>
	protected PlayerView? PlayerView { get; set; }

	// Per connection, not per element: the registration of a connection that was disconnected must not be taken for that of the same element
	// when it is hosted again right away (the late release of the old controller would release the new player)
	internal string AndroidPlayerConnectionKey => $"{MediaElement.AndroidPlayerId}:{playerConnectionId}";

	/// <summary>
	/// Occurs when ExoPlayer changes the playback parameters.
	/// </summary>
	/// <paramref name="playbackParameters">Object containing the new playback parameter values.</paramref>
	/// <remarks>
	/// This is part of the <see cref="IPlayerListener"/> implementation.
	/// While this method does not seem to have any references, it's invoked at runtime.
	/// </remarks>
	public void OnPlaybackParametersChanged(PlaybackParameters? playbackParameters)
	{
		if (playbackParameters is null || AreFloatingPointNumbersEqual(playbackParameters.Speed, MediaElement.Speed))
		{
			return;
		}

		MediaElement.Speed = playbackParameters.Speed;
	}


	/// <summary>
	/// Occurs when ExoPlayer changes the player state.
	/// </summary>
	/// <paramref name="playWhenReady">Indicates whether the player should start playing the media whenever the media is ready.</paramref>
	/// <paramref name="playbackState">The state that the player has transitioned to.</paramref>
	/// <remarks>
	/// This is part of the <see cref="IPlayerListener"/> implementation.
	/// While this method does not seem to have any references, it's invoked at runtime.
	/// </remarks>
	public void OnPlayerStateChanged(bool playWhenReady, int playbackState)
	{
		if (Player is null || MediaElement.Source is null)
		{
			return;
		}

		// ExoPlayer reports Media3's Player.STATE_* values (stateIdle to stateEnded), not android.media.session.PlaybackState values: the
		// PlaybackState arms only map the values that are not Media3 states.
		var newState = playbackState switch
		{
			// ExoPlayer is idle until a source is prepared: a source that is opening has not stopped, and a failed one stays failed
			stateIdle => MediaElement.CurrentState is MediaElementState.Opening or MediaElementState.Failed
				? MediaElement.CurrentState
				: MediaElementState.Stopped,

			stateBuffering => MediaElementState.Buffering,

			stateReady => playWhenReady
				? MediaElementState.Playing
				: MediaElementState.Paused,

			stateEnded => MediaElementState.Stopped,

			PlaybackState.StateRewinding
				or PlaybackState.StateSkippingToNext
				or PlaybackState.StateSkippingToPrevious
				or PlaybackState.StateSkippingToQueueItem => playWhenReady
					? MediaElementState.Playing
					: MediaElementState.Paused,

			PlaybackState.StateConnecting
				or PlaybackState.StateBuffering => MediaElementState.Buffering,

			PlaybackState.StateNone => MediaElementState.None,

			PlaybackState.StateError => MediaElementState.Failed,

			_ => MediaElementState.None,
		};

		MediaElement.CurrentStateChanged(newState);
		if (playbackState is stateReady)
		{
			MediaElement.Duration = TimeSpan.FromMilliseconds(Player.Duration < 0 ? 0 : Player.Duration);
			MediaElement.Position = TimeSpan.FromMilliseconds(Player.CurrentPosition < 0 ? 0 : Player.CurrentPosition);
		}
	}

	/// <summary>
	/// Occurs when a player reports that some of its state changed, after the callbacks of the individual changes.
	/// </summary>
	/// <remarks>
	/// This is part of the <see cref="IPlayerListener"/> implementation.
	/// While this method does not seem to have any references, it's invoked at runtime.
	/// </remarks>
	public void OnEvents(IPlayer? player, PlayerEvents? playerEvents)
	{
		// A MediaController reports the state through the individual callbacks and OnEvents only, never through OnPlayerStateChanged,
		// which ExoPlayer calls itself
		if (player is null or IExoPlayer || playerEvents is null)
		{
			return;
		}

		if (playerEvents.ContainsAny(eventPlaybackStateChanged, eventPlayWhenReadyChanged))
		{
			OnPlayerStateChanged(player.PlayWhenReady, player.PlaybackState);
		}
	}

	public void OnTracksChanged(Tracks? tracks)
	{
		if (tracks is null || tracks.IsEmpty)
		{
			return;
		}
		if (tracks.IsTypeSupported(C.TrackTypeText))
		{
			PlayerView?.SetShowSubtitleButton(true);
		}
		else
		{
			PlayerView?.SetShowSubtitleButton(false);
		}
	}

	/// <summary>
	/// Creates the corresponding platform view of <see cref="MediaElement"/> on Android.
	/// </summary>
	/// <returns>The platform native counterpart of <see cref="MediaElement"/>.</returns>
	/// <exception cref="NullReferenceException">Thrown when <see cref="Context"/> is <see langword="null"/> or when the platform view could not be created.</exception>
	[MemberNotNull(nameof(PlayerView))]
	public PlayerView CreatePlatformView(AndroidViewType androidViewType, bool isAndroidServiceEnabled)
	{
		this.isAndroidForegroundServiceEnabled = isAndroidServiceEnabled;
		if (androidViewType is AndroidViewType.SurfaceView)
		{
			PlayerView = new PlayerView(MauiContext.Context)
			{
				UseController = false,
				ControllerAutoShow = false,
				LayoutParameters = new RelativeLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent)
			};
		}
		else if (androidViewType is AndroidViewType.TextureView)
		{
			if (MauiContext.Context?.Resources is null)
			{
				throw new InvalidOperationException("Unable to retrieve Android Resources");
			}

			var resources = MauiContext.Context.Resources;
			var xmlResource = resources.GetXml(Microsoft.Maui.Resource.Layout.textureview);
			xmlResource.Read();

			var attributes = Xml.AsAttributeSet(xmlResource)!;

			PlayerView = new PlayerView(MauiContext.Context, attributes)
			{
				UseController = false,
				ControllerAutoShow = false,
				LayoutParameters = new RelativeLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent)
			};
		}
		else
		{
			throw new NotSupportedException($"{androidViewType} is not yet supported");
		}

		return PlayerView;
	}

	public async Task<PlatformMediaElement> CreatePlatformPlayer(CancellationToken cancellationToken = default)
	{
		PlatformMediaElement player = isAndroidForegroundServiceEnabled
			? await CreateMediaController(cancellationToken)
			: CreateLocalPlayer();

		disconnectTokenSource.Token.ThrowIfCancellationRequested();
		return player;
	}

	internal ValueTask SynchronizePlayerStateAsync()
	{
		PlatformUpdateAspect();
		PlatformUpdateShouldShowPlaybackControls();
		PlatformUpdateShouldKeepScreenOn();
		PlatformUpdateVolume();
		PlatformUpdateShouldMute();
		PlatformUpdateShouldLoopPlayback();
		PlatformUpdateSpeed();

		// A Source set before the player exists was only noted: the mapper has already run, which is not the case for a Source of the
		// MediaElement that the handler connects to, whose mapper runs right after this and loads it
		return hasPendingSourceUpdate ? UpdateSource() : ValueTask.CompletedTask;
	}

	IExoPlayer CreateLocalPlayer()
	{
		if (Player is IExoPlayer existingPlayer)
		{
			return existingPlayer;
		}

		if (PlayerView is null)
		{
			throw new InvalidOperationException($"{nameof(PlayerView)} cannot be null");
		}

		var (exoPlayer, trackSelector) = ExoPlayerFactory.Create(Platform.AppContext);
		localTrackSelector = trackSelector;

		Player = exoPlayer;
		Player.AddListener(this);
		PlayerView.SetBackgroundColor(Android.Graphics.Color.Black);
		PlayerView.Player = Player;

		return exoPlayer;
	}
	public async Task<MediaController> CreateMediaController(CancellationToken cancellationToken = default)
	{
		if (Player is MediaController mediaController)
		{
			return mediaController;
		}

		var disconnectToken = disconnectTokenSource.Token;
		disconnectToken.ThrowIfCancellationRequested();

		var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		using var connectionHints = new Android.OS.Bundle();
		connectionHints.PutString(MediaSessionCallback.PlayerIdKey, AndroidPlayerConnectionKey);

		var builder = new MediaController.Builder(Platform.AppContext, new SessionToken(Platform.AppContext, new ComponentName(Platform.AppContext, Java.Lang.Class.FromType(typeof(MediaControlsService)))));
		builder.SetConnectionHints(connectionHints);
		var future = builder.BuildAsync();
		future?.AddListener(new Runnable(() =>
		{
			try
			{
				var result = future.Get() ?? throw new InvalidOperationException("MediaController.Builder.BuildAsync().Get() returned null");
				if (result is not MediaController mc)
				{
					tcs.SetException(new InvalidOperationException("MediaController.Builder.BuildAsync().Get() did not return a MediaController"));
					return;
				}

				// The service created the registration of this connection while the controller connected: it is released again, because
				// the handler was disconnected in the meantime and nothing else would release it
				if (disconnectToken.IsCancellationRequested)
				{
					ReleaseControllerAndItsPlayerRegistration(mc);
					tcs.SetCanceled(disconnectToken);
					return;
				}

				Player = mc;
				Player.AddListener(this);
				MediaControlsService.Running?.AttachMediaManagerToRegistration(AndroidPlayerConnectionKey, this);
				if (PlayerView is null)
				{
					throw new InvalidOperationException($"{nameof(PlayerView)} cannot be null");
				}

				PlayerView.SetBackgroundColor(Android.Graphics.Color.Black);
				PlayerView.Player = Player;

				tcs.SetResult();
			}
			catch (Exception ex)
			{
				Trace.WriteLine($"Error creating MediaController: {ex}");
				tcs.SetException(ex);
			}
		}), ContextCompat.GetMainExecutor(Platform.AppContext));

		using var waitTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, disconnectToken);
		await tcs.Task.WaitAsync(waitTokenSource.Token);

		disconnectToken.ThrowIfCancellationRequested();
		return Player as MediaController ?? throw new InvalidOperationException("MediaController is null");
	}

	/// <summary>
	/// Occurs when ExoPlayer changes the playback state.
	/// </summary>
	/// <paramref name="playbackState">The state that the player has transitioned to.</paramref>
	/// <remarks>
	/// This is part of the <see cref="IPlayerListener"/> implementation.
	/// While this method does not seem to have any references, it's invoked at runtime.
	/// </remarks>
	public void OnPlaybackStateChanged(int playbackState)
	{
		if (MediaElement.Source is null)
		{
			return;
		}

		MediaElementState newState = MediaElement.CurrentState;
		switch (playbackState)
		{
			case stateBuffering:
				newState = MediaElementState.Buffering;
				break;
			case stateEnded:
				newState = MediaElementState.Stopped;
				MediaElement.MediaEnded();
				break;
			case stateReady:
				seekToTaskCompletionSource?.TrySetResult();
				break;
		}

		MediaElement.CurrentStateChanged(newState);
	}

	/// <summary>
	/// Occurs when ExoPlayer encounters an error.
	/// </summary>
	/// <paramref name="error">An instance of <seealso cref="PlaybackException"/> containing details of the error.</paramref>
	/// <remarks>
	/// This is part of the <see cref="IPlayerListener"/> implementation.
	/// While this method does not seem to have any references, it's invoked at runtime.
	/// </remarks>
	public void OnPlayerError(PlaybackException? error)
	{
		var errorMessage = string.Empty;
		var errorCode = string.Empty;
		var errorCodeName = string.Empty;

		if (!string.IsNullOrWhiteSpace(error?.LocalizedMessage))
		{
			errorMessage = $"Error message: {error.LocalizedMessage}";
		}

		if (error?.ErrorCode is not null)
		{
			errorCode = $"Error code: {error.ErrorCode}";
		}

		if (!string.IsNullOrWhiteSpace(error?.ErrorCodeName))
		{
			errorCodeName = $"Error codename: {error.ErrorCodeName}";
		}

		var message = string.Join(", ", new[]
		{
			errorCodeName,
			errorCode,
			errorMessage
		}.Where(static s => !string.IsNullOrEmpty(s)));

		MediaElement.MediaFailed(new MediaFailedEventArgs(message));

		Logger.LogError("{LogMessage}", message);
	}

	public void OnVideoSizeChanged(VideoSize? videoSize)
	{
		MediaElement.MediaWidth = videoSize?.Width ?? 0;
		MediaElement.MediaHeight = videoSize?.Height ?? 0;
	}

	/// <summary>
	/// Occurs when ExoPlayer changes volume.
	/// </summary>
	/// <param name="volume">The new value for volume.</param>
	/// <remarks>
	/// This is part of the <see cref="IPlayerListener"/> implementation.
	/// While this method does not seem to have any references, it's invoked at runtime.
	/// </remarks>
	public void OnVolumeChanged(float volume)
	{
		if (Player is null)
		{
			return;
		}

		// When currently muted, ignore
		if (MediaElement.ShouldMute)
		{
			return;
		}

		MediaElement.Volume = volume;
	}

	protected virtual partial void PlatformPlay()
	{
		if (Player is null || MediaElement.Source is null)
		{
			return;
		}

		// The player has no media: prepared, it would end at once and report MediaEnded for a media that never played
		if (!PlayerHasMediaItems())
		{
			// A source that is still loading (its artwork downloads) plays once it is loaded. A source without data, or one that could not be
			// applied, has nothing to play, like a null Source.
			if (MediaElement.CurrentState is MediaElementState.Opening)
			{
				Player.PlayWhenReady = true;
			}

			return;
		}

		// Media that played to its end stays there on play() (ExoPlayer's ended state). Like the play button of Media3's own controls
		// (Util.handlePlayButtonAction), Play() first seeks to the default position: the start of the media, or the live edge of a live stream
		if (Player.PlaybackState is stateEnded)
		{
			Player.SeekToDefaultPosition();
		}

		Player.Prepare();
		Player.Play();
	}

	protected virtual partial void PlatformPause()
	{
		if (Player is null || MediaElement.Source is null)
		{
			return;
		}

		Player.Pause();
	}

	[MemberNotNull(nameof(Player))]
	protected virtual async partial Task PlatformSeek(TimeSpan position, CancellationToken token)
	{
		if (Player is null)
		{
			throw new InvalidOperationException($"{nameof(Player)} is not yet initialized");
		}

		await seekToSemaphoreSlim.WaitAsync(token);

		seekToTaskCompletionSource = new();
		try
		{
			Player.SeekTo((long)position.TotalMilliseconds);

			// Here, we don't want to throw an exception
			// and to keep the execution on the thread that called this method
			await seekToTaskCompletionSource.Task.WaitAsync(TimeSpan.FromMinutes(2), token).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing | ConfigureAwaitOptions.ContinueOnCapturedContext);

			MediaElement.SeekCompleted();
		}
		finally
		{
			seekToSemaphoreSlim.Release();
		}
	}

	protected virtual partial void PlatformStop()
	{
		if (Player is null || MediaElement.Source is null)
		{
			return;
		}

		Player.Stop();
		Player.SeekTo(0);
		MediaElement.Position = TimeSpan.Zero;

		// The stopped player is idle: it reports no ready state for a seek that Stop() interrupted, which has ended all the same. SeekTo completes
		// and raises SeekCompleted (IMediaElement.SeekTo), once the player has stopped, instead of waiting for its two minute timeout
		seekToTaskCompletionSource?.TrySetResult();
	}

	protected virtual async partial ValueTask PlatformUpdateSource()
	{
		hasReportedForegroundServiceRefusal = false;

		if (Player is null)
		{
			hasPendingSourceUpdate = true;
			return;
		}

		hasPendingSourceUpdate = false;

		// This source replaces one that may still be loading (its artwork can take a while to download): that load must not finish
		cancellationTokenSource?.Cancel();
		cancellationTokenSource?.Dispose();
		cancellationTokenSource = null;

		if (MediaElement.Source is null)
		{
			Player.ClearMediaItems();
			MediaElement.Duration = TimeSpan.Zero;
			MediaElement.CurrentStateChanged(MediaElementState.None);

			currentStreamDataSourceFactory?.Dispose();
			currentStreamDataSourceFactory = null;

			return;
		}

		// Clear previous stream data source if switching sources
		if (MediaElement.Source is not StreamMediaSource)
		{
			currentStreamDataSourceFactory?.Dispose();
			currentStreamDataSourceFactory = null;
		}

		// The load has its token before Opening is raised: a StateChanged handler can set another Source then, whose update replaces this one
		cancellationTokenSource = new();
		var token = cancellationTokenSource.Token;
		MediaElement.CurrentStateChanged(MediaElementState.Opening);
		if (token.IsCancellationRequested)
		{
			return;
		}

		Player.PlayWhenReady = MediaElement.ShouldAutoPlay;

		// Only a failure to apply the source (to create its media item or media source, or to hand it to the player) is a failure of the source.
		// An exception of an event handler is not, whether the handler is that of an event raised after this part (MediaOpened, or None for a
		// source without data) or that of an event the player raises while it takes the source: the MediaElement catches it and reports it once
		// the update has ended (UpdateSource), so it does not stop the update
		MediaItem? item;
		var isPlayerTakingTheSource = false;
		try
		{
			// ConfigureAwait(true) is required to prevent crash on startup
			var result = await SetPlayerData(token).ConfigureAwait(true);

			// A newer source, or the disconnection of the handler, replaced this source while it was loading
			if (token.IsCancellationRequested)
			{
				return;
			}

			item = result?.Build();
			if (item?.MediaMetadata is not null)
			{
				IMediaSource? mediaSource = null;

				// If we have a custom stream data source, we need to set the media source differently
				if (currentStreamDataSourceFactory is not null && MediaElement.Source is StreamMediaSource)
				{
					mediaSource = new AndroidX.Media3.ExoPlayer.Source.ProgressiveMediaSource.Factory(currentStreamDataSourceFactory)
						.CreateMediaSource(item);
				}
				else if (MediaElement.Source is UriMediaSource uriMediaSource && uriMediaSource.HttpHeaders.Count > 0)
				{
					var httpDataSourceFactory = new DefaultHttpDataSource.Factory();
					httpDataSourceFactory.SetDefaultRequestProperties(uriMediaSource.HttpHeaders);

					var mediaSourceFactory = new DefaultMediaSourceFactory(httpDataSourceFactory);
					mediaSource = mediaSourceFactory.CreateMediaSource(item);
				}

				IExoPlayer? mediaSourcePlayer = null;
				if (mediaSource is not null && !TryGetExoPlayer(out mediaSourcePlayer))
				{
					throw new InvalidOperationException($"The {nameof(MediaElement.Source)} needs the ExoPlayer of the {nameof(MediaElement)}, which is not available yet or any more.");
				}

				// The player raises its events synchronously while it takes the source (StateChanged for Buffering, for example): from here on, only
				// an exception of the player itself, a Java exception, is a failure of the source
				isPlayerTakingTheSource = true;
				if (mediaSource is not null)
				{
					mediaSourcePlayer?.SetMediaSource(mediaSource);
				}
				else
				{
					Player.SetMediaItem(item);
				}

				Player.Prepare();
			}
		}
		catch (Exception exception) when (!isPlayerTakingTheSource || exception is Java.Lang.Throwable)
		{
			// A newer source, or the disconnection of the handler, replaced this source while it was loading: the failure is that of a source the
			// MediaElement no longer has
			if (token.IsCancellationRequested)
			{
				return;
			}

			// Like a source that cannot be played, a source that cannot be applied replaced the previous media, which must not keep playing nor
			// report its end for this source. It is reported through MediaFailed, which also resets the duration.
			Player.Stop();
			Player.ClearMediaItems();

			currentStreamDataSourceFactory?.Dispose();
			currentStreamDataSourceFactory = null;

			MediaElement.MediaFailed(new MediaFailedEventArgs(exception.Message));
			Logger.LogError(exception, "{LogMessage}", exception.Message);

			return;
		}

		// A StateChanged handler that the player raised while it took the source can set another Source, whose update replaces this one, or
		// disconnect the handler: this source then reports nothing more
		if (token.IsCancellationRequested)
		{
			return;
		}

		if (item?.MediaMetadata is null)
		{
			// A source without data (a Uri, File, Resource or Stream source with nothing in it) loads nothing, like a null Source. The player
			// is stopped first: emptying the playlist of a prepared player would end the previous media, and report MediaEnded, for this source
			Player.Stop();
			Player.ClearMediaItems();
			MediaElement.Duration = TimeSpan.Zero;
			MediaElement.CurrentStateChanged(MediaElementState.None);

			currentStreamDataSourceFactory?.Dispose();
			currentStreamDataSourceFactory = null;

			return;
		}

		if (Player.PlayerError is null)
		{
			MediaElement.MediaOpened();
		}

	}

	protected virtual partial void PlatformUpdateAspect()
	{
		if (PlayerView is null)
		{
			return;
		}

		PlayerView.ResizeMode = MediaElement.Aspect switch
		{
			Aspect.AspectFill => AspectRatioFrameLayout.ResizeModeZoom,
			Aspect.Fill => AspectRatioFrameLayout.ResizeModeFill,
			Aspect.Center or Aspect.AspectFit => AspectRatioFrameLayout.ResizeModeFit,
			_ => throw new NotSupportedException($"{nameof(Aspect)}: {MediaElement.Aspect} is not yet supported")
		};
	}

	protected virtual partial void PlatformUpdateSpeed()
	{
		if (Player is null)
		{
			return;
		}

		// First time we're getting a playback speed, set initial value
		previousSpeed ??= MediaElement.Speed;

		if (MediaElement.Speed > 0)
		{
			Player.SetPlaybackSpeed((float)MediaElement.Speed);

			if (previousSpeed is 0)
			{
				Player.Play();
			}

			previousSpeed = MediaElement.Speed;
		}
		else
		{
			previousSpeed = 0;
			Player.Pause();
		}
	}

	protected virtual partial void PlatformUpdateShouldShowPlaybackControls()
	{
		if (PlayerView is null)
		{
			return;
		}

		PlayerView.UseController = MediaElement.ShouldShowPlaybackControls;
	}

	protected virtual partial void PlatformUpdatePosition()
	{
		if (Player is null)
		{
			return;
		}

		if (MediaElement.Duration != TimeSpan.Zero)
		{
			MediaElement.Position = TimeSpan.FromMilliseconds(Player.CurrentPosition);
		}
	}

	protected virtual partial void PlatformUpdateVolume()
	{
		if (Player is null)
		{
			return;
		}

		// If the user changes while muted, change the internal field
		// and do not update the actual volume.
		if (MediaElement.ShouldMute)
		{
			volumeBeforeMute = (float)MediaElement.Volume;
			return;
		}
		Player.Volume = (float)MediaElement.Volume;
	}

	protected virtual partial void PlatformUpdateShouldKeepScreenOn()
	{
		if (PlayerView is null)
		{
			return;
		}

		PlayerView.KeepScreenOn = MediaElement.ShouldKeepScreenOn;
	}

	protected virtual partial void PlatformUpdateShouldMute()
	{
		if (Player is null)
		{
			return;
		}

		// We're going to mute state. Capture the current volume first so we can restore later.
		// That volume is the MediaElement's: the player's is not set yet when the handler connects to a MediaElement that is muted already
		if (MediaElement.ShouldMute)
		{
			volumeBeforeMute = (float)MediaElement.Volume;
		}
		else if (!AreFloatingPointNumbersEqual(volumeBeforeMute, Player.Volume) && Player.Volume > 0)
		{
			volumeBeforeMute = Player.Volume;
		}

		Player.Volume = MediaElement.ShouldMute ? 0 : volumeBeforeMute;
	}

	protected virtual partial void PlatformUpdateShouldLoopPlayback()
	{
		if (Player is null)
		{
			return;
		}

		Player.RepeatMode = MediaElement.ShouldLoopPlayback ? RepeatModeUtil.RepeatToggleModeOne : RepeatModeUtil.RepeatToggleModeNone;
	}

	// A MediaController cannot carry a MediaSource (a stream, or a source with HTTP headers): it is set on the ExoPlayer of the service, which is
	// the player the controller controls. Both run on the main thread, which is the application thread of that player.
	bool TryGetExoPlayer([NotNullWhen(true)] out IExoPlayer? exoPlayer)
	{
		exoPlayer = Player as IExoPlayer;
		return exoPlayer is not null || MediaControlsService.TryGetExoPlayer(AndroidPlayerConnectionKey, out exoPlayer);
	}

	// The controller learns of an item that was set on the player of the service only once the session has told it
	bool PlayerHasMediaItems() =>
		Player?.MediaItemCount > 0 || (TryGetExoPlayer(out var exoPlayer) && exoPlayer.MediaItemCount > 0);

	// Media3 runs the service in the foreground for media that is ready or buffering and plays, and on Android 12 and 12L also for a Play() of
	// paused media, which it then refuses: the refusal names no session, and the controller has dropped that Play() before it arrives
	[SupportedOSPlatform("android31.0")]
	internal void ReportForegroundServiceRefusalIfPlaybackNeedsTheService()
	{
		if (hasReportedForegroundServiceRefusal || Player is not MediaController mediaController || !IsAlive(mediaController))
		{
			return;
		}

		var doesPlaybackNeedTheService = mediaController.PlayWhenReady || !OperatingSystem.IsAndroidVersionAtLeast(33);
		if (!doesPlaybackNeedTheService || mediaController.PlaybackState is not (stateReady or stateBuffering))
		{
			return;
		}

		hasReportedForegroundServiceRefusal = true;
		MediaElement.ReportSourceUpdateException(new Android.App.ForegroundServiceStartNotAllowedException(foregroundServiceRefusalReason), foregroundServiceRefusalFailure, Logger);
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			disconnectTokenSource.Cancel();

			// MauiMediaElement.Dispose releases and disposes the player of its PlayerView, which can run before this does
			var isPlayerAlive = IsAlive(Player);
			if (isPlayerAlive)
			{
				Player?.Stop();
				Player?.RemoveListener(this);
			}

			// A seek that waits for the ready state of the released player would only end after its two minute timeout. It ends like a cancelled
			// seek, once the handler has finished disconnecting: no continuation of SeekTo may run in the middle of the disconnection
			if (seekToTaskCompletionSource is { Task.IsCompleted: false } pendingSeek)
			{
				Dispatcher.Dispatch(() => pendingSeek.TrySetCanceled());
			}

			// A source that is still loading must not finish loading into the released player
			cancellationTokenSource?.Cancel();
			cancellationTokenSource?.Dispose();
			cancellationTokenSource = null;

			if (isPlayerAlive && Player is IExoPlayer exoPlayer)
			{
				exoPlayer.Release();
				exoPlayer.Dispose();
			}
			else if (Player is MediaController mediaController)
			{
				if (isPlayerAlive)
				{
					ReleaseControllerAndItsPlayerRegistration(mediaController);
				}
				else if (isAndroidForegroundServiceEnabled)
				{
					// The PlayerView released the controller first: no release command can be sent, and another MediaElement can keep the service alive
					MediaControlsService.Running?.ReleasePlayer(AndroidPlayerConnectionKey);
				}
			}

			if (IsAlive(PlayerView))
			{
				PlayerView?.Player = null;
			}

			Player = null;
			PlayerView = null;
			localTrackSelector?.Dispose();
			localTrackSelector = null;

			currentStreamDataSourceFactory?.Dispose();
			currentStreamDataSourceFactory = null;

			client.Dispose();
		}

		base.Dispose(disposing);
	}

	// A disposed Java peer has no handle left, and every call on it throws ObjectDisposedException
	static bool IsAlive(Android.Runtime.IJavaObject? javaObject) => javaObject is not null && javaObject.Handle != IntPtr.Zero;

	// Media3 unbinds a released controller from the service only once every command it sent was answered, or after 30 seconds: releasing the
	// controller right after the release command would keep the service bound, and so alive, for that long.
	void ReleaseControllerAndItsPlayerRegistration(MediaController mediaController)
	{
		if (!isAndroidForegroundServiceEnabled)
		{
			mediaController.Release();
			mediaController.Dispose();
			return;
		}

		using var args = new Android.OS.Bundle();
		args.PutString(MediaSessionCallback.PlayerIdKey, AndroidPlayerConnectionKey);

		var commandResult = mediaController.SendCustomCommand(new SessionCommand(MediaSessionCallback.ReleasePlayer, new Android.OS.Bundle()), args);
		if (commandResult is null)
		{
			mediaController.Release();
			mediaController.Dispose();
			return;
		}

		commandResult.AddListener(new Runnable(() =>
		{
			try
			{
				if (commandResult.Get() is null)
				{
					throw new InvalidOperationException("MediaController.SendCustomCommand().Get() cannot be null");
				}
			}
			catch (Exception ex)
			{
				Trace.WriteLine($"Error releasing Android MediaElement player registration: {ex}");
			}
			finally
			{
				// MauiMediaElement.Dispose releases and disposes the controller of its PlayerView too, which can run before the answer arrives
				if (IsAlive(mediaController))
				{
					mediaController.Release();
					mediaController.Dispose();
				}
			}
		}), ContextCompat.GetMainExecutor(Platform.AppContext));
	}

	async Task<byte[]> GetBytesFromMetadataArtworkUrl(string url, CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrWhiteSpace(url))
		{
			return [];
		}

		Stream? stream = null;
		Uri.TryCreate(url, UriKind.Absolute, out var uri);

		try
		{
			byte[] artworkData = [];
			long? contentLength = null;

			// HTTP or HTTPS URL
			if (uri is not null &&
				(uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
			{
				var request = new HttpRequestMessage(HttpMethod.Head, url);
				var contentLengthResponse = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
				contentLength = contentLengthResponse.Content.Headers.ContentLength;

				var response = await client.GetAsync(url, HttpCompletionOption.ResponseContentRead, cancellationToken).ConfigureAwait(false);
				stream = response.IsSuccessStatusCode ? await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false) : null;
			}
			else if (uri is not null && uri.Scheme == Uri.UriSchemeFile)
			{
				// An absolute path parses as a file URI too: a file URI names its file by its local path, an absolute path is the path itself
				var normalizedFilePath = url.StartsWith($"{Uri.UriSchemeFile}:", StringComparison.OrdinalIgnoreCase) ? uri.LocalPath : NormalizeFilePath(url);

				stream = File.OpenRead(normalizedFilePath);
			}
			// Relative File Path
			else if (Uri.TryCreate(url, UriKind.Relative, out _))
			{
				var normalizedFilePath = NormalizeFilePath(url);

				stream = Platform.AppContext.Assets?.Open(normalizedFilePath) ?? throw new InvalidOperationException("Assets cannot be null");
			}

			if (stream is not null)
			{
				// A length announced by the server only sizes the buffer at first: a server can announce none (a chunked response) or a wrong one,
				// and the length of an asset stream (which cannot seek) is only known once it has been read
				using var memoryStream = new MemoryStream(contentLength is > 0 and <= int.MaxValue ? (int)contentLength.Value : 0);
				await stream.CopyToAsync(memoryStream, cancellationToken).ConfigureAwait(false);
				artworkData = memoryStream.ToArray();
			}

			return artworkData;
		}
		catch (Exception e)
		{
			Trace.WriteLine($"Unable to retrieve {nameof(MediaElement.MetadataArtworkUrl)} for {url}.{e}\n");
			return [];
		}
		finally
		{
			if (stream is not null)
			{
				stream.Close();
				await stream.DisposeAsync();
			}
		}

		static string NormalizeFilePath(string filePath) => filePath.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
	}

	async Task<MediaItem.Builder?> SetPlayerData(CancellationToken cancellationToken = default)
	{
		if (MediaElement.Source is null)
		{
			return null;
		}

		switch (MediaElement.Source)
		{
			case UriMediaSource uriMediaSource:
				{
					var uri = uriMediaSource.Uri;
					if (!string.IsNullOrWhiteSpace(uri?.AbsoluteUri))
					{
						return await CreateMediaItem(uri.AbsoluteUri, cancellationToken).ConfigureAwait(false);
					}

					break;
				}
			case FileMediaSource fileMediaSource:
				{
					var filePath = fileMediaSource.Path;
					if (!string.IsNullOrWhiteSpace(filePath))
					{
						return await CreateMediaItem(filePath, cancellationToken).ConfigureAwait(false);
					}

					break;
				}
			case ResourceMediaSource resourceMediaSource:
				{
					var package = PlayerView?.Context?.PackageName ?? "";
					var path = resourceMediaSource.Path;
					if (!string.IsNullOrWhiteSpace(path))
					{
						var assetFilePath = $"asset://{package}{Path.PathSeparator}{path}";
						return await CreateMediaItem(assetFilePath, cancellationToken).ConfigureAwait(false);
					}

					break;
				}
			case StreamMediaSource streamMediaSource:
				{
					if (streamMediaSource.Stream is not null)
					{
						return await CreateMediaItemFromStream(streamMediaSource.Stream, cancellationToken).ConfigureAwait(false);
					}

					break;
				}
			default:
				throw new NotSupportedException($"{MediaElement.Source.GetType().FullName} is not yet supported for {nameof(MediaElement.Source)}");
		}

		return null;
	}

	async Task<MediaItem.Builder> CreateMediaItem(string url, CancellationToken cancellationToken = default)
	{
		MediaMetadata.Builder mediaMetaData = new();
		mediaMetaData.SetArtist(MediaElement.MetadataArtist);
		mediaMetaData.SetTitle(MediaElement.MetadataTitle);
		var data = await GetBytesFromMetadataArtworkUrl(MediaElement.MetadataArtworkUrl, cancellationToken).ConfigureAwait(true);
		if (data is not null && data.Length > 0)
		{
			mediaMetaData.SetArtworkData(data, (Java.Lang.Integer)MediaMetadata.PictureTypeFrontCover);
		}

		var mediaItem = new MediaItem.Builder();
		mediaItem.SetUri(url);
		mediaItem.SetMediaId(url);
		mediaItem.SetMediaMetadata(mediaMetaData.Build());

		return mediaItem;
	}

	async Task<MediaItem.Builder> CreateMediaItemFromStream(Stream stream, CancellationToken cancellationToken)
	{
		MediaMetadata.Builder mediaMetaData = new();
		mediaMetaData.SetArtist(MediaElement.MetadataArtist);
		mediaMetaData.SetTitle(MediaElement.MetadataTitle);
		var data = await GetBytesFromMetadataArtworkUrl(MediaElement.MetadataArtworkUrl, cancellationToken).ConfigureAwait(true);
		if (data is not null && data.Length > 0)
		{
			mediaMetaData.SetArtworkData(data, (Java.Lang.Integer)MediaMetadata.PictureTypeFrontCover);
		}

		// Create MediaItem with metadata
		// The stream will be handled via custom data source factory when needed
		var mediaItem = new MediaItem.Builder();
		mediaItem.SetUri("stream://media");
		mediaItem.SetMediaId("stream://media");
		mediaItem.SetMediaMetadata(mediaMetaData.Build());

		// A source that was replaced while its artwork loaded is not played: its stream must not replace the data source of the current media
		if (!cancellationToken.IsCancellationRequested)
		{
			currentStreamDataSourceFactory?.Dispose();
			currentStreamDataSourceFactory = null;

			// Store the stream for later use with custom MediaSource
			currentStreamDataSourceFactory = new StreamDataSourceFactory(stream);
		}

		return mediaItem;
	}

	#region PlayerListener implementation method stubs
	public void OnAudioAttributesChanged(AudioAttributes? audioAttributes) { }
	public void OnAudioSessionIdChanged(int audioSessionId) { }
	public void OnAvailableCommandsChanged(PlayerCommands? player) { }
	public void OnCues(CueGroup? cues) { }
	public void OnDeviceInfoChanged(DeviceInfo? deviceInfo) { }
	public void OnDeviceVolumeChanged(int volume, bool muted) { }
	public void OnIsLoadingChanged(bool isLoading) { }
	public void OnIsPlayingChanged(bool isPlaying) { }
	public void OnLoadingChanged(bool isLoading) { }
	public void OnMaxSeekToPreviousPositionChanged(long maxSeekToPreviousPositionMs) { }
	public void OnMediaItemTransition(MediaItem? mediaItem, int reason) { }
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
	#endregion

	static class PlaybackState
	{
		public const int StateBuffering = 6;
		public const int StateConnecting = 8;
		public const int StateFailed = 7;
		public const int StateFastForwarding = 4;
		public const int StateNone = 0;
		public const int StatePaused = 2;
		public const int StatePlaying = 3;
		public const int StateRewinding = 5;
		public const int StateSkippingToNext = 10;
		public const int StateSkippingToPrevious = 9;
		public const int StateSkippingToQueueItem = 11;
		public const int StateStopped = 1;
		public const int StateError = 7;
	}
}