using AVFoundation;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Views;
using CommunityToolkit.Maui.DeviceTests.Runners;
using CommunityToolkit.Maui.Views;
using CoreMedia;
using Foundation;
using MediaPlayer;

namespace CommunityToolkit.Maui.DeviceTests;

[Category(TestCategory.MediaElementApple)]
[Collection(TestCollections.DisableParallelization)]
public class MediaManagerPlaybackAppleTests : AppleMediaElementTestBase
{
	static readonly TimeSpan nearTheEnd = TimeSpan.FromMilliseconds(1700);

	AppleMediaManager CreateManager(MediaElement mediaElement) => new(MauiContext, mediaElement, TestDispatcher.Current);

	[Fact]
	public async Task PlayStartsNativePlayback()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();

		await HostAsync(mediaElement, async handler =>
		{
			var player = handler.Manager!.NativePlayer!;
			await LoadAsync(mediaElement, handler, ResourceSource(VideoResource));
			Assert.Equal(0f, player.Rate);

			mediaElement.Play();

			Assert.Equal(1f, player.Rate);
			await WaitUntilAsync(() => player.TimeControlStatus is AVPlayerTimeControlStatus.Playing && mediaElement.CurrentState is MediaElementState.Playing, "The player did not report that it is playing.");
			await WaitUntilAsync(() => player.CurrentTime.Seconds > 0.2, "The playhead did not advance.");
		});
	}

	// As on Android, Play() at the end of the media starts it again from its start without reporting another end.
	[Fact]
	public async Task PlayAtTheEndOfTheMediaPlaysItAgainFromTheStart()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();
		var events = new MediaElementEvents(mediaElement);

		await HostAsync(mediaElement, async handler =>
		{
			var player = handler.Manager!.NativePlayer!;
			await LoadAsync(mediaElement, handler, ResourceSource(VideoResource));
			await mediaElement.SeekTo(nearTheEnd);
			mediaElement.Play();
			await WaitUntilAsync(() => events.Ended > 0, "The media did not play to its end.");
			Assert.True(player.CurrentTime == player.CurrentItem!.Duration, "The playhead should be at the end of the item.");

			mediaElement.Play();

			Assert.Equal(1f, player.Rate);
			await WaitUntilAsync(() => player.CurrentTime.Seconds is > 0.2 and < 1.0 && mediaElement.CurrentState is MediaElementState.Playing, "The media did not play again from its start.");
			Assert.Equal(1, events.Ended);
		});
	}

	[Fact]
	public async Task PlayWithoutSourceKeepsTheStateUnchanged()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();

		await HostAsync(mediaElement, async handler =>
		{
			var player = handler.Manager!.NativePlayer!;
			await handler.Manager.TakeSourceUpdates();
			var stateBefore = mediaElement.CurrentState;

			mediaElement.Play();
			mediaElement.Pause();

			Assert.Null(player.CurrentItem);
			Assert.Equal(stateBefore, mediaElement.CurrentState);
		});
	}

	[Fact]
	public async Task PlayAndPauseWithoutPlayerDoNothing()
	{
		SetupApple();

		await InvokeOnMainThreadAsync(() =>
		{
			using var manager = CreateManager(CreateMediaElement());

			Assert.Null(Record.Exception(manager.Play));
			Assert.Null(Record.Exception(manager.Pause));

			manager.NativePlayerItem = new AVPlayerItem(AVAsset.FromUrl(NSUrl.FromFilename(BundlePath("video", "mp4"))));
			Assert.Null(Record.Exception(manager.Play));
			Assert.Null(Record.Exception(manager.Pause));
		});
	}

	[Fact]
	public async Task PauseStopsNativePlayback()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();

		await HostAsync(mediaElement, async handler =>
		{
			var player = handler.Manager!.NativePlayer!;
			await LoadAsync(mediaElement, handler, ResourceSource(VideoResource));
			mediaElement.Play();
			await WaitUntilAsync(() => mediaElement.CurrentState is MediaElementState.Playing, "The player did not start playing.");

			mediaElement.Pause();

			Assert.Equal(0f, player.Rate);
			await WaitUntilAsync(() => mediaElement.CurrentState is MediaElementState.Paused, "The player did not report that it is paused.");
		});
	}

	[Fact]
	public async Task StopRewindsToTheStartAndPausesThePlayer()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();
		var events = new MediaElementEvents(mediaElement);

		await HostAsync(mediaElement, async handler =>
		{
			var player = handler.Manager!.NativePlayer!;
			await LoadAsync(mediaElement, handler, ResourceSource(VideoResource));
			mediaElement.Play();
			await WaitUntilAsync(() => player.CurrentTime.Seconds > 0.3, "The playhead did not advance.");

			mediaElement.Stop();

			Assert.Equal(0f, player.Rate);
			await WaitUntilAsync(() => player.CurrentTime.Seconds < 0.1, "The playhead did not return to the start.");
			Assert.Contains(MediaElementState.Stopped, events.States);
		});
	}

	[Fact]
	public async Task StopWhilePlayingLeavesTheStateStopped()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();

		await HostAsync(mediaElement, async handler =>
		{
			var player = handler.Manager!.NativePlayer!;
			await LoadAsync(mediaElement, handler, ResourceSource(VideoResource));
			mediaElement.Play();
			await WaitUntilAsync(() => mediaElement.CurrentState is MediaElementState.Playing, "The player did not start playing.");

			mediaElement.Stop();
			await WaitUntilAsync(() => player.TimeControlStatus is AVPlayerTimeControlStatus.Paused, "The player did not pause.");
			await InvokeOnMainThreadAsync(() => { });

			Assert.Equal(MediaElementState.Stopped, mediaElement.CurrentState);
		});
	}

	[Fact]
	public async Task StopWithoutPlayerStillReportsTheStoppedState()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();

		await InvokeOnMainThreadAsync(() =>
		{
			using var manager = CreateManager(mediaElement);
			Assert.Equal(MediaElementState.None, mediaElement.CurrentState);

			manager.Stop();

			Assert.Equal(MediaElementState.Stopped, mediaElement.CurrentState);
		});
	}

	[Fact]
	public async Task SeekToMovesThePlayheadAndRaisesSeekCompleted()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();
		var events = new MediaElementEvents(mediaElement);

		await HostAsync(mediaElement, async handler =>
		{
			var player = handler.Manager!.NativePlayer!;
			await LoadAsync(mediaElement, handler, ResourceSource(VideoResource));

			await mediaElement.SeekTo(TimeSpan.FromSeconds(1));

			Assert.Equal(1.0, player.CurrentTime.Seconds, precision: 1);
			Assert.Equal(1, events.SeekCompleted);
		});
	}

	[Fact]
	public async Task SeekToTheStartOfTheMediaSucceeds()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();

		await HostAsync(mediaElement, async handler =>
		{
			var player = handler.Manager!.NativePlayer!;
			await LoadAsync(mediaElement, handler, ResourceSource(VideoResource));
			await mediaElement.SeekTo(TimeSpan.FromSeconds(1));
			Assert.Equal(1.0, player.CurrentTime.Seconds, precision: 1);

			// The start of a seekable range is inclusive.
			await mediaElement.SeekTo(TimeSpan.Zero);

			Assert.Equal(0.0, player.CurrentTime.Seconds, precision: 1);
		});
	}

	[Fact]
	public async Task SeekToTheLastMillisecondBeforeTheEndSucceeds()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();

		await HostAsync(mediaElement, async handler =>
		{
			var player = handler.Manager!.NativePlayer!;
			await LoadAsync(mediaElement, handler, ResourceSource(VideoResource));

			// The end of a seekable range is exclusive, so 1999 ms is the last position that can be sought.
			// AVPlayer seeks to a nearby key frame (every 0.5 s in the test clip), not to the exact millisecond.
			await mediaElement.SeekTo(TimeSpan.FromMilliseconds(1999));

			Assert.InRange(player.CurrentTime.Seconds, 1.4, 2.0);
		});
	}

	[Theory]
	[InlineData(2000)]
	[InlineData(5000)]
	[InlineData(-1000)]
	public async Task SeekOutsideTheSeekableRangeEndsWhenTheTokenIsCancelled(int positionInMilliseconds)
	{
		SetupApple();
		var mediaElement = CreateMediaElement();

		await HostAsync(mediaElement, async handler =>
		{
			var manager = handler.Manager!;
			await LoadAsync(mediaElement, handler, ResourceSource(VideoResource));
			using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(300));

			// No seekable range contains the position (the end of a range is exclusive). Whatever the manager does with such a position,
			// the cancellation token must end the call.
			var seek = manager.Seek(TimeSpan.FromMilliseconds(positionInMilliseconds), cancellation.Token);
			var exception = await Record.ExceptionAsync(() => seek.WaitAsync(TimeSpan.FromSeconds(5)));

			Assert.IsNotType<TimeoutException>(exception);
		});
	}

	[Theory]
	[InlineData(2000)]
	[InlineData(5000)]
	[InlineData(-1000)]
	public async Task SeekOutsideTheSeekableRangeDoesNotWaitForever(int positionInMilliseconds)
	{
		SetupApple();
		var mediaElement = CreateMediaElement();

		await HostAsync(mediaElement, async handler =>
		{
			var manager = handler.Manager!;
			await LoadAsync(mediaElement, handler, ResourceSource(VideoResource));

			// MediaElement.SeekTo hands the manager CancellationToken.None. The call may complete or throw, but it must end.
			var seek = manager.Seek(TimeSpan.FromMilliseconds(positionInMilliseconds));
			var exception = await Record.ExceptionAsync(() => seek.WaitAsync(TimeSpan.FromSeconds(3)));

			Assert.IsNotType<TimeoutException>(exception);
		});
	}

	[Fact]
	public async Task SeekWithoutSeekableRangesDoesNotWaitForever()
	{
		SetupApple();

		await InvokeOnMainThreadAsync(async () =>
		{
			using var manager = CreateManager(CreateMediaElement());
			manager.CreatePlatformView();
			var item = new AppleFakeAVPlayerItem(AVAsset.FromUrl(NSUrl.FromFilename(BundlePath("video", "mp4")))) { SeekableTimeRangesOverride = [] };
			var player = new AppleFakeAVPlayer { StatusOverride = AVPlayerStatus.ReadyToPlay, UseCurrentItemOverride = true, CurrentItemOverride = item, SeekResult = true };
			manager.NativePlayer = player;

			var exception = await Record.ExceptionAsync(() => manager.Seek(TimeSpan.FromSeconds(1)).WaitAsync(TimeSpan.FromSeconds(3)));

			Assert.IsNotType<TimeoutException>(exception);
		});
	}

	// The Duration is the end of the media, although the half-open seekable range does not contain it.
	[Theory]
	[InlineData(2000, 2.0)]
	[InlineData(5000, 2.0)]
	[InlineData(-1000, 0.0)]
	public async Task SeekToOutsideTheMediaIsClampedToTheMedia(int positionInMilliseconds, double expectedSeconds)
	{
		SetupApple();
		var mediaElement = CreateMediaElement();
		var events = new MediaElementEvents(mediaElement);

		await HostAsync(mediaElement, async handler =>
		{
			var player = handler.Manager!.NativePlayer!;
			await LoadAsync(mediaElement, handler, ResourceSource(VideoResource));
			await mediaElement.SeekTo(TimeSpan.FromSeconds(1));
			Assert.Equal(1, events.SeekCompleted);

			await mediaElement.SeekTo(TimeSpan.FromMilliseconds(positionInMilliseconds)).WaitAsync(TimeSpan.FromSeconds(3));

			Assert.True(Math.Abs(player.CurrentTime.Seconds - expectedSeconds) < 0.05, $"The playhead is at {player.CurrentTime.Seconds} s, not at {expectedSeconds} s.");
			Assert.Equal(2, events.SeekCompleted);
		});
	}

	[Fact]
	public async Task SeekToTheDurationMovesToTheEndOfTheMedia()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();
		var events = new MediaElementEvents(mediaElement);

		await HostAsync(mediaElement, async handler =>
		{
			var player = handler.Manager!.NativePlayer!;
			await LoadAsync(mediaElement, handler, ResourceSource(VideoResource));
			Assert.True(mediaElement.Duration > TimeSpan.Zero);

			await mediaElement.SeekTo(mediaElement.Duration).WaitAsync(TimeSpan.FromSeconds(3));

			Assert.True(player.CurrentTime == player.CurrentItem!.Duration, $"The playhead is at {player.CurrentTime.Seconds} s, not at the end ({player.CurrentItem.Duration.Seconds} s).");
			Assert.Equal(1, events.SeekCompleted);
		});
	}

	// The duration of most media (29.97 fps video, 44.1 kHz audio) is not a whole number of milliseconds, and neither is their Duration:
	// SeekTo(Duration) must still reach the end of the media, not the key frame before the end (1.668 s).
	[Fact]
	public async Task SeekToTheDurationOfAMediaWhoseDurationIsNotAWholeNumberOfMillisecondsMovesToTheEnd()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();
		var events = new MediaElementEvents(mediaElement);

		await HostAsync(mediaElement, async handler =>
		{
			var player = handler.Manager!.NativePlayer!;
			await LoadAsync(mediaElement, handler, ResourceSource(NtscVideoResource));
			Assert.NotEqual(0, mediaElement.Duration.Ticks % TimeSpan.TicksPerMillisecond);

			await mediaElement.SeekTo(mediaElement.Duration).WaitAsync(TimeSpan.FromSeconds(3));

			Assert.True(player.CurrentTime == player.CurrentItem!.Duration, $"The playhead is at {player.CurrentTime.Seconds} s, not at the end ({player.CurrentItem.Duration.Seconds} s).");
			Assert.Equal(1, events.SeekCompleted);
		});
	}

	// The Duration of a media of 61061/30000 s (2035.3667 ms, which PlatformUpdatePosition converts from the duration of the item) asks the player
	// for the end of the media itself, not for the whole millisecond before it (2035 ms, which AVPlayer would take to the key frame before the end);
	// a position 1 ms before the Duration stays inside the media.
	[Theory]
	[InlineData(0, 61061, 30000)]
	[InlineData(-1, 2034, 1000)]
	public async Task SeekToTheDurationOfAMediaWhoseDurationIsNotAWholeNumberOfMillisecondsRequestsTheEnd(int millisecondsFromTheDuration, long expectedValue, int expectedTimescale)
	{
		SetupApple();
		var mediaElement = CreateMediaElement();
		var events = new MediaElementEvents(mediaElement);

		await InvokeOnMainThreadAsync(async () =>
		{
			using var manager = CreateManager(mediaElement);
			manager.CreatePlatformView();
			var end = new CMTime(61061, 30000);
			var item = new AppleFakeAVPlayerItem(AVAsset.FromUrl(NSUrl.FromFilename(BundlePath("video", "mp4"))))
			{
				DurationOverride = end,
				SeekableTimeRangesOverride = [NSValue.FromCMTimeRange(new CMTimeRange { Start = CMTime.Zero, Duration = end })]
			};
			var player = new AppleFakeAVPlayer { StatusOverride = AVPlayerStatus.ReadyToPlay, UseCurrentItemOverride = true, CurrentItemOverride = item, SeekResult = true };
			manager.NativePlayer = player;
			var duration = TimeSpan.FromSeconds(end.Seconds);

			await manager.Seek(duration + TimeSpan.FromMilliseconds(millisecondsFromTheDuration)).WaitAsync(TimeSpan.FromSeconds(5));

			var requested = Assert.Single(player.SeekRequests);
			Assert.True(requested == new CMTime(expectedValue, expectedTimescale), $"The player was asked for {requested.Value}/{requested.TimeScale} s, not for {expectedValue}/{expectedTimescale} s.");
			Assert.Equal(1, events.SeekCompleted);
		});
	}

	[Theory]
	[InlineData(1000, 1000)]
	[InlineData(5000, 5000)]
	[InlineData(-1000, 0)]
	public async Task SeekWithoutSeekableRangesClampsOnlyAPositionBeforeTheStart(int positionInMilliseconds, int expectedMilliseconds)
	{
		SetupApple();
		var mediaElement = CreateMediaElement();
		var events = new MediaElementEvents(mediaElement);

		await InvokeOnMainThreadAsync(async () =>
		{
			using var manager = CreateManager(mediaElement);
			manager.CreatePlatformView();
			var item = new AppleFakeAVPlayerItem(AVAsset.FromUrl(NSUrl.FromFilename(BundlePath("video", "mp4")))) { SeekableTimeRangesOverride = [] };
			var player = new AppleFakeAVPlayer { StatusOverride = AVPlayerStatus.ReadyToPlay, UseCurrentItemOverride = true, CurrentItemOverride = item, SeekResult = true };
			manager.NativePlayer = player;

			await manager.Seek(TimeSpan.FromMilliseconds(positionInMilliseconds)).WaitAsync(TimeSpan.FromSeconds(5));

			Assert.Equal(expectedMilliseconds / 1000.0, Assert.Single(player.SeekRequests).Seconds, precision: 3);
			Assert.Equal(1, events.SeekCompleted);
		});
	}

	[Fact]
	public async Task SeekWithOverlappingSeekableRangesSeeksOnce()
	{
		SetupApple();

		await InvokeOnMainThreadAsync(async () =>
		{
			using var manager = CreateManager(CreateMediaElement());
			manager.CreatePlatformView();
			var item = new AppleFakeAVPlayerItem(AVAsset.FromUrl(NSUrl.FromFilename(BundlePath("video", "mp4"))))
			{
				SeekableTimeRangesOverride =
				[
					NSValue.FromCMTimeRange(new CMTimeRange { Start = CMTime.Zero, Duration = CMTime.FromSeconds(2, 1000) }),
					NSValue.FromCMTimeRange(new CMTimeRange { Start = CMTime.FromSeconds(1, 1000), Duration = CMTime.FromSeconds(2, 1000) })
				]
			};
			// The player never reports the result, so only the requests count; the token ends the call.
			var player = new AppleFakeAVPlayer { StatusOverride = AVPlayerStatus.ReadyToPlay, UseCurrentItemOverride = true, CurrentItemOverride = item, HoldSeekCompletion = true };
			manager.NativePlayer = player;
			using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(300));

			await Assert.ThrowsAnyAsync<OperationCanceledException>(() => manager.Seek(TimeSpan.FromMilliseconds(1500), cancellation.Token).WaitAsync(TimeSpan.FromSeconds(5)));

			Assert.Equal(1.5, Assert.Single(player.SeekRequests).Seconds, precision: 3);
		});
	}

	[Fact]
	public async Task SeekCancelledWhileThePlayerIsSeekingThrowsOperationCanceledException()
	{
		SetupApple();

		await InvokeOnMainThreadAsync(async () =>
		{
			using var manager = CreateManager(CreateMediaElement());
			manager.CreatePlatformView();
			var item = new AppleFakeAVPlayerItem(AVAsset.FromUrl(NSUrl.FromFilename(BundlePath("video", "mp4"))))
			{
				SeekableTimeRangesOverride = [NSValue.FromCMTimeRange(new CMTimeRange { Start = CMTime.Zero, Duration = CMTime.FromSeconds(2, 1000) })]
			};
			var player = new AppleFakeAVPlayer { StatusOverride = AVPlayerStatus.ReadyToPlay, UseCurrentItemOverride = true, CurrentItemOverride = item, HoldSeekCompletion = true };
			manager.NativePlayer = player;
			using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(300));

			await Assert.ThrowsAnyAsync<OperationCanceledException>(() => manager.Seek(TimeSpan.FromSeconds(1), cancellation.Token));

			Assert.Equal(1.0, Assert.Single(player.SeekRequests).Seconds, precision: 3);
		});
	}

	[Fact]
	public async Task SeekWithACancelledTokenThrowsBeforeTouchingThePlayer()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();

		await HostAsync(mediaElement, async handler =>
		{
			var manager = handler.Manager!;
			var player = manager.NativePlayer!;
			await LoadAsync(mediaElement, handler, ResourceSource(VideoResource));
			using var cancellation = new CancellationTokenSource();
			cancellation.Cancel();

			await Assert.ThrowsAsync<OperationCanceledException>(() => manager.Seek(TimeSpan.FromSeconds(1), cancellation.Token));

			Assert.Equal(0.0, player.CurrentTime.Seconds, precision: 1);
		});
	}

	[Fact]
	public async Task SeekWithoutSourceThrowsInvalidOperationException()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();

		await HostAsync(mediaElement, async handler =>
		{
			await handler.Manager!.TakeSourceUpdates();

			var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Manager.Seek(TimeSpan.FromSeconds(1)));

			Assert.Contains(nameof(AVPlayer.CurrentItem), exception.Message);
		});
	}

	[Fact]
	public async Task SeekWithoutPlayerThrowsInvalidOperationException()
	{
		SetupApple();

		await InvokeOnMainThreadAsync(async () =>
		{
			using var manager = CreateManager(CreateMediaElement());

			var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => manager.Seek(TimeSpan.FromSeconds(1)));

			Assert.Contains(nameof(AVPlayer.CurrentItem), exception.Message);
		});
	}

	[Theory]
	[InlineData(AVPlayerStatus.Unknown)]
	[InlineData(AVPlayerStatus.Failed)]
	public async Task SeekBeforeThePlayerIsReadyThrowsInvalidOperationException(AVPlayerStatus status)
	{
		SetupApple();

		await InvokeOnMainThreadAsync(async () =>
		{
			using var manager = CreateManager(CreateMediaElement());
			manager.CreatePlatformView();
			var item = new AppleFakeAVPlayerItem(AVAsset.FromUrl(NSUrl.FromFilename(BundlePath("video", "mp4"))));
			manager.NativePlayer = new AppleFakeAVPlayer { StatusOverride = status, UseCurrentItemOverride = true, CurrentItemOverride = item };

			var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => manager.Seek(TimeSpan.FromSeconds(1)));

			Assert.Contains(nameof(AVPlayerStatus.ReadyToPlay), exception.Message);
		});
	}

	// AVPlayer reports a seek that something else interrupted (Stop(), another seek, the playback controls, a remote command, the restart of a
	// looping media) as not finished. The seek has ended all the same: it completes and raises SeekCompleted, as on Android.
	[Fact]
	public async Task SeekThatThePlayerReportsAsNotFinishedCompletesAndRaisesSeekCompleted()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();
		var events = new MediaElementEvents(mediaElement);

		await InvokeOnMainThreadAsync(async () =>
		{
			using var manager = CreateManager(mediaElement);
			manager.CreatePlatformView();
			var item = new AppleFakeAVPlayerItem(AVAsset.FromUrl(NSUrl.FromFilename(BundlePath("video", "mp4"))))
			{
				SeekableTimeRangesOverride = [NSValue.FromCMTimeRange(new CMTimeRange { Start = CMTime.Zero, Duration = CMTime.FromSeconds(2, 1000) })]
			};

			// The player reports the seek as not finished from a main-queue callback, as AVPlayer does.
			var player = new AppleFakeAVPlayer { StatusOverride = AVPlayerStatus.ReadyToPlay, UseCurrentItemOverride = true, CurrentItemOverride = item, SeekResult = false };
			manager.NativePlayer = player;

			await manager.Seek(TimeSpan.FromSeconds(1)).WaitAsync(TimeSpan.FromSeconds(5));

			Assert.Single(player.SeekRequests);
			Assert.Equal(1, events.SeekCompleted);
		});
	}

	// A second seek while the first one runs: AVPlayer reports the first one as not finished at once.
	[Fact]
	public async Task SeekInterruptedByAnotherSeekCompletesAndRaisesSeekCompleted()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();
		mediaElement.ShouldMute = true;
		var events = new MediaElementEvents(mediaElement);

		await HostAsync(mediaElement, async handler =>
		{
			var manager = handler.Manager!;
			await LoadAsync(mediaElement, handler, MediaSource.FromResource("avplayer/long.m4a"));

			// Same main-thread turn: the first seek is still running when the second one starts.
			var interrupted = manager.Seek(TimeSpan.FromSeconds(20));
			var second = manager.Seek(TimeSpan.FromSeconds(10));

			await Task.WhenAll(interrupted, second).WaitAsync(TimeSpan.FromSeconds(5));
			Assert.Equal(2, events.SeekCompleted);
			Assert.Equal(10, manager.NativePlayer!.CurrentTime.Seconds, precision: 0);
		});
	}

	// A position in the gap between two ranges is passed on as it is: only a position before the first range or after the last one is clamped.
	[Theory]
	[InlineData(5500, 5500)]
	[InlineData(500, 500)]
	[InlineData(3000, 3000)]
	[InlineData(7000, 6000)]
	[InlineData(-1000, 0)]
	public async Task SeekIsClampedToTheSeekableTimeRanges(int positionInMilliseconds, int expectedMilliseconds)
	{
		SetupApple();
		var mediaElement = CreateMediaElement();
		var events = new MediaElementEvents(mediaElement);

		await InvokeOnMainThreadAsync(async () =>
		{
			using var manager = CreateManager(mediaElement);
			manager.CreatePlatformView();
			var item = new AppleFakeAVPlayerItem(AVAsset.FromUrl(NSUrl.FromFilename(BundlePath("video", "mp4"))))
			{
				SeekableTimeRangesOverride =
				[
					NSValue.FromCMTimeRange(new CMTimeRange { Start = CMTime.Zero, Duration = CMTime.FromSeconds(1, 1000) }),
					NSValue.FromCMTimeRange(new CMTimeRange { Start = CMTime.FromSeconds(5, 1000), Duration = CMTime.FromSeconds(1, 1000) })
				]
			};
			var player = new AppleFakeAVPlayer { StatusOverride = AVPlayerStatus.ReadyToPlay, UseCurrentItemOverride = true, CurrentItemOverride = item, SeekResult = true };
			manager.NativePlayer = player;

			await manager.Seek(TimeSpan.FromMilliseconds(positionInMilliseconds)).WaitAsync(TimeSpan.FromSeconds(5));

			Assert.Equal(expectedMilliseconds / 1000.0, Assert.Single(player.SeekRequests).Seconds, precision: 3);
			Assert.Equal(1, events.SeekCompleted);
		});
	}

	// A live stream has an indefinite duration and a seekable window that starts after 0; the end of the window is the live edge.
	[Theory]
	[InlineData(5000, 10000)]
	[InlineData(15000, 15000)]
	[InlineData(25000, 20000)]
	public async Task SeekInALiveStreamIsClampedToItsSeekableWindow(int positionInMilliseconds, int expectedMilliseconds)
	{
		SetupApple();

		await InvokeOnMainThreadAsync(async () =>
		{
			using var manager = CreateManager(CreateMediaElement());
			manager.CreatePlatformView();
			var item = new AppleFakeAVPlayerItem(AVAsset.FromUrl(NSUrl.FromFilename(BundlePath("video", "mp4"))))
			{
				DurationOverride = CMTime.Indefinite,
				SeekableTimeRangesOverride = [NSValue.FromCMTimeRange(new CMTimeRange { Start = CMTime.FromSeconds(10, 1000), Duration = CMTime.FromSeconds(10, 1000) })]
			};
			var player = new AppleFakeAVPlayer { StatusOverride = AVPlayerStatus.ReadyToPlay, UseCurrentItemOverride = true, CurrentItemOverride = item, SeekResult = true };
			manager.NativePlayer = player;

			await manager.Seek(TimeSpan.FromMilliseconds(positionInMilliseconds)).WaitAsync(TimeSpan.FromSeconds(5));

			Assert.Equal(expectedMilliseconds / 1000.0, Assert.Single(player.SeekRequests).Seconds, precision: 3);
		});
	}

	[Fact]
	public async Task StopWhileASeekIsPendingDoesNotCrashTheApp()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();

		await HostAsync(mediaElement, async handler =>
		{
			var manager = handler.Manager!;
			await LoadAsync(mediaElement, handler, MediaSource.FromResource("avplayer/long.m4a"));
			var seek = manager.Seek(TimeSpan.FromSeconds(20));

			mediaElement.Stop();

			await WaitUntilAsync(() => seek.IsCompleted, "The pending seek never finished.");
			Assert.Equal(MediaElementState.Stopped, mediaElement.CurrentState);
		});
	}

	[Fact]
	public async Task PlayingToTheEndRaisesMediaEndedAndStopsThePlayer()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();
		var events = new MediaElementEvents(mediaElement);

		await HostAsync(mediaElement, async handler =>
		{
			var player = handler.Manager!.NativePlayer!;
			await LoadAsync(mediaElement, handler, ResourceSource(VideoResource));
			await mediaElement.SeekTo(nearTheEnd);

			mediaElement.Play();
			await WaitUntilAsync(() => events.Ended > 0 && mediaElement.CurrentState is MediaElementState.Stopped, "MediaEnded was not raised.");

			Assert.Equal(1, events.Ended);
			Assert.Equal(0f, player.Rate);
		});
	}

	[Fact]
	public async Task PlayingToTheEndRestartsTheMediaWhenShouldLoopPlayback()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();
		mediaElement.ShouldLoopPlayback = true;
		var events = new MediaElementEvents(mediaElement);

		await HostAsync(mediaElement, async handler =>
		{
			var player = handler.Manager!.NativePlayer!;
			await LoadAsync(mediaElement, handler, ResourceSource(VideoResource));
			await mediaElement.SeekTo(nearTheEnd);

			mediaElement.Play();

			await WaitUntilAsync(() => player.CurrentTime.Seconds < 1.0 && player.Rate > 0, "The media did not loop.");
			Assert.Equal(0, events.Ended);

			mediaElement.Pause();
		});
	}

	[Fact]
	public async Task PlayedToEndNotificationOfAnotherItemIsIgnored()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();
		var events = new MediaElementEvents(mediaElement);

		await HostAsync(mediaElement, async handler =>
		{
			var player = handler.Manager!.NativePlayer!;
			await LoadAsync(mediaElement, handler, ResourceSource(VideoResource));
			var otherItem = new AVPlayerItem(AVAsset.FromUrl(NSUrl.FromFilename(BundlePath("video", "mp4"))));

			NSNotificationCenter.DefaultCenter.PostNotificationName(AVPlayerItem.DidPlayToEndTimeNotification, otherItem);
			NSNotificationCenter.DefaultCenter.PostNotificationName(AVPlayerItem.DidPlayToEndTimeNotification, player.CurrentItem);

			await WaitUntilAsync(() => events.Ended > 0, "The notification of the current item was not handled.");
			await InvokeOnMainThreadAsync(() => { });
			Assert.Equal(1, events.Ended);
		});
	}

	[Fact]
	public async Task PlayedToEndNotificationIsIgnoredWhileThereIsNoPlayerViewController()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();
		var events = new MediaElementEvents(mediaElement);

		await HostAsync(mediaElement, async handler =>
		{
			var manager = handler.Manager!;
			await LoadAsync(mediaElement, handler, ResourceSource(VideoResource));
			var controller = manager.NativePlayerViewController;
			manager.NativePlayerViewController = null;

			// Without a player view controller the notification cannot be matched to the current item.
			NSNotificationCenter.DefaultCenter.PostNotificationName(AVPlayerItem.DidPlayToEndTimeNotification, manager.NativePlayerItem);
			await InvokeOnMainThreadAsync(() => { });

			manager.NativePlayerViewController = controller;
			Assert.Equal(0, events.Ended);
		});
	}

	[Fact]
	public async Task LoopingWithoutPlayerViewControllerStillRestartsPlayback()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();
		mediaElement.ShouldLoopPlayback = true;

		await HostAsync(mediaElement, async handler =>
		{
			var manager = handler.Manager!;
			var player = manager.NativePlayer!;
			await LoadAsync(mediaElement, handler, ResourceSource(VideoResource));
			var controller = manager.NativePlayerViewController;
			manager.NativePlayerViewController = null;
			Assert.Equal(0f, player.Rate);

			// AVFoundation names the item in this notification; one without an object matches a manager without player view controller.
			NSNotificationCenter.DefaultCenter.PostNotificationName(AVPlayerItem.DidPlayToEndTimeNotification, null);

			Assert.Equal(1f, player.Rate);
			manager.NativePlayerViewController = controller;
			mediaElement.Pause();
		});
	}

	[Fact]
	public async Task LoopingWhileThePlayerViewControllerHasNoPlayerRestartsPlaybackWithoutRewinding()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();
		mediaElement.ShouldLoopPlayback = true;

		await HostAsync(mediaElement, async handler =>
		{
			var manager = handler.Manager!;
			var player = manager.NativePlayer!;
			await LoadAsync(mediaElement, handler, ResourceSource(VideoResource));
			await mediaElement.SeekTo(TimeSpan.FromSeconds(1));
			var controller = manager.NativePlayerViewController!;
			controller.Player = null;

			try
			{
				// Without a player in the controller there is no current item to compare with (a notification without an object matches)
				// and nothing to rewind through the controller: the manager's own player is started where it is.
				NSNotificationCenter.DefaultCenter.PostNotificationName(AVPlayerItem.DidPlayToEndTimeNotification, null);

				Assert.Equal(1f, player.Rate);
				Assert.InRange(player.CurrentTime.Seconds, 0.9, 1.5);
			}
			finally
			{
				player.Pause();
				controller.Player = player;
			}
		});
	}

	[Fact]
	public async Task PlayedToEndNotificationIsIgnoredWhileTheManagerHasNoPlayer()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();
		var events = new MediaElementEvents(mediaElement);

		await HostAsync(mediaElement, async handler =>
		{
			var manager = handler.Manager!;
			var player = manager.NativePlayer!;
			await LoadAsync(mediaElement, handler, ResourceSource(VideoResource));
			var currentItem = player.CurrentItem;

			manager.NativePlayer = null;
			NSNotificationCenter.DefaultCenter.PostNotificationName(AVPlayerItem.DidPlayToEndTimeNotification, currentItem);
			manager.NativePlayer = player;
			NSNotificationCenter.DefaultCenter.PostNotificationName(AVPlayerItem.DidPlayToEndTimeNotification, currentItem);

			await WaitUntilAsync(() => events.Ended > 0, "The notification was not handled once the player was back.");
			await InvokeOnMainThreadAsync(() => { });
			Assert.Equal(1, events.Ended);
		});
	}
}