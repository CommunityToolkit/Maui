using System.Runtime.CompilerServices;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;
using Microsoft.Maui;

namespace CommunityToolkit.Maui.DeviceTests;

// With the foreground service on, every MediaElement controls a MediaController, which controls the player that the service builds for it.
// Every Media3 call of these tests runs on the UI thread. The late connection of a controller has no event to wait for, so the tests that
// need the end of it wait for the time it takes (controllerConnectionTime).
[Category(TestCategory.MediaElementAndroid)]
[Collection(TestCollections.DisableParallelization)]
public class MediaElementServiceModeAndroidTests : MediaElementAndroidTestBase
{
	static readonly TimeSpan controllerConnectionTime = TimeSpan.FromSeconds(3);

	[Fact]
	public async Task TwoElementsHaveTheirOwnPlayersAndSources()
	{
		SetupProbeBuilder(isAndroidForegroundServiceEnabled: true);

		await InvokeOnMainThreadAsync(async () =>
		{
			var first = await CreateMediaElementAsync(element => element.Source = MediaSource.FromResource(toneAsset));
			var second = await CreateMediaElementAsync(element => element.Source = MediaSource.FromResource(shortToneAsset));
			using var firstRecorder = new MediaElementRecorder(first);
			using var secondRecorder = new MediaElementRecorder(second);
			var firstHandler = await ConnectWithServiceAsync(first);
			var secondHandler = await ConnectWithServiceAsync(second);
			await WaitUntilLoadedAsync(first, firstRecorder);
			await WaitUntilLoadedAsync(second, secondRecorder);

			Assert.NotEqual(GetController(first).Handle, GetController(second).Handle);
			Assert.EndsWith(toneAsset, GetServicePlayer(firstHandler.Manager!)!.CurrentMediaItem!.MediaId);
			Assert.EndsWith(shortToneAsset, GetServicePlayer(secondHandler.Manager!)!.CurrentMediaItem!.MediaId);
			Assert.Equal(toneDuration, first.Duration);
			Assert.Equal(shortToneDuration, second.Duration);
		});
	}

	// Only one of the two plays at a time: the players ask for the audio focus, so starting the second one would pause the first.
	[Fact]
	public async Task PausingOneElementLeavesTheOtherUnchanged()
	{
		SetupProbeBuilder(isAndroidForegroundServiceEnabled: true);

		await InvokeOnMainThreadAsync(async () =>
		{
			var first = await CreateMediaElementAsync(element => element.Source = MediaSource.FromResource(toneAsset));
			var second = await CreateMediaElementAsync(element => element.Source = MediaSource.FromResource(toneAsset));
			using var firstRecorder = new MediaElementRecorder(first);
			using var secondRecorder = new MediaElementRecorder(second);
			await ConnectWithServiceAsync(first);
			await ConnectWithServiceAsync(second);
			await WaitUntilLoadedAsync(first, firstRecorder);
			await WaitUntilLoadedAsync(second, secondRecorder);
			await WaitForStateAsync(second, MediaElementState.Paused);
			first.Play();
			await WaitForStateAsync(first, MediaElementState.Playing);

			first.Pause();

			await WaitForStateAsync(first, MediaElementState.Paused);
			Assert.Equal(MediaElementState.Paused, second.CurrentState);
			Assert.Equal(TimeSpan.Zero, second.Position);
			second.Play();
			await WaitForStateAsync(second, MediaElementState.Playing);
			Assert.Equal(MediaElementState.Paused, first.CurrentState);
		});
	}

