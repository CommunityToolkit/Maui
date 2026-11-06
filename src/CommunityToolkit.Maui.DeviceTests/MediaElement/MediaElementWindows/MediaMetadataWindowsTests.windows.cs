using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Primitives;
using CommunityToolkit.Maui.DeviceTests.Runners;
using Microsoft.Extensions.Logging;
using CommunityToolkit.Maui.Views;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Media;
using Windows.Media.Playback;

namespace CommunityToolkit.Maui.DeviceTests;

// The artwork is served by a loopback server because RandomAccessStreamReference.CreateFromUri accepts http, https, ms-appx and ms-appdata only.
[Category(TestCategory.MediaMetadataWindows)]
[Collection(TestCollections.DisableParallelization)]
public class MediaMetadataWindowsTests : WinMediaTestBase
{
	const string title = "A Title";
	const string artist = "An Artist";

	static Task UpdateMetadataAsync(WinMediaManagerFixture fixture) =>
		InvokeOnMainThreadAsync(async () => await (ValueTask)WinMediaReflection.Invoke(fixture.Manager, "UpdateMetadata")!);

	static Metadata CreateMetadata(WinMediaManagerFixture fixture) =>
		new(fixture.Player!.SystemMediaTransportControls, fixture.View, TestDispatcher.Current);

	static Uri ArtworkUri(WinMediaHttpServer server) => server.UriFor(WinMediaAssets.ArtworkServerPath);

	static async Task AssertThumbnailIsTheArtworkAsync(WinMediaManagerFixture fixture, WinMediaHttpServer server)
	{
		byte[] bytes;
		try
		{
			bytes = await ReadThumbnailAsync(fixture);
		}
		catch (Exception ex) when (WinMediaAssets.IsTransientThumbnailReadFailure(ex))
		{
			// The stream that the thumbnail opens is served by another process, which fails now and then (RPC_E_SERVER_DIED_DNE on a Windows
			// Server 2025 runner, after the same read had worked in WinMediaProbe). The overlay would open the thumbnail again; so does the test,
			// once.
			try
			{
				bytes = await ReadThumbnailAsync(fixture);
			}
			catch (Exception again) when (WinMediaAssets.IsOutOfProcessServerGone(again))
			{
				// That process can also stay gone for the rest of the app run: the read failed with RPC_E_SERVER_DIED_DNE in the unpackaged leg of
				// CI run 38089989724, and both reads did in the packaged leg of CI run 38096555799 (never on a Windows 11 machine). What fails then is
				// the machine's media overlay, not the thumbnail the toolkit published, so the test skips, visibly. A second FileNotFoundException
				// still fails the test: a thumbnail that points to the wrong place would read as one.
				throw new SkipException($"The system process that serves this machine's media overlay thumbnails went away (HRESULT 0x{again.HResult:X8}), so the thumbnail the toolkit published cannot be read back.");
			}
		}

		Assert.Equal(WinMediaAssets.ReadArtworkBytes(), bytes);
		Assert.Contains(server.Requests, request => request.Method is "GET" && request.Path == "/" + WinMediaAssets.ArtworkServerPath);

		static async Task<byte[]> ReadThumbnailAsync(WinMediaManagerFixture fixture)
		{
			var thumbnail = await InvokeOnMainThreadAsync(() => fixture.Player!.SystemMediaTransportControls.DisplayUpdater.Thumbnail);
			Assert.NotNull(thumbnail);
			return await WinMediaAssets.ReadAllBytesAsync(thumbnail).WaitAsync(TimeSpan.FromSeconds(15));
		}
	}

	[Fact]
	public async Task SetMetadataPublishesMusicWithBothTextsToTheDisplayUpdater()
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();
		await RequireDisplayUpdaterAsync();
		var fixture = await CreateManagerAsync(view =>
		{
			view.MetadataTitle = title;
			view.MetadataArtist = artist;
		});

