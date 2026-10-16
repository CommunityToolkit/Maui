using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Media.Playback;
using Windows.Storage;
using WinMediaSource = Windows.Media.Core.MediaSource;

namespace CommunityToolkit.Maui.DeviceTests;

/// <summary>A MediaSource subclass the Windows manager does not know.</summary>
public sealed class WinMediaUnknownMediaSource : MediaSource;

// Views/MediaManager.windows.cs, PlatformUpdateSource: every source type, blank/missing inputs and the reset that precedes them. Which arm
// ran is read from what the platform did: the MediaOpened / MediaFailed events of the player, the request a loopback server saw, and the
// Source of the player for the arms that set it through MediaPlayerElement.Source (the Set*Source methods of the player are not required
// to be reflected by MediaPlayer.Source, so no test relies on that). Tests that need a real open skip visibly where Media Foundation cannot
// decode the clip.
[Category(TestCategory.MediaManagerSourceWindows)]
[Collection(TestCollections.DisableParallelization)]
public class MediaManagerSourceWindowsTests : WinMediaTestBase
{
	// The shared [InlineData] takes non-null objects, so a null path is spelled with this marker.
	const string nullPath = "<null>";

	static string? ToPath(string marker) => marker == nullPath ? null : marker;

	static Task UpdateSourceAsync(WinMediaManagerFixture fixture) => InvokeOnMainThreadAsync(async () => await fixture.Manager.UpdateSource());

	[Fact]
	public async Task UriSourceWithoutHeadersHandsTheAbsoluteUriToThePlayer()
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();
		await using var server = new WinMediaHttpServer().Start();
		var fixture = await CreateManagerAsync(view => view.Source = new UriMediaSource { Uri = server.UriFor("clip.mp4?token=abc") });

		await UpdateSourceAsync(fixture);

