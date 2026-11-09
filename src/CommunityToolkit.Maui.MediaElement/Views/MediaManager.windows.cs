using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using CommunityToolkit.Maui.Core.Extensions;
using CommunityToolkit.Maui.Core.Primitives;
using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Views;
using Microsoft.Extensions.Logging;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Dispatching;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Media;
using Windows.Media.Playback;
using Windows.Media.Streaming.Adaptive;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.System.Display;
using HttpClient = Windows.Web.Http.HttpClient;
using HttpMethod = Windows.Web.Http.HttpMethod;
using HttpRequestMessage = Windows.Web.Http.HttpRequestMessage;
using ParentWindow = CommunityToolkit.Maui.Extensions.PageExtensions.ParentWindow;
using WindowsMediaElement = Windows.Media.Playback.MediaPlayer;
using WinMediaSource = Windows.Media.Core.MediaSource;

namespace CommunityToolkit.Maui.Core.Views;

partial class MediaManager : IDisposable
{
	// States that allow changing position
	readonly IReadOnlyList<MediaElementState> allowUpdatePositionStates =
	[
		MediaElementState.Playing,
		MediaElementState.Paused,
		MediaElementState.Stopped,
	];

	// UpdateMetadata runs on the thread that raised MediaOpened and Dispose on the UI thread: the lock keeps a MediaOpened that is still being
	// handled from using metadata while Dispose releases it, and from subscribing to the controls again once Dispose cleared them
	readonly Lock metadataLock = new();
	Metadata? metadata;
	SystemMediaTransportControls? systemMediaControls;
	HttpClient? headerHttpClient;
	AdaptiveMediaSource? adaptiveMediaSource;

	// Player.MediaPlayer is a XAML property that only the UI thread can read; the player it returns, kept here, can be used on any thread.
	WindowsMediaElement? mediaPlayer;

	// Every source update, and Dispose, changes sourceUpdateVersion: an update that awaits (a file, a header source) applies its source only
	// while it is still the latest one. The lock keeps the version and the adaptive source of the latest update together across threads.
	readonly Lock sourceUpdateLock = new();
	int sourceUpdateVersion;

	// The requests to keep display active are cumulative, this bool makes sure it only gets requested once
	bool displayActiveRequested;

	// keepReportingStopped and isStopSettling change together: Play(), Stop() and a new source run on the UI thread, the native state changes arrive
	// on other threads. Nothing calls the native player while it holds the lock.
	readonly Lock playbackStateLock = new();

	// After Stopped is reported (Stop() or the end of the media) the native player pauses, and can buffer (a network source loading the
	// position Stop() rewound to, for example), before it settles: those state changes must not overwrite Stopped. Like Play(), a native
	// Playing once the stop has settled clears it: that play did not go through Play() (the play button of the transport controls, for
	// example, plays the native player directly).
	bool keepReportingStopped;

	// The Playing state of a playback that Stop() interrupted can still be reported after Stopped: PlaybackStateChanged carries no state, so
	// its handler reads the live state of the player, which stays Playing until the pause of Stop() has taken effect. Until the player reports
	// that pause (Paused), a Playing is stale and does not overwrite Stopped either. A Play() that the player has not started yet when Stop()
	// reads its state (still Paused, as right after Play() in the same UI-thread turn) does not set it: the pause of Stop() cancels that play,
	// and the player then reports no state at all (Windows 11 26200), so there is no pause to wait for. Should a player start such a play
	// anyway after Stop() read Paused, its Playing is reported as a new play.
	bool isStopSettling;

	// A failed source update took the previous media out of the player: the states that the player reports while it closes that media must
	// not overwrite Failed.
	volatile bool keepReportingFailed;

	/// <summary>
	/// The <see cref="DisplayRequest"/> is used to enable the <see cref="MediaElement.ShouldKeepScreenOn"/> functionality.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Calls to <see cref="DisplayRequest.RequestActive"/> and <see cref="DisplayRequest.RequestRelease"/> should be in balance.
	/// Not doing so will result in the screen staying on and negatively impacting the environment :(
	/// </para>
	/// <para>
	/// Play() makes the request while <see cref="MediaElement.ShouldKeepScreenOn"/> is set, as does turning it on while the media is playing, paused
	/// or stopped. Pause(), Stop(), turning it off, disposing the manager, and a source update that takes the media out of the player (a
	/// <see langword="null"/> source, a source without data, or a source that cannot be applied) end the request.
	/// </para>
	/// </remarks>
	protected DisplayRequest DisplayRequest { get; } = new();

