using System.Runtime.CompilerServices;
using CommunityToolkit.Maui.DeviceTests.Runners;
using Windows.Media;
using Windows.Media.Playback;
using Windows.Media.Streaming.Adaptive;
using Windows.Storage.Streams;
using Windows.System.Display;
using HttpClient = Windows.Web.Http.HttpClient;
using WinMediaSource = Windows.Media.Core.MediaSource;

namespace CommunityToolkit.Maui.DeviceTests;

// Run-time capability probes of the MediaElement-on-Windows tests and the local HLS streams they and the network tests use.
// Every probe asks a plain Windows object (a MediaPlayer, AdaptiveMediaSource, DisplayRequest, ... that no toolkit code touches) whether the
// platform behaves the way a test needs. A test skips, visibly and with the probe's answer, only when the platform does not; a toolkit
// regression can therefore never turn into a skip. Each probe runs once per app launch.

/// <summary>
/// HLS streams served from the loopback interface by <see cref="WinMediaHttpServer"/>. The media segment is the bundled 2 s clip as MPEG-TS.
/// </summary>
static class WinMediaHls
{
	public const string PlaylistPath = "hls/playlist.m3u8";

	/// <summary>One real 2 s segment, ended (video on demand).</summary>
	public const string VodPlaylist = "#EXTM3U\n#EXT-X-VERSION:3\n#EXT-X-TARGETDURATION:2\n#EXT-X-MEDIA-SEQUENCE:0\n#EXTINF:2.0,\nseg0.ts\n#EXT-X-ENDLIST\n";

	/// <summary>The same without #EXT-X-ENDLIST: a live playlist, whose duration the platform cannot know.</summary>
	public const string LivePlaylist = "#EXTM3U\n#EXT-X-VERSION:3\n#EXT-X-TARGETDURATION:2\n#EXT-X-MEDIA-SEQUENCE:0\n#EXTINF:2.0,\nseg0.ts\n";

	/// <summary>Two segments; the server never delivers the second (seg1.ts), so playback runs dry after the first.</summary>
	public const string StarvingPlaylist = LivePlaylist + "#EXTINF:2.0,\nseg1.ts\n#EXT-X-ENDLIST\n";

	/// <summary>Filler bytes in front of the segment inside media.ts: fetching the whole resource does not give the range the playlist names.</summary>
	public const int ByteRangeOffset = 4096;

	/// <summary>A playlist whose only segment is a byte range (#EXT-X-BYTERANGE) of media.ts.</summary>
	public static string ByteRangePlaylist(int segmentLength) =>
		$"#EXTM3U\n#EXT-X-VERSION:4\n#EXT-X-TARGETDURATION:2\n#EXT-X-MEDIA-SEQUENCE:0\n#EXTINF:2.0,\n#EXT-X-BYTERANGE:{segmentLength}@{ByteRangeOffset}\nmedia.ts\n#EXT-X-ENDLIST\n";

	/// <summary>The Range header a client must send to fetch the byte-range segment of <see cref="ByteRangePlaylist"/>.</summary>
	public static string ExpectedByteRange(int segmentLength) => $"bytes={ByteRangeOffset}-{ByteRangeOffset + segmentLength - 1}";

	/// <summary>
	/// Serves <paramref name="playlist"/> at <see cref="PlaylistPath"/>, the segment at hls/seg0.ts, nothing ever at hls/seg1.ts (the answer is
	/// held back) and the filler plus the segment at hls/media.ts. <paramref name="overrides"/> answers first when it returns a response.
	/// </summary>
	public static WinMediaHttpServer StartServer(string playlist, Func<WinMediaRequest, WinMediaResponse?>? overrides = null)
	{
		var segment = WinMediaAssets.ReadSegmentBytes();
		var media = new byte[ByteRangeOffset + segment.Length];
		segment.CopyTo(media, ByteRangeOffset);

		return new WinMediaHttpServer
		{
			Handler = request => overrides?.Invoke(request) ?? request.Path switch
			{
				"/hls/playlist.m3u8" => WinMediaResponse.Text(playlist, "application/vnd.apple.mpegurl"),
				"/hls/seg0.ts" => WinMediaResponse.ServeBytes(request, segment, "video/MP2T"),
				"/hls/seg1.ts" => new WinMediaResponse { Delay = TimeSpan.FromSeconds(60) },
				"/hls/media.ts" => WinMediaResponse.ServeBytes(request, media, "video/MP2T"),
				_ => WinMediaResponse.NotFound(),
			},
		}.Start();
	}
}

