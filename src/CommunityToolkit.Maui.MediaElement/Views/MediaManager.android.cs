﻿using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Android.Content;
using Android.Graphics;
using Android.Views;
using Android.Widget;
using AndroidX.Media3.Common;
using AndroidX.Media3.Common.Text;
using AndroidX.Media3.Common.Util;
using AndroidX.Media3.ExoPlayer;
using AndroidX.Media3.Session;
using AndroidX.Media3.UI;
using CommunityToolkit.Maui.Media.Services;
using CommunityToolkit.Maui.Services;
using CommunityToolkit.Maui.Views;
using Microsoft.Extensions.Logging;
using DeviceInfo = AndroidX.Media3.Common.DeviceInfo;
using MediaMetadata = AndroidX.Media3.Common.MediaMetadata;

namespace CommunityToolkit.Maui.Core.Views;

public partial class MediaManager : Java.Lang.Object, IPlayerListener
{
	const int bufferState = 2;
	const int readyState = 3;
	const int endedState = 4;

	static readonly HttpClient client = new();
	readonly SemaphoreSlim seekToSemaphoreSlim = new(1, 1);

	double? previousSpeed;
	float volumeBeforeMute = 1;

	TaskCompletionSource? seekToTaskCompletionSource;
	CancellationTokenSource? cancellationTokenSource;
	MediaSession? session;
	MediaItem.Builder? mediaItem;
	BoundServiceConnection? connection;

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
		if (connection?.Binder?.Service is null)
		{
			System.Diagnostics.Trace.TraceInformation("Notification Service not running.");
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

		var newState = playbackState switch
		{
			PlaybackState.StateFastForwarding
				or PlaybackState.StateRewinding
				or PlaybackState.StateSkippingToNext
				or PlaybackState.StateSkippingToPrevious
				or PlaybackState.StateSkippingToQueueItem
				or PlaybackState.StatePlaying => playWhenReady
					? MediaElementState.Playing
					: MediaElementState.Paused,

			PlaybackState.StatePaused => MediaElementState.Paused,

			PlaybackState.StateConnecting
				or PlaybackState.StateBuffering => MediaElementState.Buffering,

			PlaybackState.StateNone => MediaElementState.None,
			PlaybackState.StateStopped => MediaElement.CurrentState is not MediaElementState.Failed
				? MediaElementState.Stopped
				: MediaElementState.Failed,

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
	public (PlatformMediaElement platformView, PlayerView PlayerView) CreatePlatformView(AndroidViewType androidViewType)
	{
		Player = new ExoPlayerBuilder(MauiContext.Context).Build() ?? throw new InvalidOperationException("Player cannot be null");
		Player.AddListener(this);

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

			var attributes = Android.Util.Xml.AsAttributeSet(xmlResource)!;

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
			return;
		}

		if (connection is null)
		{
			StartService();
		}

		if (MediaElement.Source is null)
		{
			Player.ClearMediaItems();
			MediaElement.Duration = TimeSpan.Zero;
			MediaElement.CurrentStateChanged(MediaElementState.None);

			return;
		}

		MediaElement.CurrentStateChanged(MediaElementState.Opening);
		Player.PlayWhenReady = MediaElement.ShouldAutoPlay;
		cancellationTokenSource ??= new();
		// ConfigureAwait(true) is required to prevent crash on startup
		var result = await SetPlayerData(cancellationTokenSource.Token).ConfigureAwait(true);
		var item = result?.Build();

		if (item?.MediaMetadata is not null)
		{
			Player.SetMediaItem(item);
			Player.Prepare();
			hasSetSource = true;
		}

		if (hasSetSource && Player.PlayerError is null)
		{
			MediaElement.MediaOpened();
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
			session?.Release();
			session?.Dispose();
			session = null;

			cancellationTokenSource?.Dispose();
			cancellationTokenSource = null;

			if (connection is not null)
			{
				StopService(connection);
				connection.Dispose();
				connection = null;
			}

			client.Dispose();
		}
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
			Trace.WriteLine($"Unable to retrieve {nameof(MediaElement.MetadataArtworkSource)} for {url}.{e}\n");
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

		static string NormalizeFilePath(string filePath) => filePath.Replace('\\', System.IO.Path.DirectorySeparatorChar).Replace('/', System.IO.Path.DirectorySeparatorChar);

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

	[MemberNotNull(nameof(connection))]
	void StartService()
	{
		var intent = new Intent(Android.App.Application.Context, typeof(MediaControlsService));
		connection = new BoundServiceConnection(this);
		connection.MediaControlsServiceTaskRemoved += HandleMediaControlsServiceTaskRemoved;

		Android.App.Application.Context.StartForegroundService(intent);
		Android.App.Application.Context.ApplicationContext?.BindService(intent, connection, Bind.AutoCreate);
	}

	void StopService(in BoundServiceConnection boundServiceConnection)
	{
		boundServiceConnection.MediaControlsServiceTaskRemoved -= HandleMediaControlsServiceTaskRemoved;

		var serviceIntent = new Intent(Platform.AppContext, typeof(MediaControlsService));
		Android.App.Application.Context.StopService(serviceIntent);
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
						var assetFilePath = $"asset://{package}{System.IO.Path.PathSeparator}{path}";
						return await CreateMediaItem(assetFilePath, cancellationToken).ConfigureAwait(false);
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
		var data = await GetImageFromMediaSource(MediaElement.MetadataArtworkSource, cancellationToken).ConfigureAwait(true) ?? BlankByteArray();
		mediaMetaData.SetArtworkData(data, (Java.Lang.Integer)MediaMetadata.PictureTypeFrontCover);

		mediaItem = new MediaItem.Builder();
		mediaItem.SetUri(url);
		mediaItem.SetMediaId(url);
		mediaItem.SetMediaMetadata(mediaMetaData.Build());

		return mediaItem;
	}

	#region PlayerListener implementation method stubs
	public void OnAudioAttributesChanged(AudioAttributes? audioAttributes) { }
	public void OnAvailableCommandsChanged(PlayerCommands? player) { }
	public void OnCues(CueGroup? cues) { }
	public void OnDeviceInfoChanged(DeviceInfo? deviceInfo) { }
	public void OnDeviceVolumeChanged(int volume, bool muted) { }
	public void OnEvents(IPlayer? player, PlayerEvents? playerEvents) { }
	public void OnIsLoadingChanged(bool isLoading) { }
	public void OnIsPlayingChanged(bool isPlaying) { }
	public void OnLoadingChanged(bool isLoading) { }
	public void OnMaxSeekToPreviousPositionChanged(long maxSeekToPreviousPositionMs) { }
	public void OnMediaItemTransition(MediaItem? mediaItem, int reason) { }
	public void OnMediaMetadataChanged(MediaMetadata? mediaMetadata) { }
	public void OnPlayWhenReadyChanged(bool playWhenReady, int reason) { }
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
	public void OnTracksChanged(Tracks? tracks) { }
	#endregion
	static async Task<byte[]?> GetImageFromMediaSource(MediaSource? mediaSource, CancellationToken cancellationToken = default)
	{
		if (mediaSource is null)
		{
			return null;
		}
		var artworkUrl = mediaSource;
		if (artworkUrl is UriMediaSource uriMediaSource)
		{
			var uri = uriMediaSource.Uri;
			if (string.IsNullOrWhiteSpace(uri?.AbsoluteUri))
			{
				System.Diagnostics.Trace.TraceInformation("Arkwork Uri is null or empty");
				return null;
			}
			return await GetBytesFromMetadataArtworkUrl(uri.AbsoluteUri, cancellationToken).ConfigureAwait(false);
		}
		else if (artworkUrl is FileMediaSource fileMediaSource)
		{
			var filePath = fileMediaSource.Path;
			if(string.IsNullOrWhiteSpace(filePath))
			{
				System.Diagnostics.Trace.TraceInformation("Arkwork File path is null or empty");
				return null;
			}
			return await GetByteArrayFromFile(filePath, cancellationToken).ConfigureAwait(false);
		}
		else if (artworkUrl is ResourceMediaSource resourceMediaSource)
		{
			var path = resourceMediaSource.Path;
			var item = path?[(path.LastIndexOf('/') + 1)..];
			if (string.IsNullOrWhiteSpace(item))
			{
				System.Diagnostics.Trace.TraceInformation("Arkwork Resource path is null or empty");
				return null;
			}
			return await GetByteArrayFromResource(item).ConfigureAwait(false);
		}
		return null;
	}
	
	static async Task<byte[]?> GetByteArrayFromResource(string resource)
	{
		// Android file system does not report size of file so we need to use a buffer for the stream.
		using var stream = await FileSystem.Current.OpenAppPackageFileAsync(resource);
		if(stream is null)
		{
			return null;
		}
		using var memoryStream = new MemoryStream();
		await stream.CopyToAsync(memoryStream, CancellationToken.None).ConfigureAwait(false);
		var bytes = memoryStream.ToArray();
		return bytes;
	}

	static async Task<byte[]?> GetByteArrayFromFile(string filePath, CancellationToken cancellationToken = default)
	{
		if(!File.Exists(filePath))
		{
			System.Diagnostics.Trace.TraceInformation("Arkwork File does not exist");
			return null;
		}
		var stream = File.OpenRead(filePath);
		var memoryStream = new MemoryStream();
		await stream.CopyToAsync(memoryStream, cancellationToken).ConfigureAwait(false);
		var bytes = memoryStream.ToArray();
		return bytes;
	}
	static byte[] BlankByteArray()
	{
		var bitmapConfig = Bitmap.Config.Argb8888 ?? throw new InvalidOperationException("Bitmap config cannot be null");
		var bitmap = Bitmap.CreateBitmap(1024, 768, bitmapConfig, true);

		Canvas canvas = new();
		canvas.SetBitmap(bitmap);
		canvas.DrawColor(Android.Graphics.Color.Transparent);
		canvas.Save();

		MemoryStream stream = new();
		var format = Bitmap.CompressFormat.Png ?? throw new InvalidOperationException("Bitmap format cannot be null");
		bitmap.Compress(format, 100, stream);
		stream.Position = 0;
		return stream.ToArray();
	}
}
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