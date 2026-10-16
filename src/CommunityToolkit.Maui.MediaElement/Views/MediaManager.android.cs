using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Android.App;
using Android.Content;
using Android.Util;
using Android.Views;
using Android.Widget;
using AndroidX.Media3.Common;
using AndroidX.Media3.Common.Text;
using AndroidX.Media3.Common.Util;
using AndroidX.Media3.DataSource;
using AndroidX.Media3.ExoPlayer;
using AndroidX.Media3.ExoPlayer.Source;
using AndroidX.Media3.Session;
using AndroidX.Media3.UI;
using CommunityToolkit.Maui.Media.Services;
using CommunityToolkit.Maui.Services;
using CommunityToolkit.Maui.Views;
using Microsoft.Extensions.Logging;
using AudioAttributes = AndroidX.Media3.Common.AudioAttributes;
using DeviceInfo = AndroidX.Media3.Common.DeviceInfo;
using MediaMetadata = AndroidX.Media3.Common.MediaMetadata;

namespace CommunityToolkit.Maui.Core.Views;

public partial class MediaManager : Java.Lang.Object, IPlayerListener
{
	const int idleState = 1;
	const int bufferState = 2;
	const int readyState = 3;
	const int endedState = 4;

	readonly HttpClient client = new();
	readonly SemaphoreSlim seekToSemaphoreSlim = new(1, 1);
	bool isAndroidForegroundServiceEnabled = false;

	double? previousSpeed;
	float volumeBeforeMute = 1;

	TaskCompletionSource? seekToTaskCompletionSource;
	CancellationTokenSource? cancellationTokenSource;
	MediaSession? session;
	BoundServiceConnection? connection;
	StreamDataSourceFactory? currentStreamDataSourceFactory;

	/// <summary>
	/// The platform native counterpart of <see cref="MediaElement"/>.
	/// </summary>
	protected PlayerView? PlayerView { get; set; }

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