	/// <summary>
	/// Creates the corresponding platform view of <see cref="MediaElement"/> on Windows.
	/// </summary>
	/// <returns>The platform native counterpart of <see cref="MediaElement"/>.</returns>
	public PlatformMediaElement CreatePlatformView()
	{
		Player = new();
		mediaPlayer = new();
		mediaPlayer.MediaOpened += OnMediaElementMediaOpened;

		Player.SetMediaPlayer(mediaPlayer);
		Player.MediaPlayer.PlaybackSession.NaturalVideoSizeChanged += OnNaturalVideoSizeChanged;
		Player.MediaPlayer.PlaybackSession.PlaybackRateChanged += OnPlaybackSessionPlaybackRateChanged;
		Player.MediaPlayer.PlaybackSession.PlaybackStateChanged += OnPlaybackSessionPlaybackStateChanged;
		Player.MediaPlayer.PlaybackSession.SeekCompleted += OnPlaybackSessionSeekCompleted;
		Player.MediaPlayer.MediaFailed += OnMediaElementMediaFailed;
		Player.MediaPlayer.MediaEnded += OnMediaElementMediaEnded;
		Player.MediaPlayer.VolumeChanged += OnMediaElementVolumeChanged;
		Player.MediaPlayer.IsMutedChanged += OnMediaElementIsMutedChanged;

		// Off until media with metadata opens (Metadata.SetMetadata): a MediaPlayer whose system media transport controls are on is a system
		// media session, which the system media overlay shows and the media keys control, whatever it plays
		Player.MediaPlayer.SystemMediaTransportControls.IsEnabled = false;
		systemMediaControls = Player.MediaPlayer.SystemMediaTransportControls;

		return Player;
	}

	/// <summary>
	/// Releases the managed and unmanaged resources used by the <see cref="MediaManager"/>.
	/// </summary>
	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	protected virtual partial void PlatformPlay()
	{
		lock (playbackStateLock)
		{
			// The native states of the playback that starts (Buffering first, for a network source) are reported as they come
			keepReportingStopped = isStopSettling = false;
		}

		Player?.MediaPlayer.Play();

		if (MediaElement.ShouldKeepScreenOn
			&& !displayActiveRequested)
		{
			DisplayRequest.RequestActive();
			displayActiveRequested = true;
		}
	}

	protected virtual partial void PlatformPause()
	{
		Player?.MediaPlayer.Pause();

		if (displayActiveRequested)
		{
			DisplayRequest.RequestRelease();
			displayActiveRequested = false;
		}
	}

	protected virtual async partial Task PlatformSeek(TimeSpan position, CancellationToken token)
	{
		// Seek can be called on any thread, so whether the media can seek, and its duration, are read from the MediaPlayer itself (once: Dispose
		// can clear the field meanwhile) before the update is dispatched.
		if (Player is not null && mediaPlayer is WindowsMediaElement seekablePlayer && CanSeekMedia(seekablePlayer))
		{
			// A cancelled seek must not queue its update: WaitAsync(token) stops the wait for a queued update, not the update
			token.ThrowIfCancellationRequested();

			position = ClampToTheMedia(position, seekablePlayer.PlaybackSession.NaturalDuration);

			if (Dispatcher.IsDispatchRequired)
			{
				await Dispatcher.DispatchAsync(() =>
				{
					// The seek can have been cancelled while the update waited in the queue
					if (!token.IsCancellationRequested)
					{
						UpdatePosition(Player, position);
					}
				}).WaitAsync(token);
			}
			else
			{
				UpdatePosition(Player, position);
			}
		}

		static void UpdatePosition(in MediaPlayerElement? mediaPlayerElement, in TimeSpan position)
		{
			// A queued update can run after the platform view was disposed, which detached the player (SetMediaPlayer(null))
			if (mediaPlayerElement?.MediaPlayer is WindowsMediaElement player)
			{
				player.Position = position;
			}
		}
	}

	// Neither CanSeek nor the playback state alone tells whether the player has media (Windows 11 26200): a MediaPlayer without media reports
	// CanSeek, and takes a position, while its state is None (its state until media is set), and once Pause() has made such a player Paused,
	// reading its CanSeek throws COMException (E_FAIL). MediaPlayer.Source is no witness either: SetUriSource and SetFileSource, which the
	// source update uses, leave it null.
	static bool CanSeekMedia(MediaPlayer player)
	{
		if (player.PlaybackSession.PlaybackState is MediaPlaybackState.None)
		{
			return false;
		}

		try
		{
			return player.CanSeek;
		}
		catch (COMException)
		{
			return false;
		}
	}

