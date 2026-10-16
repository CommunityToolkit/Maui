using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;
using Microsoft.Extensions.Logging;
using Windows.Media.Playback;

namespace CommunityToolkit.Maui.DeviceTests;

[Category(TestCategory.MediaManagerPlaybackWindows)]
[Collection(TestCollections.DisableParallelization)]
public class MediaManagerPlaybackWindowsTests : WinMediaTestBase
{
	static bool IsNear(TimeSpan actual, double expectedSeconds, double toleranceSeconds = 0.35) =>
		Math.Abs(actual.TotalSeconds - expectedSeconds) <= toleranceSeconds;

	static MediaSource VideoSource => MediaSource.FromFile(WinMediaAssets.VideoPath);

	// Loops by default so that a slow machine cannot run into the end of the 2 s clip while a test waits.
	async Task<WinMediaManagerFixture> OpenVideoAsync(bool loop = true, Action<MediaElement>? configure = null, bool? dispatchRequired = null, bool recordDispatches = false)
	{
		await RequireDecoderAsync();

		var fixture = await CreateManagerAsync(view =>
		{
			view.ShouldLoopPlayback = loop;
			configure?.Invoke(view);
		}, dispatchRequired: dispatchRequired, recordDispatches: recordDispatches);

		await InvokeOnMainThreadAsync(() => fixture.Manager.UpdateShouldLoopPlayback());
		await OpenAsync(fixture.View, VideoSource, fixture.Manager);
		return fixture;
	}

	static async Task PlayAsync(WinMediaManagerFixture fixture)
	{
		await RequireVideoRenderingAsync();
		await InvokeOnMainThreadAsync(() => fixture.Manager.Play());
		await WaitUntilAsync(() => fixture.Player!.PlaybackSession.PlaybackState is MediaPlaybackState.Playing, "The native player never started playing.", decodeTimeoutMilliseconds);
	}

	[Fact]
	public async Task OpenedMediaReportsDurationNaturalSizeAndPausedState()
	{
		SetupWindowsBuilder();
		await RequireVideoRenderingAsync();

		var fixture = await OpenVideoAsync();

		await WaitUntilAsync(() => fixture.View.Duration > TimeSpan.Zero, "Duration was never set after MediaOpened.");
		Assert.True(IsNear(fixture.View.Duration, WinMediaAssets.VideoDuration.TotalSeconds, 0.25), $"Duration was {fixture.View.Duration}.");
		Assert.True(IsNear(fixture.Player!.PlaybackSession.NaturalDuration, WinMediaAssets.VideoDuration.TotalSeconds, 0.25));

		await WaitUntilAsync(() => fixture.View.MediaWidth == WinMediaAssets.VideoWidth && fixture.View.MediaHeight == WinMediaAssets.VideoHeight, "MediaWidth/MediaHeight did not follow the natural video size.");
		Assert.Equal((uint)WinMediaAssets.VideoWidth, fixture.Player.PlaybackSession.NaturalVideoWidth);
		Assert.Equal((uint)WinMediaAssets.VideoHeight, fixture.Player.PlaybackSession.NaturalVideoHeight);

		await WaitUntilAsync(() => fixture.View.CurrentState is MediaElementState.Paused, "The state did not become Paused after the media opened without AutoPlay.");
	}

	[Fact]
	public async Task PlayAndPauseDriveTheNativePlayerAndReportTheState()
	{
		SetupWindowsBuilder();

		var fixture = await OpenVideoAsync();

		await PlayAsync(fixture);
		await WaitUntilAsync(() => fixture.View.CurrentState is MediaElementState.Playing, "CurrentState never became Playing.");

		await InvokeOnMainThreadAsync(() => fixture.Manager.Pause());
		await WaitUntilAsync(() => fixture.Player!.PlaybackSession.PlaybackState is MediaPlaybackState.Paused, "The native player never paused.", decodeTimeoutMilliseconds);
		await WaitUntilAsync(() => fixture.View.CurrentState is MediaElementState.Paused, "CurrentState never became Paused.");
	}

	[Fact]
	public async Task StateEventsFollowTheNativePlaybackStates()
	{
		SetupWindowsBuilder();
		await RequireDecoderAsync();

		var states = new ConcurrentQueue<MediaElementState>();
		var fixture = await CreateManagerAsync(view =>
		{
			view.ShouldLoopPlayback = true;
			view.StateChanged += (_, e) => states.Enqueue(e.NewState);
		});
		await InvokeOnMainThreadAsync(() => fixture.Manager.UpdateShouldLoopPlayback());

		await OpenAsync(fixture.View, VideoSource, fixture.Manager);
		await WaitUntilAsync(() => fixture.View.CurrentState is MediaElementState.Paused, "Paused was never reported after opening.");
		await PlayAsync(fixture);
		await InvokeOnMainThreadAsync(() => fixture.Manager.Pause());
		await WaitUntilAsync(() => fixture.View.CurrentState is MediaElementState.Paused && states.Count(state => state is MediaElementState.Paused) >= 2, "Paused was not reported after pausing.");

		await SetSourceAsync(fixture.View, null, fixture.Manager);
		await WaitUntilAsync(() => fixture.View.CurrentState is MediaElementState.None, "None was not reported after the source was cleared.");

		// Opening is not in the sequence: the platform reports the Opening state of a local file so briefly that the manager may already read Paused
		// when it handles that event, so Opening has its own test with a source that stays in it
		// (MediaManagerNetworkWindowsTests.OpeningASourceThatNeverAnswersReportsTheOpeningState). The final None comes from PlatformUpdateSource itself,
		// so it does not prove the default arm of the native-state mapping (NativeNoneStateIsReportedAsNone does).
		AssertInOrder(states.ToArray(), MediaElementState.Paused, MediaElementState.Playing, MediaElementState.Paused, MediaElementState.None);
	}

	// The default arm of OnPlaybackSessionPlaybackStateChanged's mapping (MediaPlaybackState.None) cannot be shown by clearing the source through the
	// manager, because PlatformUpdateSource reports None on its own. So the manager's handler gets the real session of a player that has no media,
	// while the MediaElement still reports Playing.
	[Fact]
	public async Task NativeNoneStateIsReportedAsNone()
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();

		var fixture = await CreateManagerAsync();

