using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.DeviceTests.Runners;
using CommunityToolkit.Maui.Views;
using PlayerConsts = AndroidX.Media3.Common.BasePlayer.InterfaceConsts;

namespace CommunityToolkit.Maui.DeviceTests;

[Category(TestCategory.MediaElementAndroid)]
[Collection(TestCollections.DisableParallelization)]
public class MediaManagerPlaybackAndroidTests : MediaElementAndroidTestBase
{
	const int playerStateReady = 3;

	[Fact]
	public async Task ShouldLoopPlaybackKeepsPlayingPastTheEndOfTheMedia()
	{
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync(element =>
		{
			element.ShouldLoopPlayback = true;
			element.ShouldAutoPlay = true;
			element.Source = MediaSource.FromResource(shortToneAsset);
		});
		using var recorder = new MediaElementRecorder(mediaElement);

		await HostAsync(mediaElement, async _ =>
		{
			var player = GetPlayer(mediaElement);
			long highestPosition = 0;
			var wrapped = false;

			await WaitUntilAsync(() =>
			{
				var position = player.CurrentPosition;
				wrapped |= position < highestPosition - 100;
				highestPosition = Math.Max(highestPosition, position);
				return wrapped;
			}, "The position never went back to the start of the clip.", 15000);

			Assert.Equal(0, recorder.EndedCount);
			Assert.NotEqual(PlayerConsts.StateEnded, player.PlaybackState);
			Assert.True(player.PlayWhenReady);
		});
	}

	[Fact]
	public async Task WithoutShouldLoopPlaybackTheMediaEndsAndStaysEnded()
	{
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync(element =>
		{
			element.ShouldAutoPlay = true;
			element.Source = MediaSource.FromResource(shortToneAsset);
		});
		using var recorder = new MediaElementRecorder(mediaElement);

		await HostAsync(mediaElement, async _ =>
		{
			var player = GetPlayer(mediaElement);

			await WaitUntilAsync(() => recorder.EndedCount > 0, "The clip never ended.");

			Assert.Equal(PlayerConsts.StateEnded, player.PlaybackState);
			Assert.Equal(MediaElementState.Stopped, mediaElement.CurrentState);
		});
	}

	// ExoPlayer stays at the end of the media on play(): leaving its ended state takes a seek, so Play() starts the media again from its
	// start, like the play button of Media3's own controls.
	[Fact]
	public async Task PlayAfterTheMediaEndedPlaysItAgainFromTheStart()
	{
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync(element =>
		{
			element.ShouldAutoPlay = true;
			element.Source = MediaSource.FromResource(shortToneAsset);
		});
		using var recorder = new MediaElementRecorder(mediaElement);

		await HostAsync(mediaElement, async _ =>
		{
			var player = GetPlayer(mediaElement);
			await WaitUntilAsync(() => recorder.EndedCount > 0, "The clip never ended.");
			Assert.Equal(PlayerConsts.StateEnded, player.PlaybackState);
			var statesBeforePlay = recorder.States.Count;
			var hasGoneBackToTheStart = false;

			mediaElement.Play();

			await WaitUntilAsync(() =>
			{
				hasGoneBackToTheStart |= player.CurrentPosition < shortToneDuration.TotalMilliseconds / 2;
				return recorder.EndedCount > 1;
			}, "The clip did not play to its end again after Play().");
			Assert.True(hasGoneBackToTheStart, "The clip did not play again from its start.");
			Assert.Contains(MediaElementState.Playing, recorder.States.Skip(statesBeforePlay));
		});
	}

	// ExoPlayer clamps a position outside of the media (the last millisecond is the end), so these seeks complete and raise SeekCompleted
	// instead of waiting for a ready state that never comes.
	[Fact]
	public async Task SeekBeyondTheEndMovesToTheEndOfTheMedia()
	{
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync(element => element.Source = MediaSource.FromResource(toneAsset));
		using var recorder = new MediaElementRecorder(mediaElement);

		await HostAsync(mediaElement, async _ =>
		{
			await WaitUntilLoadedAsync(mediaElement, recorder);
			var player = GetPlayer(mediaElement);

			await mediaElement.SeekTo(TimeSpan.FromSeconds(10)).WaitAsync(TimeSpan.FromSeconds(5));

			Assert.InRange(player.CurrentPosition, toneDuration.TotalMilliseconds - 50, toneDuration.TotalMilliseconds);
			Assert.Equal(1, recorder.SeekCompletedCount);
		});
	}