	[Fact]
	public async Task LaterSourcesReuseTheSameControllerAndRegistration()
	{
		SetupProbeBuilder(isAndroidForegroundServiceEnabled: true);
		var mediaElement = await CreateMediaElementAsync(element => element.Source = MediaSource.FromResource(toneAsset));
		using var recorder = new MediaElementRecorder(mediaElement);

		await HostAsync(mediaElement, async handler =>
		{
			await WaitForRegistrationAsync(handler.Manager!);
			await WaitUntilLoadedAsync(mediaElement, recorder);
			var controllerHandle = GetController(mediaElement).Handle;
			var key = Assert.Single(GetRegisteredPlayerIds());

			mediaElement.Source = MediaSource.FromResource(shortToneAsset);
			await WaitUntilAsync(() => recorder.OpenedCount is 2, "The second source was never opened.");
			mediaElement.Source = MediaSource.FromResource(toneAsset);
			await WaitUntilAsync(() => recorder.OpenedCount is 3, "The third source was never opened.");

			Assert.Equal(controllerHandle, GetController(mediaElement).Handle);
			Assert.Equal(key, Assert.Single(GetRegisteredPlayerIds()));
			Assert.EndsWith(toneAsset, GetServicePlayer(handler.Manager!)!.CurrentMediaItem!.MediaId);
		});
	}

	[Fact]
	public async Task SourceSetBeforeTheControllerConnectsIsAppliedOnceConnected()
	{
		SetupProbeBuilder(isAndroidForegroundServiceEnabled: true);
		var mediaElement = await CreateMediaElementAsync(element => element.Source = MediaSource.FromResource(toneAsset));
		using var recorder = new MediaElementRecorder(mediaElement);

		await HostAsync(mediaElement, async handler =>
		{
			await WaitUntilLoadedAsync(mediaElement, recorder);

			Assert.Equal(toneDuration, mediaElement.Duration);
			Assert.EndsWith(toneAsset, GetServicePlayer(handler.Manager!)!.CurrentMediaItem!.MediaId);
			Assert.Equal(1, recorder.OpenedCount);
		});
	}

	// The handler connects before MAUI maps the properties: a source that is set by then must not be loaded by the connection and by the
	// mapping, which would download its artwork twice and raise Opening twice.
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task ConnectingLoadsTheSourceOnce(bool isServiceEnabled)
	{
		var artwork = await ReadAssetAsync(artworkAsset);
		using var server = new LoopbackHttpServer(new Dictionary<string, (byte[], string)> { ["/art.png"] = (artwork, "image/png") });
		SetupProbeBuilder(isAndroidForegroundServiceEnabled: isServiceEnabled);
		var mediaElement = await CreateMediaElementAsync(element =>
		{
			element.MetadataArtworkUrl = server.GetUri("/art.png").ToString();
			element.Source = MediaSource.FromResource(toneAsset);
		});
		using var recorder = new MediaElementRecorder(mediaElement);

		await HostAsync(mediaElement, async _ =>
		{
			await WaitUntilLoadedAsync(mediaElement, recorder);

			Assert.Equal(1, server.Requests.Count(request => request.StartsWith("HEAD /art.png ", StringComparison.Ordinal)));
			Assert.Equal(1, server.Requests.Count(request => request.StartsWith("GET /art.png ", StringComparison.Ordinal)));
			Assert.Equal(1, recorder.Transitions.Count(transition => transition.Current is MediaElementState.Opening));
			Assert.Equal(1, recorder.OpenedCount);
		});
	}

	// The pending source is applied through the same update as every other source, so that an exception of a StateChanged handler of the app
	// is reported through MediaFailed instead of failing the connection.
	[Fact]
	public async Task ExceptionOfAnOpeningHandlerDuringThePendingSourceUpdateIsReportedAndTheSourceIsApplied()
	{
		SetupProbeBuilder(isAndroidForegroundServiceEnabled: true);
		var handlerException = new InvalidOperationException("The StateChanged handler of the app failed.");
		var mediaElement = await CreateMediaElementAsync(element =>
		{
			element.Source = MediaSource.FromResource(toneAsset);
			element.StateChanged += (_, e) =>
			{
				if (e.NewState is MediaElementState.Opening)
				{
					throw handlerException;
				}
			};
		});
		using var recorder = new MediaElementRecorder(mediaElement);

		await HostAsync(mediaElement, async handler =>
		{
			await WaitUntilAsync(() => recorder.OpenedCount > 0 && mediaElement.Duration > TimeSpan.Zero && recorder.FailureMessages.Count > 0,
				"The source was never applied, or the exception of the handler was never reported.");

			Assert.Contains(recorder.FailureMessages, message => message.Contains(handlerException.Message, StringComparison.Ordinal));
			Assert.Equal(1, recorder.OpenedCount);
			Assert.EndsWith(toneAsset, GetServicePlayer(handler.Manager!)!.CurrentMediaItem!.MediaId);
		});
	}