/// <summary>A MediaPlayer that no toolkit code touches, created and released on the UI thread.</summary>
sealed class WinMediaPlainPlayer : IAsyncDisposable
{
	readonly TaskCompletionSource<string?> opened = new(TaskCreationOptions.RunContinuationsAsynchronously);

	WinMediaPlainPlayer(MediaPlayer player) => Player = player;

	public MediaPlayer Player { get; }

	/// <summary>
	/// Creates the player (AutoPlay off), lets <paramref name="setSource"/> give it media and waits for MediaOpened. The failure is null when the
	/// media opened, otherwise what the platform reported or that it did not answer within <paramref name="timeout"/>.
	/// </summary>
	public static async Task<(WinMediaPlainPlayer Plain, string? Failure)> OpenAsync(Action<MediaPlayer> setSource, TimeSpan timeout)
	{
		var plain = await TestDispatcher.Current.DispatchAsync(() =>
		{
			var player = new MediaPlayer { AutoPlay = false };
			var created = new WinMediaPlainPlayer(player);
			player.MediaOpened += (_, _) => created.opened.TrySetResult(null);
			player.MediaFailed += (_, args) => created.opened.TrySetResult($"MediaFailed ({args.Error}, {args.ExtendedErrorCode?.Message ?? args.ErrorMessage})");
			setSource(player);
			return created;
		});

		try
		{
			return (plain, await plain.opened.Task.WaitAsync(timeout));
		}
		catch (TimeoutException)
		{
			return (plain, $"MediaOpened was not raised within {timeout.TotalSeconds} s");
		}
	}

	/// <summary>Opens an HLS playlist through a plain AdaptiveMediaSource, the way the toolkit does for a header source but without the toolkit.</summary>
	public static async Task<(WinMediaPlainPlayer? Plain, string? Failure)> OpenHlsAsync(Uri playlist, TimeSpan timeout)
	{
		var created = await AdaptiveMediaSource.CreateFromUriAsync(playlist).AsTask().WaitAsync(timeout);
		if (created.Status is not AdaptiveMediaSourceCreationStatus.Success || created.MediaSource is null)
		{
			return (null, $"AdaptiveMediaSource.CreateFromUriAsync returned {created.Status}");
		}

		return await OpenAsync(player => player.Source = WinMediaSource.CreateFromAdaptiveMediaSource(created.MediaSource), timeout);
	}

	public async ValueTask DisposeAsync() =>
		await TestDispatcher.Current.DispatchAsync(() =>
		{
			try
			{
				Player.Pause();
				(Player.Source as IDisposable)?.Dispose();
				Player.Source = null;
				Player.Dispose();
			}
			catch (Exception)
			{
				// Already released: nothing left to clean up.
			}
		});
}

/// <summary>
/// Run-time capability probes. A machine without Media Foundation (for example a Windows Server image without the Server-Media-Foundation
/// feature) cannot create a MediaPlayer at all; the tests then end as Skipped, with the reason, instead of failing. Each probe answers null when
/// the platform behaves as the tests need, otherwise why not.
/// </summary>
static class WinMediaProbe
{
	static readonly TimeSpan openTimeout = TimeSpan.FromSeconds(30);

	static readonly Lazy<Task<string?>> playerProbe = new(ProbePlayerAsync);
	static readonly Lazy<Task<string?>> decoderProbe = new(ProbeDecoderAsync);
	static readonly Lazy<Task<string?>> videoRenderingProbe = new(ProbeVideoRenderingAsync);
	static readonly Lazy<Task<string?>> displayUpdaterProbe = new(ProbeDisplayUpdaterAsync);
	static readonly Lazy<Task<string?>> displayRequestProbe = new(ProbeDisplayRequestCountingAsync);
	static readonly Lazy<Task<string?>> disposedPlayerProbe = new(ProbeDisposedPlayerAsync);
	static readonly Lazy<Task<string?>> disposedMediaSourceProbe = new(ProbeDisposedMediaSourceAsync);
	static readonly Lazy<Task<string?>> disposedHttpClientProbe = new(ProbeDisposedHttpClientAsync);
	static readonly Lazy<Task<string?>> disposedAdaptiveSourceProbe = new(ProbeDisposedAdaptiveSourceAsync);
	static readonly Lazy<Task<string?>> mediaSessionProbe = new(ProbeMediaSessionAsync);
	static readonly Lazy<Task<string?>> zeroRateProbe = new(ProbeZeroRateAsync);
	static readonly Lazy<Task<string?>> playAtZeroRateProbe = new(ProbePlayAtZeroRateAsync);
	static readonly Lazy<Task<string?>> hlsProbe = new(ProbeHlsAsync);
	static readonly Lazy<Task<string?>> liveDurationProbe = new(ProbeLiveDurationAsync);
	static readonly Lazy<Task<string?>> starvationProbe = new(ProbeStarvationBufferingAsync);
	static readonly Lazy<Task<string?>> byteRangeProbe = new(ProbeByteRangeAsync);
	static readonly Lazy<Task<string?>> eventSubscriptionProbe = new(ProbeEventSubscriptionKeepsTheTargetAliveAsync);

