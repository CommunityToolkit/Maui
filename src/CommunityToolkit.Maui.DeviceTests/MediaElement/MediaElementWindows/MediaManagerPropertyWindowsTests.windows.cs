using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;
using Windows.Media.Playback;
using Stretch = Microsoft.UI.Xaml.Media.Stretch;

namespace CommunityToolkit.Maui.DeviceTests;

[Category(TestCategory.MediaManagerWindows)]
[Collection(TestCollections.DisableParallelization)]
public class MediaManagerPropertyWindowsTests : WinMediaTestBase
{
	[Fact]
	public async Task CreatePlatformViewAttachesAMediaPlayerWithDisabledSystemControls()
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();

		var fixture = await CreateManagerAsync();

		await InvokeOnMainThreadAsync(() =>
		{
			Assert.NotNull(fixture.Element);
			Assert.NotNull(fixture.Player);
			Assert.Same(fixture.Element.MediaPlayer, fixture.Player);
			Assert.Same(fixture.Element, fixture.Manager.PlayerElement);

			// The system controls are on by default in a MediaPlayer; only media with metadata switches them on
			// (MediaMetadataWindowsTests.OpeningMediaSwitchesTheSystemMediaControlsOnOnlyForMediaWithMetadata).
			Assert.False(fixture.Player.SystemMediaTransportControls.IsEnabled);
		});
	}

	[Theory]
	[InlineData(Aspect.Fill, Stretch.Fill)]
	[InlineData(Aspect.AspectFill, Stretch.UniformToFill)]
	[InlineData(Aspect.AspectFit, Stretch.Uniform)]
	[InlineData(Aspect.Center, Stretch.Uniform)]
	public async Task UpdateAspectSetsTheNativeStretch(Aspect aspect, Stretch expected)
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();

		var fixture = await CreateManagerAsync(view => view.Aspect = aspect);

		await InvokeOnMainThreadAsync(() =>
		{
			// A value that differs from every expectation, so that a missing update cannot pass by accident.
			fixture.Element!.Stretch = Stretch.None;

			fixture.Manager.UpdateAspect();

			Assert.Equal(expected, fixture.Element.Stretch);
		});
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task UpdateShouldShowPlaybackControlsSetsTheNativeFlag(bool show)
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();

		var fixture = await CreateManagerAsync(view => view.ShouldShowPlaybackControls = show);

		await InvokeOnMainThreadAsync(() =>
		{
			fixture.Element!.AreTransportControlsEnabled = !show;

			fixture.Manager.UpdateShouldShowPlaybackControls();

			Assert.Equal(show, fixture.Element.AreTransportControlsEnabled);
		});
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task UpdateShouldLoopPlaybackSetsTheNativeFlag(bool loop)
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();

		var fixture = await CreateManagerAsync(view => view.ShouldLoopPlayback = loop);

		await InvokeOnMainThreadAsync(() =>
		{
			fixture.Player!.IsLoopingEnabled = !loop;

			fixture.Manager.UpdateShouldLoopPlayback();

			Assert.Equal(loop, fixture.Player.IsLoopingEnabled);
		});
	}

	[Theory]
	[InlineData(0.0)]
	[InlineData(0.25)]
	[InlineData(0.5)]
	[InlineData(0.75)]
	public async Task UpdateVolumeSetsTheNativeVolume(double volume)
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();

		var fixture = await CreateManagerAsync(view => view.Volume = volume);

		await InvokeOnMainThreadAsync(() =>
		{
			// The native default is 1.0, so each row needs the update to reach the player.
			Assert.Equal(1.0, fixture.Player!.Volume, 3);

			fixture.Manager.UpdateVolume();

			Assert.Equal(volume, fixture.Player.Volume, 3);
		});
	}

	[Fact]
	public async Task UpdateVolumeIsIgnoredWhileMuted()
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();

		var fixture = await CreateManagerAsync(view =>
		{
			view.ShouldMute = true;
			view.Volume = 0.25;
		});

		await InvokeOnMainThreadAsync(() => fixture.Manager.UpdateVolume());
		await FlushDispatcherAsync();

		Assert.Equal(1.0, fixture.Player!.Volume, 3);
	}

	[Fact]
	public async Task UpdateVolumeFromABackgroundThreadIsDispatchedToTheUiThread()
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();

		var fixture = await CreateManagerAsync(view => view.Volume = 0.25);

		// MediaPlayerElement.MediaPlayer is a XAML property: reading it from a pool thread throws, so only the dispatched arm can succeed.
		await Task.Run(() => fixture.Manager.UpdateVolume());

		await WaitUntilAsync(() => Math.Abs(fixture.Player!.Volume - 0.25) < 0.001, "The volume of a background-thread UpdateVolume never reached the native player.");
	}

	[Fact]
	public async Task UpdateVolumeThatMustBeDispatchedQueuesTheUpdateOnTheDispatcher()
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();

		var fixture = await CreateManagerAsync(view => view.Volume = 0.25, dispatchRequired: true);

		await InvokeOnMainThreadAsync(() =>
		{
			fixture.Manager.UpdateVolume();

			Assert.Equal(1, fixture.Dispatcher!.DispatchCount);
			Assert.Equal(1.0, fixture.Player!.Volume, 3);
		});

		await WaitUntilAsync(() => Math.Abs(fixture.Player!.Volume - 0.25) < 0.001, "The queued volume update never reached the native player.");
	}

	[Fact]
	public async Task UpdateVolumeOnTheUiThreadWritesTheNativeVolumeWithoutQueueing()
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();

		var fixture = await CreateManagerAsync(view => view.Volume = 0.25, recordDispatches: true);

		await InvokeOnMainThreadAsync(() =>
		{
			fixture.Manager.UpdateVolume();

			Assert.Equal(0.25, fixture.Player!.Volume, 3);
			Assert.Equal(0, fixture.Dispatcher!.DispatchCount);
		});
	}

	[Fact]
	public async Task UpdateShouldMuteTogglesTheNativeMute()
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();

		var fixture = await CreateManagerAsync();

		fixture.View.ShouldMute = true;
		await InvokeOnMainThreadAsync(() => fixture.Manager.UpdateShouldMute());
		await WaitUntilAsync(() => fixture.Player!.IsMuted, "The native player was not muted.");

		fixture.View.ShouldMute = false;
		await InvokeOnMainThreadAsync(() => fixture.Manager.UpdateShouldMute());
		await WaitUntilAsync(() => !fixture.Player!.IsMuted, "The native player was not unmuted.");
	}

	// PlatformUpdateShouldMute always queues its write, even on the UI thread. Connecting a MediaElementHandler maps ShouldMute; disconnecting
	// it disposes the manager and, for a view that is not loaded, the MauiMediaElement at once (MediaPlayer.Dispose, SetMediaPlayer(null)). A
	// handler connected and disconnected in the same UI-thread turn therefore leaves a queued write behind a disposed player. The manager and
	// the platform view are built here exactly as MediaElementHandler builds them, around a dispatcher that hands the queued write to the test;
	// the test runs it inside Record.Exception, because on the real dispatcher queue an exception ends the app (microsoft-ui-xaml#8940).
	[Fact]
	public async Task QueuedMuteWriteThatRunsAfterThePlatformViewWasDisposedDoesNothing()
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();
		var dispatcher = new WinMediaCapturingDispatcher();

		await InvokeOnMainThreadAsync(() =>
		{
			var view = new MediaElement { ShouldMute = true };
			var manager = new WinMediaManager(MauiContext, view, dispatcher);
			var platformView = new WinMediaView(manager.CreatePlatformView());
			RegisterCleanup(() =>
			{
				manager.Dispose();
				platformView.Dispose();
				view.Dispose();
			});

			manager.UpdateShouldMute();
			var queuedWrite = Assert.Single(dispatcher.TakeQueued());

			manager.Dispose();
			platformView.Dispose();

			Assert.Null(Record.Exception(queuedWrite));
		});
	}

	// The same for a volume write that had to be queued (a Volume change off the UI thread): the handler can disconnect before it runs.
	[Fact]
	public async Task QueuedVolumeWriteThatRunsAfterThePlatformViewWasDisposedDoesNothing()
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();
		var dispatcher = new WinMediaCapturingDispatcher(isDispatchRequired: true);

		await InvokeOnMainThreadAsync(() =>
		{
			var view = new MediaElement { Volume = 0.25 };
			var manager = new WinMediaManager(MauiContext, view, dispatcher);
			var platformView = new WinMediaView(manager.CreatePlatformView());
			RegisterCleanup(() =>
			{
				manager.Dispose();
				platformView.Dispose();
				view.Dispose();
			});

			manager.UpdateVolume();
			var queuedWrite = Assert.Single(dispatcher.TakeQueued());

			// MediaElementHandler.DisconnectHandler disposes the manager and the unloaded platform view.
			manager.Dispose();
			platformView.Dispose();

			Assert.Null(Record.Exception(queuedWrite));
		});
	}

	[Fact]
	public async Task NativeMuteChangeFlowsBackToTheMediaElement()
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();

		var fixture = await CreateManagerAsync();

		fixture.Player!.IsMuted = true;
		await WaitUntilAsync(() => fixture.View.ShouldMute, "ShouldMute did not follow the native mute.");

		fixture.Player.IsMuted = false;
		await WaitUntilAsync(() => !fixture.View.ShouldMute, "ShouldMute did not follow the native unmute.");
	}

	[Fact]
	public async Task NativeVolumeChangeFlowsBackToTheMediaElement()
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();

		var fixture = await CreateManagerAsync();

		fixture.Player!.Volume = 0.25;

		await WaitUntilAsync(() => Math.Abs(fixture.View.Volume - 0.25) < 0.001, "Volume did not follow the native volume.");
	}

	// WinMediaDispatcher counts the updates these events queue apart from the manager's own dispatches by the names of MediaManager's handler
	// lambdas. This test fails when those names stop matching, before the tests that count dispatches turn flaky again.
	[Fact]
	public async Task PlayerVolumeAndMuteEventsAreCountedApart()
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();

		var fixture = await CreateManagerAsync(recordDispatches: true);
		var playerEventsBefore = fixture.Dispatcher!.PlayerEventDispatchCount;

		fixture.Player!.Volume = fixture.Player.Volume > 0.5 ? 0.25 : 0.75;
		await WaitUntilAsync(() => fixture.Dispatcher.PlayerEventDispatchCount > playerEventsBefore, "The update queued by the player's VolumeChanged was not counted as a player event.");

		var afterVolume = fixture.Dispatcher.PlayerEventDispatchCount;
		fixture.Player.IsMuted = !fixture.Player.IsMuted;
		await WaitUntilAsync(() => fixture.Dispatcher.PlayerEventDispatchCount > afterVolume, "The update queued by the player's IsMutedChanged was not counted as a player event.");
	}

	[Fact]
	public async Task PlayRequestsTheDisplayWhileKeepScreenOnIsSet()
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();
		await RequireDisplayRequestCountingAsync();

		var fixture = await CreateManagerAsync(view => view.ShouldKeepScreenOn = true);

		await InvokeOnMainThreadAsync(() =>
		{
			AssertDisplayRequest(fixture.Manager, active: false);

			fixture.Manager.Play();
			AssertDisplayRequest(fixture.Manager, active: true);

			// Requests are cumulative on Windows, so a second Play must not add a second one.
			fixture.Manager.Play();
			AssertDisplayRequest(fixture.Manager, active: true);

			fixture.Manager.Pause();
			AssertDisplayRequest(fixture.Manager, active: false);

			// RequestRelease without a request throws, so a second Pause must not release again.
			fixture.Manager.Pause();
			AssertDisplayRequest(fixture.Manager, active: false);
		});
	}

	[Fact]
	public async Task PlayAndPauseDoNotTouchTheDisplayWhileKeepScreenOnIsOff()
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();

		await RequireDisplayRequestCountingAsync();

		var fixture = await CreateManagerAsync(view => view.ShouldKeepScreenOn = false);

		await InvokeOnMainThreadAsync(() =>
		{
			fixture.Manager.Play();
			AssertDisplayRequest(fixture.Manager, active: false);

			fixture.Manager.Pause();
			AssertDisplayRequest(fixture.Manager, active: false);
		});
	}

	[Fact]
	public async Task StopReleasesTheDisplayRequestAndReportsStopped()
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();
		await RequireDisplayRequestCountingAsync();

		var fixture = await CreateManagerAsync(view => view.ShouldKeepScreenOn = true);

		await InvokeOnMainThreadAsync(() =>
		{
			fixture.Manager.Play();
			AssertDisplayRequest(fixture.Manager, active: true);

			fixture.Manager.Stop();

			AssertDisplayRequest(fixture.Manager, active: false);
			Assert.Equal(MediaElementState.Stopped, fixture.View.CurrentState);
			Assert.Equal(TimeSpan.Zero, fixture.Player!.Position);

			// RequestRelease without a request throws, so a second Stop must not release again.
			fixture.Manager.Stop();
			AssertDisplayRequest(fixture.Manager, active: false);
		});
	}

	[Theory]
	[InlineData(MediaElementState.Playing, true)]
	[InlineData(MediaElementState.Paused, true)]
	[InlineData(MediaElementState.Stopped, true)]
	[InlineData(MediaElementState.None, false)]
	[InlineData(MediaElementState.Opening, false)]
	[InlineData(MediaElementState.Buffering, false)]
	[InlineData(MediaElementState.Failed, false)]
	public async Task UpdateShouldKeepScreenOnRequestsTheDisplayOnlyInPlaybackStates(MediaElementState state, bool expectedRequest)
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();

		await RequireDisplayRequestCountingAsync();

		var fixture = await CreateManagerAsync(view => view.ShouldKeepScreenOn = true);

		await InvokeOnMainThreadAsync(() =>
		{
			((IMediaElement)fixture.View).CurrentStateChanged(state);

			fixture.Manager.UpdateShouldKeepScreenOn();
			AssertDisplayRequest(fixture.Manager, expectedRequest);

			// A repeated update neither toggles nor stacks the request.
			fixture.Manager.UpdateShouldKeepScreenOn();
			AssertDisplayRequest(fixture.Manager, expectedRequest);

			fixture.View.ShouldKeepScreenOn = false;
			fixture.Manager.UpdateShouldKeepScreenOn();
			AssertDisplayRequest(fixture.Manager, active: false);

			// RequestRelease without a request throws, so a second update must not release again.
			fixture.Manager.UpdateShouldKeepScreenOn();
			AssertDisplayRequest(fixture.Manager, active: false);
		});
	}

	[Fact]
	public async Task StopWithoutMediaRewindsAndReportsStopped()
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();

		var fixture = await CreateManagerAsync();

		await InvokeOnMainThreadAsync(() =>
		{
			Assert.Equal(MediaElementState.None, fixture.View.CurrentState);

			fixture.Manager.Stop();

			Assert.Equal(MediaElementState.Stopped, fixture.View.CurrentState);
			Assert.Equal(TimeSpan.Zero, fixture.Player!.Position);
		});
	}

	[Fact]
	public async Task SeekWithoutMediaCompletesWithoutMovingThePlayer()
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();

		var fixture = await CreateManagerAsync(recordDispatches: true);
		// A MediaPlayer without media reports CanSeek on Windows 11 (26200), so the guard also needs a playback state other than None.
		Assert.Equal(MediaPlaybackState.None, fixture.Player!.PlaybackSession.PlaybackState);

		await InvokeOnMainThreadAsync(async () => await fixture.Manager.Seek(TimeSpan.FromSeconds(1)));
		await FlushDispatcherAsync();

		Assert.Equal(0, fixture.Dispatcher!.DispatchCount);
		Assert.Equal(TimeSpan.Zero, fixture.Player.Position);
	}

	[Fact]
	public async Task SeekWithoutMediaDoesNotQueueAnythingEvenWhenDispatchingIsRequired()
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();

		var fixture = await CreateManagerAsync(dispatchRequired: true);
		// A MediaPlayer without media reports CanSeek on Windows 11 (26200), so the guard also needs a playback state other than None.
		Assert.Equal(MediaPlaybackState.None, fixture.Player!.PlaybackSession.PlaybackState);

		await InvokeOnMainThreadAsync(async () => await fixture.Manager.Seek(TimeSpan.FromSeconds(1)));
		await FlushDispatcherAsync();

		Assert.Equal(0, fixture.Dispatcher!.DispatchCount);
		Assert.Equal(TimeSpan.Zero, fixture.Player.Position);
	}

	[Fact]
	public async Task SeekWithoutMediaFromABackgroundThreadCompletesWithoutMovingThePlayer()
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();

		var fixture = await CreateManagerAsync();

		await Task.Run(() => fixture.Manager.Seek(TimeSpan.FromSeconds(1)));
		await FlushDispatcherAsync();

		Assert.Equal(TimeSpan.Zero, fixture.Player!.Position);
	}

	[Fact]
	public async Task ManagerWithoutPlatformViewIgnoresEveryNativeRequest()
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();

		var fixture = await CreateManagerAsync(view =>
		{
			view.ShouldKeepScreenOn = false;
			view.Source = new UriMediaSource { Uri = WinMediaAssets.VideoUri };
		}, createPlatformView: false);

		Assert.Null(fixture.Manager.PlayerElement);

		await InvokeOnMainThreadAsync(async () =>
		{
			var manager = fixture.Manager;

			manager.UpdateAspect();
			manager.UpdateSpeed();
			manager.UpdateShouldShowPlaybackControls();
			manager.UpdateShouldLoopPlayback();
			manager.UpdateShouldMute();
			manager.UpdateVolume();
			manager.Play();
			manager.Pause();
			manager.Stop();
			manager.UpdateStatus();
			await manager.UpdateSource();
			await manager.Seek(TimeSpan.FromSeconds(1));
		});
		await FlushDispatcherAsync();

		Assert.Equal(MediaElementState.None, fixture.View.CurrentState);
		Assert.Equal(TimeSpan.Zero, fixture.View.Position);
		Assert.False(fixture.Manager.IsDisplayRequestActive);
	}
}