	protected virtual partial void PlatformStop()
	{
		if (Player is null)
		{
			return;
		}

		var nativeState = Player.MediaPlayer.PlaybackSession.PlaybackState;

		lock (playbackStateLock)
		{
			// Set before the pause: the PlaybackStateChanged(Paused) it raises arrives on another thread, after Stopped was reported
			keepReportingStopped = true;

			isStopSettling = nativeState is MediaPlaybackState.Opening or MediaPlaybackState.Buffering or MediaPlaybackState.Playing;
		}

		// There's no Stop method so pause the video and reset its position
		Player.MediaPlayer.Pause();

		// Without media that can seek there is no position to reset, and the platform may refuse to set one
		if (CanSeekMedia(Player.MediaPlayer))
		{
			Player.MediaPlayer.Position = TimeSpan.Zero;
		}

		MediaElement.CurrentStateChanged(MediaElementState.Stopped);

		if (displayActiveRequested)
		{
			DisplayRequest.RequestRelease();
			displayActiveRequested = false;
		}
	}

	protected virtual partial void PlatformUpdateAspect()
	{
		if (Player is null)
		{
			return;
		}

		Player.Stretch = MediaElement.Aspect switch
		{
			Aspect.Fill => Microsoft.UI.Xaml.Media.Stretch.Fill,
			Aspect.AspectFill => Microsoft.UI.Xaml.Media.Stretch.UniformToFill,
			_ => Microsoft.UI.Xaml.Media.Stretch.Uniform,
		};
	}

	protected virtual partial void PlatformUpdateSpeed()
	{
		if (Player is null)
		{
			return;
		}

		var previousSpeed = Player.MediaPlayer.PlaybackRate;
		Player.MediaPlayer.PlaybackRate = MediaElement.Speed;

		// Only trigger once when going to the paused state
		if (IsZero<double>(MediaElement.Speed) && previousSpeed > 0)
		{
			Player.MediaPlayer.Pause();
		}
		// Only trigger once when we move from the paused state
		else if (MediaElement.Speed > 0 && IsZero<double>(previousSpeed))
		{
			MediaElement.Play();
		}
	}

	protected virtual partial void PlatformUpdateShouldShowPlaybackControls()
	{
		if (Player is null)
		{
			return;
		}

		Player.AreTransportControlsEnabled =
			MediaElement.ShouldShowPlaybackControls;
	}

	protected virtual partial void PlatformUpdatePosition()
	{
		if (Application.Current?.Windows is null || Application.Current.Windows.Count == 0)
		{
			return;
		}
		if (!ParentWindow.Exists)
		{
			// Parent window is null, so we can't update the position
			// This is a workaround for a bug where the timer keeps running after the window is closed
			return;
		}

		if (Player is not null
			&& allowUpdatePositionStates.Contains(MediaElement.CurrentState))
		{
			MediaElement.Position = Player.MediaPlayer.Position;
		}
	}

	protected virtual partial void PlatformUpdateVolume()
	{
		if (Player is null)
		{
			return;
		}

		// If currently muted, ignore
		if (MediaElement.ShouldMute)
		{
			return;
		}

		if (Dispatcher.IsDispatchRequired)
		{
			Dispatcher.Dispatch(() => UpdateVolume(Player, MediaElement.Volume));
		}
		else
		{
			UpdateVolume(Player, MediaElement.Volume);
		}

		static void UpdateVolume(in MediaPlayerElement? mediaPlayerElement, in double volume)
		{
			// A queued update can run after the platform view was disposed, which detached the player (SetMediaPlayer(null))
			if (mediaPlayerElement?.MediaPlayer is WindowsMediaElement mediaPlayer)
			{
				mediaPlayer.Volume = volume;
			}
		}
	}

	protected virtual partial void PlatformUpdateShouldKeepScreenOn()
	{
		if (MediaElement.ShouldKeepScreenOn)
		{
			if (allowUpdatePositionStates.Contains(MediaElement.CurrentState)
				&& !displayActiveRequested)
			{
				DisplayRequest.RequestActive();
				displayActiveRequested = true;
			}
		}
		else
		{
			if (displayActiveRequested)
			{
				DisplayRequest.RequestRelease();
				displayActiveRequested = false;
			}
		}
	}