	/// <summary>Null when a MediaPlayer can be created, otherwise why not.</summary>
	public static Task<string?> PlayerUnavailableReason => playerProbe.Value;

	/// <summary>Null when the H.264 test asset opens in a plain MediaPlayer, otherwise why not.</summary>
	public static Task<string?> DecoderUnavailableReason => decoderProbe.Value;

	/// <summary>
	/// Null when a plain MediaPlayer renders the H.264 test asset: it raises NaturalVideoSizeChanged while it opens the clip, and after Play()
	/// it reports Playing while its position advances. Otherwise why not: a machine can open the clip and still render no video. On a Windows 11
	/// ARM64 VM (Parallels Display Adapter) such a player stays Paused at 0 s after Play() and raises no NaturalVideoSizeChanged, while a
	/// frame-server player (IsVideoFrameServerEnabled), which needs no video renderer, plays the clip and an audio-only clip plays.
	/// </summary>
	public static Task<string?> VideoRenderingUnavailableReason => videoRenderingProbe.Value;

	/// <summary>
	/// Null when the DisplayUpdater of a plain MediaPlayer's system media controls returns what was assigned to it after Update() (type, title,
	/// artist) and its thumbnail, created from an http URI, delivers the artwork's bytes; otherwise why not.
	/// </summary>
	public static Task<string?> DisplayUpdaterRoundTripFailure => displayUpdaterProbe.Value;

	/// <summary>Null when the active requests of a DisplayRequest can be counted with <see cref="WinMediaDisplayRequests.CountActive"/>.</summary>
	public static Task<string?> DisplayRequestCountingUnavailableReason => displayRequestProbe.Value;

	/// <summary>Null when a disposed MediaPlayer is observably closed (and a live one is not).</summary>
	public static Task<string?> DisposedPlayerNotObservableReason => disposedPlayerProbe.Value;

	/// <summary>Null when a disposed Windows.Media.Core.MediaSource is observably closed (and a live one is not).</summary>
	public static Task<string?> DisposedMediaSourceNotObservableReason => disposedMediaSourceProbe.Value;

	/// <summary>Null when a disposed Windows.Web.Http.HttpClient is observably closed (and a live one is not).</summary>
	public static Task<string?> DisposedHttpClientNotObservableReason => disposedHttpClientProbe.Value;

	/// <summary>Null when a disposed AdaptiveMediaSource (of the local HLS stream) is observably closed (and a live one is not).</summary>
	public static Task<string?> DisposedAdaptiveSourceNotObservableReason => disposedAdaptiveSourceProbe.Value;

	/// <summary>Null when a plain MediaPlayer whose system media controls publish a title shows up as a system media session.</summary>
	public static Task<string?> MediaSessionUnavailableReason => mediaSessionProbe.Value;

	/// <summary>Null when a plain, playing MediaPlayer keeps a playback rate of 0 that was set before it was paused.</summary>
	public static Task<string?> ZeroRateUnavailableReason => zeroRateProbe.Value;

	/// <summary>Null when a plain MediaPlayer whose playback rate is 0 reports Playing after Play().</summary>
	public static Task<string?> PlayAtZeroRateUnavailableReason => playAtZeroRateProbe.Value;

	/// <summary>Null when a plain MediaPlayer opens the local HLS stream through a plain AdaptiveMediaSource.</summary>
	public static Task<string?> HlsUnavailableReason => hlsProbe.Value;

	/// <summary>Null when a plain MediaPlayer reports TimeSpan.MaxValue as the duration of the local live (unended) playlist.</summary>
	public static Task<string?> LiveDurationUnavailableReason => liveDurationProbe.Value;

	/// <summary>Null when a plain MediaPlayer reports Buffering while the second segment of the local playlist is withheld.</summary>
	public static Task<string?> StarvationBufferingUnavailableReason => starvationProbe.Value;

	/// <summary>Null when a plain AdaptiveMediaSource fetches an #EXT-X-BYTERANGE segment with exactly that Range header.</summary>
	public static Task<string?> ByteRangeUnavailableReason => byteRangeProbe.Value;

