using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;
using Windows.Media.Playback;

namespace CommunityToolkit.Maui.DeviceTests;

// The tests that raise native events register their own handler on the same event after the manager's: WinRT raises handlers in subscription
// order, so when the test's handler ran, the manager's handler (if it were still subscribed) already ran for the same event, and the
// MediaElement can be inspected without waiting for something that never happens.
[Category(TestCategory.MediaManagerWindows)]
[Collection(TestCollections.DisableParallelization)]
public class MediaManagerDisposeWindowsTests : WinMediaTestBase
{
	static TaskCompletionSource NewSignal() => new(TaskCreationOptions.RunContinuationsAsynchronously);

	static Task WaitAsync(TaskCompletionSource signal) => signal.Task.WaitAsync(TimeSpan.FromMilliseconds(decodeTimeoutMilliseconds));

	[Fact]
	public async Task DisposeDetachesEveryNativeEventHandler()
	{
		SetupWindowsBuilder();
		await RequireVideoRenderingAsync();

		var fixture = await CreateManagerAsync();
		var view = fixture.View;
		var player = fixture.Player!;
		var session = player.PlaybackSession;

		var viewOpened = 0;
		var viewEnded = 0;
		var viewFailed = 0;
		var viewSeekCompleted = 0;
		var viewStates = new System.Collections.Concurrent.ConcurrentQueue<MediaElementState>();
		view.MediaOpened += (_, _) => Interlocked.Increment(ref viewOpened);
		view.MediaEnded += (_, _) => Interlocked.Increment(ref viewEnded);
		view.MediaFailed += (_, _) => Interlocked.Increment(ref viewFailed);
		view.SeekCompleted += (_, _) => Interlocked.Increment(ref viewSeekCompleted);
		view.StateChanged += (_, e) => viewStates.Enqueue(e.NewState);

		var opened = NewSignal();
		var sized = NewSignal();
		var rateChanged = NewSignal();
		var seeked = NewSignal();
		var volumeChanged = NewSignal();
		var muteChanged = NewSignal();
		var playing = NewSignal();
		var ended = NewSignal();
		var failed = NewSignal();
		player.MediaOpened += (_, _) => opened.TrySetResult();
		session.NaturalVideoSizeChanged += (_, _) => sized.TrySetResult();
		session.PlaybackRateChanged += (_, _) => rateChanged.TrySetResult();
		session.SeekCompleted += (_, _) => seeked.TrySetResult();
		player.VolumeChanged += (_, _) => volumeChanged.TrySetResult();
		player.IsMutedChanged += (_, _) => muteChanged.TrySetResult();
		session.PlaybackStateChanged += (s, _) =>
		{
			if (s.PlaybackState is MediaPlaybackState.Playing)
			{
				playing.TrySetResult();
			}
		};
		player.MediaEnded += (_, _) => ended.TrySetResult();
		player.MediaFailed += (_, _) => failed.TrySetResult();

		await InvokeOnMainThreadAsync(() => fixture.Manager.Dispose());

		await InvokeOnMainThreadAsync(() => player.SetUriSource(WinMediaAssets.VideoUri));
		await WaitAsync(opened);
		await WaitAsync(sized);
		session.PlaybackRate = 2;
		await WaitAsync(rateChanged);
		session.Position = TimeSpan.FromSeconds(0.5);
		await WaitAsync(seeked);
		player.Volume = 0.25;
		await WaitAsync(volumeChanged);
		player.IsMuted = true;
		await WaitAsync(muteChanged);
		player.Play();
		await WaitAsync(playing);
		await WaitAsync(ended);

		await InvokeOnMainThreadAsync(() => player.SetUriSource(WinMediaAssets.CorruptUri));
		await WaitAsync(failed);

		Assert.Equal(0, viewOpened);
		Assert.Equal(0, viewEnded);
		Assert.Equal(0, viewFailed);
		Assert.Equal(0, viewSeekCompleted);
		Assert.Empty(viewStates);
		Assert.Equal(MediaElementState.None, view.CurrentState);
		Assert.Equal(0, view.MediaWidth);
		Assert.Equal(0, view.MediaHeight);
		Assert.Equal(TimeSpan.Zero, view.Duration);
		Assert.Equal(1.0, view.Speed);
		Assert.Equal(1.0, view.Volume);
		Assert.False(view.ShouldMute);
	}