	protected virtual partial void PlatformUpdateShouldMute()
	{
		if (Player is null)
		{
			return;
		}
		// Always queued, so it can run after the platform view was disposed, which detached the player (SetMediaPlayer(null))
		Dispatcher.Dispatch(() =>
		{
			if (Player?.MediaPlayer is WindowsMediaElement mediaPlayer)
			{
				// PlatformUpdateVolume ignores Volume while muted: unmuting applies the current one
				if (!MediaElement.ShouldMute)
				{
					mediaPlayer.Volume = MediaElement.Volume;
				}

				mediaPlayer.IsMuted = MediaElement.ShouldMute;
			}
		});
	}

	protected virtual async partial ValueTask PlatformUpdateSource()
	{
		if (Player is null)
		{
			return;
		}

		// The native states of a new source are reported as they come
		lock (playbackStateLock)
		{
			keepReportingStopped = isStopSettling = false;
		}

		keepReportingFailed = false;

		int version;
		lock (sourceUpdateLock)
		{
			version = ++sourceUpdateVersion;
			adaptiveMediaSource?.DownloadRequested -= OnAdaptiveMediaSourceDownloadRequested;
			adaptiveMediaSource = null;
		}

		await Dispatcher.DispatchAsync(() => Player.PosterSource = new BitmapImage());

		// A newer source update (or Dispose) started while this one awaited: that one applies its source
		if (!IsCurrentSourceUpdate(version))
		{
			return;
		}

		if (MediaElement.Source is not MediaSource source)
		{
			ClearSource(Player);

			MediaElement.CurrentStateChanged(MediaElementState.None);

			return;
		}

		MediaElement.Position = TimeSpan.Zero;
		MediaElement.Duration = TimeSpan.Zero;
		Player.AutoPlay = MediaElement.ShouldAutoPlay;

		// A handler of the events of these resets can set another Source, whose update replaces this one
		if (!IsCurrentSourceUpdate(version))
		{
			return;
		}

		// Only giving the source to the player is guarded: an exception of the app's own event handlers (those of the resets above, for example) is
		// not a failure of the source. The MediaElement catches it and reports it once the update has ended (UpdateSource), so it neither stops the
		// update nor takes the media out of the player
		bool hasMedia;
		try
		{
			hasMedia = await SetPlayerSource(Player, source, version);
		}
		catch (Exception) when (!IsCurrentSourceUpdate(version))
		{
			// Superseded while it was opening its source: the failure concerns a source the MediaElement no longer has
			return;
		}
		catch (Exception ex) when (ex is not OperationCanceledException)
		{
			// A source that cannot be applied replaces the previous media: that media leaves the player, so that it neither keeps playing nor
			// reports a state over Failed. The awaits above resume on the UI thread, which the player requires.
			keepReportingFailed = true;
			ClearSource(Player);
			MediaElement.MediaFailed(new MediaFailedEventArgs(ex.Message));
			Logger.LogError(ex, "{LogMessage}", ex.Message);

			return;
		}

		// A source without data behaves like a null Source: the previous media leaves the player
		if (!hasMedia && IsCurrentSourceUpdate(version))
		{
			ClearSource(Player);

			MediaElement.CurrentStateChanged(MediaElementState.None);
		}
	}

	async ValueTask<bool> SetPlayerSource(MediaPlayerElement player, MediaSource source, int version)
	{
		if (source is UriMediaSource uriMediaSource)
		{
			var uri = uriMediaSource.Uri?.AbsoluteUri;
			if (string.IsNullOrWhiteSpace(uri))
			{
				return false;
			}

			var headers = uriMediaSource.HttpHeaders;
			if (headers.Count > 0)
			{
				await SetUriSourceWithHeaders(new Uri(uri), headers, version);
			}
			else
			{
				player.MediaPlayer.SetUriSource(new Uri(uri));
			}

			return true;
		}

		if (source is FileMediaSource fileMediaSource)
		{
			var filename = fileMediaSource.Path;
			if (string.IsNullOrWhiteSpace(filename))
			{
				return false;
			}

			StorageFile storageFile = await StorageFile.GetFileFromPathAsync(filename);
			if (IsCurrentSourceUpdate(version))
			{
				player.MediaPlayer.SetFileSource(storageFile);
			}

			return true;
		}

		if (source is ResourceMediaSource resourceMediaSource)
		{
			if (string.IsNullOrWhiteSpace(resourceMediaSource.Path))
			{
				Logger.LogInformation("ResourceMediaSource Path is null or empty");
				return false;
			}

			string path = GetFullAppPackageFilePath(resourceMediaSource.Path);
			if (string.IsNullOrWhiteSpace(path))
			{
				return false;
			}

			player.MediaPlayer.SetUriSource(new Uri(path));
			return true;
		}

		if (source is StreamMediaSource streamMediaSource)
		{
			if (streamMediaSource.Stream is null)
			{
				return false;
			}

			var randomAccessStream = streamMediaSource.Stream.AsRandomAccessStream();
			player.Source = WinMediaSource.CreateFromStream(randomAccessStream, streamMediaSource.Stream.GetMimeType());
			return true;
		}

		throw new NotSupportedException($"{source.GetType().FullName} is not yet supported for {nameof(MediaElement.Source)}");
	}