	/// <summary>
	/// Null when, on a plain AdaptiveMediaSource that stays alive, a DownloadRequested subscription keeps its handler's target alive and
	/// unsubscribing releases it: the premise of the tests that prove an unsubscription through the garbage collector.
	/// </summary>
	public static Task<string?> EventSubscriptionControlFailure => eventSubscriptionProbe.Value;

	static string Describe(Exception ex) => $"{ex.GetType().Name} (HRESULT 0x{ex.HResult:X8}): {ex.Message}";

	static Task<string?> ProbePlayerAsync() =>
		TestDispatcher.Current.DispatchAsync(() =>
		{
			try
			{
				using var player = new MediaPlayer();
				return (string?)null;
			}
			catch (Exception ex)
			{
				return $"A Windows MediaPlayer cannot be created on this machine (is Media Foundation installed?): {ex.GetType().Name}: {ex.Message}";
			}
		});

	static async Task<string?> ProbeDecoderAsync()
	{
		var playerReason = await PlayerUnavailableReason;
		if (playerReason is not null)
		{
			return playerReason;
		}

		try
		{
			var (plain, failure) = await WinMediaPlainPlayer.OpenAsync(player => player.SetUriSource(WinMediaAssets.VideoUri), openTimeout);
			await using (plain)
			{
				return failure is null ? null : $"Media Foundation cannot open the H.264 test asset: {failure}.";
			}
		}
		catch (Exception ex)
		{
			return $"Media Foundation cannot open the H.264 test asset: {Describe(ex)}.";
		}
	}

	static async Task<string?> ProbeVideoRenderingAsync()
	{
		var decoderReason = await DecoderUnavailableReason;
		if (decoderReason is not null)
		{
			return decoderReason;
		}

		try
		{
			var sizeChanges = 0;
			var (plain, failure) = await WinMediaPlainPlayer.OpenAsync(player =>
			{
				player.PlaybackSession.NaturalVideoSizeChanged += (_, _) => Interlocked.Increment(ref sizeChanges);
				player.SetUriSource(WinMediaAssets.VideoUri);
			}, openTimeout);

			await using (plain)
			{
				if (failure is not null)
				{
					return $"Media Foundation cannot open the H.264 test asset: {failure}.";
				}

				var session = plain.Player.PlaybackSession;
				plain.Player.IsLoopingEnabled = true;
				plain.Player.Play();
				var played = await AssertHelpers.Wait(() => session.PlaybackState is MediaPlaybackState.Playing && session.Position > TimeSpan.Zero, 10000, 50);
				var sizeReported = await AssertHelpers.Wait(() => Volatile.Read(ref sizeChanges) > 0, 1000, 50);

				return played && sizeReported
					? null
					: $"Media Foundation opens the H.264 test asset but renders no video on this machine: 10 s after Play() a plain MediaPlayer is {session.PlaybackState} at {session.Position.TotalSeconds:0.##} s and raised NaturalVideoSizeChanged {Volatile.Read(ref sizeChanges)} times.";
			}
		}
		catch (Exception ex)
		{
			return $"Media Foundation cannot render the H.264 test asset: {Describe(ex)}.";
		}
	}

	static async Task<string?> ProbeDisplayUpdaterAsync()
	{
		var playerReason = await PlayerUnavailableReason;
		if (playerReason is not null)
		{
			return playerReason;
		}

		await using var server = WinMediaAssets.StartArtworkServer();
		var artworkUri = server.UriFor(WinMediaAssets.ArtworkServerPath);
		MediaPlayer? player = null;
		try
		{
			var assigned = await TestDispatcher.Current.DispatchAsync(() =>
			{
				player = new MediaPlayer();
				var updater = player.SystemMediaTransportControls.DisplayUpdater;
				updater.Type = MediaPlaybackType.Music;
				updater.MusicProperties.Title = "test-probe-title";
				updater.MusicProperties.Artist = "test-probe-artist";
				updater.Thumbnail = RandomAccessStreamReference.CreateFromUri(artworkUri);
				updater.Update();

				return updater.Type == MediaPlaybackType.Music
					&& updater.MusicProperties.Title == "test-probe-title"
					&& updater.MusicProperties.Artist == "test-probe-artist";
			});
			if (!assigned)
			{
				return "the DisplayUpdater does not return the type, title and artist assigned to it";
			}

			var thumbnail = await TestDispatcher.Current.DispatchAsync(() => player!.SystemMediaTransportControls.DisplayUpdater.Thumbnail);
			if (thumbnail is null)
			{
				return "the DisplayUpdater returns no thumbnail after one was assigned";
			}

			var bytes = await WinMediaAssets.ReadAllBytesAsync(thumbnail).WaitAsync(TimeSpan.FromSeconds(15));
			return bytes.AsSpan().SequenceEqual(WinMediaAssets.ReadArtworkBytes())
				? null
				: $"the thumbnail read back from the DisplayUpdater delivers {bytes.Length} bytes that are not the artwork that was assigned";
		}
		catch (Exception ex)
		{
			return Describe(ex);
		}
		finally
		{
			if (player is not null)
			{
				await TestDispatcher.Current.DispatchAsync(player.Dispose);
			}
		}
	}

