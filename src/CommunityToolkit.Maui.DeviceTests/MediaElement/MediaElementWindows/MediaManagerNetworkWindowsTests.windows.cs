using System.Diagnostics;
using System.Runtime.CompilerServices;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.DeviceTests.Runners;
using CommunityToolkit.Maui.Views;
using Windows.Media.Playback;
using Windows.Media.Streaming.Adaptive;
using HttpClient = Windows.Web.Http.HttpClient;
using WinMediaSource = Windows.Media.Core.MediaSource;

namespace CommunityToolkit.Maui.DeviceTests;

// Whether the platform plays the local HLS streams at all is decided by probes on a plain MediaPlayer / AdaptiveMediaSource before the toolkit is
// touched, so a toolkit failure always fails the test.
[Category(TestCategory.MediaManagerSourceWindows)]
[Collection(TestCollections.DisableParallelization)]
public class MediaManagerNetworkWindowsTests : WinMediaTestBase
{
	const string headerName = "X-Device-Test";
	const string headerValue = "42";

	static WinMediaHttpServer StartVideoServer(byte[] video) =>
		new WinMediaHttpServer
		{
			Handler = request => request.Path is "/video.mp4" or "/a.mp4" or "/b.mp4"
				? WinMediaResponse.ServeBytes(request, video, "video/mp4")
				: WinMediaResponse.NotFound(),
		}.Start();

	static WinMediaHttpServer StartHlsServer(Func<WinMediaResponse> segmentResponse) =>
		WinMediaHls.StartServer(WinMediaHls.VodPlaylist, request => request.Path == "/hls/seg0.ts" ? segmentResponse() : null);

	// Not playable media, which is enough for every test that looks only at the requests.
	static WinMediaResponse DummySegment() => new() { Body = [0, 1, 2, 3] };

	// Only the toolkit's requests (the ones that carry the test header) are held back, so that the plain AdaptiveMediaSource of
	// RequireAdaptiveManifestAsync gets the manifest at once.
	static WinMediaHttpServer StartHlsServerThatHoldsTheManifestBack(ManualResetEventSlim releaseManifest) =>
		WinMediaHls.StartServer(WinMediaHls.VodPlaylist, request =>
		{
			if (IsManifestRequestOfTheToolkit(request))
			{
				releaseManifest.Wait(TimeSpan.FromSeconds(30));
			}

			return null;
		});

	static bool IsManifestRequestOfTheToolkit(WinMediaRequest request) =>
		request.Path == "/" + WinMediaHls.PlaylistPath && request.Header(headerName) == headerValue;

	static UriMediaSource HeaderSource(Uri uri, string name = headerName, string value = headerValue)
	{
		var source = new UriMediaSource { Uri = uri };
		source.HttpHeaders[name] = value;
		return source;
	}

	static Task UpdateSourceAsync(WinMediaManagerFixture fixture) => InvokeOnMainThreadAsync(async () => await fixture.Manager.UpdateSource());

	static async Task RequireAdaptiveManifestAsync(Uri playlistUri)
	{
		AdaptiveMediaSourceCreationResult result;
		try
		{
			result = await AdaptiveMediaSource.CreateFromUriAsync(playlistUri).AsTask().WaitAsync(TimeSpan.FromSeconds(30));
		}
		catch (TimeoutException)
		{
			throw new SkipException("This machine's AdaptiveMediaSource did not answer for the local HLS test playlist within 30 s.");
		}

		if (result.Status is not AdaptiveMediaSourceCreationStatus.Success)
		{
			throw new SkipException($"This machine's AdaptiveMediaSource does not accept the local HLS test playlist ({result.Status}).");
		}
	}

	[Fact]
	public async Task HeaderSourceSendsTheHeadersWithEveryRequestAndFallsBackToTheRangeStream()
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();
		await using var server = StartVideoServer(WinMediaAssets.ReadVideoBytes());
		var fixture = await CreateManagerAsync(view => view.Source = HeaderSource(server.UriFor("video.mp4")));
		var opens = new WinMediaOpenWatcher(fixture.Player!);

		await UpdateSourceAsync(fixture);

		// The player opens the stream (or fails to, without a decoder) through the manager's requests: the test looks at them once that open has ended.
		await opens.UntilOpensEndedAsync(1);