	[Fact]
	public async Task SeekToTheDurationMovesToTheEndOfTheMedia()
	{
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync(element => element.Source = MediaSource.FromResource(toneAsset));
		using var recorder = new MediaElementRecorder(mediaElement);

		await HostAsync(mediaElement, async _ =>
		{
			await WaitUntilLoadedAsync(mediaElement, recorder);
			var player = GetPlayer(mediaElement);
			Assert.Equal(toneDuration, mediaElement.Duration);

			await mediaElement.SeekTo(mediaElement.Duration).WaitAsync(TimeSpan.FromSeconds(5));

			Assert.InRange(player.CurrentPosition, toneDuration.TotalMilliseconds - 50, toneDuration.TotalMilliseconds);
			Assert.Equal(1, recorder.SeekCompletedCount);
		});
	}

	[Fact]
	public async Task SeekBeforeTheStartMovesToTheStart()
	{
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync(element => element.Source = MediaSource.FromResource(toneAsset));
		using var recorder = new MediaElementRecorder(mediaElement);

		await HostAsync(mediaElement, async _ =>
		{
			await WaitUntilLoadedAsync(mediaElement, recorder);
			var player = GetPlayer(mediaElement);
			await mediaElement.SeekTo(TimeSpan.FromSeconds(1));
			Assert.Equal(1000, player.CurrentPosition);

			await mediaElement.SeekTo(TimeSpan.FromSeconds(-1)).WaitAsync(TimeSpan.FromSeconds(5));

			Assert.Equal(0, player.CurrentPosition);
			Assert.Equal(2, recorder.SeekCompletedCount);
		});
	}

	[Fact]
	public async Task SeekWithACancelledTokenThrowsWithoutMovingThePlayer()
	{
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync(element => element.Source = MediaSource.FromResource(toneAsset));
		using var recorder = new MediaElementRecorder(mediaElement);

		await HostAsync(mediaElement, async handler =>
		{
			await WaitUntilLoadedAsync(mediaElement, recorder);
			var player = GetPlayer(mediaElement);

			await Assert.ThrowsAnyAsync<OperationCanceledException>(() => handler.Manager!.Seek(TimeSpan.FromSeconds(1), new CancellationToken(canceled: true)));

			Assert.Equal(0, player.CurrentPosition);
			Assert.Equal(0, recorder.SeekCompletedCount);
		});
	}

	[Fact]
	public async Task SeekThatDoesNotCompleteEndsQuietlyWhenItsTokenIsCancelled()
	{
		SetupProbeBuilder();
		var mediaElement = await CreateQuietMediaElementAsync();
		using var recorder = new MediaElementRecorder(mediaElement);

		await WithHandlerAsync(mediaElement, async handler =>
		{
			using var cancellation = new CancellationTokenSource();
			var seek = handler.Manager!.Seek(TimeSpan.FromSeconds(1), cancellation.Token);
			Assert.False(seek.IsCompleted);
			Assert.Equal(0, recorder.SeekCompletedCount);

			cancellation.Cancel();

			// "Here, we don't want to throw an exception" (SuppressThrowing): cancelling stops the wait without an error, and the code after the wait
			// still runs, so the seek is reported as completed.
			await seek.WaitAsync(TimeSpan.FromSeconds(5));
			Assert.True(seek.IsCompletedSuccessfully);
			Assert.Equal(1, recorder.SeekCompletedCount);
		});
	}

	[Fact]
	public async Task SeekWaitsForTheSeekInProgressAndCanBeCancelledWhileWaiting()
	{
		SetupProbeBuilder();
		var mediaElement = await CreateQuietMediaElementAsync();

		await WithHandlerAsync(mediaElement, async handler =>
		{
			var manager = handler.Manager!;
			var first = manager.Seek(TimeSpan.FromSeconds(1));
			Assert.False(first.IsCompleted);

			using var cancellation = new CancellationTokenSource();
			var second = manager.Seek(TimeSpan.FromSeconds(2), cancellation.Token);
			Assert.False(second.IsCompleted);

			cancellation.Cancel();

			await Assert.ThrowsAnyAsync<OperationCanceledException>(() => second);
			Assert.False(first.IsCompleted);

			manager.OnPlaybackStateChanged(playerStateReady);
			await first.WaitAsync(TimeSpan.FromSeconds(5));
		});
	}