	protected virtual partial void PlatformUpdateShouldLoopPlayback()
	{
		if (Player is null)
		{
			return;
		}

		Player.MediaPlayer.IsLoopingEnabled = MediaElement.ShouldLoopPlayback;
	}

	/// <summary>
	/// Releases the unmanaged resources used by the <see cref="MediaManager"/> and optionally releases the managed resources.
	/// </summary>
	/// <param name="disposing"><see langword="true"/> to release both managed and unmanaged resources; <see langword="false"/> to release only unmanaged resources.</param>
	protected virtual void Dispose(bool disposing)
	{
		if (disposing)
		{
			lock (sourceUpdateLock)
			{
				// A source update that is still opening its source must not apply it any more
				sourceUpdateVersion++;
				adaptiveMediaSource?.DownloadRequested -= OnAdaptiveMediaSourceDownloadRequested;
				adaptiveMediaSource = null;
			}

			headerHttpClient?.Dispose();
			headerHttpClient = null;

			lock (metadataLock)
			{
				metadata?.Dispose();
				metadata = null;
				systemMediaControls = null;
			}

			mediaPlayer = null;

			if (Player?.MediaPlayer is not null)
			{
				if (displayActiveRequested)
				{
					DisplayRequest.RequestRelease();
					displayActiveRequested = false;
				}

				Player.MediaPlayer.MediaOpened -= OnMediaElementMediaOpened;
				Player.MediaPlayer.MediaFailed -= OnMediaElementMediaFailed;
				Player.MediaPlayer.MediaEnded -= OnMediaElementMediaEnded;
				Player.MediaPlayer.VolumeChanged -= OnMediaElementVolumeChanged;
				Player.MediaPlayer.IsMutedChanged -= OnMediaElementIsMutedChanged;

				if (Player.MediaPlayer.PlaybackSession is not null)
				{
					Player.MediaPlayer.PlaybackSession.NaturalVideoSizeChanged -= OnNaturalVideoSizeChanged;
					Player.MediaPlayer.PlaybackSession.PlaybackRateChanged -= OnPlaybackSessionPlaybackRateChanged;
					Player.MediaPlayer.PlaybackSession.PlaybackStateChanged -= OnPlaybackSessionPlaybackStateChanged;
					Player.MediaPlayer.PlaybackSession.SeekCompleted -= OnPlaybackSessionSeekCompleted;
				}
			}
		}
	}

	static string GetFullAppPackageFilePath(in string filename)
	{
		ArgumentNullException.ThrowIfNull(filename);

		var normalizedFilename = NormalizePath(filename);
		return Path.Combine(AppPackageService.FullAppPackageFilePath, normalizedFilename);

		static string NormalizePath(string filename) => filename.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
	}

	static bool IsZero<TValue>(TValue numericValue) where TValue : INumber<TValue>
	{
		return TValue.IsZero(numericValue);
	}

	// Neither a live stream (its NaturalDuration is TimeSpan.MaxValue) nor media whose duration the player does not know (zero) has an
	// end to clamp to.
	static TimeSpan ClampToTheMedia(TimeSpan position, TimeSpan naturalDuration)
	{
		if (position < TimeSpan.Zero)
		{
			return TimeSpan.Zero;
		}

		return naturalDuration > TimeSpan.Zero && position > naturalDuration ? naturalDuration : position;
	}