		// The platform opens the URI itself; the server answers 404, which is all it takes to see which URI was requested.
		await WaitUntilAsync(() => server.Requests.Any(request => request.Path == "/clip.mp4?token=abc"), "The native player never requested the URI of the source.");
		Assert.Null(WinMediaReflection.GetField<object?>(fixture.Manager, "headerHttpClient"));
		Assert.Null(WinMediaReflection.GetField<object?>(fixture.Manager, "adaptiveMediaSource"));
	}

	[Fact]
	public async Task UriSourceWithoutHeadersOpensAFileUri()
	{
		SetupWindowsBuilder();
		await RequireVideoRenderingAsync();

		var fixture = await CreateManagerAsync();

		await OpenAsync(fixture.View, new UriMediaSource { Uri = WinMediaAssets.VideoUri }, fixture.Manager);

		await WaitUntilAsync(() => fixture.View.MediaWidth == WinMediaAssets.VideoWidth, "The opened URI never reported its natural width.");
	}

	[Fact]
	public async Task UriSourceWithoutUriSetsNoNativeSource()
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();

		var fixture = await CreateManagerAsync(view => view.Source = new UriMediaSource());

		await UpdateSourceAsync(fixture);

		Assert.Null(fixture.Player!.Source);
	}

	[Fact]
	public async Task FileSourceOpensTheFile()
	{
		SetupWindowsBuilder();
		await RequireVideoRenderingAsync();

		var fixture = await CreateManagerAsync();

		await OpenAsync(fixture.View, MediaSource.FromFile(WinMediaAssets.VideoPath), fixture.Manager);

		await WaitUntilAsync(() => fixture.View.MediaWidth == WinMediaAssets.VideoWidth, "The opened file never reported its natural width.");
		Assert.True(fixture.Player!.PlaybackSession.NaturalDuration > TimeSpan.Zero);
	}

	[Theory]
	[InlineData(nullPath)]
	[InlineData("")]
	[InlineData("   ")]
	public async Task FileSourceWithBlankPathSetsNoNativeSource(string path)
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();

		var fixture = await CreateManagerAsync(view => view.Source = new FileMediaSource { Path = ToPath(path) });

		await UpdateSourceAsync(fixture);

		Assert.Null(fixture.Player!.Source);
	}

	// A source that cannot be applied is reported like a source that cannot be played: MediaFailed with the message of the platform's exception,
	// here the FileNotFoundException of StorageFile.GetFileFromPathAsync (the oracle opens the same path itself). UpdateSource does not throw it.
	[Fact]
	public async Task FileSourceWithMissingFileRaisesMediaFailedWithTheMessageOfTheFileNotFoundException()
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();

		var missing = Path.Combine(Path.GetDirectoryName(WinMediaAssets.VideoPath)!, "missing.mp4");
		var expected = Assert.IsType<FileNotFoundException>(await Record.ExceptionAsync(() => StorageFile.GetFileFromPathAsync(missing).AsTask()), exactMatch: false);
		var fixture = await CreateManagerAsync(view => view.Source = new FileMediaSource { Path = missing });
		var failures = RecordFailures(fixture.View);

		await UpdateSourceAsync(fixture);

		Assert.Equal(expected.Message, Assert.Single(failures).ErrorMessage);
		Assert.Equal(MediaElementState.Failed, fixture.View.CurrentState);
		Assert.Null(fixture.Player!.Source);
	}

	// A source that cannot be applied replaces the previous media, as a source that cannot be played does: the update reports MediaFailed, and the
	// previous media, which was playing, has left the player. The previous media is a stream here: a stream source is set through
	// MediaPlayerElement.Source, where the media of the player can be seen. A kind of MediaSource the platform does not know cannot be applied either.
	[Theory]
	[InlineData("a missing file")]
	[InlineData("a kind of source the platform does not know")]
	public async Task SourceThatCannotBeAppliedRaisesMediaFailedAndTakesThePlayingMediaOutOfThePlayer(string source)
	{
		SetupWindowsBuilder();

		var fixture = await OpenAndPlayStreamAsync();
		var failures = RecordFailures(fixture.View);

		await SetSourceAsync(fixture.View, source switch
		{
			"a missing file" => MediaSource.FromFile(Path.Combine(Path.GetDirectoryName(WinMediaAssets.VideoPath)!, "missing.mp4")),
			"a kind of source the platform does not know" => new WinMediaUnknownMediaSource(),
			_ => throw new ArgumentOutOfRangeException(nameof(source), source, null),
		}, fixture.Manager);

		Assert.Single(failures);
		Assert.Equal(MediaElementState.Failed, fixture.View.CurrentState);
		await InvokeOnMainThreadAsync(() => Assert.Null(fixture.Element!.Source));
		Assert.Null(fixture.Player!.Source);
		Assert.Equal(0, fixture.View.MediaWidth);
		Assert.Equal(0, fixture.View.MediaHeight);
		await WaitUntilAsync(() => fixture.Player.PlaybackSession.PlaybackState is not MediaPlaybackState.Playing && fixture.Player.PlaybackSession.NaturalDuration == TimeSpan.Zero,
			"The previous media is still in the player after MediaFailed.");
	}

	// The player reports the closing of the media that a failed source update took out of it on its own thread, so these states can arrive after
	// the source mapping reported Failed: they must not overwrite it, and the next source update reports the native states again. The manager's
	// handler gets the real session of a player without media.
	[Fact]
	public async Task NativeStatesAfterAFailedSourceUpdateDoNotOverwriteFailedUntilTheNextSourceUpdate()
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();

		var missing = Path.Combine(Path.GetDirectoryName(WinMediaAssets.VideoPath)!, "missing.mp4");
		var fixture = await CreateManagerAsync(view => view.Source = new FileMediaSource { Path = missing });
		await UpdateSourceAsync(fixture);
		Assert.Equal(MediaElementState.Failed, fixture.View.CurrentState);

		await InvokeOnMainThreadAsync(async () =>
		{
			var session = fixture.Player!.PlaybackSession;
			Assert.Equal(MediaPlaybackState.None, session.PlaybackState);

			WinMediaReflection.Invoke(fixture.Manager, "OnPlaybackSessionPlaybackStateChanged", session, null);
			Assert.Equal(MediaElementState.Failed, fixture.View.CurrentState);

			// A source without a path is enough: the update starts, and gives the player no media (it reports None itself).
			fixture.View.Source = new FileMediaSource { Path = string.Empty };
			await fixture.Manager.UpdateSource();

			// Another state first, so that only the handler can bring back None, the native state.
			((IMediaElement)fixture.View).CurrentStateChanged(MediaElementState.Opening);
			WinMediaReflection.Invoke(fixture.Manager, "OnPlaybackSessionPlaybackStateChanged", session, null);
			Assert.Equal(MediaElementState.None, fixture.View.CurrentState);
		});
	}

	[Fact]
	public async Task ResourceSourceHandsThePlayerAFileInThePackageFolder()
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();
		var fixture = await CreateManagerAsync(view => view.Source = new ResourceMediaSource { Path = "mediaplayer/missing_resource.mp4" });
		var failed = new TaskCompletionSource<MediaFailedEventArgs>(TaskCreationOptions.RunContinuationsAsynchronously);
		fixture.View.MediaFailed += (_, e) => failed.TrySetResult(e);

		await UpdateSourceAsync(fixture);

		// The resource does not exist, so the platform's attempt to open the path it was given ends in MediaFailed: the player got a source.
		var failure = await failed.Task.WaitAsync(TimeSpan.FromMilliseconds(decodeTimeoutMilliseconds));
		Assert.False(string.IsNullOrWhiteSpace(failure.ErrorMessage));
	}

	[Theory]
	[InlineData("mediaplayer/video.mp4")]
	[InlineData("mediaplayer\\video.mp4")]
	[InlineData("mediaplayer/sub\\video.mp4")]
	public void PackagePathResolutionCombinesTheNormalisedPathWithThePackageFolder(string path)
	{
		SetupWindowsBuilder();

		var resolved = (string)WinMediaReflection.InvokeStatic(typeof(CommunityToolkit.Maui.Core.Views.MediaManager), "GetFullAppPackageFilePath", path)!;

		// Both separators become the platform separator, and the result lives in the folder AppPackageService reports.
		Assert.Equal(Path.Combine([WinMediaPackaging.ExpectedPackageFilePath, .. path.Split('/', '\\')]), resolved, ignoreCase: true);
	}

	[Theory]
	[InlineData(nullPath)]
	[InlineData("")]
	[InlineData("   ")]
	public async Task ResourceSourceWithBlankPathLogsAndSetsNoNativeSource(string path)
	{
		var loggerFactory = new WinMediaLoggerFactory();
		SetupWindowsBuilder(loggerFactory);
		await RequireMediaPlayerAsync();

		var fixture = await CreateManagerAsync(view => view.Source = new ResourceMediaSource { Path = ToPath(path) });

		await UpdateSourceAsync(fixture);

		Assert.Null(fixture.Player!.Source);
		Assert.Contains(loggerFactory.Entries, entry => entry.Level is LogLevel.Information && entry.Message == "ResourceMediaSource Path is null or empty");
	}

	[Fact]
	public void PackagePathResolutionRejectsANullFileName()
	{
		SetupWindowsBuilder();

		// Private static helper (ResourceMediaSource never passes null: the blank check comes first), reached by reflection.
		var exception = Assert.Throws<ArgumentNullException>(() =>
			WinMediaReflection.InvokeStatic(typeof(CommunityToolkit.Maui.Core.Views.MediaManager), "GetFullAppPackageFilePath", new object?[] { null }));

		Assert.Equal("filename", exception.ParamName);
	}

	[Theory]
	[InlineData("mediaplayer/video.mp4")]
	[InlineData("mediaplayer\\video.mp4")]
	public async Task ResourceSourceOpensThePackagedAsset(string path)
	{
		SetupWindowsBuilder();
		await RequireDecoderAsync();

		var fixture = await CreateManagerAsync();

		await OpenAsync(fixture.View, MediaSource.FromResource(path), fixture.Manager);

		await WaitUntilAsync(() => fixture.View.Duration > TimeSpan.Zero, "The opened resource never reported its duration.");
		Assert.True(fixture.Player!.PlaybackSession.NaturalDuration > TimeSpan.Zero);
	}

	[Fact]
	public async Task StreamSourceOpensTheStream()
	{
		SetupWindowsBuilder();
		await RequireVideoRenderingAsync();

		var fixture = await CreateManagerAsync();
		using var stream = new MemoryStream(WinMediaAssets.ReadVideoBytes());

		await OpenAsync(fixture.View, MediaSource.FromStream(stream), fixture.Manager);

		// The stream is wrapped as a random-access stream and handed over with the MIME type sniffed from its first bytes (video/mp4).
		var native = Assert.IsType<WinMediaSource>(fixture.Player!.Source);
		Assert.Null(native.Uri);
		await WaitUntilAsync(() => fixture.View.MediaHeight == WinMediaAssets.VideoHeight, "The opened stream never reported its natural height.");
	}

	[Fact]
	public async Task StreamSourceThatCannotSeekRaisesMediaFailedWithTheMessageOfTheNotSupportedException()
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();

		// A network or pipe stream: AsRandomAccessStream cannot wrap a stream that cannot seek (CsWinRT WindowsRuntimeStreamExtensions).
		var expected = Assert.IsType<NotSupportedException>(Record.Exception(() => new WinMediaNonSeekableStream(WinMediaAssets.ReadVideoBytes()).AsRandomAccessStream()));
		var fixture = await CreateManagerAsync(view => view.Source = MediaSource.FromStream(new WinMediaNonSeekableStream(WinMediaAssets.ReadVideoBytes())));
		var failures = RecordFailures(fixture.View);

		await UpdateSourceAsync(fixture);

		Assert.Equal(expected.Message, Assert.Single(failures).ErrorMessage);
		Assert.Equal(MediaElementState.Failed, fixture.View.CurrentState);
		Assert.Null(fixture.Player!.Source);
	}

	[Fact]
	public async Task StreamSourceWithoutStreamSetsNoNativeSource()
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();

		var fixture = await CreateManagerAsync(view => view.Source = new StreamMediaSource());

		await UpdateSourceAsync(fixture);

		Assert.Null(fixture.Player!.Source);
	}

	// A kind of MediaSource the platform does not know cannot be applied: MediaFailed, as on Android, with the message of the NotSupportedException.
	[Fact]
	public async Task UnknownSourceTypeRaisesMediaFailedAndSetsNoNativeSource()
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();

		var fixture = await CreateManagerAsync(view => view.Source = new WinMediaUnknownMediaSource());
		var failures = RecordFailures(fixture.View);

		await UpdateSourceAsync(fixture);

		Assert.Equal($"{typeof(WinMediaUnknownMediaSource).FullName} is not yet supported for {nameof(MediaElement.Source)}", Assert.Single(failures).ErrorMessage);
		Assert.Equal(MediaElementState.Failed, fixture.View.CurrentState);
		Assert.Null(fixture.Player!.Source);
	}

	// A source that cannot be applied is also logged, as an error with the exception that says why (as the Android manager logs it), so that the
	// stack trace of a missing file or a refused header is not lost.
	[Fact]
	public async Task SourceThatCannotBeAppliedIsLoggedAsAnErrorWithItsException()
	{
		var loggerFactory = new WinMediaLoggerFactory();
		SetupWindowsBuilder(loggerFactory);
		await RequireMediaPlayerAsync();

		var missing = Path.Combine(Path.GetDirectoryName(WinMediaAssets.VideoPath)!, "missing.mp4");
		var fixture = await CreateManagerAsync(view => view.Source = new FileMediaSource { Path = missing });
		var failures = RecordFailures(fixture.View);

		await UpdateSourceAsync(fixture);

		var failure = Assert.Single(failures);
		var entry = Assert.Single(loggerFactory.Entries, logged => logged.Level is LogLevel.Error);
		Assert.Equal(failure.ErrorMessage, entry.Message);
		Assert.IsType<FileNotFoundException>(entry.Exception, exactMatch: false);
	}

	// Play() asks Windows to keep the screen on while ShouldKeepScreenOn is set. A source update that takes the media out of the player ends that
	// request, as Pause() and Stop() do: without media, nothing plays.
	[Theory]
	[InlineData("no source")]
	[InlineData("a source without data")]
	[InlineData("a source that cannot be applied")]
	public async Task SourceUpdateThatTakesTheMediaOutOfThePlayerReleasesTheDisplayRequest(string source)
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();
		await RequireDisplayRequestCountingAsync();

		var fixture = await CreateManagerAsync(view => view.ShouldKeepScreenOn = true);
		await InvokeOnMainThreadAsync(() =>
		{
			fixture.Manager.Play();
			AssertDisplayRequest(fixture.Manager, active: true);
		});

		await SetSourceAsync(fixture.View, source switch
		{
			"no source" => null,
			"a source without data" => new FileMediaSource { Path = string.Empty },
			"a source that cannot be applied" => MediaSource.FromFile(Path.Combine(Path.GetDirectoryName(WinMediaAssets.VideoPath)!, "missing.mp4")),
			_ => throw new ArgumentOutOfRangeException(nameof(source), source, null),
		}, fixture.Manager);

		await InvokeOnMainThreadAsync(() => AssertDisplayRequest(fixture.Manager, active: false));
	}

	// A source without data loads nothing, like a null Source: the media that was playing stops and leaves the player, and the state is None.
	[Theory]
	[InlineData("a Uri source without a Uri")]
	[InlineData("a File source with a blank path")]
	[InlineData("a Resource source with a blank path")]
	[InlineData("a Stream source without a stream")]
	public async Task SourceWithoutDataAfterAPlayingSourceEmptiesThePlayerAndReportsNone(string source)
	{
		SetupWindowsBuilder();

		var fixture = await OpenAndPlayStreamAsync();
		var failures = RecordFailures(fixture.View);

		await SetSourceAsync(fixture.View, SourceWithoutData(source), fixture.Manager);

		await InvokeOnMainThreadAsync(() => Assert.Null(fixture.Element!.Source));
		Assert.Null(fixture.Player!.Source);
		Assert.Equal(0, fixture.View.MediaWidth);
		Assert.Equal(0, fixture.View.MediaHeight);
		await WaitUntilAsync(() => fixture.Player.PlaybackSession.PlaybackState is not MediaPlaybackState.Playing && fixture.Player.PlaybackSession.NaturalDuration == TimeSpan.Zero,
			"The previous media is still in the player after a source without data.");

		// The player reports the closing of the media on its own thread, so the state is read once the media has left the player.
		await WaitUntilAsync(() => fixture.View.CurrentState is MediaElementState.None, "The state did not become None.");
		Assert.Empty(failures);
	}

	// The update raises the events of the MediaElement (here PropertyChanged of the Duration it resets) before it gives the player the new source.
	// An exception of the app's handler propagates from UpdateSource: it is not a failure of the source (no MediaFailed, no Failed state), and the
	// update stops there, so the media that was playing stays in the player and its states are still reported.
	[Fact]
	public async Task ExceptionOfAnAppEventHandlerDuringTheSourceUpdatePropagatesAndLeavesThePlayingMedia()
	{
		SetupWindowsBuilder();

		var fixture = await OpenAndPlayStreamAsync();
		var nativeSource = await InvokeOnMainThreadAsync(() => fixture.Element!.Source);
		var failures = RecordFailures(fixture.View);
		var appException = new InvalidOperationException("Thrown by an app handler.");
		fixture.View.PropertyChanged += (_, e) =>
		{
			if (e.PropertyName == nameof(MediaElement.Duration))
			{
				throw appException;
			}
		};

		var thrown = await Record.ExceptionAsync(() => SetSourceAsync(fixture.View, MediaSource.FromFile(WinMediaAssets.VideoPath), fixture.Manager));

		Assert.Same(appException, thrown);
		Assert.Empty(failures);
		Assert.NotEqual(MediaElementState.Failed, fixture.View.CurrentState);
		await InvokeOnMainThreadAsync(() => Assert.Same(nativeSource, fixture.Element!.Source));
		Assert.Equal(MediaPlaybackState.Playing, fixture.Player!.PlaybackSession.PlaybackState);

		// No Failed latch either: the native state is reported.
		await InvokeOnMainThreadAsync(() =>
		{
			((IMediaElement)fixture.View).CurrentStateChanged(MediaElementState.None);
			WinMediaReflection.Invoke(fixture.Manager, "OnPlaybackSessionPlaybackStateChanged", fixture.Player.PlaybackSession, null);
			Assert.Equal(MediaElementState.Playing, fixture.View.CurrentState);
		});
	}

	// A handler of the events that the update raises before it gives the player its source (here PropertyChanged of the Duration it resets) can set
	// another Source. That source's update replaces this one, which then gives the player nothing: the stream of the replaced source never reaches
	// MediaPlayerElement.Source, where the media of a stream source shows. The newer source is a Uri, which the player gets through SetUriSource.
	[Fact]
	public async Task SourceSetByAHandlerOfTheResetsReplacesTheSourceOfTheUpdate()
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();
		using var stream = new MemoryStream(WinMediaAssets.ReadVideoBytes());
		var fixture = await CreateManagerAsync(view =>
		{
			((IMediaElement)view).Duration = TimeSpan.FromSeconds(5);
			view.Source = MediaSource.FromStream(stream);
		});
		var opens = new WinMediaOpenWatcher(fixture.Player!);
		Task? newerUpdate = null;
		fixture.View.PropertyChanged += (_, e) =>
		{
			if (e.PropertyName == nameof(MediaElement.Duration) && newerUpdate is null)
			{
				fixture.View.Source = new UriMediaSource { Uri = new Uri("http://127.0.0.1:1/never.mp4") };
				newerUpdate = fixture.Manager.UpdateSource().AsTask();
			}
		};

		await UpdateSourceAsync(fixture);
		Assert.NotNull(newerUpdate);
		await newerUpdate.WaitAsync(TimeSpan.FromMilliseconds(decodeTimeoutMilliseconds));

		// The player tries to open the newer source (nothing listens on port 1); that open must end before the test does.
		await opens.UntilOpensEndedAsync(1, refusedConnectionTimeoutMilliseconds);
		await InvokeOnMainThreadAsync(() => Assert.Null(fixture.Element!.Source));
	}

	static MediaSource SourceWithoutData(string source) => source switch
	{
		"a Uri source without a Uri" => new UriMediaSource(),
		"a File source with a blank path" => new FileMediaSource { Path = " " },
		"a Resource source with a blank path" => new ResourceMediaSource { Path = string.Empty },
		"a Stream source without a stream" => new StreamMediaSource(),
		_ => throw new ArgumentOutOfRangeException(nameof(source), source, null),
	};

	// Opens the clip as a looping stream source (set through MediaPlayerElement.Source, where the media of the player can be seen) and plays it.
	async Task<WinMediaManagerFixture> OpenAndPlayStreamAsync()
	{
		await RequireVideoRenderingAsync();

		// Registered before the cleanup of the manager, so that the stream is disposed after the player that reads it was released (the cleanups
		// run last registered first)
		var stream = new MemoryStream(WinMediaAssets.ReadVideoBytes());
		RegisterCleanup(stream.Dispose);
		var fixture = await CreateManagerAsync(view => view.ShouldLoopPlayback = true);
		await InvokeOnMainThreadAsync(() => fixture.Manager.UpdateShouldLoopPlayback());
		await OpenAsync(fixture.View, MediaSource.FromStream(stream), fixture.Manager);
		await WaitUntilAsync(() => fixture.View.MediaWidth == WinMediaAssets.VideoWidth && fixture.View.MediaHeight == WinMediaAssets.VideoHeight, "The natural size was never reported.");
		await InvokeOnMainThreadAsync(() => Assert.NotNull(fixture.Element!.Source));

		await InvokeOnMainThreadAsync(() => fixture.Manager.Play());
		await WaitUntilAsync(() => fixture.Player!.PlaybackSession.PlaybackState is MediaPlaybackState.Playing, "The previous media never played.");
		return fixture;
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task NewSourceResetsPositionDurationAndAutoPlay(bool shouldAutoPlay)
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();

		var fixture = await CreateManagerAsync(view =>
		{
			view.ShouldAutoPlay = shouldAutoPlay;
			((IMediaElement)view).Duration = TimeSpan.FromSeconds(5);
			((IMediaElement)view).Position = TimeSpan.FromSeconds(3);
			view.Source = new FileMediaSource { Path = string.Empty };
		});
		await InvokeOnMainThreadAsync(() =>
		{
			fixture.Element!.Width = 160;
			fixture.Element.Height = 120;
		});

		// MediaPlayerElement passes AutoPlay on to its MediaPlayer only while it is in a live visual tree, so the element is on screen (as in an
		// app). The opposite value is set first, through the element, so that each row needs the manager's update to reach the native player.
		await HostAsync(fixture.Element!, async _ =>
		{
			fixture.Element!.AutoPlay = !shouldAutoPlay;
			Assert.Equal(!shouldAutoPlay, fixture.Player!.AutoPlay);

			await fixture.Manager.UpdateSource();

			Assert.Equal(shouldAutoPlay, fixture.Player.AutoPlay);
		});

		Assert.Equal(TimeSpan.Zero, fixture.View.Duration);
		Assert.Equal(TimeSpan.Zero, fixture.View.Position);
	}

	[Fact]
	public async Task NullSourceClearsTheNativeSourceAndResetsSizeAndState()
	{
		SetupWindowsBuilder();
		await RequireVideoRenderingAsync();

		var fixture = await CreateManagerAsync();
		using var stream = new MemoryStream(WinMediaAssets.ReadVideoBytes());
		await OpenAsync(fixture.View, MediaSource.FromStream(stream), fixture.Manager);
		await WaitUntilAsync(() => fixture.View.MediaWidth == WinMediaAssets.VideoWidth && fixture.View.MediaHeight == WinMediaAssets.VideoHeight, "The natural size was never reported.");
		Assert.NotNull(fixture.Player!.Source);

		await SetSourceAsync(fixture.View, null, fixture.Manager);

		// A stream source is set through MediaPlayerElement.Source, so clearing it is a property change that the element forwards to its player.
		Assert.Null(fixture.Player.Source);
		await WaitUntilAsync(() => fixture.View.MediaWidth == 0 && fixture.View.MediaHeight == 0, "The size was not reset when the source was cleared.");
		await WaitUntilAsync(() => fixture.View.CurrentState is MediaElementState.None, "The state did not become None when the source was cleared.");
	}

	[Fact]
	public async Task NullSourceReleasesTheMediaThatWasOpenedFromAFile()
	{
		SetupWindowsBuilder();
		await RequireDecoderAsync();

		var fixture = await CreateManagerAsync();
		await OpenAsync(fixture.View, MediaSource.FromFile(WinMediaAssets.VideoPath), fixture.Manager);
		await WaitUntilAsync(() => fixture.Player!.PlaybackSession.NaturalDuration > TimeSpan.Zero, "The opened file never reported its duration.");

		await SetSourceAsync(fixture.View, null, fixture.Manager);

		// What the player reports once the media is gone. A file source is handed to the player with SetFileSource, never through
		// MediaPlayerElement.Source, whose Source is then still null: assigning null to it again is no property change and reaches no player.
		await AssertHelpers.Wait(() => fixture.Player!.PlaybackSession.NaturalDuration == TimeSpan.Zero, timeout: 5000, interval: 50);

		Assert.Equal(TimeSpan.Zero, fixture.Player!.PlaybackSession.NaturalDuration);
	}

	[Fact]
	public async Task EverySourceChangeResetsThePoster()
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();

		var fixture = await CreateManagerAsync(view => view.Source = new FileMediaSource { Path = string.Empty });
		BitmapImage? posterBefore = null;
		await InvokeOnMainThreadAsync(() =>
		{
			posterBefore = new BitmapImage(WinMediaAssets.ArtworkUri);
			fixture.Element!.PosterSource = posterBefore;
		});

		await UpdateSourceAsync(fixture);

		await InvokeOnMainThreadAsync(() =>
		{
			var poster = Assert.IsType<BitmapImage>(fixture.Element!.PosterSource);
			Assert.NotSame(posterBefore, poster);
			Assert.Null(poster.UriSource);
		});
	}
}