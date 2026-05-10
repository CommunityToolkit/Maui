using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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

	static readonly HttpClient client = new();
	readonly SemaphoreSlim seekToSemaphoreSlim = new(1, 1);
	bool isAndroidForegroundServiceEnabled = false;
	bool hasPendingSourceUpdate;
	DefaultTrackSelector? localTrackSelector;

	double? previousSpeed;
	float volumeBeforeMute = 1;
	TaskCompletionSource? seekToTaskCompletionSource;
	MediaItem.Builder? mediaItem;

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

		var newState = playbackState switch
		{
			stateBuffering => MediaElementState.Buffering,
			stateEnded => MediaElementState.Stopped,
			stateReady => playWhenReady
				? MediaElementState.Playing
				: MediaElementState.Paused,
			stateIdle => MediaElementState.None,
			_ => MediaElementState.None,
		};

		MediaElement.CurrentStateChanged(newState);

		if (playbackState is stateReady)
		{
			MediaElement.Duration = TimeSpan.FromMilliseconds(
				Player.Duration < 0 ? 0 : Player.Duration
			);
			MediaElement.Position = TimeSpan.FromMilliseconds(
				Player.CurrentPosition < 0 ? 0 : Player.CurrentPosition
			);
		}
		else if (playbackState is stateEnded)
		{
			MediaElement.MediaEnded();
		}
	}

	public void OnIsPlayingChanged(bool isPlaying)
	{
		if (Player is null || MediaElement.Source is null)
		{
			return;
		}

		var newState = isPlaying
			? MediaElementState.Playing
			: MediaElementState.Paused;

		MediaElement.CurrentStateChanged(newState);
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
		=> isAndroidForegroundServiceEnabled
		? await CreateMediaController(cancellationToken)
		: CreateLocalPlayer();

	internal ValueTask SynchronizePlayerStateAsync()
	{
		PlatformUpdateAspect();
		PlatformUpdateShouldShowPlaybackControls();
		PlatformUpdateShouldKeepScreenOn();
		PlatformUpdateVolume();
		PlatformUpdateShouldMute();
		PlatformUpdateShouldLoopPlayback();
		PlatformUpdateSpeed();

		if (hasPendingSourceUpdate || MediaElement.Source is not null)
		{
			return PlatformUpdateSource();
		}

		return ValueTask.CompletedTask;
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

		var audioAttribute = new AndroidX.Media3.Common.AudioAttributes.Builder()?
			.SetContentType(C.AudioContentTypeMusic)?
			.SetUsage(C.UsageMedia)?
			.Build();

		localTrackSelector = new DefaultTrackSelector(Platform.AppContext);
		var trackSelectionParameters = localTrackSelector.BuildUponParameters()?
			.SetPreferredAudioLanguage(C.LanguageUndetermined)?
			.SetPreferredTextLanguage(C.LanguageUndetermined)?
			.SetIgnoredTextSelectionFlags(C.SelectionFlagAutoselect);
		localTrackSelector.SetParameters((DefaultTrackSelector.Parameters.Builder?)trackSelectionParameters);

		var loadControlBuilder = new DefaultLoadControl.Builder();
		loadControlBuilder.SetBufferDurationsMs(
			minBufferMs: 15000,
			maxBufferMs: 50000,
			bufferForPlaybackMs: 2500,
			bufferForPlaybackAfterRebufferMs: 5000);

		var builder = new ExoPlayerBuilder(Platform.AppContext) ?? throw new InvalidOperationException("ExoPlayerBuilder returned null");
		builder.SetTrackSelector(localTrackSelector);
		builder.SetAudioAttributes(audioAttribute, true);
		builder.SetHandleAudioBecomingNoisy(true);
		builder.SetLoadControl(loadControlBuilder.Build());
		var exoPlayer = builder.Build() ?? throw new InvalidOperationException("ExoPlayerBuilder.Build() returned null");

		Player = exoPlayer;
		Player.AddListener(this);
		PlayerView.SetBackgroundColor(Android.Graphics.Color.Black);
		PlayerView.Player = Player;

		return exoPlayer;
	}
	public async Task<AndroidX.Media3.Session.MediaController> CreateMediaController(CancellationToken cancellationToken = default)
	{
		if (Player is MediaController mediaController)
		{
			return mediaController;
		}

		var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		using var connectionHints = new Android.OS.Bundle();
		connectionHints.PutString(MediaSessionCallback.PlayerIdKey, MediaElement.AndroidPlayerId);

		var builder = new MediaController.Builder(Platform.AppContext, new SessionToken(Platform.AppContext, new ComponentName(Platform.AppContext, Java.Lang.Class.FromType(typeof(MediaControlsService)))));
		builder.SetConnectionHints(connectionHints);
		var future = builder.BuildAsync();
		future?.AddListener(new Runnable(() =>
		{
			try
			{
				var result = future.Get() ?? throw new InvalidOperationException("MediaController.Builder.BuildAsync().Get() returned null");
				if (result is MediaController mc)
				{
					Player = mc;
					Player.AddListener(this);
					if (PlayerView is null)
					{
						throw new InvalidOperationException($"{nameof(PlayerView)} cannot be null");
					}

					PlayerView.SetBackgroundColor(Android.Graphics.Color.Black);
					PlayerView.Player = Player;

					if (isAndroidForegroundServiceEnabled)
					{
						using var intent = new Intent(Android.App.Application.Context, typeof(MediaControlsService));
						Android.App.Application.Context.StartForegroundService(intent);
					}

					tcs.SetResult();
				}
				else
				{
					tcs.SetException(new InvalidOperationException("MediaController.Builder.BuildAsync().Get() did not return a MediaController"));
				}
			}
			catch (Exception ex)
			{
				Trace.WriteLine($"Error creating MediaController: {ex}");
				tcs.SetException(ex);
			}
		}), ContextCompat.GetMainExecutor(Platform.AppContext));
		await tcs.Task.WaitAsync(cancellationToken);
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
				// Update duration and position when ready
				if (Player is not null)
				{
					MediaElement.Duration = TimeSpan.FromMilliseconds(
						Player.Duration < 0 ? 0 : Player.Duration
					);
					MediaElement.Position = TimeSpan.FromMilliseconds(
						Player.CurrentPosition < 0 ? 0 : Player.CurrentPosition
					);
				}
				break;
			case stateIdle:
				newState = MediaElementState.None;
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
	}

	protected virtual async partial ValueTask PlatformUpdateSource()
	{
		var hasSetSource = false;

		if (Player is null)
		{
			hasPendingSourceUpdate = true;
			System.Diagnostics.Trace.WriteLine("IExoPlayer is null, cannot update source");
			return;
		}

		hasPendingSourceUpdate = false;

		if (MediaElement.Source is null)
		{
			Player.ClearMediaItems();
			MediaElement.Duration = TimeSpan.Zero;
			MediaElement.CurrentStateChanged(MediaElementState.None);

			return;
		}

		MediaElement.CurrentStateChanged(MediaElementState.Opening);
		Player.PlayWhenReady = MediaElement.ShouldAutoPlay;
		var result = await SetPlayerData().ConfigureAwait(true);
		var item = result?.Build();

		if (item?.MediaMetadata is not null)
		{
			if (MediaElement.Source is UriMediaSource uriMediaSource && uriMediaSource.HttpHeaders.Count > 0 && Player is IExoPlayer exoPlayer)
			{
				var httpDataSourceFactory = new DefaultHttpDataSource.Factory();
				httpDataSourceFactory.SetDefaultRequestProperties(uriMediaSource.HttpHeaders);

				var mediaSourceFactory = new DefaultMediaSourceFactory(httpDataSourceFactory);
				var mediaSource = mediaSourceFactory.CreateMediaSource(item) ?? throw new InvalidOperationException("Failed to create media source.");
				exoPlayer.SetMediaSource(mediaSource);
			}
			else
			{
				Player.SetMediaItem(item);
			}

			Player.Prepare();
			hasSetSource = true;
		}

		if (hasSetSource)
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
		if (MediaElement.ShouldMute)
		{
			volumeBeforeMute = Player.Volume;
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
			seekToSemaphoreSlim?.Dispose();
			ReleasePlayerRegistration();
			Player?.RemoveListener(this);

			if (Player is IExoPlayer exoPlayer)
			{
				exoPlayer.Release();
				exoPlayer.Dispose();
			}
			else if (Player is MediaController mediaController)
			{
				mediaController.Release();
				mediaController.Dispose();
			}

			Player = null;
			PlayerView?.Player = null;
			PlayerView?.Dispose();
			PlayerView = null;
			localTrackSelector?.Dispose();
			localTrackSelector = null;
		}
	}

	void ReleasePlayerRegistration()
	{
		if (!isAndroidForegroundServiceEnabled || Player is not MediaController mediaController)
		{
			return;
		}

		using var args = new Android.OS.Bundle();
		args.PutString(MediaSessionCallback.PlayerIdKey, MediaElement.AndroidPlayerId);

		var commandResult = mediaController.SendCustomCommand(new SessionCommand(MediaSessionCallback.ReleasePlayer, new Android.OS.Bundle()), args);
		commandResult?.AddListener(new Runnable(() =>
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
		}), ContextCompat.GetMainExecutor(Platform.AppContext));
	}

	async Task<MediaItem.Builder?> SetPlayerData()
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
						return await CreateMediaItem(uri.AbsoluteUri).ConfigureAwait(true);
					}

					break;
				}
			case FileMediaSource fileMediaSource:
				{
					var filePath = fileMediaSource.Path;
					if (!string.IsNullOrWhiteSpace(filePath))
					{
						return await CreateMediaItem(filePath).ConfigureAwait(true);
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
						return await CreateMediaItem(assetFilePath).ConfigureAwait(true);
					}

					break;
				}
			default:
				throw new NotSupportedException($"{MediaElement.Source.GetType().FullName} is not yet supported for {nameof(MediaElement.Source)}");
		}

		return mediaItem;
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

		mediaItem = new MediaItem.Builder();
		mediaItem.SetUri(url);
		mediaItem.SetMediaId(url);
		mediaItem.SetMediaMetadata(mediaMetaData.Build());

		return mediaItem;
	}

	static async Task<byte[]> GetBytesFromMetadataArtworkUrl(string url, CancellationToken cancellationToken = default)
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
				contentLength = contentLengthResponse.Content.Headers.ContentLength ?? 0;

				var response = await client.GetAsync(url, HttpCompletionOption.ResponseContentRead, cancellationToken).ConfigureAwait(false);
				stream = response.IsSuccessStatusCode ? await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false) : null;
			}
			// Absolute File Path
			else if (uri is not null && uri.Scheme == Uri.UriSchemeFile)
			{
				var normalizedFilePath = NormalizeFilePath(url);

				stream = File.Open(normalizedFilePath, FileMode.Create);
				contentLength = await GetByteCountFromStream(stream, cancellationToken);
			}
			// Relative File Path
			else if (Uri.TryCreate(url, UriKind.Relative, out _))
			{
				var normalizedFilePath = NormalizeFilePath(url);

				stream = Platform.AppContext.Assets?.Open(normalizedFilePath) ?? throw new InvalidOperationException("Assets cannot be null");
				contentLength = await GetByteCountFromStream(stream, cancellationToken);
			}

			if (stream is not null)
			{
				if (!contentLength.HasValue)
				{
					throw new InvalidOperationException($"{nameof(contentLength)} must be set when {nameof(stream)} is not null");
				}

				artworkData = new byte[contentLength.Value];
				using var memoryStream = new MemoryStream(artworkData);
				await stream.CopyToAsync(memoryStream, cancellationToken).ConfigureAwait(false);
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

		static async ValueTask<long> GetByteCountFromStream(Stream stream, CancellationToken token)
		{
			if (stream.CanSeek)
			{
				return stream.Length;
			}

			long countedStreamBytes = 0;

			var buffer = new byte[8192];
			int bytesRead;

			while ((bytesRead = await stream.ReadAsync(buffer, token)) > 0)
			{
				countedStreamBytes += bytesRead;
			}

			return countedStreamBytes;
		}
	}

	#region PlayerListener implementation method stubs
	public void OnAudioAttributesChanged(AudioAttributes? audioAttributes) { }
	public void OnAudioSessionIdChanged(int audioSessionId) { }
	public void OnAvailableCommandsChanged(PlayerCommands? player) { }
	public void OnCues(CueGroup? cues) { }
	public void OnDeviceInfoChanged(DeviceInfo? deviceInfo) { }
	public void OnDeviceVolumeChanged(int volume, bool muted) { }
	public void OnEvents(IPlayer? player, PlayerEvents? playerEvents) { }
	public void OnIsLoadingChanged(bool isLoading) { }
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
}