	async Task SetUriSourceWithHeaders(Uri uri, IDictionary<string, string> headers, int version)
	{
		// A newer update can already have started: the handlers of the PositionChanged and PropertyChanged events that PlatformUpdateSource
		// raises before this call run synchronously and can set Source again.
		if (Player is null || !IsCurrentSourceUpdate(version))
		{
			return;
		}

		var httpClient = headerHttpClient ??= new HttpClient();
		httpClient.DefaultRequestHeaders.Clear();

		foreach (var header in headers)
		{
			if (!httpClient.DefaultRequestHeaders.TryAppendWithoutValidation(header.Key, header.Value))
			{
				throw new InvalidOperationException($"Failed to append HTTP header '{header.Key}'. The header name may be empty or contain invalid characters.");
			}
		}

		var adaptiveResult = await AdaptiveMediaSource.CreateFromUriAsync(uri, httpClient).AsTask().ConfigureAwait(ConfigureAwaitOptions.ForceYielding);

		if (adaptiveResult.Status is AdaptiveMediaSourceCreationStatus.Success && adaptiveResult.MediaSource is not null)
		{
			var newAdaptiveMediaSource = adaptiveResult.MediaSource;
			bool isSuperseded;
			lock (sourceUpdateLock)
			{
				// Superseded while the manifest loaded: the newer update has already released the previous adaptive source
				isSuperseded = version != sourceUpdateVersion;
				if (!isSuperseded)
				{
					adaptiveMediaSource = newAdaptiveMediaSource;
					adaptiveMediaSource.DownloadRequested += OnAdaptiveMediaSourceDownloadRequested;
				}
			}

			if (isSuperseded)
			{
				// Nothing uses the adaptive source this update created: it is closed, not left to the garbage collector
				newAdaptiveMediaSource.Dispose();
				return;
			}

			var mediaSource = WinMediaSource.CreateFromAdaptiveMediaSource(newAdaptiveMediaSource);
			await Dispatcher.DispatchAsync(() =>
			{
				// Checked again on the UI thread, where source updates start: a newer update has also released this adaptive source, so neither
				// source is used, and both are closed
				if (!IsCurrentSourceUpdate(version))
				{
					mediaSource.Dispose();
					newAdaptiveMediaSource.Dispose();
					return;
				}

				Player.AutoPlay = MediaElement.ShouldAutoPlay;
				Player.Source = mediaSource;
			});
		}
		else
		{
			// The response of the manifest request (here a media file, not a manifest) comes with its body unread, and it holds its connection
			// until its content is closed: left to the garbage collector, six of them use up the connections that this process may open to that
			// host (HttpBaseProtocolFilter.MaxConnectionsPerServer), and every later request to the host, the HEAD below included, waits for one
			// until it times out
			if (adaptiveResult.HttpResponseMessage is Windows.Web.Http.HttpResponseMessage manifestResponse)
			{
				manifestResponse.Content?.Dispose();
				manifestResponse.Dispose();
			}

			if (IsCurrentSourceUpdate(version))
			{
				var stream = await HttpRandomAccessStream.CreateAsync(httpClient, uri);
				await Dispatcher.DispatchAsync(() =>
				{
					if (IsCurrentSourceUpdate(version))
					{
						Player.AutoPlay = MediaElement.ShouldAutoPlay;
						Player.Source = WinMediaSource.CreateFromStream(stream, string.Empty);
					}
				});
			}
		}
	}

	bool IsCurrentSourceUpdate(int version) => Volatile.Read(ref sourceUpdateVersion) == version;

	void ClearSource(MediaPlayerElement player)
	{
		player.Source = null;

		// File, Uri and Resource sources are given to the MediaPlayer itself (Set*Source), not through MediaPlayerElement.Source. Its Source can
		// then still be null, and setting null again is not forwarded to the player, so the player's own source is cleared as well.
		player.MediaPlayer?.Source = null;
		MediaElement.MediaWidth = MediaElement.MediaHeight = 0;

		lock (metadataLock)
		{
			// Without media there is no system media session (Metadata.SetMetadata switches the controls on again for media with metadata)
			systemMediaControls?.IsEnabled = false;
		}

		// Without media nothing plays: the request to keep the screen on that Play() made ends, as Pause() and Stop() end it
		if (displayActiveRequested)
		{
			DisplayRequest.RequestRelease();
			displayActiveRequested = false;
		}
	}

	async void OnAdaptiveMediaSourceDownloadRequested(AdaptiveMediaSource sender, AdaptiveMediaSourceDownloadRequestedEventArgs args)
	{
		if (headerHttpClient is null)
		{
			return;
		}

		var deferral = args.GetDeferral();
		try
		{
			using var request = new HttpRequestMessage(HttpMethod.Get, args.ResourceUri);

			// A byte-range resource (an #EXT-X-BYTERANGE segment, for example) is a part of a larger file: fetch only that part
			if (args.ResourceByteRangeOffset is ulong offset)
			{
				request.Headers.TryAppendWithoutValidation("Range", args.ResourceByteRangeLength is ulong length and > 0
					? $"bytes={offset}-{offset + length - 1}"
					: $"bytes={offset}-");
			}

			using var response = await headerHttpClient.SendRequestAsync(request).AsTask().ConfigureAwait(ConfigureAwaitOptions.ForceYielding);
			response.EnsureSuccessStatusCode();
			args.Result.InputStream = await response.Content.ReadAsInputStreamAsync().AsTask();
		}
		catch (Exception ex)
		{
			Trace.WriteLine($"MediaElement [Windows]: DownloadRequested failed for {args.ResourceUri}: {ex.Message}");
		}
		finally
		{
			deferral.Complete();
		}
	}