		var requests = server.Requests;
		Assert.NotEmpty(requests);
		Assert.All(requests, request => Assert.Equal(headerValue, request.Header(headerName)));
		Assert.Contains(requests, request => request.Method is "HEAD" && request.Path == "/video.mp4");
		var native = Assert.IsType<WinMediaSource>(fixture.Player!.Source);
		Assert.Null(native.AdaptiveMediaSource);
		Assert.Null(WinMediaReflection.GetField<object?>(fixture.Manager, "adaptiveMediaSource"));
	}

	[Fact]
	public async Task HeaderSourceOpensThroughRangeRequestsThatCarryTheHeaders()
	{
		SetupWindowsBuilder();
		await RequireVideoRenderingAsync();
		await using var server = StartVideoServer(WinMediaAssets.ReadVideoBytes());
		var fixture = await CreateManagerAsync();

		await OpenAsync(fixture.View, HeaderSource(server.UriFor("video.mp4")), fixture.Manager);

		Assert.Contains(server.Requests, request => request.Method is "GET" && request.Header("Range") is not null && request.Header(headerName) == headerValue);
		await WaitUntilAsync(() => fixture.View.MediaWidth == WinMediaAssets.VideoWidth, "The clip streamed over HTTP never reported its natural width.");
	}

	// The expected message is the one of the exception that the refused HEAD request raises (the oracle makes the same request once the manager is
	// done); its HResult carries the HTTP status: 0x80190000 + 405.
	[Fact]
	public async Task HeaderSourceWhoseServerRejectsHeadRaisesMediaFailedWithTheMessageOfTheHttpError()
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();
		var video = WinMediaAssets.ReadVideoBytes();

		// Servers that authorise each method separately (presigned URLs, for example) refuse HEAD while ranged GETs work.
		await using var server = new WinMediaHttpServer
		{
			Handler = request => request.Method is "HEAD"
				? new WinMediaResponse { StatusCode = 405, ReasonPhrase = "Method Not Allowed" }
				: WinMediaResponse.ServeBytes(request, video, "video/mp4"),
		}.Start();
		var fixture = await CreateManagerAsync(view => view.Source = HeaderSource(server.UriFor("video.mp4")));
		var failures = RecordFailures(fixture.View);

		await UpdateSourceAsync(fixture);

		Assert.Contains(server.Requests, request => request.Method is "HEAD" && request.Path == "/video.mp4");
		var failure = Assert.Single(failures);
		Assert.Equal(MediaElementState.Failed, fixture.View.CurrentState);
		Assert.Null(fixture.Player!.Source);

		using var oracleClient = new HttpClient();
		var expected = await Record.ExceptionAsync(() => HttpRandomAccessStream.CreateAsync(oracleClient, server.UriFor("video.mp4")));
		Assert.NotNull(expected);
		Assert.Equal(unchecked((int)0x80190195), expected.HResult);
		Assert.Equal(expected.Message, failure.ErrorMessage);
	}

	[Fact]
	public async Task SourceChangedWhileAHeaderSourceIsStillOpeningKeepsTheNewerSource()
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();
		var video = WinMediaAssets.ReadVideoBytes();
		using var releaseHead = new ManualResetEventSlim();
		await using var server = new WinMediaHttpServer
		{
			Handler = request =>
			{
				if (request.Method is "HEAD")
				{
					releaseHead.Wait(TimeSpan.FromSeconds(30));
				}

				return WinMediaResponse.ServeBytes(request, video, "video/mp4");
			},
		}.Start();
		var fixture = await CreateManagerAsync(view => view.Source = HeaderSource(server.UriFor("video.mp4")));
		var opens = new WinMediaOpenWatcher(fixture.Player!);

		try
		{
			var olderUpdate = InvokeOnMainThreadAsync(async () => await fixture.Manager.UpdateSource());
			await WaitUntilAsync(() => server.Requests.Any(request => request.Method is "HEAD"), "The header source never measured its resource.");

			using var stream = new MemoryStream(video);
			await SetSourceAsync(fixture.View, MediaSource.FromStream(stream), fixture.Manager);
			var newerNativeSource = await InvokeOnMainThreadAsync(() => fixture.Element!.Source);
			Assert.NotNull(newerNativeSource);

			releaseHead.Set();
			await olderUpdate.WaitAsync(TimeSpan.FromMilliseconds(decodeTimeoutMilliseconds));

			await opens.UntilOpensEndedAsync(1);

			await InvokeOnMainThreadAsync(() => Assert.Same(newerNativeSource, fixture.Element!.Source));
		}
		finally
		{
			releaseHead.Set();
		}
	}

	[Fact]
	public async Task FailureOfAHeaderSourceThatWasReplacedWhileOpeningIsNeitherThrownNorReported()
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();
		var video = WinMediaAssets.ReadVideoBytes();
		using var releaseHead = new ManualResetEventSlim();
		await using var server = new WinMediaHttpServer
		{
			Handler = request =>
			{
				if (request.Method is "HEAD")
				{
					releaseHead.Wait(TimeSpan.FromSeconds(30));
					return new WinMediaResponse { StatusCode = 405, ReasonPhrase = "Method Not Allowed" };
				}

				return WinMediaResponse.ServeBytes(request, video, "video/mp4");
			},
		}.Start();
		var fixture = await CreateManagerAsync(view => view.Source = HeaderSource(server.UriFor("video.mp4")));
		var opens = new WinMediaOpenWatcher(fixture.Player!);
		var failures = 0;
		fixture.View.MediaFailed += (_, _) => Interlocked.Increment(ref failures);

		try
		{
			var olderUpdate = InvokeOnMainThreadAsync(async () => await fixture.Manager.UpdateSource());
			await WaitUntilAsync(() => server.Requests.Any(request => request.Method is "HEAD"), "The header source never measured its resource.");

			using var stream = new MemoryStream(video);
			await SetSourceAsync(fixture.View, MediaSource.FromStream(stream), fixture.Manager);
			var newerNativeSource = await InvokeOnMainThreadAsync(() => fixture.Element!.Source);
			Assert.NotNull(newerNativeSource);

			releaseHead.Set();
			var failure = await Record.ExceptionAsync(() => olderUpdate.WaitAsync(TimeSpan.FromMilliseconds(decodeTimeoutMilliseconds)));

			await opens.UntilOpensEndedAsync(1);

			Assert.Null(failure);
			Assert.Equal(0, Volatile.Read(ref failures));
			await InvokeOnMainThreadAsync(() => Assert.Same(newerNativeSource, fixture.Element!.Source));
		}
		finally
		{
			releaseHead.Set();
		}
	}

	// Once the manifest arrives, the older update must neither keep the adaptive source it created (the manager would answer its downloads) nor
	// give it to the player.
	[Fact]
	public async Task HeaderHlsSourceReplacedWhileItsManifestLoadsKeepsTheNewerSource()
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();
		using var releaseManifest = new ManualResetEventSlim();
		await using var server = StartHlsServerThatHoldsTheManifestBack(releaseManifest);
		await RequireAdaptiveManifestAsync(server.UriFor(WinMediaHls.PlaylistPath));
		var fixture = await CreateManagerAsync(view => view.Source = HeaderSource(server.UriFor(WinMediaHls.PlaylistPath)));
		var opens = new WinMediaOpenWatcher(fixture.Player!);

		try
		{
			var olderUpdate = InvokeOnMainThreadAsync(async () => await fixture.Manager.UpdateSource());
			await WaitUntilAsync(() => server.Requests.Any(IsManifestRequestOfTheToolkit), "The header source never requested its manifest.");

			using var stream = new MemoryStream(WinMediaAssets.ReadVideoBytes());
			await SetSourceAsync(fixture.View, MediaSource.FromStream(stream), fixture.Manager);
			var newerNativeSource = await InvokeOnMainThreadAsync(() => fixture.Element!.Source);
			Assert.NotNull(newerNativeSource);

			releaseManifest.Set();
			var failure = await Record.ExceptionAsync(() => olderUpdate.WaitAsync(TimeSpan.FromMilliseconds(decodeTimeoutMilliseconds)));

			await opens.UntilOpensEndedAsync(1);

			Assert.Null(failure);
			await InvokeOnMainThreadAsync(() => Assert.Same(newerNativeSource, fixture.Element!.Source));
			Assert.Null(WinMediaReflection.GetField<object?>(fixture.Manager, "adaptiveMediaSource"));
			Assert.DoesNotContain(server.Requests, request => request.Path == "/hls/seg0.ts");
		}
		finally
		{
			releaseManifest.Set();
		}
	}

	[Fact]
	public async Task DisposeWhileAHeaderHlsSourceLoadsItsManifestAppliesNothing()
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();
		using var releaseManifest = new ManualResetEventSlim();
		await using var server = StartHlsServerThatHoldsTheManifestBack(releaseManifest);
		await RequireAdaptiveManifestAsync(server.UriFor(WinMediaHls.PlaylistPath));
		var fixture = await CreateManagerAsync(view => view.Source = HeaderSource(server.UriFor(WinMediaHls.PlaylistPath)));

		try
		{
			var update = InvokeOnMainThreadAsync(async () => await fixture.Manager.UpdateSource());
			await WaitUntilAsync(() => server.Requests.Any(IsManifestRequestOfTheToolkit), "The header source never requested its manifest.");

			await InvokeOnMainThreadAsync(fixture.Manager.Dispose);

			releaseManifest.Set();
			var failure = await Record.ExceptionAsync(() => update.WaitAsync(TimeSpan.FromMilliseconds(decodeTimeoutMilliseconds)));

			Assert.Null(failure);
			Assert.Null(fixture.Player!.Source);
			Assert.Null(WinMediaReflection.GetField<object?>(fixture.Manager, "adaptiveMediaSource"));
			Assert.DoesNotContain(server.Requests, request => request.Path == "/hls/seg0.ts");
		}
		finally
		{
			releaseManifest.Set();
		}
	}

	// Covers the version check that SetUriSourceWithHeaders repeats on the UI thread.
	[Fact]
	public async Task HeaderHlsSourceReplacedOnItsWayToThePlayerIsNotApplied()
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();
		await using var server = StartHlsServer(DummySegment);
		await RequireAdaptiveManifestAsync(server.UriFor(WinMediaHls.PlaylistPath));

		var (fixture, _) = await ReplaceAHeaderHlsSourceOnItsWayToThePlayerAsync(server);

		await InvokeOnMainThreadAsync(() => Assert.Null(Assert.IsType<WinMediaSource>(fixture.Element!.Source).AdaptiveMediaSource));
		Assert.Null(WinMediaReflection.GetField<object?>(fixture.Manager, "adaptiveMediaSource"));
		Assert.DoesNotContain(server.Requests, request => request.Path == "/hls/seg0.ts");
	}

	// Nothing uses the adaptive source of the superseded update any more (the newer update has unsubscribed it), so the update closes it, and the
	// MediaSource it created around it, instead of leaving their resources to the garbage collector.
	[Fact]
	public async Task HeaderHlsSourceReplacedOnItsWayToThePlayerIsClosed()
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();
		var notObservable = await WinMediaProbe.DisposedAdaptiveSourceNotObservableReason;
		Skip.If(notObservable is not null, $"Whether an AdaptiveMediaSource was closed cannot be observed on this machine ({notObservable}).");
		await using var server = StartHlsServer(DummySegment);
		await RequireAdaptiveManifestAsync(server.UriFor(WinMediaHls.PlaylistPath));

		var (_, superseded) = await ReplaceAHeaderHlsSourceOnItsWayToThePlayerAsync(server);

		Assert.True(WinMediaClosed.IsClosed(() => superseded.IsLive), "The adaptive source of the superseded update was not closed.");
	}

	// At the moment the manager hands the adaptive source of the older update to the UI thread (its manifest has loaded, the version check under the
	// lock has passed), the dispatcher starts the update to a newer stream source on the UI thread and waits until its synchronous part (the new
	// version) has run before it queues the older update's work.
	async Task<(WinMediaManagerFixture Fixture, AdaptiveMediaSource Superseded)> ReplaceAHeaderHlsSourceOnItsWayToThePlayerAsync(WinMediaHttpServer server)
	{
		// Registered before the fixture, so that it is disposed after the player that reads it is released (cleanups run last registered first).
		var stream = new MemoryStream(WinMediaAssets.ReadVideoBytes());
		RegisterCleanup(stream.Dispose);
		var fixture = await CreateManagerAsync(view => view.Source = HeaderSource(server.UriFor(WinMediaHls.PlaylistPath)), recordDispatches: true);
		var opens = new WinMediaOpenWatcher(fixture.Player!);

		var started = 0;
		AdaptiveMediaSource? superseded = null;
		Task? newerUpdate = null;
		Exception? hookFailure = null;
		fixture.Dispatcher!.BeforeDispatch = () =>
		{
			var adaptive = WinMediaReflection.GetField<AdaptiveMediaSource?>(fixture.Manager, "adaptiveMediaSource");
			if (adaptive is null || Interlocked.Exchange(ref started, 1) is not 0)
			{
				return;
			}

			superseded = adaptive;
			try
			{
				var newerStarted = TestDispatcher.Current.DispatchAsync(() =>
				{
					fixture.View.Source = MediaSource.FromStream(stream);
					newerUpdate = fixture.Manager.UpdateSource().AsTask();
				});

				if (!newerStarted.Wait(TimeSpan.FromSeconds(10)))
				{
					throw new TimeoutException("The newer source update did not start on the UI thread within 10 s.");
				}
			}
			catch (Exception ex)
			{
				// Thrown here, it would surface in the older update, which hides the failures of a superseded update.
				hookFailure = ex;
			}
		};

		await InvokeOnMainThreadAsync(async () => await fixture.Manager.UpdateSource());

		Assert.Null(hookFailure);
		Assert.True(superseded is not null && newerUpdate is not null, "The older update never handed an adaptive source to the dispatcher.");
		await newerUpdate.WaitAsync(TimeSpan.FromMilliseconds(decodeTimeoutMilliseconds));
		await FlushDispatcherAsync();

		await opens.UntilOpensEndedAsync(1);
		return (fixture, superseded);
	}

	[Fact]
	public async Task SecondHeaderSourceReusesTheClientAndDropsTheStaleHeaders()
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();
		await using var server = StartVideoServer(WinMediaAssets.ReadVideoBytes());
		var fixture = await CreateManagerAsync(view => view.Source = HeaderSource(server.UriFor("a.mp4"), "X-Device-Test-A", "1"));
		var opens = new WinMediaOpenWatcher(fixture.Player!);
		await UpdateSourceAsync(fixture);
		var clientAfterFirstSource = WinMediaReflection.GetField<object?>(fixture.Manager, "headerHttpClient");
		Assert.NotNull(clientAfterFirstSource);

		// Each source is opened to its end (opened, or failed without a decoder), so no open is in flight when the next step comes, and the second
		// source's requests below include those of its open.
		await opens.UntilOpensEndedAsync(1);
		await SetSourceAsync(fixture.View, HeaderSource(server.UriFor("b.mp4"), "X-Device-Test-B", "2"), fixture.Manager);
		await opens.UntilOpensEndedAsync(2);

		Assert.Same(clientAfterFirstSource, WinMediaReflection.GetField<object?>(fixture.Manager, "headerHttpClient"));
		var secondSourceRequests = server.Requests.Where(request => request.Path == "/b.mp4").ToList();
		Assert.NotEmpty(secondSourceRequests);
		Assert.All(secondSourceRequests, request =>
		{
			Assert.Equal("2", request.Header("X-Device-Test-B"));
			Assert.Null(request.Header("X-Device-Test-A"));
		});
	}

	// SetUriSourceWithHeaders asks AdaptiveMediaSource first. For a media file, which is no manifest, the response of that request comes back with
	// its body unread, and it holds its connection until its content is closed: left to the garbage collector, six of them used up the
	// connections that the process may open to the host (HttpBaseProtocolFilter.MaxConnectionsPerServer, shared by every HttpClient and port of
	// the host), and every later request to it waited until it timed out (0x80072EE2, after 60 s). CI run 38089989724 hit it in
	// SecondHeaderSourceReusesTheClientAndDropsTheStaleHeaders, once the header sources of the earlier tests had left theirs.
	[Fact]
	public async Task HeaderSourcesOfMediaFilesLeaveNoConnectionToTheHostInUse()
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();
		var video = WinMediaAssets.ReadVideoBytes();
		await using var server = new WinMediaHttpServer { Handler = request => WinMediaResponse.ServeBytes(request, video, "video/mp4") }.Start();
		var fixture = await CreateManagerAsync();
		var opens = new WinMediaOpenWatcher(fixture.Player!);

		// More header sources than the six connections of a host.
		const int sources = 8;
		for (var i = 1; i <= sources; i++)
		{
			await SetSourceAsync(fixture.View, HeaderSource(server.UriFor($"video{i}.mp4")), fixture.Manager).WaitAsync(TimeSpan.FromMilliseconds(decodeTimeoutMilliseconds));
			await opens.UntilOpensEndedAsync(i);
		}

		Assert.All(Enumerable.Range(1, sources), i => Assert.Contains(server.Requests, request => request.Method is "HEAD" && request.Path == $"/video{i}.mp4"));
	}

	[Fact]
	public async Task OpeningASourceThatNeverAnswersReportsTheOpeningState()
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();

		// The server holds every answer back, so the player stays in MediaPlaybackState.Opening for as long as the test looks at it.
		await using var server = new WinMediaHttpServer { Handler = _ => new WinMediaResponse { Delay = TimeSpan.FromSeconds(60) } }.Start();
		var fixture = await CreateManagerAsync(view => view.Source = new UriMediaSource { Uri = server.UriFor("never.mp4") });

		await UpdateSourceAsync(fixture);

		await WaitUntilAsync(() => fixture.Player!.PlaybackSession.PlaybackState is MediaPlaybackState.Opening, "The native player never entered the Opening state.");
		await WaitUntilAsync(() => fixture.View.CurrentState is MediaElementState.Opening, "The manager did not report the Opening state.");
	}

	[Fact]
	public async Task HeaderWithAnIllegalNameRaisesMediaFailedThatNamesTheHeader()
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();

		// The manager fails the source only when the platform refuses the header: make sure this platform refuses this name.
		const string illegalName = "Invalid Header";
		bool accepted;
		try
		{
			using var probeClient = new HttpClient();
			accepted = probeClient.DefaultRequestHeaders.TryAppendWithoutValidation(illegalName, "x");
		}
		catch (Exception ex)
		{
			throw new SkipException($"This machine's HttpClient throws {ex.GetType().Name} for the header name '{illegalName}' instead of returning false, so the branch that rejects a header cannot be reached with it.");
		}

		Skip.If(accepted, $"This machine's HttpClient accepts the header name '{illegalName}', so the branch that rejects a header cannot be reached with it.");

		await using var server = StartVideoServer(WinMediaAssets.ReadVideoBytes());
		var fixture = await CreateManagerAsync(view => view.Source = HeaderSource(server.UriFor("video.mp4"), illegalName, "x"));
		var failures = RecordFailures(fixture.View);

		await UpdateSourceAsync(fixture);

		Assert.Equal($"Failed to append HTTP header '{illegalName}'. The header name may be empty or contain invalid characters.", Assert.Single(failures).ErrorMessage);
		Assert.Equal(MediaElementState.Failed, fixture.View.CurrentState);
		Assert.Empty(server.Requests);
		Assert.Null(fixture.Player!.Source);
	}

	[Fact]
	public async Task HlsPlaylistWithHeadersUsesTheAdaptiveSource()
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();
		await using var server = StartHlsServer(DummySegment);
		await RequireAdaptiveManifestAsync(server.UriFor(WinMediaHls.PlaylistPath));
		var baseline = server.Requests.Count;
		var fixture = await CreateManagerAsync(view => view.Source = HeaderSource(server.UriFor(WinMediaHls.PlaylistPath)));

		await UpdateSourceAsync(fixture);

		var native = Assert.IsType<WinMediaSource>(fixture.Player!.Source);
		Assert.NotNull(native.AdaptiveMediaSource);
		Assert.NotNull(WinMediaReflection.GetField<object?>(fixture.Manager, "adaptiveMediaSource"));
		var requests = server.Requests.Skip(baseline).ToList();
		Assert.DoesNotContain(requests, request => request.Method is "HEAD");
		var manifestRequests = requests.Where(request => request.Path == "/" + WinMediaHls.PlaylistPath).ToList();
		Assert.NotEmpty(manifestRequests);
		Assert.All(manifestRequests, request => Assert.Equal(headerValue, request.Header(headerName)));
	}

	[Fact]
	public async Task LiveHlsStreamReportsAZeroDurationBecauseTheNativeDurationIsUnknown()
	{
		SetupWindowsBuilder();
		await RequireDecoderAsync();
		await RequireLiveDurationAsync();
		await using var server = WinMediaHls.StartServer(WinMediaHls.LivePlaylist);
		var fixture = await CreateManagerAsync();

		await OpenAsync(fixture.View, HeaderSource(server.UriFor(WinMediaHls.PlaylistPath)), fixture.Manager);

		// The duration of a live stream is unknown: the platform reports TimeSpan.MaxValue (a plain player was probed for exactly that), and the
		// manager must not publish that as the Duration.
		Assert.Equal(TimeSpan.MaxValue, fixture.Player!.PlaybackSession.NaturalDuration);
		await FlushDispatcherAsync();
		Assert.Equal(TimeSpan.Zero, fixture.View.Duration);

		// The same arm with a stale duration that only the handler can clear, run on the UI thread (it then writes the duration inline).
		await InvokeOnMainThreadAsync(() =>
		{
			((IMediaElement)fixture.View).Duration = TimeSpan.FromSeconds(42);
			using var capture = new WinMediaAsyncVoidCapture();

			WinMediaReflection.Invoke(fixture.Manager, "OnMediaElementMediaOpened", fixture.Player, null);

			Assert.Equal(TimeSpan.Zero, fixture.View.Duration);
			Assert.Empty(capture.Exceptions);
		});
	}

	[Fact]
	public async Task PlaybackThatRunsOutOfDataIsReportedAsBuffering()
	{
		SetupWindowsBuilder();
		await RequireDecoderAsync();
		await RequireStarvationBufferingAsync();

		await using var server = WinMediaHls.StartServer(WinMediaHls.StarvingPlaylist);
		var fixture = await CreateManagerAsync();
		await OpenAsync(fixture.View, HeaderSource(server.UriFor(WinMediaHls.PlaylistPath)), fixture.Manager);

		await InvokeOnMainThreadAsync(() => fixture.Manager.Play());

		await WaitUntilAsync(() => fixture.Player!.PlaybackSession.PlaybackState is MediaPlaybackState.Buffering, "The native player did not enter Buffering while the second segment was withheld.", 15000);
		await WaitUntilAsync(() => fixture.View.CurrentState is MediaElementState.Buffering, "The manager did not report the Buffering state.");
	}

	// After Stop() (or the end of the media) the player can buffer before it settles, for example while a network source loads the position Stop()
	// rewound to. The manager's handler gets the session of a player that really is buffering (the second segment never arrives) while the manager
	// is in the state Stop() leaves it in; Stop() itself is not called, because its pause would end the buffering.
	[Fact]
	public async Task NativeBufferingAfterStopKeepsReportingStoppedUntilPlay()
	{
		SetupWindowsBuilder();
		await RequireDecoderAsync();
		await RequireStarvationBufferingAsync();
		await using var server = WinMediaHls.StartServer(WinMediaHls.StarvingPlaylist);
		var fixture = await CreateManagerAsync();
		await OpenAsync(fixture.View, HeaderSource(server.UriFor(WinMediaHls.PlaylistPath)), fixture.Manager);
		await InvokeOnMainThreadAsync(() => fixture.Manager.Play());
		await WaitUntilAsync(() => fixture.Player!.PlaybackSession.PlaybackState is MediaPlaybackState.Buffering, "The native player did not enter Buffering while the second segment was withheld.", 15000);

		// Reported: the manager's own handler for that native change has run, so it cannot overwrite the state the test sets below.
		await WaitUntilAsync(() => fixture.View.CurrentState is MediaElementState.Buffering, "The manager did not report the Buffering state.");

		await InvokeOnMainThreadAsync(() =>
		{
			var session = fixture.Player!.PlaybackSession;

			((IMediaElement)fixture.View).CurrentStateChanged(MediaElementState.Stopped);
			WinMediaReflection.SetField(fixture.Manager, "keepReportingStopped", true);

			WinMediaReflection.Invoke(fixture.Manager, "OnPlaybackSessionPlaybackStateChanged", session, null);
			Assert.Equal(MediaElementState.Stopped, fixture.View.CurrentState);

			fixture.Manager.Play();
			WinMediaReflection.Invoke(fixture.Manager, "OnPlaybackSessionPlaybackStateChanged", session, null);
			Assert.Equal(MediaElementState.Buffering, fixture.View.CurrentState);
		});
	}

	[Fact]
	public async Task EndedHlsStreamReportsItsNativeDuration()
	{
		SetupWindowsBuilder();
		await RequireDecoderAsync();
		await RequireHlsAsync();
		await using var server = WinMediaHls.StartServer(WinMediaHls.VodPlaylist);
		var fixture = await CreateManagerAsync();

		await OpenAsync(fixture.View, HeaderSource(server.UriFor(WinMediaHls.PlaylistPath)), fixture.Manager);

		await WaitUntilAsync(() => fixture.View.Duration > TimeSpan.Zero, "The duration of the opened HLS stream was never published.");
		Assert.True((fixture.View.Duration - fixture.Player!.PlaybackSession.NaturalDuration).Duration() < TimeSpan.FromMilliseconds(50), $"Duration {fixture.View.Duration}, native {fixture.Player.PlaybackSession.NaturalDuration}.");
	}

	[Fact]
	public async Task AdaptiveSegmentRequestsCarryTheHeaders()
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();
		await using var server = StartHlsServer(DummySegment);
		await RequireAdaptiveManifestAsync(server.UriFor(WinMediaHls.PlaylistPath));
		var baseline = server.Requests.Count;
		var fixture = await CreateManagerAsync(view => view.Source = HeaderSource(server.UriFor(WinMediaHls.PlaylistPath)), recordDispatches: true);
		var probe = WinMediaDownloadProbe.AttachToTheNextAdaptiveSource(fixture);

		await UpdateSourceAsync(fixture);

		Assert.True(await probe.FirstSegmentAnsweredByTheManager.Task.WaitAsync(TimeSpan.FromMilliseconds(decodeTimeoutMilliseconds)), "The manager did not supply the downloaded segment (AdaptiveMediaSourceDownloadResult.InputStream) within 10 s.");

		var segmentRequests = server.Requests.Skip(baseline).Where(request => request.Path == "/hls/seg0.ts").ToList();
		Assert.NotEmpty(segmentRequests);
		Assert.All(segmentRequests, request => Assert.Equal(headerValue, request.Header(headerName)));
	}

	[Fact]
	public async Task AdaptiveSegmentDownloadedByTheToolkitIsPlayed()
	{
		SetupWindowsBuilder();
		await RequireVideoRenderingAsync();
		await RequireHlsAsync();
		await using var server = WinMediaHls.StartServer(WinMediaHls.VodPlaylist);
		var fixture = await CreateManagerAsync(recordDispatches: true);
		var probe = WinMediaDownloadProbe.AttachToTheNextAdaptiveSource(fixture);

		await OpenAsync(fixture.View, HeaderSource(server.UriFor(WinMediaHls.PlaylistPath)), fixture.Manager);
		Assert.True(await probe.FirstSegmentAnsweredByTheManager.Task.WaitAsync(TimeSpan.FromMilliseconds(decodeTimeoutMilliseconds)), "The manager did not supply the downloaded segment, so this test would not exercise its download handler.");

		await InvokeOnMainThreadAsync(() => fixture.Manager.Play());

		// The platform hands the segment to the decoder only once every deferral of its download request is complete: playback that advances
		// proves that the manager completed its deferral (in the finally block of OnAdaptiveMediaSourceDownloadRequested).
		await WaitUntilAsync(() => fixture.Player!.PlaybackSession.Position > TimeSpan.FromSeconds(0.3), "Playback of the segment the manager downloaded never advanced.");
	}

	[Fact]
	public async Task ByteRangeSegmentsAreFetchedWithTheirRange()
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();
		await RequireByteRangeAsync();
		var segmentLength = WinMediaAssets.ReadSegmentBytes().Length;
		await using var server = WinMediaHls.StartServer(WinMediaHls.ByteRangePlaylist(segmentLength));
		var fixture = await CreateManagerAsync(view => view.Source = HeaderSource(server.UriFor(WinMediaHls.PlaylistPath)));
		var opens = new WinMediaOpenWatcher(fixture.Player!);

		await UpdateSourceAsync(fixture);

		await WaitUntilAsync(() => server.Requests.Any(request => request.Method is "GET" && request.Path == "/hls/media.ts"), "The byte-range segment was never requested.");

		// The segment is real media, so the player may open it.
		await opens.UntilOpensEndedAsync(1);
		var request = server.Requests.First(request => request.Method is "GET" && request.Path == "/hls/media.ts");
		Assert.Equal(headerValue, request.Header(headerName));
		Assert.Equal(WinMediaHls.ExpectedByteRange(segmentLength), request.Header("Range"));
	}

	// The manager fetches only the range of media.ts that the playlist names and supplies that part as the download, leaving the result's byte range
	// as the platform set it, so the platform must take the stream as exactly that range: had it applied the offset to the stream again, the segment
	// would be cut and would not play.
	[Fact]
	public async Task ByteRangeSegmentDownloadedByTheToolkitIsPlayed()
	{
		SetupWindowsBuilder();
		await RequireHlsAsync();
		await RequireByteRangeAsync();
		var segmentLength = WinMediaAssets.ReadSegmentBytes().Length;
		await using var server = WinMediaHls.StartServer(WinMediaHls.ByteRangePlaylist(segmentLength));
		var fixture = await CreateManagerAsync(recordDispatches: true);
		var probe = WinMediaDownloadProbe.AttachToTheNextAdaptiveSource(fixture);

		await OpenAsync(fixture.View, HeaderSource(server.UriFor(WinMediaHls.PlaylistPath)), fixture.Manager);
		Assert.True(await probe.FirstSegmentAnsweredByTheManager.Task.WaitAsync(TimeSpan.FromMilliseconds(decodeTimeoutMilliseconds)), "The manager did not supply the byte-range segment, so this test would not exercise its download handler.");
		Assert.Equal(WinMediaHls.ExpectedByteRange(segmentLength), server.Requests.First(request => request.Method is "GET" && request.Path == "/hls/media.ts").Header("Range"));

		await InvokeOnMainThreadAsync(() => fixture.Manager.Play());

		await WaitUntilAsync(() => fixture.Player!.PlaybackSession.Position > TimeSpan.FromSeconds(0.3), "Playback of the byte-range segment the manager downloaded never advanced.");
	}

	[Fact]
	public async Task AdaptiveDownloadFailureIsTracedAndDoesNotThrow()
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();
		await using var server = StartHlsServer(WinMediaResponse.ServerError);
		await RequireAdaptiveManifestAsync(server.UriFor(WinMediaHls.PlaylistPath));
		var listener = new WinMediaTraceListener();
		Trace.Listeners.Add(listener);
		RegisterCleanup(() => Trace.Listeners.Remove(listener));
		var fixture = await CreateManagerAsync(view => view.Source = HeaderSource(server.UriFor(WinMediaHls.PlaylistPath)));

		await UpdateSourceAsync(fixture);

		await WaitUntilAsync(() => listener.Messages.Any(message => message.Contains("DownloadRequested failed for", StringComparison.Ordinal) && message.Contains("seg0.ts", StringComparison.Ordinal)),
			"The failed segment download was not traced.");
	}

	[Fact]
	public async Task ChangingTheSourceDropsTheAdaptiveSource()
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();
		await using var server = StartHlsServer(DummySegment);
		await RequireAdaptiveManifestAsync(server.UriFor(WinMediaHls.PlaylistPath));
		var fixture = await CreateManagerAsync(view => view.Source = HeaderSource(server.UriFor(WinMediaHls.PlaylistPath)));
		await UpdateSourceAsync(fixture);
		Assert.NotNull(WinMediaReflection.GetField<object?>(fixture.Manager, "adaptiveMediaSource"));

		await SetSourceAsync(fixture.View, null, fixture.Manager);

		Assert.Null(WinMediaReflection.GetField<object?>(fixture.Manager, "adaptiveMediaSource"));
		Assert.Null(fixture.Player!.Source);
	}

	// The old AdaptiveMediaSource stays alive (the test holds it). Its DownloadRequested subscription would keep the manager alive, so a manager
	// that can be collected proves that the source change unsubscribed. Dispose removes the manager's other subscriptions (the player's events)
	// but cannot touch the old source any more: the source change already cleared the field.
	[Fact]
	public async Task ChangingTheSourceUnsubscribesTheOldAdaptiveSource()
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();
		await RequireEventSubscriptionControlAsync();
		await using var server = StartHlsServer(DummySegment);
		await RequireAdaptiveManifestAsync(server.UriFor(WinMediaHls.PlaylistPath));

		var (oldAdaptiveSource, manager) = await OpenAHeaderSourceThenAsync(server, replaceTheSourceBeforeDisposing: true);

		await AssertionExtensions.WaitForGC(manager);
		GC.KeepAlive(oldAdaptiveSource);
	}

	// The same oracle for Dispose: with the adaptive source still current, only Dispose's own unsubscription can release the manager.
	[Fact]
	public async Task DisposeUnsubscribesTheAdaptiveSource()
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();
		await RequireEventSubscriptionControlAsync();
		await using var server = StartHlsServer(DummySegment);
		await RequireAdaptiveManifestAsync(server.UriFor(WinMediaHls.PlaylistPath));

		var (adaptiveSource, manager) = await OpenAHeaderSourceThenAsync(server, replaceTheSourceBeforeDisposing: false);

		await AssertionExtensions.WaitForGC(manager);
		GC.KeepAlive(adaptiveSource);
	}

	// Only this method references the manager (the caller gets it weakly): the method is not inlined, and the manager is never captured by a lambda
	// that outlives it.
	[MethodImpl(MethodImplOptions.NoInlining)]
	async Task<(AdaptiveMediaSource AdaptiveSource, WeakReference Manager)> OpenAHeaderSourceThenAsync(WinMediaHttpServer server, bool replaceTheSourceBeforeDisposing)
	{
		var (manager, view) = await CreateManagerThatOnlyTheCallerReferencesAsync(HeaderSource(server.UriFor(WinMediaHls.PlaylistPath)));

		await InvokeOnMainThreadAsync(async () => await manager.UpdateSource());
		var adaptiveSource = WinMediaReflection.GetField<AdaptiveMediaSource?>(manager, "adaptiveMediaSource");
		Assert.NotNull(adaptiveSource);
		await WaitUntilAsync(() => server.Requests.Any(request => request.Path == "/hls/seg0.ts"), "The adaptive source never requested its segment through the manager.");

		if (replaceTheSourceBeforeDisposing)
		{
			await SetSourceAsync(view, null, manager);
			Assert.Null(WinMediaReflection.GetField<object?>(manager, "adaptiveMediaSource"));
		}

		await InvokeOnMainThreadAsync(manager.Dispose);
		return (adaptiveSource, new WeakReference(manager));
	}

	// CreateManagerAsync registers a cleanup that holds the manager until the test ends; this manager is held only by the caller. The view and
	// the native player (neither references the manager) are released by a cleanup of their own.
	async Task<(WinMediaManager Manager, MediaElement View)> CreateManagerThatOnlyTheCallerReferencesAsync(MediaSource source)
	{
		var view = await InvokeOnMainThreadAsync(() =>
		{
			var created = new MediaElement { Source = source };

			// Resolve the view's dispatcher here, on the UI thread: MediaElement creates its status timer from it.
			_ = created.Dispatcher;
			return created;
		});
		var (manager, player) = await InvokeOnMainThreadAsync(() => CreateManagerOnTheUiThread(view));
		RegisterViewAndPlayerCleanup(view, player);
		return (manager, view);
	}

	(WinMediaManager Manager, MediaPlayer Player) CreateManagerOnTheUiThread(MediaElement view)
	{
		var manager = new WinMediaManager(MauiContext, view, TestDispatcher.Current);
		return (manager, manager.CreatePlatformView().MediaPlayer);
	}

	void RegisterViewAndPlayerCleanup(MediaElement view, MediaPlayer player) =>
		RegisterCleanup(() =>
		{
			view.Dispose();
			ReleasePlayer(player);
		});

	[Fact]
	public async Task DisposeClearsTheHeaderClientAndTheAdaptiveSource()
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();
		await using var server = StartHlsServer(DummySegment);
		await RequireAdaptiveManifestAsync(server.UriFor(WinMediaHls.PlaylistPath));
		var fixture = await CreateManagerAsync(view => view.Source = HeaderSource(server.UriFor(WinMediaHls.PlaylistPath)));
		await UpdateSourceAsync(fixture);
		Assert.NotNull(WinMediaReflection.GetField<object?>(fixture.Manager, "headerHttpClient"));
		Assert.NotNull(WinMediaReflection.GetField<object?>(fixture.Manager, "adaptiveMediaSource"));

		await InvokeOnMainThreadAsync(() => fixture.Manager.Dispose());

		Assert.Null(WinMediaReflection.GetField<object?>(fixture.Manager, "headerHttpClient"));
		Assert.Null(WinMediaReflection.GetField<object?>(fixture.Manager, "adaptiveMediaSource"));
	}

	[Fact]
	public async Task DisposeClosesTheHeaderHttpClient()
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();
		var notObservable = await WinMediaProbe.DisposedHttpClientNotObservableReason;
		Skip.If(notObservable is not null, $"Whether an HttpClient was closed cannot be observed on this machine ({notObservable}).");
		await using var server = StartVideoServer(WinMediaAssets.ReadVideoBytes());
		var fixture = await CreateManagerAsync(view => view.Source = HeaderSource(server.UriFor("video.mp4")));
		var opens = new WinMediaOpenWatcher(fixture.Player!);
		await UpdateSourceAsync(fixture);

		// No open may be in flight when the manager is disposed.
		await opens.UntilOpensEndedAsync(1);
		var client = WinMediaReflection.GetField<HttpClient>(fixture.Manager, "headerHttpClient");
		Assert.False(WinMediaClosed.IsClosed(() => client.DefaultRequestHeaders), "Precondition: the live client answers.");

		await InvokeOnMainThreadAsync(() => fixture.Manager.Dispose());

		Assert.True(WinMediaClosed.IsClosed(() => client.DefaultRequestHeaders), "The header HttpClient was not closed by Dispose.");
	}

	[Fact]
	public async Task HeaderSourceWithoutPlatformViewReturnsWithoutCreatingAClient()
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();
		await using var server = StartVideoServer(WinMediaAssets.ReadVideoBytes());
		var fixture = await CreateManagerAsync(createPlatformView: false);
		var headers = new Dictionary<string, string> { [headerName] = headerValue };

		// The current source update, so that only the Player guard can stop the call.
		var currentVersion = WinMediaReflection.GetField<int>(fixture.Manager, "sourceUpdateVersion");

		// SetUriSourceWithHeaders is private and only called after PlatformUpdateSource checked Player, so this guard is reached by reflection.
		await InvokeOnMainThreadAsync(async () => await (Task)WinMediaReflection.Invoke(fixture.Manager, "SetUriSourceWithHeaders", server.UriFor("video.mp4"), headers, currentVersion)!);

		Assert.Null(WinMediaReflection.GetField<object?>(fixture.Manager, "headerHttpClient"));
		Assert.Empty(server.Requests);
	}

	// PlatformUpdateSource raises PositionChanged and PropertyChanged (Position and Duration back to zero) before it opens a header source, and
	// their handlers run synchronously: one that sets Source again starts a newer source update, which opens that source itself. The superseded
	// update passes its own version, so it neither touches the shared header client nor requests anything (and cannot adopt the newer update's
	// version and subscribe a second adaptive source).
	[Fact]
	public async Task HeaderSourceOfASupersededSourceUpdateRequestsNothing()
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();
		await using var server = StartVideoServer(WinMediaAssets.ReadVideoBytes());
		var fixture = await CreateManagerAsync();
		var headers = new Dictionary<string, string> { [headerName] = headerValue };
		var supersededVersion = WinMediaReflection.GetField<int>(fixture.Manager, "sourceUpdateVersion") - 1;

		await InvokeOnMainThreadAsync(async () => await (Task)WinMediaReflection.Invoke(fixture.Manager, "SetUriSourceWithHeaders", server.UriFor("video.mp4"), headers, supersededVersion)!);

		Assert.Null(WinMediaReflection.GetField<object?>(fixture.Manager, "headerHttpClient"));
		Assert.Empty(server.Requests);
		Assert.Null(fixture.Player!.Source);
	}

	[Fact]
	public async Task DownloadRequestWithoutAHeaderClientReturnsBeforeTouchingItsArguments()
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();
		var fixture = await CreateManagerAsync(createPlatformView: false);

		await InvokeOnMainThreadAsync(() =>
		{
			using var capture = new WinMediaAsyncVoidCapture();

			// The event arrived after Dispose cleared the client (or before any header source): the handler must not call args.GetDeferral().
			WinMediaReflection.Invoke(fixture.Manager, "OnAdaptiveMediaSourceDownloadRequested", null, null);

			Assert.Empty(capture.Exceptions);
		});
	}
}