	[Fact]
	public async Task PropertiesSetBeforeTheControllerConnectsAreApplied()
	{
		SetupProbeBuilder(isAndroidForegroundServiceEnabled: true);
		var mediaElement = await CreateMediaElementAsync(element =>
		{
			element.Volume = 0.4;
			element.Speed = 1.5;
			element.ShouldLoopPlayback = true;
		});

		await HostAsync(mediaElement, async handler =>
		{
			await WaitForRegistrationAsync(handler.Manager!);
			var servicePlayer = GetServicePlayer(handler.Manager!)!;

			await WaitUntilAsync(() => Math.Abs(servicePlayer.Volume - 0.4f) < 0.01f && Math.Abs(servicePlayer.PlaybackParameters!.Speed - 1.5f) < 0.01f && servicePlayer.RepeatMode is 1,
				"The volume, the speed or the repeat mode of the MediaElement never reached the player of the service.");
		});
	}

	[Fact]
	public async Task MutedElementIsMutedWhenTheControllerConnects()
	{
		SetupProbeBuilder(isAndroidForegroundServiceEnabled: true);
		var mediaElement = await CreateMediaElementAsync(element =>
		{
			element.Volume = 0.4;
			element.ShouldMute = true;
		});

		await HostAsync(mediaElement, async handler =>
		{
			await WaitForRegistrationAsync(handler.Manager!);
			var servicePlayer = GetServicePlayer(handler.Manager!)!;

			await WaitUntilAsync(() => servicePlayer.Volume is 0f, "The player of the service never got muted.");
		});
	}

	// Setting the sources in one turn of the UI thread: the artwork of every one is held back, so the updates are all running when the last
	// source is set, and the last one must win.
	[Fact]
	public async Task RapidSourceChangesEndWithTheLastSource()
	{
		var artwork = await ReadAssetAsync(artworkAsset);
		var lastSourcePath = await CopyAssetToCacheAsync(toneAsset);
		using var server = new LoopbackHttpServer(new Dictionary<string, (byte[], string)> { ["/art.png"] = (artwork, "image/png") });
		var heldArtworkResponse = server.HoldResponses("/art.png");
		SetupProbeBuilder(isAndroidForegroundServiceEnabled: true);

		await InvokeOnMainThreadAsync(async () =>
		{
			var mediaElement = await CreateMediaElementAsync(element => element.MetadataArtworkUrl = server.GetUri("/art.png").ToString());
			using var recorder = new MediaElementRecorder(mediaElement);
			var handler = await ConnectWithServiceAsync(mediaElement);

			mediaElement.Source = MediaSource.FromResource(toneAsset);
			mediaElement.Source = MediaSource.FromResource(shortToneAsset);
			mediaElement.Source = MediaSource.FromFile(lastSourcePath);
			await WaitUntilAsync(() => server.HasRequest("HEAD", "/art.png"), "The artwork was never requested.");
			heldArtworkResponse.SetResult();
			await WaitUntilLoadedAsync(mediaElement, recorder);

			Assert.Equal(lastSourcePath, GetServicePlayer(handler.Manager!)!.CurrentMediaItem!.MediaId);
			Assert.Equal(1, recorder.OpenedCount);
		});
	}