	static Task<string?> ProbeDisplayRequestCountingAsync() =>
		TestDispatcher.Current.DispatchAsync(() =>
		{
			try
			{
				var request = new DisplayRequest();
				try
				{
					request.RequestRelease();
					return (string?)"DisplayRequest.RequestRelease does not throw when no request is active, so the active requests of a DisplayRequest cannot be counted";
				}
				catch (Exception)
				{
					// Expected: there is nothing to release.
				}

				request.RequestActive();
				request.RequestActive();
				var two = WinMediaDisplayRequests.CountActive(request);

				request.RequestRelease();
				request.RequestRelease();
				var none = WinMediaDisplayRequests.CountActive(request);

				return two is 2 && none is 0
					? null
					: $"counting the requests of a plain DisplayRequest gave {two} for two active requests and {none} for none";
			}
			catch (Exception ex)
			{
				return Describe(ex);
			}
		});

	static async Task<string?> ProbeDisposedPlayerAsync()
	{
		var playerReason = await PlayerUnavailableReason;
		if (playerReason is not null)
		{
			return playerReason;
		}

		return await TestDispatcher.Current.DispatchAsync(() =>
		{
			try
			{
				var player = new MediaPlayer();
				if (WinMediaClosed.IsClosed(() => player.Volume))
				{
					return (string?)"a live MediaPlayer cannot be read (MediaPlayer.Volume)";
				}

				player.Dispose();
				return WinMediaClosed.IsClosed(() => player.Volume)
					? null
					: "a disposed MediaPlayer still answers (MediaPlayer.Volume), so disposing a player is not observable";
			}
			catch (Exception ex)
			{
				return Describe(ex);
			}
		});
	}

	static async Task<string?> ProbeDisposedMediaSourceAsync()
	{
		var playerReason = await PlayerUnavailableReason;
		if (playerReason is not null)
		{
			return playerReason;
		}

		return await TestDispatcher.Current.DispatchAsync(() =>
		{
			try
			{
				var source = WinMediaSource.CreateFromUri(WinMediaAssets.VideoUri);
				if (WinMediaClosed.IsClosed(source))
				{
					return (string?)"a live MediaSource reports that it is closed";
				}

				source.Dispose();
				return WinMediaClosed.IsClosed(source)
					? null
					: "a disposed MediaSource neither reports MediaSourceState.Closed nor refuses calls, so disposing a MediaSource is not observable";
			}
			catch (Exception ex)
			{
				return Describe(ex);
			}
		});
	}

	static Task<string?> ProbeDisposedHttpClientAsync() =>
		Task.Run(() =>
		{
			try
			{
				var client = new HttpClient();
				if (WinMediaClosed.IsClosed(() => client.DefaultRequestHeaders))
				{
					return (string?)"a live HttpClient cannot be read (HttpClient.DefaultRequestHeaders)";
				}

				client.Dispose();
				return WinMediaClosed.IsClosed(() => client.DefaultRequestHeaders)
					? null
					: "a disposed Windows.Web.Http.HttpClient still answers (DefaultRequestHeaders), so disposing a client is not observable";
			}
			catch (Exception ex)
			{
				return Describe(ex);
			}
		});

	static async Task<string?> ProbeDisposedAdaptiveSourceAsync()
	{
		var playerReason = await PlayerUnavailableReason;
		if (playerReason is not null)
		{
			return playerReason;
		}

		try
		{
			await using var server = WinMediaHls.StartServer(WinMediaHls.VodPlaylist);
			var created = await AdaptiveMediaSource.CreateFromUriAsync(server.UriFor(WinMediaHls.PlaylistPath)).AsTask().WaitAsync(openTimeout);
			if (created.Status is not AdaptiveMediaSourceCreationStatus.Success || created.MediaSource is null)
			{
				return $"AdaptiveMediaSource.CreateFromUriAsync returned {created.Status} for the local playlist";
			}

			var adaptive = created.MediaSource;
			if (WinMediaClosed.IsClosed(() => adaptive.IsLive))
			{
				return "a live AdaptiveMediaSource cannot be read (AdaptiveMediaSource.IsLive)";
			}

			adaptive.Dispose();
			return WinMediaClosed.IsClosed(() => adaptive.IsLive)
				? null
				: "a disposed AdaptiveMediaSource still answers (IsLive), so disposing an adaptive source is not observable";
		}
		catch (Exception ex)
		{
			return Describe(ex);
		}
	}