	async ValueTask UpdateMetadata()
	{
		if (Player is null)
		{
			return;
		}

		lock (metadataLock)
		{
			// No controls: there was no platform view, or the manager was disposed while a MediaOpened was still being handled
			if (systemMediaControls is null)
			{
				return;
			}

			metadata ??= new(systemMediaControls, MediaElement, Dispatcher);
			metadata.SetMetadata(MediaElement);
		}

		if (string.IsNullOrEmpty(MediaElement.MetadataArtworkUrl))
		{
			return;
		}
		// BitmapImage needs an absolute URI (a relative one cannot be converted to a Windows.Foundation.Uri)
		if (!Uri.TryCreate(MediaElement.MetadataArtworkUrl, UriKind.Absolute, out var metadataArtworkUri))
		{
			Trace.TraceError($"{nameof(MediaElement)} unable to update artwork because {nameof(MediaElement.MetadataArtworkUrl)} is not a valid URI");
			return;
		}

		if (Dispatcher.IsDispatchRequired)
		{
			await Dispatcher.DispatchAsync(() => UpdatePosterSource(Player, metadataArtworkUri));
		}
		else
		{
			UpdatePosterSource(Player, metadataArtworkUri);
		}

		static void UpdatePosterSource(in MediaPlayerElement player, in Uri metadataArtworkUri)
		{
			player.PosterSource = new BitmapImage(metadataArtworkUri);
		}
	}

	async void OnMediaElementMediaOpened(WindowsMediaElement sender, object args)
	{
		if (Player is null)
		{
			return;
		}

		if (Dispatcher.IsDispatchRequired)
		{
			Dispatcher.Dispatch(() => SetDuration(MediaElement, Player));
		}
		else
		{
			SetDuration(MediaElement, Player);
		}

		MediaElement.MediaOpened();

		await UpdateMetadata();

		static void SetDuration(in IMediaElement mediaElement, in MediaPlayerElement? mediaPlayerElement)
		{
			// A queued update can run after the platform view was disposed, which detached the player (SetMediaPlayer(null))
			if (mediaPlayerElement?.MediaPlayer is not WindowsMediaElement mediaPlayer)
			{
				return;
			}

			mediaElement.Duration = mediaPlayer.NaturalDuration == TimeSpan.MaxValue
				? TimeSpan.Zero
				: mediaPlayer.NaturalDuration;
		}
	}

	void OnMediaElementMediaEnded(WindowsMediaElement sender, object args)
	{
		lock (playbackStateLock)
		{
			// The player settles in Paused at the end of the media, which must not overwrite the Stopped state reported here. The media has played
			// to its end, so a Playing that follows comes from a new play (the transport controls) and is reported.
			keepReportingStopped = true;
			isStopSettling = false;
		}

		MediaElement?.MediaEnded();
	}

	void OnMediaElementMediaFailed(WindowsMediaElement sender, MediaPlayerFailedEventArgs args)
	{
		string errorMessage = string.Empty;
		string errorCode = string.Empty;
		string error = args.Error.ToString();

		if (!string.IsNullOrWhiteSpace(args.ErrorMessage))
		{
			errorMessage = $"Error message: {args.ErrorMessage}";
		}

		if (args.ExtendedErrorCode != null)
		{
			errorCode = $"Error code: {args.ExtendedErrorCode.Message}";
		}

		var message = string.Join(", ",
			new[] { error, errorCode, errorMessage }
			.Where(s => !string.IsNullOrEmpty(s)));

		MediaElement?.MediaFailed(new MediaFailedEventArgs(message));

		Logger?.LogError("{LogMessage}", message);
	}