	[Fact]
	public async Task SeekCanStartAgainAfterAnEarlierSeekWasCancelled()
	{
		SetupProbeBuilder();
		var mediaElement = await CreateQuietMediaElementAsync();
		using var recorder = new MediaElementRecorder(mediaElement);

		await WithHandlerAsync(mediaElement, async handler =>
		{
			var manager = handler.Manager!;
			using (var cancellation = new CancellationTokenSource())
			{
				var cancelled = manager.Seek(TimeSpan.FromSeconds(1), cancellation.Token);
				cancellation.Cancel();
				await cancelled.WaitAsync(TimeSpan.FromSeconds(5));
			}

			var next = manager.Seek(TimeSpan.FromSeconds(2));
			// The cancelled seek released the semaphore: this one is in progress, not waiting for it.
			Assert.False(next.IsCompleted);
			var seeksCompletedBefore = recorder.SeekCompletedCount;

			manager.OnPlaybackStateChanged(playerStateReady);

			await next.WaitAsync(TimeSpan.FromSeconds(5));
			Assert.Equal(seeksCompletedBefore + 1, recorder.SeekCompletedCount);
		});
	}

	// Stop() idles the player, which then never reports the ready state that the interrupted seek waits for. The seek has ended all the same
	// (IMediaElement.SeekTo), as on iOS and Mac Catalyst, so it must not wait for its two minute timeout.
	[Fact]
	public async Task SeekToInterruptedByStopCompletesAndRaisesSeekCompleted()
	{
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync(element => element.Source = MediaSource.FromResource(toneAsset));
		using var recorder = new MediaElementRecorder(mediaElement);

		await HostAsync(mediaElement, async _ =>
		{
			await WaitUntilLoadedAsync(mediaElement, recorder);
			var player = GetPlayer(mediaElement);

			// Same main-thread turn: the seek still waits for the ready state when Stop() idles the player.
			var seek = mediaElement.SeekTo(TimeSpan.FromSeconds(2));
			Assert.False(seek.IsCompleted);
			mediaElement.Stop();

			await seek.WaitAsync(TimeSpan.FromSeconds(5));
			Assert.Equal(1, recorder.SeekCompletedCount);
			Assert.Equal(PlayerConsts.StateIdle, player.PlaybackState);
			Assert.Equal(0, player.CurrentPosition);
			await WaitForStateAsync(mediaElement, MediaElementState.Stopped);
		});
	}

	// Only disposing the manager ends the wait when the handler disconnects (ready never comes, MapSeekRequested passes CancellationToken.None,
	// the timeout is two minutes), so a SeekTo that is pending then ends like a cancelled seek.
	[Fact]
	public async Task SeekPendingWhenTheHandlerDisconnectsDoesNotHangTheCaller()
	{
		SetupProbeBuilder();
		var mediaElement = await CreateQuietMediaElementAsync();
		Task? seek = null;

		await WithHandlerAsync(mediaElement, handler =>
		{
			seek = mediaElement.SeekTo(TimeSpan.FromSeconds(1));
			Assert.False(seek.IsCompleted);
		});

		// Completing or being cancelled are both fine; staying pending is not.
		Assert.True(await AssertHelpers.Wait(() => seek!.IsCompleted, timeout: 5000), "The SeekTo that was pending when the handler disconnected is still pending.");
	}

	// What waits for the pending seek (MapSeekRequested, the code after the app's await SeekTo) must not run in the middle of the
	// disconnection, while the handler is half disconnected.
	// Catches: Dispose cancelling the pending seek inline instead of on a later turn of the main thread.
	[Fact]
	public async Task SeekPendingWhenTheHandlerDisconnectsEndsOnlyAfterTheDisconnection()
	{
		SetupProbeBuilder();
		var mediaElement = await CreateQuietMediaElementAsync();
		var hasDisconnected = false;
		var hadDisconnectedWhenTheSeekEnded = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

		await WithHandlerAsync(mediaElement, handler =>
		{
			var seek = mediaElement.SeekTo(TimeSpan.FromSeconds(1));
			_ = seek.ContinueWith(_ => hadDisconnectedWhenTheSeekEnded.TrySetResult(hasDisconnected), CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
			Assert.False(seek.IsCompleted);

			((IElementHandler)handler).DisconnectHandler();
			hasDisconnected = true;
		});

		Assert.True(await hadDisconnectedWhenTheSeekEnded.Task.WaitAsync(TimeSpan.FromSeconds(5)), "The SeekTo that was pending when the handler disconnected ended while DisconnectHandler was still running.");
	}

	[Fact]
	public async Task SeekWithoutAPlayerThrowsInvalidOperation()
	{
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync();

		await InvokeOnMainThreadAsync(async () =>
		{
			using var manager = new AndroidMediaManagerProbe(MauiContext, mediaElement, TestDispatcher.Current);

			var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => manager.Seek(TimeSpan.FromSeconds(1)));

			Assert.Contains("not yet initialized", exception.Message);
		});
	}