	static async Task<string?> ProbeMediaSessionAsync()
	{
		var playerReason = await PlayerUnavailableReason;
		if (playerReason is not null)
		{
			return playerReason;
		}

		var token = $"test-probe-{Guid.NewGuid():N}";
		MediaPlayer? player = null;
		try
		{
			await TestDispatcher.Current.DispatchAsync(() =>
			{
				player = new MediaPlayer();

				// Manual control of the system media transport controls (the command manager would otherwise own them).
				player.CommandManager.IsEnabled = false;
				var controls = player.SystemMediaTransportControls;
				controls.IsEnabled = true;
				controls.IsPlayEnabled = true;
				controls.IsPauseEnabled = true;
				controls.PlaybackStatus = MediaPlaybackStatus.Playing;
				controls.DisplayUpdater.Type = MediaPlaybackType.Music;
				controls.DisplayUpdater.MusicProperties.Title = token;
				controls.DisplayUpdater.MusicProperties.Artist = token;
				controls.DisplayUpdater.Update();
			});

			return await WinMediaMediaSessions.FindAsync(token) is null
				? "this machine shows no system media session for a plain MediaPlayer whose system media controls publish a title (no media session service, or no interactive desktop)"
				: null;
		}
		catch (Exception ex)
		{
			return Describe(ex);
		}
		finally
		{
			if (player is not null)
			{
				await TestDispatcher.Current.DispatchAsync(player.Dispose);
			}
		}
	}

	static async Task<string?> ProbeZeroRateAsync()
	{
		var decoderReason = await DecoderUnavailableReason;
		if (decoderReason is not null)
		{
			return decoderReason;
		}

		try
		{
			var (plain, failure) = await WinMediaPlainPlayer.OpenAsync(player => player.SetUriSource(WinMediaAssets.VideoUri), openTimeout);
			await using (plain)
			{
				if (failure is not null)
				{
					return $"a plain MediaPlayer did not open the clip: {failure}";
				}

				var session = plain.Player.PlaybackSession;
				plain.Player.IsLoopingEnabled = true;
				plain.Player.Play();
				if (!await AssertHelpers.Wait(() => session.PlaybackState is MediaPlaybackState.Playing, 10000, 50))
				{
					return "a plain MediaPlayer did not start playing the clip";
				}

				// What MediaManager.PlatformUpdateSpeed does for Speed = 0 while playing: MediaPlayer.PlaybackRate = 0, then Pause().
				plain.Player.PlaybackRate = 0;
				plain.Player.Pause();
				var kept = await AssertHelpers.Wait(() => session.PlaybackState is MediaPlaybackState.Paused && session.PlaybackRate == 0, 5000, 50);
				return kept ? null : $"a plain MediaPlayer does not keep a playback rate of 0 (it reads back {session.PlaybackRate} in state {session.PlaybackState})";
			}
		}
		catch (Exception ex)
		{
			return Describe(ex);
		}
	}

	static async Task<string?> ProbePlayAtZeroRateAsync()
	{
		var zeroRateReason = await ZeroRateUnavailableReason;
		if (zeroRateReason is not null)
		{
			return zeroRateReason;
		}

		try
		{
			var (plain, failure) = await WinMediaPlainPlayer.OpenAsync(player => player.SetUriSource(WinMediaAssets.VideoUri), openTimeout);
			await using (plain)
			{
				if (failure is not null)
				{
					return $"a plain MediaPlayer did not open the clip: {failure}";
				}

				var session = plain.Player.PlaybackSession;
				plain.Player.IsLoopingEnabled = true;
				plain.Player.PlaybackRate = 0;
				plain.Player.Play();
				return await AssertHelpers.Wait(() => session.PlaybackState is MediaPlaybackState.Playing, 10000, 50)
					? null
					: $"a plain MediaPlayer whose playback rate is 0 does not report Playing after Play() (state {session.PlaybackState})";
			}
		}
		catch (Exception ex)
		{
			return Describe(ex);
		}
	}

