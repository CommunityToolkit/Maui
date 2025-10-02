﻿using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Android.Content;
using Android.Views;
using AndroidX.Core.Content;
using AndroidX.Media3.Common;
using AndroidX.Media3.Common.Text;
using AndroidX.Media3.Common.Util;
using AndroidX.Media3.ExoPlayer;
using AndroidX.Media3.Session;
using AndroidX.Media3.UI;
using CommunityToolkit.Maui.Media.Services;
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

	double? previousSpeed;
	float volumeBeforeMute = 1;
	TaskCompletionSource? seekToTaskCompletionSource;
	CancellationTokenSource? cancellationTokenSource;
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

	/// <summary>
	/// Creates the corresponding platform view of <see cref="MediaElement"/> on Android.
	/// </summary>
	/// <returns>The platform native counterpart of <see cref="MediaElement"/>.</returns>
	/// <exception cref="NullReferenceException">Thrown when <see cref="Context"/> is <see langword="null"/> or when the platform view could not be created.</exception>
	[MemberNotNull(nameof(PlayerView))]
	public PlayerView CreatePlatformView(AndroidViewType androidViewType)
	{
		if (androidViewType is AndroidViewType.SurfaceView)
		{
			PlayerView = new PlayerView(MauiContext.Context)
			{
				UseController = false,
				ControllerAutoShow = false,
				LayoutParameters = new Android.Widget.RelativeLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent)
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
				UseController = false,
				ControllerAutoShow = false,
				LayoutParameters = new Android.Widget.RelativeLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent)
			};
		}
		else
		{
			throw new NotSupportedException($"{androidViewType} is not yet supported");
		}
		return PlayerView;
	}

	public async Task<AndroidX.Media3.Session.MediaController> CreateMediaController()
	{
		var tcs = new TaskCompletionSource();
		var future = new MediaController.Builder(Platform.AppContext, new SessionToken(Platform.AppContext, new ComponentName(Platform.AppContext, Java.Lang.Class.FromType(typeof(MediaControlsService))))).BuildAsync();
		future?.AddListener(new Runnable(() =>
		{
			try
			{
				var result = future.Get() ?? throw new InvalidOperationException("MediaController.Builder.BuildAsync().Get() returned null");
				if (result is MediaController mc)
				{
					Player = mc ?? throw new InvalidOperationException("MediaController cannot be set on Player");
					Player.AddListener(this);
					if (PlayerView is null)
					{
						throw new InvalidOperationException($"{nameof(PlayerView)} cannot be null");
					}
					PlayerView.SetBackgroundColor(Android.Graphics.Color.Black);
					PlayerView.Player = Player;
					var intent = new Intent(Android.App.Application.Context, typeof(MediaControlsService));
					Android.App.Application.Context.StartForegroundService(intent);
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
			}
		}), ContextCompat.GetMainExecutor(Platform.AppContext));
		await tcs.Task.WaitAsync(CancellationToken.None);
		return Player ?? throw new InvalidOperationException("MediaController is null");
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
			System.Diagnostics.Debug.WriteLine("IExoPlayer is null, cannot update source");
			return;
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
			cancellationTokenSource?.Dispose();
			cancellationTokenSource = null;
			
			Player?.Stop();
			Player?.ClearMediaItems();
			Player?.RemoveListener(this);
			Player?.Release();
			Player?.Dispose();
			Player = null;
			PlayerView?.Dispose();
			PlayerView = null;
			client.Dispose();
			var serviceIntent = new Intent(Platform.AppContext, typeof(MediaControlsService));
			Android.App.Application.Context.StopService(serviceIntent);
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

	#region PlayerListener implementation method stubs
	public void OnAudioAttributesChanged(AudioAttributes? audioAttributes) { }
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