	// The release that a controller sends after its handler was disconnected must be the end of its own registration only: while another
	// controller keeps the service alive, a registration that was left behind would stay until the service is destroyed.
	[Fact]
	public async Task DisconnectWhileTheControllerConnectsLeavesNoRegistration()
	{
		SetupProbeBuilder(isAndroidForegroundServiceEnabled: true);

		await InvokeOnMainThreadAsync(async () =>
		{
			var anchor = await CreateMediaElementAsync();
			var anchorHandler = await ConnectWithServiceAsync(anchor);
			var anchorKey = anchorHandler.Manager!.AndroidPlayerConnectionKey;
			var mediaElement = await CreateMediaElementAsync();
			using var recorder = new MediaElementRecorder(mediaElement);

			var handler = CreateHandler<AndroidMediaElementProbeHandler>(mediaElement);
			((IElementHandler)handler).DisconnectHandler();
			mediaElement.Handler = null;
			await Task.Delay(controllerConnectionTime);

			Assert.Equal(anchorKey, Assert.Single(GetRegisteredPlayerIds()));
			Assert.Empty(recorder.FailureMessages);
		});
		await WaitForMediaControlsServiceToStopAsync();
	}

	// One turn of the UI thread: the first handler is disconnected before its controller connects, and a second one is connected for the same
	// element. The late controller of the first handler releases its registration after the second handler has joined the service, and must
	// not take the player of the second one with it.
	[Fact]
	public async Task ReconnectingTheSameElementRightAwayGetsAWorkingPlayer()
	{
		SetupProbeBuilder(isAndroidForegroundServiceEnabled: true);

		await InvokeOnMainThreadAsync(async () =>
		{
			var mediaElement = await CreateMediaElementAsync(element => element.Source = MediaSource.FromResource(toneAsset));
			DisposeWhenTheTestEnds(mediaElement);
			using var recorder = new MediaElementRecorder(mediaElement);

			var firstHandler = CreateHandler<AndroidMediaElementProbeHandler>(mediaElement);
			((IElementHandler)firstHandler).DisconnectHandler();
			mediaElement.Handler = null;
			var secondHandler = CreateHandler<AndroidMediaElementProbeHandler>(mediaElement);
			await WaitForRegistrationAsync(secondHandler.Manager!);
			await WaitUntilLoadedAsync(mediaElement, recorder);
			await Task.Delay(controllerConnectionTime);

			Assert.Equal(secondHandler.Manager!.AndroidPlayerConnectionKey, Assert.Single(GetRegisteredPlayerIds()));
			Assert.False(GetServicePlayer(secondHandler.Manager!)!.IsReleased);
			mediaElement.Play();
			await WaitForStateAsync(mediaElement, MediaElementState.Playing);
		});
	}

	[Fact]
	public async Task StreamSourcePlaysWithTheService()
	{
		SetupProbeBuilder(isAndroidForegroundServiceEnabled: true);
		var tone = await ReadAssetAsync(toneAsset);
		var mediaElement = await CreateMediaElementAsync(element => element.Source = MediaSource.FromStream(new MemoryStream(tone)));
		using var recorder = new MediaElementRecorder(mediaElement);

		await HostAsync(mediaElement, async _ =>
		{
			await WaitUntilLoadedAsync(mediaElement, recorder);

			mediaElement.Play();

			await WaitForStateAsync(mediaElement, MediaElementState.Playing);
			Assert.Equal(toneDuration, mediaElement.Duration);
		});
	}