	static async Task<string?> ProbeHlsAsync()
	{
		var decoderReason = await DecoderUnavailableReason;
		if (decoderReason is not null)
		{
			return decoderReason;
		}

		try
		{
			await using var server = WinMediaHls.StartServer(WinMediaHls.VodPlaylist);
			var (plain, failure) = await WinMediaPlainPlayer.OpenHlsAsync(server.UriFor(WinMediaHls.PlaylistPath), openTimeout);
			if (plain is not null)
			{
				await plain.DisposeAsync();
			}

			return failure is null ? null : $"a plain MediaPlayer does not open the local HLS stream: {failure}";
		}
		catch (Exception ex)
		{
			return Describe(ex);
		}
	}

	static async Task<string?> ProbeLiveDurationAsync()
	{
		var hlsReason = await HlsUnavailableReason;
		if (hlsReason is not null)
		{
			return hlsReason;
		}

		try
		{
			await using var server = WinMediaHls.StartServer(WinMediaHls.LivePlaylist);
			var (plain, failure) = await WinMediaPlainPlayer.OpenHlsAsync(server.UriFor(WinMediaHls.PlaylistPath), openTimeout);
			if (plain is null)
			{
				return $"a plain MediaPlayer does not open the local live HLS stream: {failure}";
			}

			await using (plain)
			{
				if (failure is not null)
				{
					return $"a plain MediaPlayer does not open the local live HLS stream: {failure}";
				}

				var duration = plain.Player.PlaybackSession.NaturalDuration;
				return duration == TimeSpan.MaxValue ? null : $"a plain MediaPlayer reports a duration of {duration} for a live playlist instead of TimeSpan.MaxValue";
			}
		}
		catch (Exception ex)
		{
			return Describe(ex);
		}
	}

	static async Task<string?> ProbeStarvationBufferingAsync()
	{
		var hlsReason = await HlsUnavailableReason;
		if (hlsReason is not null)
		{
			return hlsReason;
		}

		try
		{
			await using var server = WinMediaHls.StartServer(WinMediaHls.StarvingPlaylist);
			var (plain, failure) = await WinMediaPlainPlayer.OpenHlsAsync(server.UriFor(WinMediaHls.PlaylistPath), openTimeout);
			if (plain is null)
			{
				return $"a plain MediaPlayer does not open the local HLS stream: {failure}";
			}

			await using (plain)
			{
				if (failure is not null)
				{
					return $"a plain MediaPlayer does not open the local HLS stream: {failure}";
				}

				var session = plain.Player.PlaybackSession;
				plain.Player.Play();
				return await AssertHelpers.Wait(() => session.PlaybackState is MediaPlaybackState.Buffering, 15000, 50)
					? null
					: $"a plain MediaPlayer does not report Buffering while the second HLS segment is withheld (state {session.PlaybackState})";
			}
		}
		catch (Exception ex)
		{
			return Describe(ex);
		}
	}

	static async Task<string?> ProbeByteRangeAsync()
	{
		var playerReason = await PlayerUnavailableReason;
		if (playerReason is not null)
		{
			return playerReason;
		}

		try
		{
			var segmentLength = WinMediaAssets.ReadSegmentBytes().Length;
			await using var server = WinMediaHls.StartServer(WinMediaHls.ByteRangePlaylist(segmentLength));
			var (plain, _) = await WinMediaPlainPlayer.OpenHlsAsync(server.UriFor(WinMediaHls.PlaylistPath), openTimeout);
			if (plain is null)
			{
				return "a plain AdaptiveMediaSource does not accept the local #EXT-X-BYTERANGE playlist";
			}

			await using (plain)
			{
				var requested = await AssertHelpers.Wait(() => server.Requests.Any(request => request.Method is "GET" && request.Path == "/hls/media.ts"), 15000, 50);
				if (!requested)
				{
					return "a plain AdaptiveMediaSource never requested the #EXT-X-BYTERANGE segment";
				}

				var range = server.Requests.First(request => request.Method is "GET" && request.Path == "/hls/media.ts").Header("Range");
				return range == WinMediaHls.ExpectedByteRange(segmentLength)
					? null
					: $"a plain AdaptiveMediaSource fetches the #EXT-X-BYTERANGE segment with Range '{range ?? "<none>"}' instead of '{WinMediaHls.ExpectedByteRange(segmentLength)}'";
			}
		}
		catch (Exception ex)
		{
			return Describe(ex);
		}
	}

