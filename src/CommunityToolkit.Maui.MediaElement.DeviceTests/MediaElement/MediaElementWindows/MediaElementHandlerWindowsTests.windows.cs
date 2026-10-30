using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Handlers;
using CommunityToolkit.Maui.Core.Views;
using CommunityToolkit.Maui.Primitives;
using CommunityToolkit.Maui.Views;
using Windows.Media.Playback;
using Stretch = Microsoft.UI.Xaml.Media.Stretch;

namespace CommunityToolkit.Maui.DeviceTests;

[Category(TestCategory.MediaElementHandlerWindows)]
[Collection(TestCollections.DisableParallelization)]
public class MediaElementHandlerWindowsTests : WinMediaTestBase
{
	static MediaSource VideoSource => MediaSource.FromFile(WinMediaAssets.VideoPath);

	[Fact]
	public async Task HandlerWrapsTheNativePlayerInAMauiMediaElement()
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();

		var handler = await ConnectAsync(NewMediaElement());

		await InvokeOnMainThreadAsync(() =>
		{
			Assert.IsType<MauiMediaElement>(handler.PlatformView);
			Assert.NotNull(handler.Manager);
			Assert.NotNull(handler.PlayerElement.MediaPlayer);
			Assert.IsType<CustomTransportControls>(handler.PlayerElement.TransportControls);
		});
	}

	[Fact]
	public async Task PropertyMapperKnowsShouldLoopPlayback()
	{
		SetupWindowsBuilder();

		// Windows is one of the platforms (#if ANDROID || WINDOWS || TIZEN) that map ShouldLoopPlayback.
		await InvokeOnMainThreadAsync(() => Assert.Contains(nameof(IMediaElement.ShouldLoopPlayback), MediaElementHandler.PropertyMapper.GetKeys()));
	}

	[Fact]
	public async Task ConnectingAppliesTheInitialPropertyValuesToTheNativePlayer()
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();

		var handler = await ConnectAsync(NewMediaElement(view =>
		{
			view.Aspect = Aspect.Fill;
			view.ShouldShowPlaybackControls = true;
			view.ShouldLoopPlayback = true;
			view.Volume = 0.25;
		}));

		await InvokeOnMainThreadAsync(() =>
		{
			Assert.Equal(Stretch.Fill, handler.PlayerElement.Stretch);
			Assert.True(handler.PlayerElement.AreTransportControlsEnabled);
			Assert.True(handler.Player.IsLoopingEnabled);
			Assert.Equal(0.25, handler.Player.Volume, 3);
		});
	}

	[Fact]
	public async Task ConnectingAMutedMediaElementMutesTheNativePlayer()
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();

		var handler = await ConnectAsync(NewMediaElement(view => view.ShouldMute = true));
		var player = await GetPlayerAsync(handler);

		await WaitUntilAsync(() => player.IsMuted, "The native player was not muted by the initial mapping.");
	}

	[Theory]
	[InlineData(Aspect.Fill, Stretch.Fill)]
	[InlineData(Aspect.AspectFill, Stretch.UniformToFill)]
	[InlineData(Aspect.AspectFit, Stretch.Uniform)]
	public async Task AspectChangeUpdatesTheNativeStretch(Aspect aspect, Stretch expected)
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();
		var view = NewMediaElement(view => view.Aspect = aspect is Aspect.AspectFit ? Aspect.Fill : Aspect.AspectFit);
		var handler = await ConnectAsync(view);

		await InvokeOnMainThreadAsync(() =>
		{
			view.Aspect = aspect;

			Assert.Equal(expected, handler.PlayerElement.Stretch);
		});
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task ShouldShowPlaybackControlsChangeUpdatesTheNativeFlag(bool show)
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();
		var view = NewMediaElement(view => view.ShouldShowPlaybackControls = !show);
		var handler = await ConnectAsync(view);

		await InvokeOnMainThreadAsync(() =>
		{
			view.ShouldShowPlaybackControls = show;

			Assert.Equal(show, handler.PlayerElement.AreTransportControlsEnabled);
		});
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task ShouldLoopPlaybackChangeUpdatesTheNativeFlag(bool loop)
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();
		var view = NewMediaElement(view => view.ShouldLoopPlayback = !loop);
		var handler = await ConnectAsync(view);

		await InvokeOnMainThreadAsync(() =>
		{
			view.ShouldLoopPlayback = loop;

			Assert.Equal(loop, handler.Player.IsLoopingEnabled);
		});
	}

	[Fact]
	public async Task VolumeChangeUpdatesTheNativeVolume()
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();
		var view = NewMediaElement();
		var handler = await ConnectAsync(view);

		await InvokeOnMainThreadAsync(() =>
		{
			view.Volume = 0.25;
			Assert.Equal(0.25, handler.Player.Volume, 3);

			view.Volume = 1;
			Assert.Equal(1.0, handler.Player.Volume, 3);
		});
	}

	[Fact]
	public async Task ShouldMuteChangeUpdatesTheNativeMute()
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();
		var view = NewMediaElement();
		var handler = await ConnectAsync(view);
		var player = await GetPlayerAsync(handler);

		await InvokeOnMainThreadAsync(() => view.ShouldMute = true);
		await WaitUntilAsync(() => player.IsMuted, "The native player was not muted.");

		await InvokeOnMainThreadAsync(() => view.ShouldMute = false);
		await WaitUntilAsync(() => !player.IsMuted, "The native player was not unmuted.");
	}

	[Fact]
	public async Task ShouldKeepScreenOnChangeReachesTheManager()
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();
		await RequireDisplayRequestCountingAsync();
		var view = NewMediaElement();
		var handler = await ConnectAsync(view);

		await InvokeOnMainThreadAsync(() =>
		{
			var manager = handler.Manager!;
			var displayRequest = WinMediaReflection.GetProperty<Windows.System.Display.DisplayRequest>(manager, "DisplayRequest");
			((IMediaElement)view).CurrentStateChanged(MediaElementState.Paused);

			view.ShouldKeepScreenOn = true;
			Assert.True(WinMediaReflection.GetField<bool>(manager, "displayActiveRequested"));
			Assert.Equal(1, WinMediaDisplayRequests.CountActive(displayRequest));

			view.ShouldKeepScreenOn = false;
			Assert.False(WinMediaReflection.GetField<bool>(manager, "displayActiveRequested"));
			Assert.Equal(0, WinMediaDisplayRequests.CountActive(displayRequest));
		});
	}

	[Fact]
	public async Task VolumeChangedWhileMutedIsAppliedWhenUnmuted()
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();
		var view = NewMediaElement();
		var handler = await ConnectAsync(view);
		var player = await GetPlayerAsync(handler);

		await InvokeOnMainThreadAsync(() => view.ShouldMute = true);
		await WaitUntilAsync(() => player.IsMuted, "The native player was not muted.");
		await InvokeOnMainThreadAsync(() => view.Volume = 0.25);
		await InvokeOnMainThreadAsync(() => view.ShouldMute = false);
		await WaitUntilAsync(() => !player.IsMuted, "The native player was not unmuted.");
		await FlushDispatcherAsync();

		Assert.Equal(0.25, player.Volume, 3);
	}

	[Theory]
	[InlineData("a missing file")]
	[InlineData("a header the platform refuses")]
	[InlineData("a server that rejects HEAD")]
	[InlineData("a stream that cannot seek")]
	[InlineData("a kind of source the platform does not know")]
	public async Task SourceThatCannotBeAppliedRaisesMediaFailed(string source)
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();
		var video = WinMediaAssets.ReadVideoBytes();
		await using var server = StartServerThatRejectsHead(video);
		var view = NewMediaElement();
		await ConnectAsync(view);
		var failed = new TaskCompletionSource<MediaFailedEventArgs>(TaskCreationOptions.RunContinuationsAsynchronously);
		view.MediaFailed += (_, e) => failed.TrySetResult(e);

		await SetSourceAsync(view, SourceThatCannotBeApplied(source, server, video));

		await failed.Task.WaitAsync(TimeSpan.FromMilliseconds(decodeTimeoutMilliseconds));
		Assert.Equal(MediaElementState.Failed, view.CurrentState);
	}

	// The previous media must leave the player, or it would keep playing (it loops, so it would not end by itself) and report a state over Failed.
	[Theory]
	[InlineData("a missing file")]
	[InlineData("a header the platform refuses")]
	[InlineData("a server that rejects HEAD")]
	[InlineData("a stream that cannot seek")]
	[InlineData("a kind of source the platform does not know")]
	public async Task SourceThatCannotBeAppliedRemovesThePreviousMedia(string source)
	{
		SetupWindowsBuilder();
		await RequireVideoRenderingAsync();
		var video = WinMediaAssets.ReadVideoBytes();
		await using var server = StartServerThatRejectsHead(video);
		var view = NewMediaElement(view => view.ShouldLoopPlayback = true);
		var handler = await ConnectAsync(view);
		await OpenAsync(view, VideoSource);
		var player = await GetPlayerAsync(handler);
		await InvokeOnMainThreadAsync(view.Play);
		await WaitUntilAsync(() => player.PlaybackSession.PlaybackState is MediaPlaybackState.Playing, "The first source never played.");
		var failed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		view.MediaFailed += (_, _) => failed.TrySetResult();

		await SetSourceAsync(view, SourceThatCannotBeApplied(source, server, video));

		await failed.Task.WaitAsync(TimeSpan.FromMilliseconds(decodeTimeoutMilliseconds));
		Assert.Equal(MediaElementState.Failed, view.CurrentState);
		Assert.Null(player.Source);
		Assert.Equal(0, view.MediaWidth);
		Assert.Equal(0, view.MediaHeight);
		await WaitUntilAsync(() => player.PlaybackSession.PlaybackState is not MediaPlaybackState.Playing && player.PlaybackSession.NaturalDuration == TimeSpan.Zero,
			"The previous media is still in the player after MediaFailed.");

		// The player reports the closing of the previous media on its own thread: that must not overwrite Failed either.
		var isOverwritten = await AssertHelpers.Wait(() => view.CurrentState is not MediaElementState.Failed, timeout: (int)WinMediaAssets.VideoDuration.TotalMilliseconds + 1000);
		Assert.False(isOverwritten, $"The state went from Failed to {view.CurrentState} after MediaFailed.");
	}

	// Servers that authorise each method separately (presigned URLs, for example) refuse HEAD while ranged GETs work.
	static WinMediaHttpServer StartServerThatRejectsHead(byte[] video) =>
		new WinMediaHttpServer
		{
			Handler = request => request.Method is "HEAD"
				? new WinMediaResponse { StatusCode = 405, ReasonPhrase = "Method Not Allowed" }
				: WinMediaResponse.ServeBytes(request, video, "video/mp4"),
		}.Start();

	static MediaSource SourceThatCannotBeApplied(string source, WinMediaHttpServer server, byte[] video) => source switch
	{
		"a missing file" => MediaSource.FromFile(Path.Combine(Path.GetDirectoryName(WinMediaAssets.VideoPath)!, "missing.mp4")),
		"a header the platform refuses" => MediaSource.FromUri(server.UriFor("video.mp4"), new Dictionary<string, string> { ["Invalid Header"] = "x" }),
		"a server that rejects HEAD" => MediaSource.FromUri(server.UriFor("video.mp4"), new Dictionary<string, string> { ["X-Device-Test"] = "42" }),
		"a stream that cannot seek" => MediaSource.FromStream(new WinMediaNonSeekableStream(video)),
		"a kind of source the platform does not know" => new WinMediaUnknownMediaSource(),
		_ => throw new ArgumentOutOfRangeException(nameof(source), source, null),
	};

	// A source without data loads nothing, like a null Source, so nothing is reported as a failure. The media that was playing loops, so it would
	// not end by itself.
	[Theory]
	[InlineData("a Uri source without a Uri")]
	[InlineData("a File source with a blank path")]
	[InlineData("a Resource source with a blank path")]
	[InlineData("a Stream source without a stream")]
	public async Task SourceWithoutDataStopsThePlayingMediaAndReportsNone(string source)
	{
		SetupWindowsBuilder();
		await RequireVideoRenderingAsync();
		var view = NewMediaElement(view => view.ShouldLoopPlayback = true);
		var handler = await ConnectAsync(view);
		await OpenAsync(view, VideoSource);
		var player = await GetPlayerAsync(handler);
		await InvokeOnMainThreadAsync(view.Play);
		await WaitUntilAsync(() => player.PlaybackSession.PlaybackState is MediaPlaybackState.Playing, "The first source never played.");
		var failures = 0;
		view.MediaFailed += (_, _) => Interlocked.Increment(ref failures);

		await SetSourceAsync(view, source switch
		{
			"a Uri source without a Uri" => new UriMediaSource(),
			"a File source with a blank path" => new FileMediaSource { Path = " " },
			"a Resource source with a blank path" => new ResourceMediaSource { Path = string.Empty },
			"a Stream source without a stream" => new StreamMediaSource(),
			_ => throw new ArgumentOutOfRangeException(nameof(source), source, null),
		});

		await WaitUntilAsync(() => player.PlaybackSession.PlaybackState is not MediaPlaybackState.Playing && player.PlaybackSession.NaturalDuration == TimeSpan.Zero,
			"The previous media is still in the player after a source without data.");
		Assert.Null(player.Source);
		Assert.Equal(0, view.MediaWidth);
		Assert.Equal(0, view.MediaHeight);

		await WaitUntilAsync(() => view.CurrentState is MediaElementState.None, "The state did not become None.");
		Assert.Equal(0, Volatile.Read(ref failures));
	}

	// The source mapping cannot await the update, which resets Duration after an await: an exception that the update throws from then on reaches
	// the dispatcher, which ends the app. The capture receives what the mapping would rethrow there. A source without data needs no decoder.
	[Fact]
	public async Task ExceptionOfAnEventHandlerDuringAMappedSourceChangeIsReportedThroughMediaFailedAndNothingReachesTheDispatcher()
	{
		var loggerFactory = new WinMediaLoggerFactory();
		SetupWindowsBuilder(loggerFactory);
		await RequireMediaPlayerAsync();
		var view = NewMediaElement(view => ((IMediaElement)view).Duration = TimeSpan.FromSeconds(5));
		await ConnectAsync(view);
		var failures = RecordFailures(view);
		var reported = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		view.MediaFailed += (_, _) => reported.TrySetResult();
		var handlerException = new InvalidOperationException("The PropertyChanged handler of the app failed.");
		var hasThrown = false;
		view.PropertyChanged += (_, e) =>
		{
			if (e.PropertyName == nameof(MediaElement.Duration) && !hasThrown)
			{
				hasThrown = true;
				throw handlerException;
			}
		};

		List<Exception> dispatcherExceptions = [];
		await InvokeOnMainThreadAsync(() =>
		{
			using var capture = new WinMediaAsyncVoidCapture();
			dispatcherExceptions = capture.Exceptions;
			view.Source = new FileMediaSource { Path = " " };
		});
		await reported.Task.WaitAsync(TimeSpan.FromMilliseconds(decodeTimeoutMilliseconds));

		// The mapping's continuation runs on the UI thread after the update has reported the exception.
		await FlushDispatcherAsync();
		await InvokeOnMainThreadAsync(() => Assert.Empty(dispatcherExceptions));
		Assert.True(hasThrown);
		AssertReportsTheExceptionOfTheHandler(failures, loggerFactory, handlerException);
		Assert.Equal(MediaElementState.None, view.CurrentState);
		Assert.Equal(TimeSpan.Zero, view.Duration);

		// MAUI applies no later change of a property whose setting threw.
		await InvokeOnMainThreadAsync(() => ((IMediaElement)view).Duration = TimeSpan.FromSeconds(1));
		Assert.Equal(TimeSpan.FromSeconds(1), view.Duration);
	}

	[Fact]
	public async Task SourceChangeOpensTheMediaAndReportsDurationAndSize()
	{
		SetupWindowsBuilder();
		await RequireVideoRenderingAsync();
		var view = NewMediaElement();
		await ConnectAsync(view);

		await OpenAsync(view, VideoSource);

		await WaitUntilAsync(() => view.Duration > TimeSpan.Zero && view.MediaWidth == WinMediaAssets.VideoWidth && view.MediaHeight == WinMediaAssets.VideoHeight,
			"The opened media never reported its duration and natural size.");
	}

	[Fact]
	public async Task PlayPauseAndStopCommandsReachTheNativePlayer()
	{
		SetupWindowsBuilder();
		await RequireVideoRenderingAsync();
		var view = NewMediaElement(view => view.ShouldLoopPlayback = true);
		var handler = await ConnectAsync(view);
		await OpenAsync(view, VideoSource);
		var session = (await GetPlayerAsync(handler)).PlaybackSession;

		await InvokeOnMainThreadAsync(() => view.Play());
		await WaitUntilAsync(() => session.PlaybackState is MediaPlaybackState.Playing, "Play() did not start the native player.");

		await InvokeOnMainThreadAsync(() => view.Pause());
		await WaitUntilAsync(() => session.PlaybackState is MediaPlaybackState.Paused, "Pause() did not pause the native player.");

		await InvokeOnMainThreadAsync(() => view.Play());
		await WaitUntilAsync(() => session.PlaybackState is MediaPlaybackState.Playing, "The second Play() did not start the native player.");

		await InvokeOnMainThreadAsync(() => view.Stop());
		await WaitUntilAsync(() => session.PlaybackState is MediaPlaybackState.Paused && session.Position < TimeSpan.FromSeconds(0.3), "Stop() did not pause and rewind the native player.");
	}

	[Fact]
	public async Task SeekToCompletesAndMovesTheNativePlayer()
	{
		SetupWindowsBuilder();
		await RequireDecoderAsync();
		var view = NewMediaElement(view => view.ShouldLoopPlayback = true);
		var handler = await ConnectAsync(view);
		await OpenAsync(view, VideoSource);
		var session = (await GetPlayerAsync(handler)).PlaybackSession;

		// The waits are bounded: a SeekTo that never completes must fail this test, not hang the run.
		await InvokeOnMainThreadAsync(async () => await view.SeekTo(TimeSpan.FromSeconds(1)).WaitAsync(TimeSpan.FromMilliseconds(decodeTimeoutMilliseconds)));
		await WaitUntilAsync(() => Math.Abs(session.Position.TotalSeconds - 1) <= 0.35, "SeekTo(1 s) on the UI thread did not move the native position.");

		await InvokeOnMainThreadAsync(async () => await view.SeekTo(TimeSpan.FromSeconds(0.5)).WaitAsync(TimeSpan.FromMilliseconds(decodeTimeoutMilliseconds)));
		await WaitUntilAsync(() => Math.Abs(session.Position.TotalSeconds - 0.5) <= 0.35, "A second SeekTo(0.5 s) did not move the native position (the seek task of the first one was not renewed).");
	}

	[Fact]
	public async Task SeekToFromAPoolThreadMovesTheNativePlayerAndCompletes()
	{
		SetupWindowsBuilder();
		await RequireDecoderAsync();
		var view = NewMediaElement(view => view.ShouldLoopPlayback = true);
		var handler = await ConnectAsync(view);
		await OpenAsync(view, VideoSource);
		var session = (await GetPlayerAsync(handler)).PlaybackSession;

		await Task.Run(() => view.SeekTo(TimeSpan.FromSeconds(1.5))).WaitAsync(TimeSpan.FromMilliseconds(decodeTimeoutMilliseconds));

		await WaitUntilAsync(() => Math.Abs(session.Position.TotalSeconds - 1.5) <= 0.35, "SeekTo(1.5 s) from a pool thread did not move the native position.");
	}

	// The clip does not loop and stays paused, and SeekTo(1 s) moves the player away from both ends first, so that every seek moves it.
	[Theory]
	[InlineData(WinMediaSeekOutsideTheMedia.BeforeTheStart)]
	[InlineData(WinMediaSeekOutsideTheMedia.BeyondTheEnd)]
	[InlineData(WinMediaSeekOutsideTheMedia.TheEnd)]
	public async Task SeekToOutsideTheMediaGoesToItsStartOrItsEndCompletesAndRaisesSeekCompleted(WinMediaSeekOutsideTheMedia target)
	{
		SetupWindowsBuilder();
		// A machine that renders no video raises no native SeekCompleted either, and its seeks outside the clip never complete.
		await RequireVideoRenderingAsync();
		var view = NewMediaElement();
		var handler = await ConnectAsync(view);
		await OpenAsync(view, VideoSource);
		var session = (await GetPlayerAsync(handler)).PlaybackSession;
		var seeksCompleted = 0;
		view.SeekCompleted += (_, _) => Interlocked.Increment(ref seeksCompleted);
		await InvokeOnMainThreadAsync(async () => await view.SeekTo(TimeSpan.FromSeconds(1)).WaitAsync(TimeSpan.FromMilliseconds(decodeTimeoutMilliseconds)));
		await WaitUntilAsync(() => Volatile.Read(ref seeksCompleted) > 0 && Math.Abs(session.Position.TotalSeconds - 1) <= 0.35, "SeekTo(1 s) did not move the native position and raise SeekCompleted.");
		var seeksCompletedBefore = Volatile.Read(ref seeksCompleted);
		var (requested, expected) = WinMediaSeeks.OutsideTheMedia(target, session.NaturalDuration);

		// Bounded: a SeekTo that never completes must fail this test, not hang the run.
		await InvokeOnMainThreadAsync(async () => await view.SeekTo(requested).WaitAsync(TimeSpan.FromMilliseconds(decodeTimeoutMilliseconds)));

		await WaitUntilAsync(() => Volatile.Read(ref seeksCompleted) > seeksCompletedBefore, "SeekTo outside the media raised no SeekCompleted.");
		await WaitUntilAsync(() => Math.Abs((session.Position - expected).TotalSeconds) <= 0.35, "SeekTo outside the media did not go to the start or the end of the media.");
	}

	[Fact]
	public async Task PositionAdvancesWhilePlayingBecauseTheStatusTimerUpdatesIt()
	{
		SetupWindowsBuilder();
		await RequireVideoRenderingAsync();
		var view = NewMediaElement(view => view.ShouldLoopPlayback = true);
		await ConnectAsync(view);
		await OpenAsync(view, VideoSource);

		// UpdateStatus does nothing without an application window; the test relies on the runner's window.
		await InvokeOnMainThreadAsync(() => view.Play());

		await WaitUntilAsync(() => view.Position > TimeSpan.FromSeconds(0.3), "The status timer never copied the advancing native position to MediaElement.Position.");
	}

	[Fact]
	public async Task SpeedChangeReachesTheNativePlayer()
	{
		SetupWindowsBuilder();
		await RequireDecoderAsync();
		var view = NewMediaElement(view => view.ShouldLoopPlayback = true);
		var handler = await ConnectAsync(view);
		await OpenAsync(view, VideoSource);
		var session = (await GetPlayerAsync(handler)).PlaybackSession;

		await InvokeOnMainThreadAsync(() => view.Speed = 2);

		await WaitUntilAsync(() => Math.Abs(session.PlaybackRate - 2) < 0.001, "Speed did not reach the native player through the handler.");
	}

	[Theory]
	[InlineData(nameof(MediaElementHandler.MapAspect))]
	[InlineData(nameof(MediaElementHandler.MapShouldShowPlaybackControls))]
	[InlineData(nameof(MediaElementHandler.MapSource))]
	[InlineData(nameof(MediaElementHandler.MapSpeed))]
	[InlineData(nameof(MediaElementHandler.MapStatusUpdated))]
	[InlineData(nameof(MediaElementHandler.MapVolume))]
	[InlineData(nameof(MediaElementHandler.MapShouldKeepScreenOn))]
	[InlineData(nameof(MediaElementHandler.MapShouldMute))]
	[InlineData(nameof(MediaElementHandler.MapPlayRequested))]
	[InlineData(nameof(MediaElementHandler.MapPauseRequested))]
	[InlineData(nameof(MediaElementHandler.MapSeekRequested))]
	[InlineData(nameof(MediaElementHandler.MapStopRequested))]
	[InlineData(nameof(MediaElementHandler.ShouldLoopPlayback))]
	public async Task MappersDoNothingAfterTheHandlerWasDisposed(string mapper)
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();
		var view = NewMediaElement();
		var handler = await ConnectAsync(view);

		await InvokeOnMainThreadAsync(() =>
		{
			handler.Dispose();
			Assert.Null(handler.Manager);

			// MapSeekRequested is async void: its exceptions would otherwise reach the UI thread.
			using var capture = new WinMediaAsyncVoidCapture();
			InvokeMapper(mapper, handler, view);

			Assert.Empty(capture.Exceptions);
		});
	}

	static void InvokeMapper(string mapper, WinMediaHandler handler, MediaElement view)
	{
		switch (mapper)
		{
			case nameof(MediaElementHandler.MapAspect):
				MediaElementHandler.MapAspect(handler, view);
				break;
			case nameof(MediaElementHandler.MapShouldShowPlaybackControls):
				MediaElementHandler.MapShouldShowPlaybackControls(handler, view);
				break;
			case nameof(MediaElementHandler.MapSource):
				MediaElementHandler.MapSource(handler, view);
				break;
			case nameof(MediaElementHandler.MapSpeed):
				MediaElementHandler.MapSpeed(handler, view);
				break;
			case nameof(MediaElementHandler.MapStatusUpdated):
				MediaElementHandler.MapStatusUpdated(handler, view, null);
				break;
			case nameof(MediaElementHandler.MapVolume):
				MediaElementHandler.MapVolume(handler, view);
				break;
			case nameof(MediaElementHandler.MapShouldKeepScreenOn):
				MediaElementHandler.MapShouldKeepScreenOn(handler, view);
				break;
			case nameof(MediaElementHandler.MapShouldMute):
				MediaElementHandler.MapShouldMute(handler, view);
				break;
			case nameof(MediaElementHandler.MapPlayRequested):
				MediaElementHandler.MapPlayRequested(handler, view, null);
				break;
			case nameof(MediaElementHandler.MapPauseRequested):
				MediaElementHandler.MapPauseRequested(handler, view, null);
				break;
			case nameof(MediaElementHandler.MapSeekRequested):
				MediaElementHandler.MapSeekRequested(handler, view, new MediaSeekRequestedEventArgs(TimeSpan.FromSeconds(1)));
				break;
			case nameof(MediaElementHandler.MapStopRequested):
				MediaElementHandler.MapStopRequested(handler, view, null);
				break;
			case nameof(MediaElementHandler.ShouldLoopPlayback):
				MediaElementHandler.ShouldLoopPlayback(handler, view);
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(mapper), mapper, null);
		}
	}

	[Fact]
	public async Task ShouldLoopPlaybackMapperToleratesANullHandler()
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();

		await InvokeOnMainThreadAsync(() => MediaElementHandler.ShouldLoopPlayback(null!, NewMediaElement()));
	}

	[Fact]
	public async Task SeekRequestWithoutArgumentsThrowsArgumentNullException()
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();
		var view = NewMediaElement();
		var handler = await ConnectAsync(view);

		await InvokeOnMainThreadAsync(() =>
		{
			using var capture = new WinMediaAsyncVoidCapture();

			MediaElementHandler.MapSeekRequested(handler, view, null);

			Assert.IsType<ArgumentNullException>(Assert.Single(capture.Exceptions));
		});
	}

	[Fact]
	public async Task SeekRequestWithTheWrongArgumentsDoesNothing()
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();
		var view = NewMediaElement();
		var handler = await ConnectAsync(view);

		await InvokeOnMainThreadAsync(() =>
		{
			using var capture = new WinMediaAsyncVoidCapture();

			MediaElementHandler.MapSeekRequested(handler, view, EventArgs.Empty);

			Assert.Empty(capture.Exceptions);
			Assert.False(((IAsynchronousMediaElementHandler)view).SeekCompletedTCS.Task.IsCompleted);
		});
	}

	[Fact]
	public async Task SeekRequestWithoutManagerStillCompletesTheSeekTask()
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();
		var view = NewMediaElement();
		var handler = await ConnectAsync(view);

		await InvokeOnMainThreadAsync(() =>
		{
			handler.Dispose();
			var seekCompleted = ((IAsynchronousMediaElementHandler)view).SeekCompletedTCS.Task;
			Assert.False(seekCompleted.IsCompleted);

			MediaElementHandler.MapSeekRequested(handler, view, new MediaSeekRequestedEventArgs(TimeSpan.FromSeconds(1)));

			Assert.True(seekCompleted.IsCompletedSuccessfully);
		});
	}

	[Fact]
	public async Task HandlerDisposeIsIdempotentAndClearsTheManager()
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();
		var handler = await ConnectAsync(NewMediaElement());

		await InvokeOnMainThreadAsync(() =>
		{
			var player = handler.Player;
			Assert.NotNull(handler.Manager);

			handler.Dispose();
			Assert.Null(handler.Manager);

			handler.Dispose();
			Assert.Null(handler.Manager);

			// Dispose() releases the manager only; the platform view (and its player) belongs to DisconnectHandler.
			Assert.NotNull(player);
			Assert.False(handler.PlayerElement.MediaPlayer is null);
		});
	}

	[Fact]
	public async Task HandlerDisposeWithDisposingFalseKeepsTheManager()
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();
		var handler = await ConnectAsync(NewMediaElement());

		await InvokeOnMainThreadAsync(() =>
		{
			handler.CallDispose(false);

			Assert.NotNull(handler.Manager);
		});
	}

	[Fact]
	public async Task HandlerCreatedWithoutAMauiContextThrowsNullReferenceException()
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();

		await InvokeOnMainThreadAsync(() =>
		{
			var handler = new WinMediaHandler();

			Assert.Throws<NullReferenceException>(() => handler.SetVirtualView(NewMediaElement()));
		});
	}

	[Fact]
	public async Task HandlerCreatedOffTheUiThreadThrowsInvalidOperationException()
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();
		var handler = new WinMediaHandler();
		handler.SetMauiContext(MauiContext);

		// A pool thread has no dispatcher; the check comes before any XAML object is created.
		var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => Task.Run(() => handler.SetVirtualView(NewMediaElement())));

		Assert.Contains("IDispatcher", exception.Message, StringComparison.Ordinal);
	}

	[Fact]
	public async Task CreatingThePlatformViewAgainReusesTheExistingManager()
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();
		var handler = await ConnectAsync(NewMediaElement());

		await InvokeOnMainThreadAsync(() =>
		{
			var manager = handler.Manager;
			var firstView = handler.MauiView;

			var secondView = handler.CallCreatePlatformView();
			RegisterCleanup(secondView.Dispose);

			Assert.NotNull(manager);
			Assert.Same(manager, handler.Manager);
			Assert.NotSame(firstView, secondView);
			Assert.NotSame(firstView.Children.OfType<Microsoft.UI.Xaml.Controls.MediaPlayerElement>().Single(), WinMediaHandler.GetPlayerElement(secondView));
		});
	}

	[Fact]
	public async Task DisconnectingAnUnloadedViewDisposesManagerAndPlatformViewImmediately()
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();
		var view = NewMediaElement();
		var handler = await ConnectAsync(view);

		await InvokeOnMainThreadAsync(() =>
		{
			var platformView = handler.MauiView;
			var element = handler.PlayerElement;
			Assert.False(platformView.IsLoaded);

			((IElementHandler)handler).DisconnectHandler();

			Assert.Null(handler.Manager);
			Assert.Null(view.Handler);
			Assert.Null(element.MediaPlayer);
		});
	}

	// Dispose leaves the handler as the view's handler, so property changes still reach the toolkit's mappers, which must find no manager and do
	// nothing. After DisconnectHandler, by contrast, MAUI has already cleared view.Handler, so a property change reaches no toolkit code at all.
	[Fact]
	public async Task PropertyChangesAfterTheHandlerWasDisposedLeaveTheNativePlayerAlone()
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();
		var view = NewMediaElement();
		var handler = await ConnectAsync(view);
		var player = await GetPlayerAsync(handler);

		await InvokeOnMainThreadAsync(() =>
		{
			Assert.Equal(1.0, player.Volume, 3);
			Assert.False(player.IsMuted);
			Assert.False(player.IsLoopingEnabled);
			Assert.Equal(Stretch.Uniform, handler.PlayerElement.Stretch);
			Assert.False(handler.PlayerElement.AreTransportControlsEnabled);

			handler.Dispose();
			Assert.Same(handler, view.Handler);

			view.Volume = 0.25;
			view.Aspect = Aspect.Fill;
			view.ShouldMute = true;
			view.Speed = 2;
			view.ShouldLoopPlayback = true;
			view.ShouldShowPlaybackControls = true;
			view.ShouldKeepScreenOn = true;
		});

		// A mute that reached the manager would be queued: let it run before looking.
		await FlushDispatcherAsync();

		await InvokeOnMainThreadAsync(() =>
		{
			Assert.Equal(1.0, player.Volume, 3);
			Assert.False(player.IsMuted);
			Assert.False(player.IsLoopingEnabled);
			Assert.Equal(Stretch.Uniform, handler.PlayerElement.Stretch);
			Assert.False(handler.PlayerElement.AreTransportControlsEnabled);
		});
	}

	[Fact]
	public async Task DisconnectingALoadedViewDisposesThePlatformViewWhenItUnloads()
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();
		var view = NewMediaElement();
		RegisterCleanup(() =>
		{
			((IElementHandler?)view.Handler)?.DisconnectHandler();
			view.Dispose();
		});
		Microsoft.UI.Xaml.Controls.MediaPlayerElement? element = null;

		await AttachAndRun(view, handler =>
		{
			var winMediaHandler = (WinMediaHandler)handler;
			element = winMediaHandler.PlayerElement;
			Assert.True(winMediaHandler.MauiView.IsLoaded);

			((IElementHandler)winMediaHandler).DisconnectHandler();

			// The manager is gone at once, but a view that is still in the window keeps its player until the window lets go of it.
			Assert.Null(winMediaHandler.Manager);
			Assert.NotNull(element.MediaPlayer);
		});

		await WaitUntilOnUiAsync(() => element!.MediaPlayer is null, "The platform view was not disposed after it unloaded.");
	}

	[Fact]
	public async Task ConnectingAgainAfterDisconnectCreatesANewManagerAndPlatformView()
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();
		var view = NewMediaElement();
		var first = await ConnectAsync(view);
		MediaManager? firstManager = null;
		Microsoft.UI.Xaml.Controls.MediaPlayerElement? firstElement = null;
		await InvokeOnMainThreadAsync(() =>
		{
			firstManager = first.Manager;
			firstElement = first.PlayerElement;
			((IElementHandler)first).DisconnectHandler();
		});

		var second = await CreateHandlerAsync<WinMediaHandler>(view);

		await InvokeOnMainThreadAsync(() =>
		{
			Assert.NotSame(first, second);
			Assert.NotNull(second.Manager);
			Assert.NotSame(firstManager, second.Manager);
			Assert.NotSame(firstElement, second.PlayerElement);
			Assert.NotNull(second.Player);
		});
	}

	[Fact]
	public async Task DisconnectedHandlerAndNativeViewsAreCollected()
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();
		WeakReference viewReference = null!, handlerReference = null!, platformViewReference = null!, playerElementReference = null!, managerReference = null!, controlsReference = null!;

		await ConnectAndDisconnectAsync();

		await AssertionExtensions.WaitForGC(viewReference, handlerReference, platformViewReference, playerElementReference, managerReference, controlsReference);

		// Not inlined into the test body: the locals must be unreachable when the collection starts.
		async Task ConnectAndDisconnectAsync()
		{
			var handler = await InvokeOnMainThreadAsync(() =>
			{
				var view = NewMediaElement();
				var created = CreateHandler<WinMediaHandler>(view);
				viewReference = new(view);
				handlerReference = new(created);
				platformViewReference = new(created.PlatformView);
				playerElementReference = new(created.PlayerElement);
				managerReference = new(created.Manager);
				controlsReference = new(created.PlayerElement.TransportControls);
				return created;
			});

			// Connecting queued the manager's mute write (PlatformUpdateShouldMute always queues it). It runs here, before the disconnect disposes
			// the platform view, as it does for every MediaElement that stays connected for one UI-thread turn (a write that runs after the
			// disposal does nothing: MediaManagerPropertyWindowsTests.QueuedMuteWriteThatRunsAfterThePlatformViewWasDisposedDoesNothing).
			await FlushDispatcherAsync();

			await InvokeOnMainThreadAsync(() =>
			{
				var view = handler.VirtualView;
				((IElementHandler)handler).DisconnectHandler();
				view.Handler = null;
			});
		}
	}

	[Fact]
	public async Task HostedHandlerAndNativeViewsAreCollectedAfterTheViewUnloads()
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();
		WeakReference viewReference = null!, handlerReference = null!, platformViewReference = null!, playerElementReference = null!, managerReference = null!, controlsReference = null!;

		await HostAndDisconnectAsync();

		await AssertionExtensions.WaitForGC(viewReference, handlerReference, platformViewReference, playerElementReference, managerReference, controlsReference);

		// Not inlined into the test body: the locals must be unreachable when the collection starts.
		async Task HostAndDisconnectAsync()
		{
			var view = NewMediaElement();
			await AttachAndRun(view, handler =>
			{
				var winMediaHandler = (WinMediaHandler)handler;
				viewReference = new(view);
				handlerReference = new(winMediaHandler);
				platformViewReference = new(winMediaHandler.PlatformView);
				playerElementReference = new(winMediaHandler.PlayerElement);
				managerReference = new(winMediaHandler.Manager);
				controlsReference = new(winMediaHandler.PlayerElement.TransportControls);

				((IElementHandler)winMediaHandler).DisconnectHandler();
			});

			await WaitUntilOnUiAsync(() => !((MauiMediaElement?)platformViewReference.Target ?? throw new InvalidOperationException("The platform view is gone.")).IsLoaded, "The view never unloaded.");
		}
	}

	[Fact]
	public async Task HandlerOfADisposedMediaElementWithOpenedMediaIsCollected()
	{
		SetupWindowsBuilder();
		await RequireDecoderAsync();
		WeakReference viewReference = null!, handlerReference = null!, platformViewReference = null!, managerReference = null!;

		await OpenAndDisconnectAsync();

		// Opening the media created the status timer (a root while it runs) and the Metadata helper, which subscribes to the player's system media
		// transport controls and holds the MediaElement: MediaManager.Dispose removes that handler with every other one.
		if (!await AssertionExtensions.WaitForCollect(viewReference, handlerReference, platformViewReference, managerReference))
		{
			Assert.Fail($"Still alive after 40 collections: MediaElement {viewReference.IsAlive}, handler {handlerReference.IsAlive}, platform view {platformViewReference.IsAlive}, manager {managerReference.IsAlive}.");
		}

		async Task OpenAndDisconnectAsync()
		{
			var view = NewMediaElement();
			var handler = await InvokeOnMainThreadAsync(() => CreateHandler<WinMediaHandler>(view));
			await OpenAsync(view, VideoSource);

			// OnMediaElementMediaOpened raises MediaOpened first and creates the Metadata helper right after it.
			var openedManager = new WeakReference(handler.Manager);
			await WaitUntilAsync(() => openedManager.Target is MediaManager manager && WinMediaReflection.GetField<object?>(manager, "metadata") is not null,
				"Opening the media did not create the Metadata helper, so this test would not cover its subscription.");

			await InvokeOnMainThreadAsync(() =>
			{
				viewReference = new(view);
				handlerReference = new(handler);
				platformViewReference = new(handler.PlatformView);
				managerReference = new(handler.Manager);

				((IElementHandler)handler).DisconnectHandler();
				view.Handler = null;
				view.Dispose();
			});
		}
	}
}