		await InvokeOnMainThreadAsync(() =>
		{
			CreateMetadata(fixture).SetMetadata(fixture.View);

			var updater = fixture.Player!.SystemMediaTransportControls.DisplayUpdater;
			Assert.Equal(MediaPlaybackType.Music, updater.Type);
			// Which property holds which text is the next test. Both sides are sorted the same way, so the comparison holds for either assignment
			// and for no other pair of texts.
			Assert.Equal(new[] { title, artist }.Order(StringComparer.Ordinal), new[] { updater.MusicProperties.Title, updater.MusicProperties.Artist }.Order(StringComparer.Ordinal));
		});
	}

	[Fact]
	public async Task SetMetadataMapsTheTitleToTheTitleAndTheArtistToTheArtist()
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();
		await RequireDisplayUpdaterAsync();
		var fixture = await CreateManagerAsync(view =>
		{
			view.MetadataTitle = title;
			view.MetadataArtist = artist;
		});

		await InvokeOnMainThreadAsync(() =>
		{
			CreateMetadata(fixture).SetMetadata(fixture.View);

			var music = fixture.Player!.SystemMediaTransportControls.DisplayUpdater.MusicProperties;
			Assert.Equal(title, music.Title);
			Assert.Equal(artist, music.Artist);
		});
	}

	[Fact]
	public async Task SetMetadataWithAnAbsoluteArtworkUrlSetsTheThumbnail()
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();
		await RequireDisplayUpdaterAsync();
		await using var server = WinMediaAssets.StartArtworkServer();
		var fixture = await CreateManagerAsync(view => view.MetadataArtworkUrl = ArtworkUri(server).AbsoluteUri);

		await InvokeOnMainThreadAsync(() => CreateMetadata(fixture).SetMetadata(fixture.View));

		await AssertThumbnailIsTheArtworkAsync(fixture, server);
	}

	[Fact]
	public async Task SetMetadataWithoutArtworkLeavesTheThumbnailUntouched()
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();
		await RequireDisplayUpdaterAsync();
		var fixture = await CreateManagerAsync();

		await InvokeOnMainThreadAsync(() =>
		{
			CreateMetadata(fixture).SetMetadata(fixture.View);

			Assert.Null(fixture.Player!.SystemMediaTransportControls.DisplayUpdater.Thumbnail);
		});
	}

	// RandomAccessStreamReference.CreateFromUri accepts http, https, ms-appx and ms-appdata only, and the Android MediaElement takes a file path as
	// the artwork. SetMetadata runs inside the async void MediaOpened handler, where an exception ends the app.
	[Theory]
	[InlineData(@"C:\Users\Public\Pictures\cover.png")]
	[InlineData("file:///C:/Users/Public/Pictures/cover.png")]
	[InlineData("ftp://localhost/cover.png")]
	public async Task SetMetadataWithAnArtworkUrlOfAnUnsupportedSchemeLeavesTheThumbnailUntouched(string artworkUrl)
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();
		await RequireDisplayUpdaterAsync();
		var fixture = await CreateManagerAsync(view =>
		{
			view.MetadataTitle = title;
			view.MetadataArtworkUrl = artworkUrl;
		});

		await InvokeOnMainThreadAsync(() =>
		{
			var exception = Record.Exception(() => CreateMetadata(fixture).SetMetadata(fixture.View));

			Assert.Null(exception);
			var updater = fixture.Player!.SystemMediaTransportControls.DisplayUpdater;
			Assert.Null(updater.Thumbnail);
			Assert.Equal(title, updater.MusicProperties.Title);
		});
	}

	// The constructor only stores the MediaElement (it dereferences the controls, never the element), so a Metadata built with a null element
	// reaches the element guards of SetMetadata and of the media-key handler. The manager always passes its own element; this is the only way in.
	[Fact]
	public async Task MetadataWithoutAMediaElementPublishesNothingAndIgnoresTheMediaKeys()
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();
		await RequireDisplayUpdaterAsync();
		var fixture = await CreateManagerAsync(view =>
		{
			view.MetadataTitle = title;
			view.MetadataArtist = artist;
		});

		await InvokeOnMainThreadAsync(() =>
		{
			var controls = fixture.Player!.SystemMediaTransportControls;
			Assert.Equal(MediaPlaybackType.Unknown, controls.DisplayUpdater.Type);
			var metadata = new Metadata(controls, null!, TestDispatcher.Current);

			// The element passed to SetMetadata has texts to publish; the guard on the stored element must stop it first.
			metadata.SetMetadata(fixture.View);

			Assert.Equal(MediaPlaybackType.Unknown, controls.DisplayUpdater.Type);
			Assert.Null(controls.DisplayUpdater.Thumbnail);
			Assert.False(controls.IsEnabled);

			// The handler returns before it reads its arguments: without the guard, args.Button would throw NullReferenceException.
			Assert.Null(Record.Exception(() => WinMediaReflection.Invoke(metadata, "OnSystemMediaControlsButtonPressed", controls, null)));
		});
	}

	[Theory]
	[InlineData("images/cover.png")]
	[InlineData("not a uri at all")]
	public async Task UpdateMetadataWithAnUnusableArtworkUrlIsIgnoredWithoutThrowing(string artworkUrl)
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();
		var fixture = await CreateManagerAsync(view => view.MetadataArtworkUrl = artworkUrl);

		var exception = await Record.ExceptionAsync(() => UpdateMetadataAsync(fixture));

		Assert.Null(exception);
	}

	[Fact]
	public async Task UpdateMetadataPublishesToTheDisplayAndSetsThePosterOnTheUiThread()
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();
		await RequireDisplayUpdaterAsync();
		await using var server = WinMediaAssets.StartArtworkServer();
		var fixture = await CreateManagerAsync(view =>
		{
			view.MetadataTitle = title;
			view.MetadataArtist = artist;
			view.MetadataArtworkUrl = ArtworkUri(server).AbsoluteUri;
		}, recordDispatches: true);

		await UpdateMetadataAsync(fixture);

		Assert.Equal(0, fixture.Dispatcher!.DispatchCount);
		var metadata = WinMediaReflection.GetField<object?>(fixture.Manager, "metadata");
		Assert.NotNull(metadata);
		Assert.Equal(MediaPlaybackType.Music, fixture.Player!.SystemMediaTransportControls.DisplayUpdater.Type);
		await AssertThumbnailIsTheArtworkAsync(fixture, server);
		await InvokeOnMainThreadAsync(() =>
		{
			var poster = Assert.IsType<BitmapImage>(fixture.Element!.PosterSource);
			Assert.Equal(ArtworkUri(server).AbsoluteUri, poster.UriSource.AbsoluteUri, ignoreCase: true);
		});

		await UpdateMetadataAsync(fixture);
		Assert.Same(metadata, WinMediaReflection.GetField<object?>(fixture.Manager, "metadata"));
	}

	[Fact]
	public async Task UpdateMetadataThatMustBeDispatchedQueuesThePosterUpdate()
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();

		await using var server = WinMediaAssets.StartArtworkServer();

		var fixture = await CreateManagerAsync(view => view.MetadataArtworkUrl = ArtworkUri(server).AbsoluteUri, dispatchRequired: true);

		await UpdateMetadataAsync(fixture);

		Assert.Equal(1, fixture.Dispatcher!.DispatchCount);
		await InvokeOnMainThreadAsync(() =>
		{
			var poster = Assert.IsType<BitmapImage>(fixture.Element!.PosterSource);
			Assert.Equal(ArtworkUri(server).AbsoluteUri, poster.UriSource.AbsoluteUri, ignoreCase: true);
		});
	}

	[Fact]
	public async Task UpdateMetadataFromABackgroundThreadDispatchesThePosterUpdate()
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();
		await using var server = WinMediaAssets.StartArtworkServer();
		var fixture = await CreateManagerAsync(view => view.MetadataArtworkUrl = ArtworkUri(server).AbsoluteUri);

		// BitmapImage is a XAML object, so only the dispatched arm can create it.
		await Task.Run(async () => await (ValueTask)WinMediaReflection.Invoke(fixture.Manager, "UpdateMetadata")!);

		await WaitUntilOnUiAsync(() => fixture.Element!.PosterSource is BitmapImage { UriSource: not null }, "The poster was never set from the background-thread UpdateMetadata.");
		await InvokeOnMainThreadAsync(() => Assert.Equal(ArtworkUri(server).AbsoluteUri, ((BitmapImage)fixture.Element!.PosterSource).UriSource.AbsoluteUri, ignoreCase: true));
	}

	[Fact]
	public async Task UpdateMetadataWithoutArtworkLeavesThePoster()
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();
		await RequireDisplayUpdaterAsync();
		var fixture = await CreateManagerAsync(view =>
		{
			view.MetadataTitle = title;
			view.MetadataArtist = artist;
		});

		await UpdateMetadataAsync(fixture);

		await InvokeOnMainThreadAsync(() => Assert.Null(fixture.Element!.PosterSource));
		Assert.Equal(MediaPlaybackType.Music, fixture.Player!.SystemMediaTransportControls.DisplayUpdater.Type);
	}

	[Fact]
	public async Task UpdateMetadataWithoutPlatformViewDoesNothing()
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();
		var fixture = await CreateManagerAsync(view => view.MetadataTitle = title, createPlatformView: false);

		await UpdateMetadataAsync(fixture);

		Assert.Null(WinMediaReflection.GetField<object?>(fixture.Manager, "metadata"));
	}

	// The native MediaOpened arrives on a worker thread, so its handler can still be running when the MediaElement's handler disconnects and
	// disposes the manager, and reach UpdateMetadata after Dispose released the Metadata helper. A new helper would subscribe to the system media
	// controls of the disposed player again, and they would keep the MediaElement reachable.
	[Fact]
	public async Task UpdateMetadataAfterDisposeCreatesNoNewMetadataHelper()
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();
		var fixture = await CreateManagerAsync(view => view.MetadataTitle = title);
		await UpdateMetadataAsync(fixture);
		Assert.NotNull(WinMediaReflection.GetField<object?>(fixture.Manager, "metadata"));

		await InvokeOnMainThreadAsync(() => fixture.Manager.Dispose());
		Assert.Null(WinMediaReflection.GetField<object?>(fixture.Manager, "metadata"));

		await UpdateMetadataAsync(fixture);

		Assert.Null(WinMediaReflection.GetField<object?>(fixture.Manager, "metadata"));
	}

	[Fact]
	public async Task OpeningMediaPublishesTheMetadata()
	{
		SetupWindowsBuilder();
		await RequireDecoderAsync();
		await RequireDisplayUpdaterAsync();
		var fixture = await CreateManagerAsync(view =>
		{
			view.MetadataTitle = title;
			view.MetadataArtist = artist;
		});

		await OpenAsync(fixture.View, MediaSource.FromFile(WinMediaAssets.VideoPath), fixture.Manager);

		// OnMediaElementMediaOpened awaits UpdateMetadata after raising MediaOpened.
		await WaitUntilAsync(() => WinMediaReflection.GetField<object?>(fixture.Manager, "metadata") is not null, "MediaOpened never created the Metadata helper.");
		Assert.Equal(MediaPlaybackType.Music, fixture.Player!.SystemMediaTransportControls.DisplayUpdater.Type);
	}

	// What users get: a playing MediaElement with metadata, in the configuration the toolkit leaves its player in (nothing reconfigured here).
	// The system media overlay, the media keys and the lock screen find an app through its system media session.
	[Fact]
	public async Task OpenedMediaWithMetadataAppearsAsASystemMediaSession()
	{
		SetupWindowsBuilder();
		await RequireDecoderAsync();
		await RequireMediaSessionAsync();
		var token = $"test-{Guid.NewGuid():N}";
		var view = NewMediaElement(view =>
		{
			view.ShouldLoopPlayback = true;
			view.MetadataTitle = token;
			view.MetadataArtist = token;
		});
		await ConnectAsync(view);
		await OpenAsync(view, MediaSource.FromFile(WinMediaAssets.VideoPath));

		await InvokeOnMainThreadAsync(view.Play);

		var session = await WinMediaMediaSessions.FindAsync(token, timeoutMilliseconds: 10000);

		// RequireMediaSessionAsync saw the session of a plain MediaPlayer on this machine: a missing session is the toolkit's.
		Assert.True(session is not null, "This machine shows the system media session of a plain MediaPlayer, but none for the playing MediaElement whose metadata the toolkit published.");
		Assert.Equal(token, (await WinMediaMediaSessions.Bounded(session.TryGetMediaPropertiesAsync())).Title);
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task OpeningMediaSwitchesTheSystemMediaControlsOnOnlyForMediaWithMetadata(bool hasMetadata)
	{
		SetupWindowsBuilder();
		await RequireDecoderAsync();
		await RequireDisplayUpdaterAsync();
		var fixture = await CreateManagerAsync(view =>
		{
			if (hasMetadata)
			{
				view.MetadataTitle = title;
				view.MetadataArtist = artist;
			}
		});

		await OpenAsync(fixture.View, MediaSource.FromFile(WinMediaAssets.VideoPath), fixture.Manager);

		// SetMetadata runs on the thread that raised MediaOpened, and sets the display type after the state of the controls.
		var controls = fixture.Player!.SystemMediaTransportControls;
		await WaitUntilAsync(() => controls.DisplayUpdater.Type is MediaPlaybackType.Music, "MediaOpened never published the metadata.");
		Assert.Equal(hasMetadata, controls.IsEnabled);
	}

	[Theory]
	[InlineData(nameof(MediaElement.MetadataTitle))]
	[InlineData(nameof(MediaElement.MetadataArtist))]
	[InlineData(nameof(MediaElement.MetadataArtworkUrl))]
	public async Task SetMetadataWithMetadataSwitchesTheSystemMediaControlsOn(string metadata)
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();
		await using var server = WinMediaAssets.StartArtworkServer();
		var fixture = await CreateManagerAsync(view =>
		{
			switch (metadata)
			{
				case nameof(MediaElement.MetadataTitle):
					view.MetadataTitle = title;
					break;
				case nameof(MediaElement.MetadataArtist):
					view.MetadataArtist = artist;
					break;
				case nameof(MediaElement.MetadataArtworkUrl):
					view.MetadataArtworkUrl = ArtworkUri(server).AbsoluteUri;
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(metadata), metadata, null);
			}
		});

		await InvokeOnMainThreadAsync(() =>
		{
			var controls = fixture.Player!.SystemMediaTransportControls;
			Assert.False(controls.IsEnabled);

			CreateMetadata(fixture).SetMetadata(fixture.View);

			Assert.True(controls.IsEnabled);
		});
	}

	[Theory]
	[InlineData("")]
	[InlineData("   ")]
	public async Task SetMetadataWithoutMetadataSwitchesTheSystemMediaControlsOff(string blank)
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();
		var fixture = await CreateManagerAsync(view =>
		{
			view.MetadataTitle = blank;
			view.MetadataArtist = blank;
			view.MetadataArtworkUrl = blank;
		});

		await InvokeOnMainThreadAsync(() =>
		{
			var controls = fixture.Player!.SystemMediaTransportControls;
			controls.IsEnabled = true;

			CreateMetadata(fixture).SetMetadata(fixture.View);

			Assert.False(controls.IsEnabled);
		});
	}

	[Theory]
	[InlineData("no source")]
	[InlineData("a source that cannot be applied")]
	public async Task RemovingTheMediaSwitchesTheSystemMediaControlsOff(string replacement)
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();
		var fixture = await CreateManagerAsync(view => view.MetadataTitle = title);
		await UpdateMetadataAsync(fixture);
		var controls = fixture.Player!.SystemMediaTransportControls;
		Assert.True(controls.IsEnabled);

		switch (replacement)
		{
			case "no source":
				await SetSourceAsync(fixture.View, null, fixture.Manager);
				break;
			case "a source that cannot be applied":
				var missing = Path.Combine(Path.GetDirectoryName(WinMediaAssets.VideoPath)!, "missing.mp4");
				await SetSourceAsync(fixture.View, MediaSource.FromFile(missing), fixture.Manager);
				Assert.Equal(MediaElementState.Failed, fixture.View.CurrentState);
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(replacement), replacement, null);
		}

		Assert.False(controls.IsEnabled);
	}

	// The hardware media keys (and the system media overlay) raise SystemMediaTransportControls.ButtonPressed on a background thread. The app cannot
	// raise it itself, but the system media sessions can: asking this app's own session to pause, play or skip is what a key press does. The
	// session is found by a title that the test publishes itself, so no other app is touched; the tests skip, visibly, on a machine that shows
	// no session for the app.
	[Fact]
	public async Task SystemPlayAndPauseCommandsRaiseThePlayAndPauseRequestsOnTheUiThread()
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();
		var token = $"test-{Guid.NewGuid():N}";
		var fixture = await CreateManagerAsync(view =>
		{
			view.MetadataTitle = token;
			view.MetadataArtist = token;
		});
		var session = await PublishSystemMediaSessionAsync(fixture, token);
		var uiThread = await InvokeOnMainThreadAsync(() => Environment.CurrentManagedThreadId);
		var played = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
		var paused = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
		fixture.View.PlayRequested += (_, _) => played.TrySetResult(Environment.CurrentManagedThreadId);
		fixture.View.PauseRequested += (_, _) => paused.TrySetResult(Environment.CurrentManagedThreadId);

		Assert.True(await WinMediaMediaSessions.Bounded(session.TryPauseAsync()), "The system refused the pause command.");
		Assert.Equal(uiThread, await paused.Task.WaitAsync(TimeSpan.FromSeconds(15)));
		Assert.False(played.Task.IsCompleted);

		// What an app does when it paused: tell the system, so that Play is a meaningful request.
		fixture.Player!.SystemMediaTransportControls.PlaybackStatus = MediaPlaybackStatus.Paused;
		Assert.True(await WinMediaMediaSessions.Bounded(session.TryPlayAsync()), "The system refused the play command.");
		Assert.Equal(uiThread, await played.Task.WaitAsync(TimeSpan.FromSeconds(15)));
	}

	[Fact]
	public async Task SystemPlayAndPauseCommandsRunInlineWhenNoDispatchIsRequired()
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();
		var token = $"test-{Guid.NewGuid():N}";

		var fixture = await CreateManagerAsync(view =>
		{
			view.MetadataTitle = token;
			view.MetadataArtist = token;
		}, dispatchRequired: false);
		var session = await PublishSystemMediaSessionAsync(fixture, token);
		var uiThread = await InvokeOnMainThreadAsync(() => Environment.CurrentManagedThreadId);
		var played = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
		var paused = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
		fixture.View.PlayRequested += (_, _) => played.TrySetResult(Environment.CurrentManagedThreadId);
		fixture.View.PauseRequested += (_, _) => paused.TrySetResult(Environment.CurrentManagedThreadId);

		Assert.True(await WinMediaMediaSessions.Bounded(session.TryPauseAsync()), "The system refused the pause command.");
		Assert.NotEqual(uiThread, await paused.Task.WaitAsync(TimeSpan.FromSeconds(15)));
		fixture.Player!.SystemMediaTransportControls.PlaybackStatus = MediaPlaybackStatus.Paused;
		Assert.True(await WinMediaMediaSessions.Bounded(session.TryPlayAsync()), "The system refused the play command.");
		Assert.NotEqual(uiThread, await played.Task.WaitAsync(TimeSpan.FromSeconds(15)));
		Assert.Equal(0, fixture.Dispatcher!.DispatchCount);
	}

	[Fact]
	public async Task SystemNextCommandRaisesNeitherPlayNorPauseRequest()
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();
		var token = $"test-{Guid.NewGuid():N}";
		var fixture = await CreateManagerAsync(view =>
		{
			view.MetadataTitle = token;
			view.MetadataArtist = token;
		});
		var session = await PublishSystemMediaSessionAsync(fixture, token);
		var playRequests = 0;
		var pauseRequests = 0;
		var played = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		fixture.View.PlayRequested += (_, _) =>
		{
			Interlocked.Increment(ref playRequests);
			played.TrySetResult();
		};
		fixture.View.PauseRequested += (_, _) => Interlocked.Increment(ref pauseRequests);

		// The play command that follows is the fence: ButtonPressed events are raised in order, so once the play request arrived, a request caused by
		// Next would have arrived before it.
		Assert.True(await WinMediaMediaSessions.Bounded(session.TrySkipNextAsync()), "The system refused the next-track command.");
		fixture.Player!.SystemMediaTransportControls.PlaybackStatus = MediaPlaybackStatus.Paused;
		Assert.True(await WinMediaMediaSessions.Bounded(session.TryPlayAsync()), "The system refused the play command.");
		await played.Task.WaitAsync(TimeSpan.FromSeconds(15));
		await FlushDispatcherAsync();

		Assert.Equal(1, playRequests);
		Assert.Equal(0, pauseRequests);
	}

	// The same handlers with the real keys: nothing in the app can press them, so the tests below wait for a person. They run only when the app
	// is started without arguments (visual runner). The player's command manager is switched off: otherwise it would act on the keys itself and
	// the tests would pass without the toolkit's handler.
	async Task<(MediaElement View, MediaPlayer Player)> StartPlaybackForButtonTestAsync()
	{
		SetupWindowsBuilder();
		await RequireDecoderAsync();
		var view = NewMediaElement(view =>
		{
			view.ShouldLoopPlayback = true;
			view.MetadataTitle = title;
			view.MetadataArtist = artist;
		});
		var handler = await ConnectAsync(view);
		await OpenAsync(view, MediaSource.FromFile(WinMediaAssets.VideoPath));
		var player = await GetPlayerAsync(handler);
		await InvokeOnMainThreadAsync(() =>
		{
			player.CommandManager.IsEnabled = false;
			var controls = player.SystemMediaTransportControls;
			controls.IsEnabled = true;
			controls.IsPlayEnabled = true;
			controls.IsPauseEnabled = true;
			controls.IsNextEnabled = true;
			controls.IsStopEnabled = true;
		});
		return (view, player);
	}

	static async Task PlayAndPublishAsync(MediaElement view, MediaPlayer player)
	{
		await InvokeOnMainThreadAsync(() =>
		{
			view.Play();
			player.SystemMediaTransportControls.PlaybackStatus = MediaPlaybackStatus.Playing;
		});
		await WaitUntilAsync(() => player.PlaybackSession.PlaybackState is MediaPlaybackState.Playing, "The media never started playing.");
	}

	[Fact]
	[Category(TestCategory.RequiresUserInteraction)]
	public async Task PressingThePauseMediaKeyPausesTheMediaElement()
	{
		var (view, player) = await StartPlaybackForButtonTestAsync();
		await PlayAndPublishAsync(view, player);
		var pauseRequested = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		view.PauseRequested += (_, _) => pauseRequested.TrySetResult();
		TestRunnerLogger?.LogWarning("MANUAL STEP: press the Pause (or Play/Pause) media key or use the system media overlay within 60 seconds.");

		await pauseRequested.Task.WaitAsync(TimeSpan.FromSeconds(60));

		await WaitUntilAsync(() => view.CurrentState is MediaElementState.Paused, "ButtonPressed(Pause) requested a pause, but the MediaElement did not pause.");
	}

	[Fact]
	[Category(TestCategory.RequiresUserInteraction)]
	public async Task PressingThePlayMediaKeyPlaysTheMediaElement()
	{
		var (view, player) = await StartPlaybackForButtonTestAsync();
		await PlayAndPublishAsync(view, player);
		await InvokeOnMainThreadAsync(() =>
		{
			view.Pause();
			player.SystemMediaTransportControls.PlaybackStatus = MediaPlaybackStatus.Paused;
		});
		await WaitUntilAsync(() => player.PlaybackSession.PlaybackState is MediaPlaybackState.Paused, "The media never paused.");
		var playRequested = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		view.PlayRequested += (_, _) => playRequested.TrySetResult();
		TestRunnerLogger?.LogWarning("MANUAL STEP: press the Play (or Play/Pause) media key or use the system media overlay within 60 seconds.");

		await playRequested.Task.WaitAsync(TimeSpan.FromSeconds(60));

		await WaitUntilAsync(() => view.CurrentState is MediaElementState.Playing, "ButtonPressed(Play) requested playback, but the MediaElement did not play.");
	}

	[Fact]
	[Category(TestCategory.RequiresUserInteraction)]
	public async Task PressingAnyOtherMediaKeyLeavesThePlaybackAlone()
	{
		var (view, player) = await StartPlaybackForButtonTestAsync();
		await PlayAndPublishAsync(view, player);
		var requests = 0;
		view.PlayRequested += (_, _) => Interlocked.Increment(ref requests);
		view.PauseRequested += (_, _) => Interlocked.Increment(ref requests);

		// Registered after the toolkit's handler (the Metadata helper subscribed when the media opened): when it runs, the toolkit's ran.
		var buttonPressed = new TaskCompletionSource<SystemMediaTransportControlsButton>(TaskCreationOptions.RunContinuationsAsynchronously);
		player.SystemMediaTransportControls.ButtonPressed += (_, args) => buttonPressed.TrySetResult(args.Button);
		TestRunnerLogger?.LogWarning("MANUAL STEP: press the Next track or Stop media key within 60 seconds (not Play, Pause or Play/Pause).");

		var button = await buttonPressed.Task.WaitAsync(TimeSpan.FromSeconds(60));
		await FlushDispatcherAsync();

		Assert.True(button is not (SystemMediaTransportControlsButton.Play or SystemMediaTransportControlsButton.Pause), $"A {button} press arrived; press another key.");
		Assert.Equal(0, requests);
		Assert.Equal(MediaElementState.Playing, view.CurrentState);
	}
}