	// The MediaPlayer raises IsMutedChanged and VolumeChanged on a worker thread, and the event of a change can arrive after the app made another
	// one: the event of the volume the manager applied when it connected the player can come after the app's next Volume. So the view gets the
	// player's state when the UI thread handles the event, not the state the event reports, and a late event cannot undo a newer ShouldMute or
	// Volume (both of which the app sets on the UI thread).
	void OnMediaElementIsMutedChanged(WindowsMediaElement sender, object args) => Dispatcher.Dispatch(() =>
	{
		if (GetPlayerInUse() is WindowsMediaElement player)
		{
			MediaElement.ShouldMute = player.IsMuted;
		}
	});

	void OnMediaElementVolumeChanged(WindowsMediaElement sender, object args) => Dispatcher.Dispatch(() =>
	{
		if (GetPlayerInUse() is WindowsMediaElement player)
		{
			MediaElement.Volume = player.Volume;
		}
	});

	// An update queued by a player event can run after Dispose, which removed the event handlers and released the player, or after the
	// platform view was disposed, which closed the player and detached it (SetMediaPlayer(null)). UI thread only (MediaPlayerElement.MediaPlayer).
	WindowsMediaElement? GetPlayerInUse() => mediaPlayer is WindowsMediaElement player && ReferenceEquals(Player?.MediaPlayer, player) ? player : null;

	void OnNaturalVideoSizeChanged(MediaPlaybackSession sender, object args)
	{
		if (MediaElement is not null)
		{
			MediaElement.MediaWidth = (int)sender.NaturalVideoWidth;
			MediaElement.MediaHeight = (int)sender.NaturalVideoHeight;
		}
	}

	void OnPlaybackSessionPlaybackRateChanged(MediaPlaybackSession sender, object args)
	{
		// Read here: a queued update can run after the platform view was disposed, which closed the player and this session
		var playbackRate = sender.PlaybackRate;

		if (!AreFloatingPointNumbersEqual(MediaElement.Speed, playbackRate))
		{
			if (Dispatcher.IsDispatchRequired)
			{
				// The event of every write of the rate arrives on a worker thread, so the app may have applied a newer Speed by the time the update
				// runs, which the rate of an earlier write must not undo: only a rate that the open session still has is applied.
				Dispatcher.Dispatch(() =>
				{
					if (Player?.MediaPlayer is not null && AreFloatingPointNumbersEqual(sender.PlaybackRate, playbackRate))
					{
						UpdateSpeed(MediaElement, playbackRate);
					}
				});
			}
			else
			{
				UpdateSpeed(MediaElement, playbackRate);
			}
		}

		static void UpdateSpeed(in IMediaElement mediaElement, in double playbackRate) => mediaElement.Speed = playbackRate;
	}

	void OnPlaybackSessionPlaybackStateChanged(MediaPlaybackSession sender, object args)
	{
		if (keepReportingFailed)
		{
			return;
		}

		var newState = sender.PlaybackState switch
		{
			MediaPlaybackState.Buffering => MediaElementState.Buffering,
			MediaPlaybackState.Playing => MediaElementState.Playing,
			MediaPlaybackState.Paused => MediaElementState.Paused,
			MediaPlaybackState.Opening => MediaElementState.Opening,
			_ => MediaElementState.None,
		};

		bool isReported;
		lock (playbackStateLock)
		{
			isReported = IsNativeStateReported(newState);
		}

		if (isReported)
		{
			MediaElement?.CurrentStateChanged(newState);
		}

		if (sender.PlaybackState == MediaPlaybackState.Playing && IsZero<double>(sender.PlaybackRate))
		{
			Dispatcher.Dispatch(() =>
			{
				// The queued write can run after the platform view was disposed, which closed the player and this session
				if (Player?.MediaPlayer is not null)
				{
					sender.PlaybackRate = 1;
				}
			});
		}
	}

	// Also changes keepReportingStopped and isStopSettling, so call it under playbackStateLock.
	bool IsNativeStateReported(MediaElementState newState)
	{
		if (!keepReportingStopped)
		{
			return true;
		}

		switch (newState)
		{
			case MediaElementState.Paused:
				// The pause of Stop() (or of the end of the media) has taken effect: a Playing from now on comes from a new play
				isStopSettling = false;
				return false;

			case MediaElementState.Buffering:
			case MediaElementState.Playing when isStopSettling:
				// After Stop() or the end of the media, the player buffers before it settles, and a play that came before Stop() is stale
				return false;

			default:
				// A play started after the stop settled without Play() (the transport controls), or the media changes (Opening) or closes (None)
				keepReportingStopped = isStopSettling = false;
				return true;
		}
	}

	void OnPlaybackSessionSeekCompleted(MediaPlaybackSession sender, object args)
	{
		MediaElement?.SeekCompleted();
	}
}