	public void UpdateNotifications()
	{
		if (connection?.Binder?.Service is null || !isAndroidForegroundServiceEnabled)
		{
			return;
		}

		if (session is not null && Player is not null)
		{
			connection.Binder.Service.UpdateNotifications(session, Player);
		}
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

		// ExoPlayer reports Media3's Player.STATE_* values (idleState to endedState), not android.media.session.PlaybackState values: the
		// PlaybackState arms only map the values that are not Media3 states.
		var newState = playbackState switch
		{
			// ExoPlayer is idle until a source is prepared: a source that is opening has not stopped, and a failed one stays failed
			idleState => MediaElement.CurrentState is MediaElementState.Opening or MediaElementState.Failed
				? MediaElement.CurrentState
				: MediaElementState.Stopped,

			bufferState => MediaElementState.Buffering,

			readyState => playWhenReady
				? MediaElementState.Playing
				: MediaElementState.Paused,

			endedState => MediaElementState.Stopped,

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
		if (playbackState is readyState)
		{
			MediaElement.Duration = TimeSpan.FromMilliseconds(Player.Duration < 0 ? 0 : Player.Duration);
			MediaElement.Position = TimeSpan.FromMilliseconds(Player.CurrentPosition < 0 ? 0 : Player.CurrentPosition);
		}
	}

	/// <summary>
	/// Creates the corresponding platform view of <see cref="MediaElement"/> on Android.
	/// </summary>
	/// <returns>The platform native counterpart of <see cref="MediaElement"/>.</returns>
	/// <exception cref="NullReferenceException">Thrown when <see cref="Context"/> is <see langword="null"/> or when the platform view could not be created.</exception>
	[MemberNotNull(nameof(Player), nameof(PlayerView), nameof(session))]
	public (PlatformMediaElement platformView, PlayerView PlayerView) CreatePlatformView(AndroidViewType androidViewType, bool isAndroidServiceEnabled)
	{
		Player = new ExoPlayerBuilder(MauiContext.Context).Build() ?? throw new InvalidOperationException("Player cannot be null");
		Player.AddListener(this);
		this.isAndroidForegroundServiceEnabled = isAndroidServiceEnabled;
		if (androidViewType is AndroidViewType.SurfaceView)
		{
			PlayerView = new PlayerView(MauiContext.Context)
			{
				Player = Player,
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
				Player = Player,
				UseController = false,
				ControllerAutoShow = false,
				LayoutParameters = new RelativeLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent)
			};
		}
		else
		{
			throw new NotSupportedException($"{androidViewType} is not yet supported");
		}

		var mediaSession = new MediaSession.Builder(Platform.AppContext, Player);
		mediaSession.SetId(Convert.ToBase64String(Guid.NewGuid().ToByteArray())[..8]);

		session ??= mediaSession.Build() ?? throw new InvalidOperationException("Session cannot be null");
		ArgumentNullException.ThrowIfNull(session.Id);

		return (Player, PlayerView);
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
			case bufferState:
				newState = MediaElementState.Buffering;
				break;
			case endedState:
				newState = MediaElementState.Stopped;
				MediaElement.MediaEnded();
				break;
			case readyState:
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

	#region PlayerListener implementation method stubs

	public void OnAudioAttributesChanged(AudioAttributes? audioAttributes)
	{
	}

	public void OnAudioSessionIdChanged(int audioSessionId)
	{
	}

	public void OnAvailableCommandsChanged(PlayerCommands? player)
	{
	}

	public void OnCues(CueGroup? cues)
	{
	}

	public void OnDeviceInfoChanged(DeviceInfo? deviceInfo)
	{
	}

	public void OnDeviceVolumeChanged(int volume, bool muted)
	{
	}

	public void OnEvents(IPlayer? player, PlayerEvents? playerEvents)
	{
	}

	public void OnIsLoadingChanged(bool isLoading)
	{
	}

	public void OnIsPlayingChanged(bool isPlaying)
	{
	}

	public void OnLoadingChanged(bool isLoading)
	{
	}

	public void OnMaxSeekToPreviousPositionChanged(long maxSeekToPreviousPositionMs)
	{
	}

	public void OnMediaItemTransition(MediaItem? mediaItem, int reason)
	{
	}

	public void OnMediaMetadataChanged(MediaMetadata? mediaMetadata)
	{
	}

	public void OnMetadata(Metadata? metadata)
	{
	}

	public void OnPlayWhenReadyChanged(bool playWhenReady, int reason)
	{
	}

	public void OnPositionDiscontinuity(PlayerPositionInfo? oldPosition, PlayerPositionInfo? newPosition, int reason)
	{
	}

	public void OnPlaybackSuppressionReasonChanged(int playbackSuppressionReason)
	{
	}

	public void OnPlayerErrorChanged(PlaybackException? error)
	{
	}

	public void OnPlaylistMetadataChanged(MediaMetadata? mediaMetadata)
	{
	}

	public void OnRenderedFirstFrame()
	{
	}

	public void OnRepeatModeChanged(int repeatMode)
	{
	}

	public void OnSeekBackIncrementChanged(long seekBackIncrementMs)
	{
	}

	public void OnSeekForwardIncrementChanged(long seekForwardIncrementMs)
	{
	}

	public void OnShuffleModeEnabledChanged(bool shuffleModeEnabled)
	{
	}

	public void OnSkipSilenceEnabledChanged(bool skipSilenceEnabled)
	{
	}

	public void OnSurfaceSizeChanged(int width, int height)
	{
	}

	public void OnTimelineChanged(Timeline? timeline, int reason)
	{
	}

	public void OnTrackSelectionParametersChanged(TrackSelectionParameters? trackSelectionParameters)
	{
	}

	public void OnTracksChanged(Tracks? tracks)
	{
	}

	protected virtual partial void PlatformPlay()
	{
		if (Player is null || MediaElement.Source is null)
		{
			return;
		}

		// The player has no media: prepared, it would end at once and report MediaEnded for a media that never played
		if (Player.MediaItemCount is 0)
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
		if (Player.PlaybackState is endedState)
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
			throw new InvalidOperationException($"{nameof(IExoPlayer)} is not yet initialized");
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

		Player.SeekTo(0);
		Player.Stop();
		MediaElement.Position = TimeSpan.Zero;

		// The stopped player is idle: it reports no ready state for a seek that Stop() interrupted, which has ended all the same. SeekTo completes
		// and raises SeekCompleted (IMediaElement.SeekTo), once the player has stopped, instead of waiting for its two minute timeout
		seekToTaskCompletionSource?.TrySetResult();
	}

	protected virtual async partial ValueTask PlatformUpdateSource()
	{
		if (Player is null)
		{
			return;
		}

		if (connection is null && isAndroidForegroundServiceEnabled)
		{
			StartService();
		}

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
		// An exception of an event handler is not, and propagates: of the events raised after this part (MediaOpened, or None for a source
		// without data), and of those that the player raises while it takes the source
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

				// The player raises its events synchronously while it takes the source (StateChanged for Buffering, for example): from here on, only
				// an exception of the player itself, a Java exception, is a failure of the source
				isPlayerTakingTheSource = true;
				if (mediaSource is not null)
				{
					Player.SetMediaSource(mediaSource);
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

			// The source cannot be applied. Like a source that cannot be played, it replaced the previous media, which must not keep playing nor
			// report its end for this source, and it is reported through MediaFailed (which also resets the duration)
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

		if (isAndroidForegroundServiceEnabled)
		{
			UpdateNotifications();
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
		// It is the MediaElement's: the player's is not set yet when the handler connects to a MediaElement that is muted already
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

	protected override void Dispose(bool disposing)
	{
		base.Dispose(disposing);

		if (disposing)
		{
			// A seek that waits for the ready state of the released player would only end after its two minute timeout. It ends like a cancelled
			// seek, once the handler has finished disconnecting: no continuation of SeekTo may run in the middle of the disconnection
			if (seekToTaskCompletionSource is { Task.IsCompleted: false } pendingSeek)
			{
				Dispatcher.Dispatch(() => pendingSeek.TrySetCanceled());
			}

			// The service keeps running while another MediaElement is bound to it: its notification must forget this session and player
			if (session is not null && Player is not null)
			{
				connection?.Binder?.Service.ForgetSession(session, Player);
			}

			session?.Release();
			session?.Dispose();
			session = null;

			// A source that is still loading must not finish loading into the released player
			cancellationTokenSource?.Cancel();
			cancellationTokenSource?.Dispose();
			cancellationTokenSource = null;

			if (connection is not null)
			{
				StopService(connection);
				connection.Dispose();
				connection = null;
			}

			currentStreamDataSourceFactory?.Dispose();
			currentStreamDataSourceFactory = null;

			client.Dispose();
		}
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
			// Absolute File Path or file URI
			else if (uri is not null && uri.Scheme == Uri.UriSchemeFile)
			{
				// A file URI names its file by its local path, an absolute path is the path itself
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

	void StartService()
	{
		if (!isAndroidForegroundServiceEnabled)
		{
			return;
		}

		var intent = new Intent(global::Android.App.Application.Context, typeof(MediaControlsService));
		connection = new BoundServiceConnection(this);
		connection.MediaControlsServiceTaskRemoved += HandleMediaControlsServiceTaskRemoved;

		global::Android.App.Application.Context.StartForegroundService(intent);
		global::Android.App.Application.Context.ApplicationContext?.BindService(intent, connection, Bind.AutoCreate);
	}

	void StopService(in BoundServiceConnection boundServiceConnection)
	{
		if (!isAndroidForegroundServiceEnabled)
		{
			return;
		}

		boundServiceConnection.MediaControlsServiceTaskRemoved -= HandleMediaControlsServiceTaskRemoved;

		var serviceIntent = new Intent(Platform.AppContext, typeof(MediaControlsService));
		global::Android.App.Application.Context.StopService(serviceIntent);
		Platform.AppContext.UnbindService(boundServiceConnection);
	}

	void HandleMediaControlsServiceTaskRemoved(object? sender, EventArgs e) => Player?.Stop();

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

		// The source has no data: there is no media item to play
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