	static async Task<string?> ProbeEventSubscriptionKeepsTheTargetAliveAsync()
	{
		var playerReason = await PlayerUnavailableReason;
		if (playerReason is not null)
		{
			return playerReason;
		}

		try
		{
			await using var server = WinMediaHls.StartServer(WinMediaHls.VodPlaylist);
			var created = await AdaptiveMediaSource.CreateFromUriAsync(server.UriFor(WinMediaHls.PlaylistPath)).AsTask().WaitAsync(openTimeout);
			if (created.Status is not AdaptiveMediaSourceCreationStatus.Success || created.MediaSource is null)
			{
				return $"AdaptiveMediaSource.CreateFromUriAsync returned {created.Status} for the local playlist";
			}

			var adaptive = created.MediaSource;
			var subscribed = SubscribeUnreferencedTarget(adaptive);
			var unsubscribed = SubscribeAndUnsubscribeUnreferencedTarget(adaptive);

			var subscribedSurvived = await subscribed.WaitForCollect();
			var unsubscribedSurvived = await unsubscribed.WaitForCollect();

			if (subscribed.Target is WinMediaEventTarget target)
			{
				adaptive.DownloadRequested -= target.OnDownloadRequested;
			}

			GC.KeepAlive(adaptive);

			if (!subscribedSurvived)
			{
				return "the target of a handler that is still subscribed to a live AdaptiveMediaSource was garbage collected, so a forgotten unsubscription cannot be seen through the garbage collector";
			}

			return unsubscribedSurvived
				? "the target of a handler that was unsubscribed from a live AdaptiveMediaSource was not garbage collected, so an unsubscription cannot be seen through the garbage collector"
				: null;
		}
		catch (Exception ex)
		{
			return Describe(ex);
		}
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	static WeakReference SubscribeUnreferencedTarget(AdaptiveMediaSource source)
	{
		var target = new WinMediaEventTarget();
		source.DownloadRequested += target.OnDownloadRequested;
		return new WeakReference(target);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	static WeakReference SubscribeAndUnsubscribeUnreferencedTarget(AdaptiveMediaSource source)
	{
		var target = new WinMediaEventTarget();
		source.DownloadRequested += target.OnDownloadRequested;
		source.DownloadRequested -= target.OnDownloadRequested;
		return new WeakReference(target);
	}

	sealed class WinMediaEventTarget
	{
		public void OnDownloadRequested(AdaptiveMediaSource sender, AdaptiveMediaSourceDownloadRequestedEventArgs args)
		{
		}
	}
}

/// <summary>
/// Subscribes to the DownloadRequested event of the AdaptiveMediaSource a MediaManager creates for a header source, after the manager's own
/// handler, and reports for the first media-segment request whether the manager answered it (AdaptiveMediaSourceDownloadResult.InputStream
/// supplied) within 10 s. Without the manager's handler the platform downloads the segment itself and InputStream stays null. The probe takes
/// its own deferral, so the platform waits for it as well, and it polls, so it does not depend on the order in which the two handlers run.
/// </summary>
sealed class WinMediaDownloadProbe
{
	int attached;

	public TaskCompletionSource<bool> FirstSegmentAnsweredByTheManager { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

	/// <summary>
	/// Attaches a probe to the AdaptiveMediaSource the manager of <paramref name="fixture"/> creates next. The fixture must have a
	/// <see cref="WinMediaDispatcher"/> (recordDispatches: true): the probe subscribes when the manager hands the new source to the UI thread, which
	/// is after the manager subscribed and before the player can request anything from the source.
	/// </summary>
	public static WinMediaDownloadProbe AttachToTheNextAdaptiveSource(WinMediaManagerFixture fixture)
	{
		var dispatcher = fixture.Dispatcher ?? throw new InvalidOperationException("The fixture needs a WinMediaDispatcher (recordDispatches: true).");
		var probe = new WinMediaDownloadProbe();
		dispatcher.BeforeDispatch = () =>
		{
			var adaptive = WinMediaReflection.GetField<AdaptiveMediaSource?>(fixture.Manager, "adaptiveMediaSource");
			if (adaptive is not null && Interlocked.Exchange(ref probe.attached, 1) is 0)
			{
				adaptive.DownloadRequested += probe.OnDownloadRequested;
			}
		};
		return probe;
	}

	async void OnDownloadRequested(AdaptiveMediaSource sender, AdaptiveMediaSourceDownloadRequestedEventArgs args)
	{
		if (args.ResourceType is not AdaptiveMediaSourceResourceType.MediaSegment || FirstSegmentAnsweredByTheManager.Task.IsCompleted)
		{
			return;
		}

		var deferral = args.GetDeferral();
		try
		{
			FirstSegmentAnsweredByTheManager.TrySetResult(await AssertHelpers.Wait(() => args.Result.InputStream is not null, 10000, 50));
		}
		catch (Exception ex)
		{
			FirstSegmentAnsweredByTheManager.TrySetException(ex);
		}
		finally
		{
			deferral.Complete();
		}
	}
}