	[Fact]
	public async Task DisposeStopsTheNativeVolumeAndMuteFromFlowingBack()
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();

		var fixture = await CreateManagerAsync();
		var volumeChanged = NewSignal();
		var muteChanged = NewSignal();
		fixture.Player!.VolumeChanged += (_, _) => volumeChanged.TrySetResult();
		fixture.Player.IsMutedChanged += (_, _) => muteChanged.TrySetResult();
		await InvokeOnMainThreadAsync(() => fixture.Manager.Dispose());

		fixture.Player.Volume = 0.25;
		await WaitAsync(volumeChanged);
		fixture.Player.IsMuted = true;
		await WaitAsync(muteChanged);

		Assert.Equal(1.0, fixture.View.Volume);
		Assert.False(fixture.View.ShouldMute);
	}

	[Fact]
	public async Task DisposeWithDisposingFalseDoesNothing()
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
		var volumeChanged = NewSignal();
		fixture.Player!.VolumeChanged += (_, _) => volumeChanged.TrySetResult();

		// The finalizer path must not touch other managed objects, so the display request and the event handlers stay.
		await InvokeOnMainThreadAsync(() =>
		{
			fixture.Manager.CallDispose(false);
			AssertDisplayRequest(fixture.Manager, active: true);
		});
		fixture.Player.Volume = 0.25;
		await WaitAsync(volumeChanged);

		await WaitUntilAsync(() => Math.Abs(fixture.View.Volume - 0.25) < 0.001, "The native volume no longer flows back after Dispose(false).");
	}

	[Fact]
	public async Task DisposeReleasesAnActiveDisplayRequest()
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();
		await RequireDisplayRequestCountingAsync();

		var fixture = await CreateManagerAsync(view => view.ShouldKeepScreenOn = true);

		await InvokeOnMainThreadAsync(() =>
		{
			fixture.Manager.Play();
			AssertDisplayRequest(fixture.Manager, active: true);

			fixture.Manager.Dispose();

			AssertDisplayRequest(fixture.Manager, active: false);
		});
	}

	[Fact]
	public async Task DisposeWithoutADisplayRequestLeavesTheFlagCleared()
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();
		await RequireDisplayRequestCountingAsync();

		var fixture = await CreateManagerAsync();

		await InvokeOnMainThreadAsync(() =>
		{
			fixture.Manager.Dispose();

			// Nothing was requested, so nothing may be released: RequestRelease without a request would throw.
			AssertDisplayRequest(fixture.Manager, active: false);
		});
	}

	[Fact]
	public async Task DisposeTwiceDoesNotThrow()
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();

		var fixture = await CreateManagerAsync();

		await InvokeOnMainThreadAsync(() =>
		{
			fixture.Manager.Dispose();
			fixture.Manager.Dispose();
		});
	}

	[Fact]
	public async Task DisposeWithoutPlatformViewDoesNotThrow()
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();

		var fixture = await CreateManagerAsync(createPlatformView: false);

		await InvokeOnMainThreadAsync(() => fixture.Manager.Dispose());
	}

	[Fact]
	public async Task DisposeAfterTheViewReleasedThePlayerDoesNotThrow()
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();

		var fixture = await CreateManagerAsync();

		await InvokeOnMainThreadAsync(() =>
		{
			// MauiMediaElement.Dispose releases the player this way.
			fixture.Element!.SetMediaPlayer(null);

			fixture.Manager.Dispose();
		});
	}
}