	[Fact]
	public async Task UriSourceWithHeadersSendsThemWithTheService()
	{
		using var server = new LoopbackHttpServer(new Dictionary<string, (byte[], string)> { ["/tone.wav"] = (await ReadAssetAsync(toneAsset), "audio/wav") });
		SetupProbeBuilder(isAndroidForegroundServiceEnabled: true);
		var headers = new Dictionary<string, string> { ["X-Device-Test"] = "42", ["Authorization"] = "Bearer abc" };
		var mediaElement = await CreateMediaElementAsync(element => element.Source = MediaSource.FromUri(server.GetUri("/tone.wav"), headers));
		using var recorder = new MediaElementRecorder(mediaElement);

		await HostAsync(mediaElement, async _ =>
		{
			await WaitUntilLoadedAsync(mediaElement, recorder);

			mediaElement.Play();

			await WaitForStateAsync(mediaElement, MediaElementState.Playing);
			Assert.Equal(toneDuration, mediaElement.Duration);
			Assert.Equal("42", server.GetRequestHeader("GET", "/tone.wav", "X-Device-Test"));
			Assert.Equal("Bearer abc", server.GetRequestHeader("GET", "/tone.wav", "Authorization"));
		});
	}

	[Fact]
	public async Task ElementWithTheServiceDoesNotLeak()
	{
		SetupProbeBuilder(isAndroidForegroundServiceEnabled: true);

		var references = await ConnectAndDisconnectAsync();

		await WaitForMediaControlsServiceToStopAsync();
		Assert.Empty(GetRegisteredPlayerIds());
		await AssertCollectedAsync(references);
	}

	[Fact]
	public async Task ElementDisconnectedWhileAnotherKeepsTheServiceRunningDoesNotLeak()
	{
		SetupProbeBuilder(isAndroidForegroundServiceEnabled: true);
		var remainingElement = await CreateMediaElementAsync();
		var remainingHandler = await ConnectWithServiceAsync(remainingElement);
		var remainingKey = remainingHandler.Manager!.AndroidPlayerConnectionKey;

		var references = await ConnectAndDisconnectAsync();

		await WaitUntilAsync(() => GetRegisteredPlayerIds().Count == 1, "The registration of the disconnected element was never released.");
		Assert.NotNull(FindMediaControlsService());
		Assert.Equal(remainingKey, Assert.Single(GetRegisteredPlayerIds()));
		await AssertCollectedAsync(references);
	}

	// A separate method, so that no local of the test keeps the objects alive while the garbage collector runs
	[MethodImpl(MethodImplOptions.NoInlining)]
	async Task<(string Name, WeakReference Reference)[]> ConnectAndDisconnectAsync()
	{
		var mediaElement = await CreateMediaElementAsync();
		var handler = await InvokeOnMainThreadAsync(() => CreateHandler<AndroidMediaElementProbeHandler>(mediaElement));
		await WaitForRegistrationAsync(handler.Manager!);
		await WaitForControllerAsync(mediaElement);

		return await InvokeOnMainThreadAsync(() =>
		{
			var platformView = GetMauiMediaElement(mediaElement);
			(string, WeakReference)[] references =
			[
				("MediaElement", new(mediaElement)),
				("handler", new(handler)),
				("MauiMediaElement", new(platformView)),
				("MediaManager", new(handler.Manager)),
				("MediaController", new(GetPlayerView(platformView).Player)),
			];

			((IElementHandler)handler).DisconnectHandler();
			mediaElement.Handler = null;
			return references;
		});
	}

	[Fact]
	public async Task StateTransitionsWithTheServiceMatchTheLocalPlayer()
	{
		SetupProbeBuilder(isAndroidForegroundServiceEnabled: true);
		var mediaElement = await CreateMediaElementAsync(element =>
		{
			element.ShouldAutoPlay = true;
			element.Source = MediaSource.FromResource(shortToneAsset);
		});
		using var recorder = new MediaElementRecorder(mediaElement);

		await HostAsync(mediaElement, async _ =>
		{
			await WaitUntilAsync(() => recorder.EndedCount > 0, "The short tone never played to its end.");
			await WaitForStateAsync(mediaElement, MediaElementState.Stopped);

			var states = recorder.States.ToList();
			var opening = states.IndexOf(MediaElementState.Opening);
			var playing = states.IndexOf(MediaElementState.Playing);
			var stopped = states.LastIndexOf(MediaElementState.Stopped);
			Assert.True(opening >= 0 && playing > opening && stopped > playing, $"Expected Opening, Playing, Stopped: {string.Join(" > ", states)}");
			Assert.Equal(1, recorder.EndedCount);
		});
	}