		await InvokeOnMainThreadAsync(() =>
		{
			var session = fixture.Player!.PlaybackSession;
			Assert.Equal(MediaPlaybackState.None, session.PlaybackState);
			((IMediaElement)fixture.View).CurrentStateChanged(MediaElementState.Playing);

			WinMediaReflection.Invoke(fixture.Manager, "OnPlaybackSessionPlaybackStateChanged", session, null);

			Assert.Equal(MediaElementState.None, fixture.View.CurrentState);
		});
	}

	static void AssertInOrder(MediaElementState[] actual, params MediaElementState[] expected)
	{
		var index = 0;
		foreach (var state in actual)
		{
			if (index < expected.Length && state == expected[index])
			{
				index++;
			}
		}

		Assert.True(index == expected.Length, $"Expected the state sequence [{string.Join(", ", expected)}] as a subsequence of [{string.Join(", ", actual)}].");
	}

	[Fact]
	public async Task StopRewindsAndPausesPlayingMediaAndReportsStopped()
	{
		SetupWindowsBuilder();

		var fixture = await OpenVideoAsync();
		await InvokeOnMainThreadAsync(async () => await fixture.Manager.Seek(TimeSpan.FromSeconds(1)));
		await PlayAsync(fixture);

		MediaElementState stateRightAfterStop = default;
		await InvokeOnMainThreadAsync(() =>
		{
			fixture.Manager.Stop();
			stateRightAfterStop = fixture.View.CurrentState;
		});

		Assert.Equal(MediaElementState.Stopped, stateRightAfterStop);
		await WaitUntilAsync(() => fixture.Player!.PlaybackSession.PlaybackState is MediaPlaybackState.Paused, "Stop did not pause the native player.", decodeTimeoutMilliseconds);
		await WaitUntilAsync(() => fixture.Player!.PlaybackSession.Position < TimeSpan.FromSeconds(0.3), "Stop did not rewind the native player.", decodeTimeoutMilliseconds);
	}

	[Fact]
	public async Task StopKeepsReportingStoppedAfterTheNativePauseEventArrives()
	{
		SetupWindowsBuilder();

		var fixture = await OpenVideoAsync();
		await PlayAsync(fixture);

		// The manager reported the native Playing state, so its handler for that state change cannot run after Stop() any more. A Stop() that
		// comes before that handler ran is covered by StopRightAfterPlayKeepsReportingStoppedOnceTheNativePlayerSettles.
		await WaitUntilAsync(() => fixture.View.CurrentState is MediaElementState.Playing, "CurrentState never became Playing.");

		// Subscribed after the manager's own handler: when this runs, the manager has already handled the same native event.
		var nativePaused = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		fixture.Player!.PlaybackSession.PlaybackStateChanged += (session, _) =>
		{
			if (session.PlaybackState is MediaPlaybackState.Paused)
			{
				nativePaused.TrySetResult();
			}
		};

		await InvokeOnMainThreadAsync(() => fixture.Manager.Stop());
		await nativePaused.Task.WaitAsync(TimeSpan.FromMilliseconds(decodeTimeoutMilliseconds));

		Assert.Equal(MediaElementState.Stopped, fixture.View.CurrentState);
	}

	// Play() and Stop() in one UI-thread turn, so Stop() reports Stopped before the manager handled any native state of that Play(). Which of two
	// things happens depends on timing: the pause of Stop() cancels a play that the player has not started yet and the player then reports no state
	// at all, or the player has started it and a handler that runs before that pause has taken effect reads a stale Playing (PlaybackStateChanged
	// carries no state). Neither that Playing nor the Paused that Stop() causes may overwrite Stopped.
	// PlayingReadAfterTheStopOfAPlayingPlayerIsStaleUntilThePlayerReportsThePause hits the second case every time.
	[Fact]
	public async Task StopRightAfterPlayKeepsReportingStoppedOnceTheNativePlayerSettles()
	{
		SetupWindowsBuilder();

		var fixture = await OpenVideoAsync();
		await WaitUntilAsync(() => fixture.View.CurrentState is MediaElementState.Paused, "Paused was never reported after opening.");
		var session = fixture.Player!.PlaybackSession;

		// Registered after the manager's handler: once it ran for an event, the manager's handler already ran for the same event.
		var pausedEventsHandled = 0;
		session.PlaybackStateChanged += (sender, _) =>
		{
			if (sender.PlaybackState is MediaPlaybackState.Paused)
			{
				Interlocked.Increment(ref pausedEventsHandled);
			}
		};

		await InvokeOnMainThreadAsync(() =>
		{
			fixture.Manager.Play();
			fixture.Manager.Stop();
		});

		// The player settles in Paused. The wait is not asserted: if 5 s pass without the manager handling that event, the player never left Paused,
		// and no late native state can overwrite Stopped either.
		await AssertHelpers.Wait(() => Volatile.Read(ref pausedEventsHandled) > 0 && session.PlaybackState is MediaPlaybackState.Paused, 5000, 50);
		await FlushDispatcherAsync();

		Assert.Equal(MediaElementState.Stopped, fixture.View.CurrentState);
	}

	// The second case of the test above, every time: Stop() of a player that plays, before the manager handled the native states that follow. The
	// manager's handler gets them within the UI-thread turn of Stop(), as the sessions of two plain players (one playing, one paused). Its own player
	// plays the clip and pauses for real on Stop(), and its state changes arrive on other threads, possibly within that turn, so the handler is
	// detached from it first.
	[Fact]
	public async Task PlayingReadAfterTheStopOfAPlayingPlayerIsStaleUntilThePlayerReportsThePause()
	{
		SetupWindowsBuilder();
		var fixture = await OpenVideoAsync();
		await WaitUntilAsync(() => fixture.View.CurrentState is MediaElementState.Paused, "Paused was never reported after opening.");
		DetachNativeStateHandler(fixture);
		await PlayAsync(fixture);
		var timeout = TimeSpan.FromMilliseconds(decodeTimeoutMilliseconds);
		var (playing, playingFailure) = await WinMediaPlainPlayer.OpenAsync(player => player.SetUriSource(WinMediaAssets.VideoUri), timeout);
		await using (playing)
		{
			var (paused, pausedFailure) = await WinMediaPlainPlayer.OpenAsync(player => player.SetUriSource(WinMediaAssets.VideoUri), timeout);
			await using (paused)
			{
				Assert.True(playingFailure is null && pausedFailure is null, $"A plain MediaPlayer did not open the clip: {playingFailure ?? pausedFailure}");
				playing.Player.IsLoopingEnabled = true;
				playing.Player.Play();
				await WaitUntilAsync(() => playing.Player.PlaybackSession.PlaybackState is MediaPlaybackState.Playing, "The first plain player did not start playing.");
				await WaitUntilAsync(() => paused.Player.PlaybackSession.PlaybackState is MediaPlaybackState.Paused, "The second plain player did not settle in Paused after it opened.");

				await InvokeOnMainThreadAsync(() =>
				{
					fixture.Manager.Stop();
					Assert.Equal(MediaElementState.Stopped, fixture.View.CurrentState);

					// Run before the pause of Stop() has taken effect, so this Playing is stale.
					WinMediaReflection.Invoke(fixture.Manager, "OnPlaybackSessionPlaybackStateChanged", playing.Player.PlaybackSession, null);
					Assert.Equal(MediaElementState.Stopped, fixture.View.CurrentState);

					// The pause of Stop() has taken effect: Paused does not overwrite Stopped either.
					WinMediaReflection.Invoke(fixture.Manager, "OnPlaybackSessionPlaybackStateChanged", paused.Player.PlaybackSession, null);
					Assert.Equal(MediaElementState.Stopped, fixture.View.CurrentState);

					// The stop has settled: a Playing from now on is a play that did not go through Play() (the transport controls), and it is reported.
					WinMediaReflection.Invoke(fixture.Manager, "OnPlaybackSessionPlaybackStateChanged", playing.Player.PlaybackSession, null);
					Assert.Equal(MediaElementState.Playing, fixture.View.CurrentState);
				});
			}
		}
	}

	// The pause cancels a play that the player has not started yet, and the player then reports no state at all. So there is no pause for the stop to
	// wait for: the next play that does not go through Play() is reported, also after a second Stop() of the paused player, or after a Stop() that
	// follows the Pause(). The native player is played directly, which is what the play button of the transport controls does.
	[Theory]
	[InlineData("Play() and Stop()")]
	[InlineData("Play() and Stop(), then Stop()")]
	[InlineData("Play() and Pause(), then Stop()")]
	public async Task NativePlayAfterPlayAndAPauseInOneTurnIsReported(string sequence)
	{
		SetupWindowsBuilder();
		// The native player has to play the clip, which a machine that renders no video does not do (it stays Paused at 0 s).
		await RequireVideoRenderingAsync();
		var fixture = await OpenVideoAsync();
		await WaitUntilAsync(() => fixture.View.CurrentState is MediaElementState.Paused, "Paused was never reported after opening.");

		await InvokeOnMainThreadAsync(() =>
		{
			fixture.Manager.Play();
			if (sequence.StartsWith("Play() and Stop()", StringComparison.Ordinal))
			{
				fixture.Manager.Stop();
			}
			else
			{
				fixture.Manager.Pause();
			}
		});
		await WaitUntilTheOwnPlayerRestsInPausedAsync(fixture);
		await FlushDispatcherAsync();

		if (sequence.EndsWith("then Stop()", StringComparison.Ordinal))
		{
			await InvokeOnMainThreadAsync(() => fixture.Manager.Stop());
		}

		Assert.Equal(MediaElementState.Stopped, fixture.View.CurrentState);
		fixture.Player!.Play();

		await WaitUntilAsync(() => fixture.View.CurrentState is MediaElementState.Playing, $"A play that did not go through Play() was not reported after {sequence}.");
	}

	static void DetachNativeStateHandler(WinMediaManagerFixture fixture)
	{
		var handler = typeof(CommunityToolkit.Maui.Core.Views.MediaManager).GetMethod("OnPlaybackSessionPlaybackStateChanged", BindingFlags.Instance | BindingFlags.NonPublic)
			?? throw new MissingMethodException(typeof(CommunityToolkit.Maui.Core.Views.MediaManager).FullName, "OnPlaybackSessionPlaybackStateChanged");

		fixture.Player!.PlaybackSession.PlaybackStateChanged -= handler.CreateDelegate<Windows.Foundation.TypedEventHandler<MediaPlaybackSession, object>>(fixture.Manager);
	}

	// After the Play() and the pause of the test, the manager's own player either played and paused or never started, so a second spent in Paused
	// means it has settled.
	static Task WaitUntilTheOwnPlayerRestsInPausedAsync(WinMediaManagerFixture fixture)
	{
		var pausedFor = new Stopwatch();
		return WaitUntilAsync(() =>
		{
			if (fixture.Player!.PlaybackSession.PlaybackState is not MediaPlaybackState.Paused)
			{
				pausedFor.Reset();
				return false;
			}

			pausedFor.Start();
			return pausedFor.Elapsed >= TimeSpan.FromSeconds(1);
		}, "The player did not come to rest in Paused after Play() and the pause.");
	}

	// Stop() of a paused player: nothing that came before can still report Playing, so the stop settles at once, and a Playing that follows is a new
	// play. The native player is played directly, which is what the play button of the transport controls does.
	[Fact]
	public async Task NativePlayAfterTheStopOfAPausedPlayerIsReported()
	{
		SetupWindowsBuilder();

		var fixture = await OpenVideoAsync();
		await PlayAsync(fixture);
		await WaitUntilAsync(() => fixture.View.CurrentState is MediaElementState.Playing, "CurrentState never became Playing.");
		await InvokeOnMainThreadAsync(() => fixture.Manager.Pause());
		await WaitUntilAsync(() => fixture.View.CurrentState is MediaElementState.Paused, "CurrentState never became Paused.");

		await InvokeOnMainThreadAsync(() => fixture.Manager.Stop());
		Assert.Equal(MediaElementState.Stopped, fixture.View.CurrentState);
		fixture.Player!.Play();

		await WaitUntilAsync(() => fixture.View.CurrentState is MediaElementState.Playing, "A play that did not go through Play() was not reported after the Stop() of a paused player.");
	}

	// Stop() of a playing player settles when the player reports its pause. The native player is played directly, as the play button of the transport
	// controls does.
	[Fact]
	public async Task NativePlayAfterTheStopOfAPlayingPlayerHasSettledIsReported()
	{
		SetupWindowsBuilder();

		var fixture = await OpenVideoAsync();
		await PlayAsync(fixture);
		await WaitUntilAsync(() => fixture.View.CurrentState is MediaElementState.Playing, "CurrentState never became Playing.");
		var session = fixture.Player!.PlaybackSession;

		// Registered after the manager's handler: once it ran for an event, the manager's handler already ran for the same event.
		var pausedEventsHandled = 0;
		session.PlaybackStateChanged += (sender, _) =>
		{
			if (sender.PlaybackState is MediaPlaybackState.Paused)
			{
				Interlocked.Increment(ref pausedEventsHandled);
			}
		};

		await InvokeOnMainThreadAsync(() => fixture.Manager.Stop());
		await WaitUntilAsync(() => Volatile.Read(ref pausedEventsHandled) > 0, "The native player never reported the pause of Stop().");
		Assert.Equal(MediaElementState.Stopped, fixture.View.CurrentState);
		fixture.Player.Play();

		await WaitUntilAsync(() => fixture.View.CurrentState is MediaElementState.Playing, "A play that did not go through Play() was not reported after the stop settled.");
	}

	// The end of the media leaves nothing that can still report Playing: a play that did not go through Play() after it (the transport controls) is
	// reported. The clip does not loop, so the play is recorded as it is reported (it lasts until the clip ends again).
	[Fact]
	public async Task NativePlayAfterTheMediaEndedIsReported()
	{
		SetupWindowsBuilder();

		var fixture = await OpenVideoAsync(loop: false);
		var ended = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		fixture.View.MediaEnded += (_, _) => ended.TrySetResult();
		await PlayAsync(fixture);
		await ended.Task.WaitAsync(TimeSpan.FromMilliseconds(decodeTimeoutMilliseconds));
		await WaitUntilAsync(() => fixture.Player!.PlaybackSession.PlaybackState is not MediaPlaybackState.Playing, "The native player kept playing after the end of the media.");
		Assert.Equal(MediaElementState.Stopped, fixture.View.CurrentState);
		var states = new ConcurrentQueue<MediaElementState>();
		fixture.View.StateChanged += (_, e) => states.Enqueue(e.NewState);

		// From the start, so that the play lasts as long as the clip.
		fixture.Player!.PlaybackSession.Position = TimeSpan.Zero;
		fixture.Player.Play();

		await WaitUntilAsync(() => states.Contains(MediaElementState.Playing), "A play that did not go through Play() was not reported after the end of the media.");
	}

	[Fact]
	public async Task MediaEndedRaisesTheEventWithTheStoppedState()
	{
		SetupWindowsBuilder();
		await RequireVideoRenderingAsync();

		var fixture = await OpenVideoAsync(loop: false);
		var stateWhenRaised = new TaskCompletionSource<MediaElementState>(TaskCreationOptions.RunContinuationsAsynchronously);
		fixture.View.MediaEnded += (_, _) => stateWhenRaised.TrySetResult(fixture.View.CurrentState);

		await InvokeOnMainThreadAsync(() => fixture.Manager.Play());

		Assert.Equal(MediaElementState.Stopped, await stateWhenRaised.Task.WaitAsync(TimeSpan.FromMilliseconds(decodeTimeoutMilliseconds)));
	}

	[Fact]
	public async Task MediaEndedStateStaysStoppedAfterTheNativePlayerSettles()
	{
		SetupWindowsBuilder();

		var fixture = await OpenVideoAsync(loop: false);
		var session = fixture.Player!.PlaybackSession;
		var pausedEventsHandled = 0;

		// Registered after the manager's handler: once it ran for an event, the manager's handler already ran for the same event.
		session.PlaybackStateChanged += (sender, _) =>
		{
			if (sender.PlaybackState is MediaPlaybackState.Paused)
			{
				Interlocked.Increment(ref pausedEventsHandled);
			}
		};
		var ended = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		fixture.View.MediaEnded += (_, _) => ended.TrySetResult();

		await PlayAsync(fixture);
		var pausedEventsBeforeTheEnd = Volatile.Read(ref pausedEventsHandled);
		await ended.Task.WaitAsync(TimeSpan.FromMilliseconds(decodeTimeoutMilliseconds));

		// A player that reaches the end without looping settles in Paused. The wait is not asserted: if 5 s pass without the manager handling that
		// event, no late native state can overwrite Stopped either.
		await AssertHelpers.Wait(() => Volatile.Read(ref pausedEventsHandled) > pausedEventsBeforeTheEnd && session.PlaybackState is MediaPlaybackState.Paused, 5000, 50);
		await FlushDispatcherAsync();

		Assert.Equal(MediaElementState.Stopped, fixture.View.CurrentState);
	}

	[Fact]
	public async Task ShouldAutoPlayStartsPlaybackWhenTheMediaOpens()
	{
		SetupWindowsBuilder();
		await RequireVideoRenderingAsync();

		var fixture = await CreateManagerAsync(view =>
		{
			view.ShouldAutoPlay = true;
			view.ShouldLoopPlayback = true;
		});
		await InvokeOnMainThreadAsync(() =>
		{
			fixture.Manager.UpdateShouldLoopPlayback();
			fixture.Element!.Width = 160;
			fixture.Element.Height = 120;
		});

		// MediaPlayerElement passes AutoPlay on to its MediaPlayer only while it is in a live visual tree (the MediaPlayer's own default is
		// false), so the element is on screen while the source is applied.
		await HostAsync(fixture.Element!, async _ =>
		{
			await OpenAsync(fixture.View, VideoSource, fixture.Manager);

			Assert.True(fixture.Player!.AutoPlay);
			await WaitUntilAsync(() => fixture.Player.PlaybackSession.PlaybackState is MediaPlaybackState.Playing, "AutoPlay did not start the native player.", decodeTimeoutMilliseconds);
		});
	}

	[Fact]
	public async Task SeekOnTheUiThreadMovesTheNativePositionWithoutDispatching()
	{
		SetupWindowsBuilder();

		var fixture = await OpenVideoAsync(recordDispatches: true);
		var dispatchesBefore = fixture.Dispatcher!.DispatchCount;

		await InvokeOnMainThreadAsync(async () => await fixture.Manager.Seek(TimeSpan.FromSeconds(1)));

		Assert.Equal(dispatchesBefore, fixture.Dispatcher.DispatchCount);
		await WaitUntilAsync(() => IsNear(fixture.Player!.PlaybackSession.Position, 1), $"The position stayed at {fixture.Player!.PlaybackSession.Position}.");
	}

	[Fact]
	public async Task SeekThatMustBeDispatchedQueuesThePositionUpdateOnTheDispatcher()
	{
		SetupWindowsBuilder();

		var fixture = await OpenVideoAsync(dispatchRequired: true);
		var dispatchesBefore = fixture.Dispatcher!.DispatchCount;

		await InvokeOnMainThreadAsync(async () => await fixture.Manager.Seek(TimeSpan.FromSeconds(1.5)));

		Assert.True(fixture.Dispatcher.DispatchCount > dispatchesBefore, "The position update was not queued on the dispatcher.");
		await WaitUntilAsync(() => IsNear(fixture.Player!.PlaybackSession.Position, 1.5), $"The position stayed at {fixture.Player!.PlaybackSession.Position}.");
	}

	[Fact]
	public async Task SeekFromABackgroundThreadIsDispatchedToTheUiThread()
	{
		SetupWindowsBuilder();

		var fixture = await OpenVideoAsync();

		await Task.Run(() => fixture.Manager.Seek(TimeSpan.FromSeconds(1.5)));

		await WaitUntilAsync(() => IsNear(fixture.Player!.PlaybackSession.Position, 1.5), $"The position stayed at {fixture.Player!.PlaybackSession.Position}.");
	}

	[Fact]
	public async Task SeekOnTheUiThreadWithACancelledTokenThrowsAndKeepsThePosition()
	{
		SetupWindowsBuilder();

		var fixture = await OpenVideoAsync();
		using var cancellation = new CancellationTokenSource();
		await cancellation.CancelAsync();

		await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
			InvokeOnMainThreadAsync(async () => await fixture.Manager.Seek(TimeSpan.FromSeconds(1), cancellation.Token)));

		await FlushDispatcherAsync();
		Assert.True(IsNear(fixture.Player!.PlaybackSession.Position, 0, 0.2), $"The position moved to {fixture.Player.PlaybackSession.Position}.");
	}

	[Fact]
	public async Task SeekThatMustBeDispatchedWithACancelledTokenThrows()
	{
		SetupWindowsBuilder();

		var fixture = await OpenVideoAsync(dispatchRequired: true);
		using var cancellation = new CancellationTokenSource();
		await cancellation.CancelAsync();

		await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
			InvokeOnMainThreadAsync(async () => await fixture.Manager.Seek(TimeSpan.FromSeconds(1), cancellation.Token)));
	}

	[Fact]
	public async Task SeekThatMustBeDispatchedWithACancelledTokenKeepsThePosition()
	{
		SetupWindowsBuilder();

		var fixture = await OpenVideoAsync(dispatchRequired: true);
		using var cancellation = new CancellationTokenSource();
		await cancellation.CancelAsync();

		await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
			InvokeOnMainThreadAsync(async () => await fixture.Manager.Seek(TimeSpan.FromSeconds(1), cancellation.Token)));

		await FlushDispatcherAsync();
		Assert.True(IsNear(fixture.Player!.PlaybackSession.Position, 0, 0.2), $"The cancelled seek moved the position to {fixture.Player.PlaybackSession.Position}.");
	}

	// A token cancelled after the manager checked it, while the update waits in the dispatcher queue (here: cancelled by the dispatcher at the
	// moment the update is queued). WaitAsync(token) ends the wait, not the queued update: only the update's own check keeps it from moving the
	// position when the UI thread runs it.
	[Fact]
	public async Task SeekCancelledWhileItsUpdateIsQueuedKeepsThePosition()
	{
		SetupWindowsBuilder();

		var fixture = await OpenVideoAsync(dispatchRequired: true);
		using var cancellation = new CancellationTokenSource();
		var dispatchesBefore = fixture.Dispatcher!.DispatchCount;
		fixture.Dispatcher.BeforeDispatch = cancellation.Cancel;

		try
		{
			await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
				InvokeOnMainThreadAsync(async () => await fixture.Manager.Seek(TimeSpan.FromSeconds(1), cancellation.Token)));
		}
		finally
		{
			fixture.Dispatcher.BeforeDispatch = null;
		}

		Assert.True(fixture.Dispatcher.DispatchCount > dispatchesBefore, "The position update was not queued on the dispatcher.");
		await FlushDispatcherAsync();

		// A position update that ran would show within a moment, so the wait is expected to time out.
		var moved = await AssertHelpers.Wait(() => !IsNear(fixture.Player!.PlaybackSession.Position, 0, 0.2), 1000, 50);
		Assert.False(moved, $"The cancelled seek moved the position to {fixture.Player!.PlaybackSession.Position}.");
	}

	[Fact]
	public async Task SeekFromABackgroundThreadWithACancelledTokenThrows()
	{
		SetupWindowsBuilder();

		var fixture = await OpenVideoAsync();
		using var cancellation = new CancellationTokenSource();
		await cancellation.CancelAsync();

		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Task.Run(() => fixture.Manager.Seek(TimeSpan.FromSeconds(1), cancellation.Token)));
	}

	[Fact]
	public async Task SeekRaisesSeekCompleted()
	{
		SetupWindowsBuilder();
		// A machine that renders no video raises no native SeekCompleted either (a Windows 11 26200 ARM64 VM: none within 20 s).
		await RequireVideoRenderingAsync();

		var fixture = await OpenVideoAsync();
		var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		fixture.View.SeekCompleted += (_, _) => completed.TrySetResult();

		await InvokeOnMainThreadAsync(async () => await fixture.Manager.Seek(TimeSpan.FromSeconds(1)));

		await completed.Task.WaitAsync(TimeSpan.FromMilliseconds(decodeTimeoutMilliseconds));
	}

	// The clip does not loop and stays paused, and the player is moved away from both ends first, so that every seek moves it.
	[Theory]
	[InlineData(WinMediaSeekOutsideTheMedia.BeforeTheStart)]
	[InlineData(WinMediaSeekOutsideTheMedia.BeyondTheEnd)]
	[InlineData(WinMediaSeekOutsideTheMedia.TheEnd)]
	public async Task SeekOutsideTheMediaGoesToItsStartOrItsEndAndRaisesSeekCompleted(WinMediaSeekOutsideTheMedia target)
	{
		SetupWindowsBuilder();
		// A machine that renders no video raises no native SeekCompleted either, and its seeks outside the clip never complete.
		await RequireVideoRenderingAsync();

		var fixture = await OpenVideoAsync(loop: false);
		var seeksCompleted = 0;
		fixture.View.SeekCompleted += (_, _) => Interlocked.Increment(ref seeksCompleted);
		await SeekNativeAsync(fixture, 1);
		await WaitUntilAsync(() => Volatile.Read(ref seeksCompleted) > 0, "The native seek to 1 s raised no SeekCompleted.");
		var seeksCompletedBefore = Volatile.Read(ref seeksCompleted);
		var (requested, expected) = WinMediaSeeks.OutsideTheMedia(target, fixture.Player!.PlaybackSession.NaturalDuration);

		await InvokeOnMainThreadAsync(async () => await fixture.Manager.Seek(requested));

		await WaitUntilAsync(() => Volatile.Read(ref seeksCompleted) > seeksCompletedBefore, "The seek outside the media raised no SeekCompleted.");
		await WaitUntilAsync(() => IsNear(fixture.Player.PlaybackSession.Position, expected.TotalSeconds), "The seek outside the media did not go to the start or the end of the media.");
	}

	[Fact]
	public async Task SpeedChangeSetsTheNativePlaybackRate()
	{
		SetupWindowsBuilder();

		var fixture = await OpenVideoAsync();
		var playRequests = 0;
		fixture.View.PlayRequested += (_, _) => Interlocked.Increment(ref playRequests);

		fixture.View.Speed = 2;
		await InvokeOnMainThreadAsync(() => fixture.Manager.UpdateSpeed());
		await WaitUntilAsync(() => Math.Abs(fixture.Player!.PlaybackSession.PlaybackRate - 2) < 0.001, "The native playback rate did not follow Speed.");

		fixture.View.Speed = 1.5;
		await InvokeOnMainThreadAsync(() => fixture.Manager.UpdateSpeed());
		await WaitUntilAsync(() => Math.Abs(fixture.Player!.PlaybackSession.PlaybackRate - 1.5) < 0.001, "The native playback rate did not follow the second Speed.");

		// Neither 1 -> 2 nor 2 -> 1.5 passes through zero, so neither pauses nor requests Play.
		Assert.Equal(0, playRequests);
	}

	[Fact]
	public async Task SpeedZeroPausesPlayingMediaAndPositiveSpeedRequestsPlayAgain()
	{
		SetupWindowsBuilder();
		await RequireZeroRateAsync();

		var fixture = await OpenVideoAsync();
		await PlayAsync(fixture);
		var playRequests = 0;
		fixture.View.PlayRequested += (_, _) => Interlocked.Increment(ref playRequests);

		fixture.View.Speed = 0;
		await InvokeOnMainThreadAsync(() => fixture.Manager.UpdateSpeed());
		await WaitUntilAsync(() => fixture.Player!.PlaybackSession.PlaybackState is MediaPlaybackState.Paused, "Speed 0 did not pause the native player.", decodeTimeoutMilliseconds);
		Assert.Equal(0, fixture.Player!.PlaybackSession.PlaybackRate);
		Assert.Equal(0, playRequests);

		// Zero to zero neither pauses nor requests Play.
		await InvokeOnMainThreadAsync(() => fixture.Manager.UpdateSpeed());
		Assert.Equal(0, playRequests);

		// MediaElement.Play() only raises PlayRequested here: without a handler, nothing else happens.
		fixture.View.Speed = 1;
		await InvokeOnMainThreadAsync(() => fixture.Manager.UpdateSpeed());
		Assert.Equal(1, playRequests);
		Assert.Equal(1, fixture.Player.PlaybackSession.PlaybackRate, 3);
	}

	[Fact]
	public async Task PlayingAtZeroSpeedRestoresTheNativeRateAndSpeed()
	{
		SetupWindowsBuilder();
		await RequirePlayAtZeroRateAsync();

		var fixture = await OpenVideoAsync();
		fixture.View.Speed = 0;
		await InvokeOnMainThreadAsync(() => fixture.Manager.UpdateSpeed());
		await WaitUntilAsync(() => fixture.Player!.PlaybackSession.PlaybackRate == 0, "The native rate did not become 0.");

		var nativePlaying = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		fixture.Player!.PlaybackSession.PlaybackStateChanged += (session, _) =>
		{
			if (session.PlaybackState is MediaPlaybackState.Playing)
			{
				nativePlaying.TrySetResult();
			}
		};

		// Playing with rate 0 is reported as Playing by the platform (a plain MediaPlayer was probed for exactly that).
		await InvokeOnMainThreadAsync(() => fixture.Manager.Play());
		await nativePlaying.Task.WaitAsync(TimeSpan.FromMilliseconds(decodeTimeoutMilliseconds));

		await WaitUntilAsync(() => Math.Abs(fixture.Player!.PlaybackSession.PlaybackRate - 1) < 0.001, "The native rate was not restored to 1.");
		await WaitUntilAsync(() => Math.Abs(fixture.View.Speed - 1) < 0.001, "Speed did not follow the restored native rate.");
	}

	[Fact]
	public async Task NativeRateChangeFlowsBackToSpeed()
	{
		SetupWindowsBuilder();

		var fixture = await OpenVideoAsync();

		// The event arrives off the UI thread.
		fixture.Player!.PlaybackSession.PlaybackRate = 1.5;

		await WaitUntilAsync(() => Math.Abs(fixture.View.Speed - 1.5) < 0.001, "Speed did not follow the native playback rate.");
	}

	[Fact]
	public async Task NativeRateChangeWithinTheToleranceDoesNotChangeSpeed()
	{
		SetupWindowsBuilder();

		var fixture = await OpenVideoAsync();
		fixture.View.Speed = 1.5;
		await InvokeOnMainThreadAsync(() => fixture.Manager.UpdateSpeed());
		await WaitUntilAsync(() => Math.Abs(fixture.Player!.PlaybackSession.PlaybackRate - 1.5) < 0.001, "The native rate did not become 1.5.");

		// Registered after the manager's handler: once it ran, the manager's handler ran for the same event.
		var rateChanged = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		fixture.Player!.PlaybackSession.PlaybackRateChanged += (_, _) => rateChanged.TrySetResult();

		fixture.Player.PlaybackSession.PlaybackRate = 1.505;
		await rateChanged.Task.WaitAsync(TimeSpan.FromMilliseconds(decodeTimeoutMilliseconds));
		await FlushDispatcherAsync();

		// 1.5 vs 1.505 is within the 0.01 tolerance of the comparison.
		Assert.Equal(1.5, fixture.View.Speed);
	}

	[Fact]
	public async Task NativeRateChangeHandledOnTheUiThreadUpdatesSpeedInline()
	{
		SetupWindowsBuilder();

		var fixture = await OpenVideoAsync();

		await InvokeOnMainThreadAsync(() =>
		{
			var session = fixture.Player!.PlaybackSession;
			session.PlaybackRate = 1.5;

			WinMediaReflection.Invoke(fixture.Manager, "OnPlaybackSessionPlaybackRateChanged", session, null);

			Assert.Equal(1.5, fixture.View.Speed, 3);
		});
	}

	// The worker thread of a rate event reads the rate and queues the update of Speed, so the app can apply a newer Speed before the update
	// runs. The manager's handler gets the session of a plain player, whose rate the test sets as that newer Speed would set it: no event of
	// the manager's own player can then queue an update of its own.
	[Fact]
	public async Task QueuedRateChangeOfAnEarlierSpeedDoesNotUndoANewerSpeed()
	{
		SetupWindowsBuilder();
		await RequireDecoderAsync();
		var (plain, failure) = await WinMediaPlainPlayer.OpenAsync(player => player.SetUriSource(WinMediaAssets.VideoUri), TimeSpan.FromMilliseconds(decodeTimeoutMilliseconds));
		await using (plain)
		{
			Assert.True(failure is null, $"A plain MediaPlayer did not open the clip: {failure}");
			var session = plain.Player.PlaybackSession;
			var dispatcher = new WinMediaCapturingDispatcher(isDispatchRequired: true);

			await InvokeOnMainThreadAsync(() =>
			{
				var view = new MediaElement();
				var manager = new WinMediaManager(MauiContext, view, dispatcher);
				var platformView = new WinMediaView(manager.CreatePlatformView());
				RegisterCleanup(() =>
				{
					manager.Dispose();
					platformView.Dispose();
					view.Dispose();
				});

				session.PlaybackRate = 2;
				WinMediaReflection.Invoke(manager, "OnPlaybackSessionPlaybackRateChanged", session, null);
				var queuedUpdate = Assert.Single(dispatcher.TakeQueued());

				view.Speed = 1.5;
				session.PlaybackRate = 1.5;
				queuedUpdate();

				Assert.Equal(1.5, view.Speed);
			});
		}
	}

	[Fact]
	public async Task MediaOpenedHandledOnTheUiThreadSetsTheDurationInline()
	{
		SetupWindowsBuilder();

		var fixture = await OpenVideoAsync();
		await WaitUntilAsync(() => fixture.View.Duration > TimeSpan.Zero, "Duration was never set after MediaOpened.");

		await InvokeOnMainThreadAsync(() =>
		{
			((IMediaElement)fixture.View).Duration = TimeSpan.Zero;
			using var capture = new WinMediaAsyncVoidCapture();

			// SetDuration runs before the first await of the async void handler, so Duration is already set when the call returns.
			WinMediaReflection.Invoke(fixture.Manager, "OnMediaElementMediaOpened", fixture.Player, null);

			Assert.Equal(fixture.Player!.PlaybackSession.NaturalDuration, fixture.View.Duration);
			Assert.Empty(capture.Exceptions);
		});
	}

	// The native MediaOpened arrives on a worker thread, so the handler queues SetDuration. If the MediaElement's handler is disconnected before
	// the queue runs (the user leaves the page while the media is opening; an unloaded MauiMediaElement is disposed at once: MediaPlayer.Dispose,
	// SetMediaPlayer(null)), the queued read finds no player. The manager and the platform view are built as MediaElementHandler builds them,
	// around a dispatcher that reports a worker thread and hands the queued update to the test, which runs it inside Record.Exception: on the
	// real dispatcher queue an exception ends the app (microsoft-ui-xaml#8940). No media is decoded: the handler is called as the event would.
	[Fact]
	public async Task QueuedDurationUpdateThatRunsAfterThePlatformViewWasDisposedDoesNothing()
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();
		var dispatcher = new WinMediaCapturingDispatcher(isDispatchRequired: true);

		await InvokeOnMainThreadAsync(() =>
		{
			var view = new MediaElement();
			var manager = new WinMediaManager(MauiContext, view, dispatcher);
			var platformView = new WinMediaView(manager.CreatePlatformView());
			RegisterCleanup(() =>
			{
				manager.Dispose();
				platformView.Dispose();
				view.Dispose();
			});

			// The rest of the handler (MediaOpened on the view, the metadata) runs to its end right here; only SetDuration is queued.
			using (var capture = new WinMediaAsyncVoidCapture())
			{
				WinMediaReflection.Invoke(manager, "OnMediaElementMediaOpened", manager.PlayerElement!.MediaPlayer, null);
				Assert.Empty(capture.Exceptions);
			}

			var queuedDurationUpdate = Assert.Single(dispatcher.TakeQueued());

			manager.Dispose();
			platformView.Dispose();

			Assert.Null(Record.Exception(queuedDurationUpdate));
			Assert.Equal(TimeSpan.Zero, view.Duration);
		});
	}

	// The same for a position update that had to be queued (a Seek off the UI thread). The clip is opened on the native player directly: the
	// manager's own source update would wait for the dispatcher that this test holds. DispatchAsync hands an exception of the queued update to the
	// task it returns, so a failing update shows in the Seek task, not in the queued action.
	[Fact]
	public async Task QueuedSeekUpdateThatRunsAfterThePlatformViewWasDisposedDoesNothing()
	{
		SetupWindowsBuilder();
		await RequireDecoderAsync();
		var dispatcher = new WinMediaCapturingDispatcher(isDispatchRequired: true);
		var opened = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

		var (manager, platformView) = await InvokeOnMainThreadAsync(() =>
		{
			var view = new MediaElement();

			// Resolve the view's dispatcher here, on the UI thread: MediaElement creates its status timer when the media opens, from a native thread.
			_ = view.Dispatcher;
			var createdManager = new WinMediaManager(MauiContext, view, dispatcher);
			var createdPlatformView = new WinMediaView(createdManager.CreatePlatformView());
			RegisterCleanup(() =>
			{
				createdManager.Dispose();
				createdPlatformView.Dispose();
				view.Dispose();
			});

			// Subscribed after the manager: when these run, the manager's handlers have run for the same event.
			var player = createdManager.PlayerElement!.MediaPlayer;
			player.MediaOpened += (_, _) => opened.TrySetResult();
			player.MediaFailed += (_, args) => opened.TrySetException(new InvalidOperationException($"The clip did not open: {args.Error}."));
			player.SetUriSource(WinMediaAssets.VideoUri);

			return (createdManager, createdPlatformView);
		});
		await opened.Task.WaitAsync(TimeSpan.FromMilliseconds(decodeTimeoutMilliseconds));

		await InvokeOnMainThreadAsync(async () =>
		{
			// What the open queued (SetDuration) is not part of this test.
			_ = dispatcher.TakeQueued();

			var seek = manager.Seek(TimeSpan.FromSeconds(1));
			var queuedUpdate = Assert.Single(dispatcher.TakeQueued());

			// MediaElementHandler.DisconnectHandler disposes the manager and the unloaded platform view.
			manager.Dispose();
			platformView.Dispose();

			Assert.Null(Record.Exception(queuedUpdate));
			await seek.WaitAsync(TimeSpan.FromMilliseconds(decodeTimeoutMilliseconds));
		});
	}

	// The same for the write of rate 1 that a Playing state at rate 0 queues. The manager's handler gets the session of a plain player that plays at
	// rate 0, so the write the guard must stop stays observable: that player outlives the manager's platform view, and its rate must stay 0.
	[Fact]
	public async Task QueuedRateWriteThatRunsAfterThePlatformViewWasDisposedDoesNothing()
	{
		SetupWindowsBuilder();
		await RequirePlayAtZeroRateAsync();
		var (plain, failure) = await WinMediaPlainPlayer.OpenAsync(player => player.SetUriSource(WinMediaAssets.VideoUri), TimeSpan.FromMilliseconds(decodeTimeoutMilliseconds));
		await using (plain)
		{
			Assert.True(failure is null, $"A plain MediaPlayer did not open the clip: {failure}");
			var session = plain.Player.PlaybackSession;
			plain.Player.IsLoopingEnabled = true;
			plain.Player.PlaybackRate = 0;
			plain.Player.Play();
			await WaitUntilAsync(() => session.PlaybackState is MediaPlaybackState.Playing, "The plain player did not report Playing at rate 0.");
			var dispatcher = new WinMediaCapturingDispatcher();

			await InvokeOnMainThreadAsync(() =>
			{
				var view = new MediaElement();
				var manager = new WinMediaManager(MauiContext, view, dispatcher);
				var platformView = new WinMediaView(manager.CreatePlatformView());
				RegisterCleanup(() =>
				{
					manager.Dispose();
					platformView.Dispose();
					view.Dispose();
				});
				Assert.Equal(0, session.PlaybackRate);

				WinMediaReflection.Invoke(manager, "OnPlaybackSessionPlaybackStateChanged", session, null);
				var queuedWrite = Assert.Single(dispatcher.TakeQueued());

				// MediaElementHandler.DisconnectHandler disposes the manager and the unloaded platform view.
				manager.Dispose();
				platformView.Dispose();

				Assert.Null(Record.Exception(queuedWrite));
				Assert.Equal(0, session.PlaybackRate);
			});
		}
	}

	[Fact]
	public async Task MediaOpenedWithoutPlayerIsIgnored()
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();

		var fixture = await CreateManagerAsync();
		var opened = 0;
		fixture.View.MediaOpened += (_, _) => Interlocked.Increment(ref opened);

		await InvokeOnMainThreadAsync(() =>
		{
			fixture.Manager.PlayerElement = null;
			using var capture = new WinMediaAsyncVoidCapture();

			WinMediaReflection.Invoke(fixture.Manager, "OnMediaElementMediaOpened", fixture.Player, null);

			Assert.Empty(capture.Exceptions);
		});

		Assert.Equal(0, opened);
	}

	[Theory]
	[InlineData(MediaElementState.Playing)]
	[InlineData(MediaElementState.Paused)]
	[InlineData(MediaElementState.Stopped)]
	public async Task UpdateStatusCopiesTheNativePositionInPlaybackStates(MediaElementState state)
	{
		SetupWindowsBuilder();

		var fixture = await OpenVideoAsync();
		await SeekNativeAsync(fixture, 1);

		await InvokeOnMainThreadAsync(() =>
		{
			((IMediaElement)fixture.View).CurrentStateChanged(state);

			fixture.Manager.UpdateStatus();

			// The player is paused at about 1 s, so it does not move between the manager's read and ours.
			var nativePosition = fixture.Player!.PlaybackSession.Position;
			Assert.True(nativePosition > TimeSpan.Zero, "Precondition: the native player is not at the start.");
			Assert.True((nativePosition - fixture.View.Position).Duration() < TimeSpan.FromMilliseconds(50), $"MediaElement.Position is {fixture.View.Position}, the native position is {nativePosition}.");
		});
	}

	[Theory]
	[InlineData(MediaElementState.None)]
	[InlineData(MediaElementState.Opening)]
	[InlineData(MediaElementState.Buffering)]
	[InlineData(MediaElementState.Failed)]
	public async Task UpdateStatusKeepsThePositionInOtherStates(MediaElementState state)
	{
		SetupWindowsBuilder();

		var fixture = await OpenVideoAsync();
		await SeekNativeAsync(fixture, 1);

		await InvokeOnMainThreadAsync(() =>
		{
			((IMediaElement)fixture.View).CurrentStateChanged(state);

			fixture.Manager.UpdateStatus();

			Assert.Equal(TimeSpan.Zero, fixture.View.Position);
		});
	}

	[Fact]
	public async Task UpdateStatusWithoutPlayerKeepsThePosition()
	{
		SetupWindowsBuilder();

		var fixture = await OpenVideoAsync();
		await SeekNativeAsync(fixture, 1);

		await InvokeOnMainThreadAsync(() =>
		{
			((IMediaElement)fixture.View).CurrentStateChanged(MediaElementState.Playing);
			fixture.Manager.PlayerElement = null;

			fixture.Manager.UpdateStatus();

			Assert.Equal(TimeSpan.Zero, fixture.View.Position);
		});
	}

	[Fact]
	public async Task UpdateStatusKeepsThePositionWhenThereIsNoApplication()
	{
		SetupWindowsBuilder();

		var fixture = await OpenVideoAsync();
		await SeekNativeAsync(fixture, 1);

		await InvokeOnMainThreadAsync(() =>
		{
			((IMediaElement)fixture.View).CurrentStateChanged(MediaElementState.Playing);
			using var scope = new WinMediaApplicationScope();
			Application.Current = null;

			fixture.Manager.UpdateStatus();

			Assert.Equal(TimeSpan.Zero, fixture.View.Position);
		});
	}

	[Fact]
	public async Task UpdateStatusKeepsThePositionWhenTheApplicationHasNoWindow()
	{
		SetupWindowsBuilder();

		var fixture = await OpenVideoAsync();
		await SeekNativeAsync(fixture, 1);

		await InvokeOnMainThreadAsync(() =>
		{
			((IMediaElement)fixture.View).CurrentStateChanged(MediaElementState.Playing);
			using var scope = new WinMediaApplicationScope();
			var application = new WinMediaFakeApplication();
			Assert.Empty(application.Windows);

			fixture.Manager.UpdateStatus();

			Assert.Equal(TimeSpan.Zero, fixture.View.Position);
		});
	}

	[Fact]
	public async Task UpdateStatusKeepsThePositionWhenTheLastWindowHasNoPlatformWindow()
	{
		SetupWindowsBuilder();

		var fixture = await OpenVideoAsync();
		await SeekNativeAsync(fixture, 1);

		await InvokeOnMainThreadAsync(() =>
		{
			((IMediaElement)fixture.View).CurrentStateChanged(MediaElementState.Playing);
			using var scope = new WinMediaApplicationScope();
			var application = new WinMediaFakeApplication(() => new Window(new ContentPage()));
			var window = application.AddWindow();
			Assert.Null(window.Handler);

			fixture.Manager.UpdateStatus();

			// The workaround for the timer that outlives its window: ParentWindow.Exists is false because the window of the current page has no handler.
			Assert.Equal(TimeSpan.Zero, fixture.View.Position);
		});
	}

	[Fact]
	public async Task UpdateStatusKeepsThePositionWhenTheLastWindowHasNoPage()
	{
		SetupWindowsBuilder();

		var fixture = await OpenVideoAsync();
		await SeekNativeAsync(fixture, 1);

		await InvokeOnMainThreadAsync(() =>
		{
			((IMediaElement)fixture.View).CurrentStateChanged(MediaElementState.Playing);
			using var scope = new WinMediaApplicationScope();
			var application = new WinMediaFakeApplication(() => new Window());
			application.AddWindow();

			Assert.Null(Record.Exception(() => fixture.Manager.UpdateStatus()));
			Assert.Equal(TimeSpan.Zero, fixture.View.Position);
		});
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task OpeningAnUnusableSourceRaisesMediaFailedWithTheNativeErrorText(bool useCorruptFile)
	{
		var loggerFactory = new WinMediaLoggerFactory();
		SetupWindowsBuilder(loggerFactory);
		await RequireMediaPlayerAsync();

		var states = new ConcurrentQueue<MediaElementState>();
		var fixture = await CreateManagerAsync(view => view.StateChanged += (_, e) => states.Enqueue(e.NewState));
		var failed = new TaskCompletionSource<MediaFailedEventArgs>(TaskCreationOptions.RunContinuationsAsynchronously);
		fixture.View.MediaFailed += (_, e) => failed.TrySetResult(e);

		// Registered after the manager's handler, so the manager's handler has already run when this reports what the platform said.
		var native = new TaskCompletionSource<(MediaPlayerError Error, string? ExtendedMessage, string ErrorMessage)>(TaskCreationOptions.RunContinuationsAsynchronously);
		fixture.Player!.MediaFailed += (_, args) => native.TrySetResult((args.Error, args.ExtendedErrorCode?.Message, args.ErrorMessage));

		// Nothing listens on port 1, so the connection is refused.
		await SetSourceAsync(fixture.View, useCorruptFile
			? new FileMediaSource { Path = WinMediaAssets.CorruptPath }
			: new UriMediaSource { Uri = new Uri("http://127.0.0.1:1/never.mp4") }, fixture.Manager);

		var raised = await failed.Task.WaitAsync(TimeSpan.FromMilliseconds(refusedConnectionTimeoutMilliseconds));
		var (error, extendedMessage, errorMessage) = await native.Task.WaitAsync(TimeSpan.FromMilliseconds(refusedConnectionTimeoutMilliseconds));

		// The parts of the message depend on what the platform reported, so the expectation is rebuilt from the platform's own values.
		var expected = string.Join(", ", new[]
		{
			error.ToString(),
			extendedMessage is null ? string.Empty : $"Error code: {extendedMessage}",
			string.IsNullOrWhiteSpace(errorMessage) ? string.Empty : $"Error message: {errorMessage}",
		}.Where(part => part.Length > 0));
		Assert.Equal(expected, raised.ErrorMessage);
		Assert.Contains(loggerFactory.Entries, entry => entry.Level is LogLevel.Error && entry.Message == expected);

		// The platform also moves its own state to None after a failed open, and the manager reports that too, so the state that is current a moment
		// later can be either; only "Failed was entered" does not depend on the order of the two events.
		Assert.Contains(MediaElementState.Failed, states);
		Assert.Equal(TimeSpan.Zero, fixture.View.Duration);
		Assert.Equal(TimeSpan.Zero, fixture.View.Position);
	}

	static async Task SeekNativeAsync(WinMediaManagerFixture fixture, double seconds)
	{
		fixture.Player!.PlaybackSession.Position = TimeSpan.FromSeconds(seconds);
		await WaitUntilAsync(() => IsNear(fixture.Player.PlaybackSession.Position, seconds), "The native position did not move.");
	}

	// OnMediaElementMediaFailed builds the message from an ErrorMessage (blank or not) and an ExtendedErrorCode (null or not), and which unusable
	// source produces which arm is up to the platform. So each arm has its own test that uses the first of several unusable sources for which the
	// platform delivered that arm, and skips visibly when none did.
	sealed record FailureObservation(string SourceName, MediaFailedEventArgs Raised, MediaPlayerError Error, string? ExtendedMessage, string ErrorMessage);

	static (string Name, Func<WinMediaHttpServer, MediaSource> Create)[] UnusableSources =>
	[
		("a file that is not a media container", _ => new FileMediaSource { Path = WinMediaAssets.CorruptPath }),
		("a refused connection", _ => new UriMediaSource { Uri = new Uri("http://127.0.0.1:1/never.mp4") }),
		("an HTTP 404", server => new UriMediaSource { Uri = server.UriFor("missing.mp4") }),
		("an HTTP resource that is not a media container", server => new UriMediaSource { Uri = server.UriFor("corrupt.mp4") }),
	];

	async Task<FailureObservation> ObserveFailureAsync(string sourceName, MediaSource source)
	{
		var fixture = await CreateManagerAsync();
		var failed = new TaskCompletionSource<MediaFailedEventArgs>(TaskCreationOptions.RunContinuationsAsynchronously);
		fixture.View.MediaFailed += (_, e) => failed.TrySetResult(e);

		// Registered after the manager's handler, so the manager's handler has already run when this reports what the platform delivered.
		var native = new TaskCompletionSource<(MediaPlayerError Error, string? ExtendedMessage, string ErrorMessage)>(TaskCreationOptions.RunContinuationsAsynchronously);
		fixture.Player!.MediaFailed += (_, args) => native.TrySetResult((args.Error, args.ExtendedErrorCode?.Message, args.ErrorMessage));

		await SetSourceAsync(fixture.View, source, fixture.Manager);

		// One of the sources is a refused connection, which needs the longer timeout.
		var raised = await failed.Task.WaitAsync(TimeSpan.FromMilliseconds(refusedConnectionTimeoutMilliseconds));
		var (error, extendedMessage, errorMessage) = await native.Task.WaitAsync(TimeSpan.FromMilliseconds(refusedConnectionTimeoutMilliseconds));
		Assert.StartsWith(error.ToString(), raised.ErrorMessage, StringComparison.Ordinal);
		return new FailureObservation(sourceName, raised, error, extendedMessage, errorMessage);
	}

	async Task AssertTheArmOfTheFirstFailureThatReachesItAsync(Func<FailureObservation, bool> reachesTheArm, string arm, Action<FailureObservation> assertTheArm)
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();
		var corrupt = File.ReadAllBytes(WinMediaAssets.CorruptPath);
		await using var server = new WinMediaHttpServer
		{
			Handler = request => request.Path == "/corrupt.mp4" ? WinMediaResponse.ServeBytes(request, corrupt, "video/mp4") : WinMediaResponse.NotFound(),
		}.Start();

		var delivered = new List<string>();
		foreach (var (name, create) in UnusableSources)
		{
			var observation = await ObserveFailureAsync(name, create(server));
			if (reachesTheArm(observation))
			{
				assertTheArm(observation);
				return;
			}

			delivered.Add($"{name}: {observation.Error}, ExtendedErrorCode {(observation.ExtendedMessage is null ? "null" : "set")}, ErrorMessage {(string.IsNullOrWhiteSpace(observation.ErrorMessage) ? "blank" : "set")}");
		}

		throw new SkipException($"The platform delivered {arm} for none of the unusable sources ({string.Join("; ", delivered)}), so this arm of OnMediaElementMediaFailed cannot be reached on this machine.");
	}

	[Fact]
	public Task MediaFailedMessageContainsTheErrorMessageThePlatformDelivered() =>
		AssertTheArmOfTheFirstFailureThatReachesItAsync(
			observation => !string.IsNullOrWhiteSpace(observation.ErrorMessage),
			"an ErrorMessage",
			observation => Assert.Contains($"Error message: {observation.ErrorMessage}", observation.Raised.ErrorMessage, StringComparison.Ordinal));

	[Fact]
	public Task MediaFailedMessageLeavesOutABlankErrorMessage() =>
		AssertTheArmOfTheFirstFailureThatReachesItAsync(
			observation => string.IsNullOrWhiteSpace(observation.ErrorMessage),
			"a blank ErrorMessage",
			observation => Assert.DoesNotContain("Error message:", observation.Raised.ErrorMessage, StringComparison.Ordinal));

	[Fact]
	public Task MediaFailedMessageContainsTheExtendedErrorCodeThePlatformDelivered() =>
		AssertTheArmOfTheFirstFailureThatReachesItAsync(
			observation => observation.ExtendedMessage is not null,
			"an ExtendedErrorCode",
			observation => Assert.Contains($"Error code: {observation.ExtendedMessage}", observation.Raised.ErrorMessage, StringComparison.Ordinal));

	[Fact]
	public Task MediaFailedMessageLeavesOutAMissingExtendedErrorCode() =>
		AssertTheArmOfTheFirstFailureThatReachesItAsync(
			observation => observation.ExtendedMessage is null,
			"no ExtendedErrorCode",
			observation => Assert.DoesNotContain("Error code:", observation.Raised.ErrorMessage, StringComparison.Ordinal));
}