	[Fact]
	public async Task StatusUpdateKeepsThePositionWhileTheDurationIsUnknown()
	{
		SetupProbeBuilder();
		var mediaElement = await CreateQuietMediaElementAsync();

		await WithHandlerAsync(mediaElement, handler =>
		{
			((IMediaElement)mediaElement).Position = TimeSpan.FromSeconds(3);
			Assert.Equal(TimeSpan.Zero, mediaElement.Duration);

			mediaElement.Handler!.Invoke(nameof(MediaElement.StatusUpdated));

			// Duration zero means nothing is loaded: the player's position (0) must not overwrite the position.
			Assert.Equal(TimeSpan.FromSeconds(3), mediaElement.Position);
		});
	}

	[Fact]
	public async Task UpdatingNotificationsWithoutTheForegroundServiceDoesNothing()
	{
		SetupProbeBuilder();
		var mediaElement = await CreateQuietMediaElementAsync();

		await WithHandlerAsync(mediaElement, handler =>
		{
			Assert.False(mediaElement.IsAndroidForegroundServiceEnabled);

			var exception = Record.Exception(() => handler.Manager!.UpdateNotifications());

			Assert.Null(exception);
		});
	}

	// An empty player that is prepared ends at once, which would report MediaEnded (and Stopped) for a media that never existed.
	[Fact]
	public async Task PlayOfASourceWithoutDataDoesNothing()
	{
		SetupProbeBuilder();
		var mediaElement = await CreateQuietMediaElementAsync();
		using var recorder = new MediaElementRecorder(mediaElement);

		await WithHandlerAsync(mediaElement, async _ =>
		{
			var player = GetPlayer(mediaElement);
			Assert.Equal(MediaElementState.None, mediaElement.CurrentState);

			mediaElement.Play();

			var isReported = await AssertHelpers.Wait(() => recorder.EndedCount > 0 || mediaElement.CurrentState is not MediaElementState.None, timeout: 2000);
			Assert.False(isReported, $"Play() of a source without data: MediaEnded raised {recorder.EndedCount} time(s), state {mediaElement.CurrentState}.");
			Assert.Equal(PlayerConsts.StateIdle, player.PlaybackState);
		});
	}

	// Holding the artwork response keeps the source loading, so the player has no media yet when Play() is called.
	[Fact]
	public async Task PlayWhileTheSourceIsLoadingStartsPlaybackOnceItIsLoaded()
	{
		var artwork = await ReadAssetAsync(artworkAsset);
		using var server = new LoopbackHttpServer(new Dictionary<string, (byte[], string)> { ["/art.png"] = (artwork, "image/png") });
		var heldArtworkResponse = server.HoldResponses("/art.png");
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync(element =>
		{
			element.MetadataArtworkUrl = server.GetUri("/art.png").ToString();
			element.Source = MediaSource.FromResource(toneAsset);
		});
		using var recorder = new MediaElementRecorder(mediaElement);

		await HostAsync(mediaElement, async _ =>
		{
			await WaitUntilAsync(() => server.HasRequest("HEAD", "/art.png"), "The artwork of the source was never requested.");
			Assert.Equal(MediaElementState.Opening, mediaElement.CurrentState);

			mediaElement.Play();

			Assert.Equal(0, recorder.EndedCount);
			Assert.Equal(MediaElementState.Opening, mediaElement.CurrentState);

			heldArtworkResponse.SetResult();
			await WaitForStateAsync(mediaElement, MediaElementState.Playing);
			Assert.Equal(0, recorder.EndedCount);
		});
	}

	// A Source without data loads nothing, so ExoPlayer never reports "ready" by itself: a seek stays pending until the test calls
	// OnPlaybackStateChanged(ready).
	static Task<MediaElement> CreateQuietMediaElementAsync() =>
		CreateMediaElementAsync(element => element.Source = new UriMediaSource());
}