	// With the service the state arrives from the controller, which never calls OnPlayerStateChanged: OpeningASourceRaisesNoSpuriousStoppedOrPausedState
	// of the local player must hold for it too.
	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task OpeningASourceRaisesNoSpuriousStoppedOrPausedStateWithTheService(bool shouldAutoPlay)
	{
		SetupProbeBuilder(isAndroidForegroundServiceEnabled: true);
		var mediaElement = await CreateMediaElementAsync(element =>
		{
			element.ShouldAutoPlay = shouldAutoPlay;
			element.ShouldLoopPlayback = true;
			element.Source = MediaSource.FromResource(toneAsset);
		});
		using var recorder = new MediaElementRecorder(mediaElement);

		await HostAsync(mediaElement, async _ =>
		{
			await WaitUntilLoadedAsync(mediaElement, recorder);
			await WaitForStateAsync(mediaElement, shouldAutoPlay ? MediaElementState.Playing : MediaElementState.Paused);

			var states = recorder.States.ToList();
			var opening = states.IndexOf(MediaElementState.Opening);
			var buffering = states.IndexOf(MediaElementState.Buffering);
			Assert.True(opening >= 0 && buffering > opening, $"Expected Opening, then Buffering: {string.Join(" > ", states)}");
			Assert.DoesNotContain(states[(opening + 1)..buffering], state => state is MediaElementState.Stopped or MediaElementState.Paused);
		});
	}

	[Fact]
	public async Task SeekWithTheServiceCompletes()
	{
		SetupProbeBuilder(isAndroidForegroundServiceEnabled: true);
		var mediaElement = await CreateMediaElementAsync(element => element.Source = MediaSource.FromResource(toneAsset));
		using var recorder = new MediaElementRecorder(mediaElement);

		await HostAsync(mediaElement, async handler =>
		{
			await WaitUntilLoadedAsync(mediaElement, recorder);

			await mediaElement.SeekTo(TimeSpan.FromSeconds(1)).WaitAsync(TimeSpan.FromSeconds(5));

			Assert.Equal(1, recorder.SeekCompletedCount);
			Assert.InRange(GetServicePlayer(handler.Manager!)!.CurrentPosition, 900, 1100);
		});
	}

	[Fact]
	public async Task PlayerErrorWithTheServiceRaisesMediaFailed()
	{
		SetupProbeBuilder(isAndroidForegroundServiceEnabled: true);
		var mediaElement = await CreateMediaElementAsync(element => element.Source = MediaSource.FromResource("exoplayer/does_not_exist.wav"));
		using var recorder = new MediaElementRecorder(mediaElement);

		await HostAsync(mediaElement, async _ =>
		{
			await WaitUntilAsync(() => recorder.FailureMessages.Count > 0, "MediaFailed was never raised.");

			Assert.Equal(MediaElementState.Failed, mediaElement.CurrentState);
			Assert.Contains("Error code:", recorder.FailureMessages[0]);
		});
	}

	[Fact]
	public async Task VideoSizeIsReportedWithTheService()
	{
		SkipWhereTheVideoDecoderIsUnreliable();
		SetupProbeBuilder(isAndroidForegroundServiceEnabled: true);
		var path = await CopyAssetToCacheAsync(videoAsset);
		var mediaElement = await CreateMediaElementAsync(element => element.Source = MediaSource.FromFile(path));
		using var recorder = new MediaElementRecorder(mediaElement);

		await HostAsync(mediaElement, async _ =>
		{
			await WaitUntilVideoSizeIsKnownAsync(mediaElement, recorder);

			Assert.Equal((64, 48), (mediaElement.MediaWidth, mediaElement.MediaHeight));
		});
	}
}
