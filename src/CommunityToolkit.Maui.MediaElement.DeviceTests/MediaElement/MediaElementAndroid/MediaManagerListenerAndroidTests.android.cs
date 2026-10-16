using AndroidX.Media3.Common;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Handlers;
using CommunityToolkit.Maui.Core.Views;
using CommunityToolkit.Maui.DeviceTests.Runners;
using CommunityToolkit.Maui.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CommunityToolkit.Maui.DeviceTests;

// MediaManager's IPlayerListener implementation (MediaManager.android.cs): how ExoPlayer's callbacks become MediaElement state.
// ExoPlayer would deliver them itself, asynchronously and only for real media, so most tests call the public listener methods directly
// on the handler's MediaManager. A MediaElement whose Source is a UriMediaSource without a Uri is the quiet fixture: it has a Source
// (the listener methods ignore a MediaElement without one), but nothing is loaded, so the player stays idle and sends no callbacks of
// its own. The tests put the MediaElement into a baseline state themselves (Opening, which no mapping produces) instead of relying on the
// state that the source mapping leaves behind for a source without data (see MediaManagerSourceAndroidTests.SourceWithoutDataReportsStateNone).
[Category(TestCategory.MediaElementAndroid)]
[Collection(TestCollections.DisableParallelization)]
public class MediaManagerListenerAndroidTests : MediaElementAndroidTestBase
{
	// ExoPlayer's Player.STATE_* values are 1 to 4; the manager's OnPlayerStateChanged switch reads them as Media3 states and keeps
	// android.media.session.PlaybackState arms for the other values (see Media3PlayerStateMapsToTheMediaElementState).
	const int playerStateIdle = 1, playerStateBuffering = 2, playerStateReady = 3, playerStateEnded = 4;

	// Every arm of the switch except idle (1), which PlayerThatGoesIdleAfterPlaybackStopsTheMediaElement covers, and buffering (2) and ended (4),
	// which Media3PlayerStateMapsToTheMediaElementState covers. ExoPlayer itself only sends 1 to 4: rows 0 and 5 to 12 (and -1) reach the
	// PlaybackState arms that only another caller of this public method can reach.
	[Theory]
	[InlineData(true, 3, MediaElementState.Playing)]       // PlaybackState.StatePlaying (Media3 STATE_READY)
	[InlineData(false, 3, MediaElementState.Paused)]
	[InlineData(true, 5, MediaElementState.Playing)]       // StateRewinding
	[InlineData(false, 5, MediaElementState.Paused)]
	[InlineData(true, 9, MediaElementState.Playing)]       // StateSkippingToPrevious
	[InlineData(false, 9, MediaElementState.Paused)]
	[InlineData(true, 10, MediaElementState.Playing)]      // StateSkippingToNext
	[InlineData(false, 10, MediaElementState.Paused)]
	[InlineData(true, 11, MediaElementState.Playing)]      // StateSkippingToQueueItem
	[InlineData(false, 11, MediaElementState.Paused)]
	[InlineData(true, 6, MediaElementState.Buffering)]     // StateBuffering
	[InlineData(false, 6, MediaElementState.Buffering)]
	[InlineData(true, 8, MediaElementState.Buffering)]     // StateConnecting
	[InlineData(false, 8, MediaElementState.Buffering)]
	[InlineData(true, 0, MediaElementState.None)]          // StateNone
	[InlineData(false, 0, MediaElementState.None)]
	[InlineData(true, 7, MediaElementState.Failed)]        // StateError
	[InlineData(false, 7, MediaElementState.Failed)]
	[InlineData(true, 12, MediaElementState.None)]         // anything else
	[InlineData(false, -1, MediaElementState.None)]
	public async Task PlayerStateChangeMapsToTheMediaElementState(bool playWhenReady, int playbackState, MediaElementState expected)
	{
		SetupProbeBuilder();
		var mediaElement = await CreateQuietMediaElementAsync();

		await WithHandlerAsync(mediaElement, handler =>
		{
			SetCurrentState(mediaElement, MediaElementState.Opening);

			handler.Manager!.OnPlayerStateChanged(playWhenReady, playbackState);

			Assert.Equal(expected, mediaElement.CurrentState);
		});
	}

	// Idle (1: PlaybackState.StateStopped, Media3 STATE_IDLE) of a player that was playing or paused, which is what Player.Stop leaves behind
	// (MediaElement.Stop): the MediaElement stops. The rows start from Playing and Paused, not from Opening: ExoPlayer also reports idle while a
	// source opens, and that must not become Stopped (OpeningASourceRaisesNoSpuriousStoppedOrPausedState).
	[Theory]
	[InlineData(true, MediaElementState.Playing)]
	[InlineData(false, MediaElementState.Paused)]
	public async Task PlayerThatGoesIdleAfterPlaybackStopsTheMediaElement(bool playWhenReady, MediaElementState stateBefore)
	{
		SetupProbeBuilder();
		var mediaElement = await CreateQuietMediaElementAsync();

		await WithHandlerAsync(mediaElement, handler =>
		{
			SetCurrentState(mediaElement, stateBefore);

			handler.Manager!.OnPlayerStateChanged(playWhenReady, playerStateIdle);

			Assert.Equal(MediaElementState.Stopped, mediaElement.CurrentState);
		});
	}

	// ExoPlayer is idle until the source is prepared, and the source mapping sets PlayWhenReady before the media item: idle while the source
	// opens is not the end of playback.
	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task PlayerThatIsIdleWhileTheSourceOpensKeepsOpening(bool playWhenReady)
	{
		SetupProbeBuilder();
		var mediaElement = await CreateQuietMediaElementAsync();

		await WithHandlerAsync(mediaElement, handler =>
		{
			SetCurrentState(mediaElement, MediaElementState.Opening);

			handler.Manager!.OnPlayerStateChanged(playWhenReady, playerStateIdle);

			Assert.Equal(MediaElementState.Opening, mediaElement.CurrentState);
		});
	}

	// ExoPlayer calls OnPlayerStateChanged with Media3's Player.STATE_* values (1 idle, 2 buffering, 3 ready, 4 ended), not with
	// android.media.session.PlaybackState values (2 paused, 4 fast-forwarding): buffering is Buffering and the end of the media is Stopped.
	[Theory]
	[InlineData(true, playerStateBuffering, MediaElementState.Buffering)]
	[InlineData(false, playerStateBuffering, MediaElementState.Buffering)]
	[InlineData(true, playerStateEnded, MediaElementState.Stopped)]
	[InlineData(false, playerStateEnded, MediaElementState.Stopped)]
	public async Task Media3PlayerStateMapsToTheMediaElementState(bool playWhenReady, int playbackState, MediaElementState expected)
	{
		SetupProbeBuilder();
		var mediaElement = await CreateQuietMediaElementAsync();

		await WithHandlerAsync(mediaElement, handler =>
		{
			SetCurrentState(mediaElement, MediaElementState.Opening);

			handler.Manager!.OnPlayerStateChanged(playWhenReady, playbackState);

			Assert.Equal(expected, mediaElement.CurrentState);
		});
	}

	// The same mapping, seen through a real load: ExoPlayer reports idle (with playWhenReady set) and then buffering through OnPlayerStateChanged
	// before OnPlaybackStateChanged reports buffering. Neither may raise StateChanged with Stopped or Paused between Opening and Buffering.
	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task OpeningASourceRaisesNoSpuriousStoppedOrPausedState(bool shouldAutoPlay)
	{
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync(element =>
		{
			element.ShouldAutoPlay = shouldAutoPlay;
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

	// Only the ready state (3) copies the duration and position of the player; every other value leaves them alone.
	[Theory]
	[InlineData(-1)]
	[InlineData(0)]
	[InlineData(1)]
	[InlineData(2)]
	[InlineData(4)]
	[InlineData(5)]
	[InlineData(6)]
	[InlineData(7)]
	[InlineData(8)]
	[InlineData(9)]
	[InlineData(10)]
	[InlineData(11)]
	[InlineData(12)]
	public async Task PlayerStateOtherThanReadyKeepsTheDurationAndPosition(int playbackState)
	{
		SetupProbeBuilder();
		var mediaElement = await CreateQuietMediaElementAsync();

		await WithHandlerAsync(mediaElement, handler =>
		{
			((IMediaElement)mediaElement).Duration = TimeSpan.FromSeconds(10);
			((IMediaElement)mediaElement).Position = TimeSpan.FromSeconds(4);

			handler.Manager!.OnPlayerStateChanged(true, playbackState);

			Assert.Equal((TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(4)), (mediaElement.Duration, mediaElement.Position));
		});
	}

	[Fact]
	public async Task PlayerStateChangeStoppedKeepsAFailedMediaElementFailed()
	{
		SetupProbeBuilder();
		var mediaElement = await CreateQuietMediaElementAsync();

		await WithHandlerAsync(mediaElement, handler =>
		{
			handler.Manager!.OnPlayerError(new PlaybackException("boom", null, PlaybackException.ErrorCodeIoUnspecified));
			Assert.Equal(MediaElementState.Failed, mediaElement.CurrentState);

			handler.Manager.OnPlayerStateChanged(true, 1);

			Assert.Equal(MediaElementState.Failed, mediaElement.CurrentState);
		});
	}

	[Fact]
	public async Task PlayerStateChangeIsIgnoredWithoutASource()
	{
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync();

		await WithHandlerAsync(mediaElement, handler =>
		{
			handler.Manager!.OnPlayerStateChanged(true, playerStateReady);

			Assert.Equal(MediaElementState.None, mediaElement.CurrentState);
		});
	}

	[Fact]
	public async Task PlayerStateChangeIsIgnoredWithoutAPlayer()
	{
		SetupProbeBuilder();
		var mediaElement = await CreateQuietMediaElementAsync();
		DisposeWhenTheTestEnds(mediaElement);

		await InvokeOnMainThreadAsync(() =>
		{
			// The manager was never asked to create a player: no handler, no platform view.
			using var manager = new AndroidMediaManagerProbe(MauiContext, mediaElement, TestDispatcher.Current);
			Assert.Null(manager.ExposedPlayer);

			manager.OnPlayerStateChanged(true, playerStateReady);

			Assert.Equal(MediaElementState.None, mediaElement.CurrentState);
		});
	}

	// An idle player has no duration (a negative sentinel): ready then reports a zero duration and position.
	[Fact]
	public async Task ReadyPlayerWithoutMediaReportsZeroDurationAndPosition()
	{
		SetupProbeBuilder();
		var mediaElement = await CreateQuietMediaElementAsync();

		await WithHandlerAsync(mediaElement, handler =>
		{
			((IMediaElement)mediaElement).Duration = TimeSpan.FromSeconds(10);
			((IMediaElement)mediaElement).Position = TimeSpan.FromSeconds(4);
			Assert.True(GetPlayer(mediaElement).Duration < 0);

			handler.Manager!.OnPlayerStateChanged(false, playerStateReady);

			Assert.Equal(TimeSpan.Zero, mediaElement.Duration);
			Assert.Equal(TimeSpan.Zero, mediaElement.Position);
		});
	}

	// With nothing loaded (an empty timeline) ExoPlayer keeps a seek position as it is given (ExoPlayerImpl's masking position) and reports it as
	// its current position, a negative one too: ready then reports position zero instead of a negative position.
	[Fact]
	public async Task ReadyPlayerWithANegativePositionReportsPositionZero()
	{
		SetupProbeBuilder();
		var mediaElement = await CreateQuietMediaElementAsync();

		await WithHandlerAsync(mediaElement, handler =>
		{
			var player = GetPlayer(mediaElement);
			player.SeekTo(-1500);
			Assert.Equal(-1500, player.CurrentPosition);
			((IMediaElement)mediaElement).Position = TimeSpan.FromSeconds(4);

			handler.Manager!.OnPlayerStateChanged(false, playerStateReady);

			Assert.Equal(TimeSpan.Zero, mediaElement.Position);
		});
	}

	[Fact]
	public async Task ReadyPlayerWithMediaReportsItsDurationAndPosition()
	{
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync(element => element.Source = MediaSource.FromResource(toneAsset));
		using var recorder = new MediaElementRecorder(mediaElement);

		await HostAsync(mediaElement, async handler =>
		{
			await WaitUntilLoadedAsync(mediaElement, recorder);
			var player = GetPlayer(mediaElement);
			player.SeekTo(1200);
			((IMediaElement)mediaElement).Duration = TimeSpan.FromSeconds(10);
			((IMediaElement)mediaElement).Position = TimeSpan.FromSeconds(4);

			handler.Manager!.OnPlayerStateChanged(false, playerStateReady);

			Assert.Equal(TimeSpan.FromMilliseconds(player.Duration), mediaElement.Duration);
			Assert.Equal(TimeSpan.FromMilliseconds(player.CurrentPosition), mediaElement.Position);
			Assert.Equal(toneDuration, mediaElement.Duration);
		});
	}

	[Fact]
	public async Task PlaybackStateChangeIsIgnoredWithoutASource()
	{
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync();
		using var recorder = new MediaElementRecorder(mediaElement);

		await WithHandlerAsync(mediaElement, handler =>
		{
			handler.Manager!.OnPlaybackStateChanged(playerStateEnded);

			Assert.Equal(MediaElementState.None, mediaElement.CurrentState);
			Assert.Equal(0, recorder.EndedCount);
		});
	}

	[Fact]
	public async Task BufferingPlaybackStateMapsToBuffering()
	{
		SetupProbeBuilder();
		var mediaElement = await CreateQuietMediaElementAsync();

		await WithHandlerAsync(mediaElement, handler =>
		{
			handler.Manager!.OnPlaybackStateChanged(playerStateBuffering);

			Assert.Equal(MediaElementState.Buffering, mediaElement.CurrentState);
		});
	}

	[Fact]
	public async Task EndedPlaybackStateStopsTheMediaElementAndRaisesMediaEndedOnce()
	{
		SetupProbeBuilder();
		var mediaElement = await CreateQuietMediaElementAsync();
		using var recorder = new MediaElementRecorder(mediaElement);

		await WithHandlerAsync(mediaElement, handler =>
		{
			handler.Manager!.OnPlaybackStateChanged(playerStateEnded);

			Assert.Equal(MediaElementState.Stopped, mediaElement.CurrentState);
			Assert.Equal(1, recorder.EndedCount);
		});
	}

	// Idle (1) and the values the switch has no case for leave the state alone.
	[Theory]
	[InlineData(playerStateIdle)]
	[InlineData(0)]
	[InlineData(5)]
	public async Task OtherPlaybackStatesLeaveTheMediaElementStateUnchanged(int playbackState)
	{
		SetupProbeBuilder();
		var mediaElement = await CreateQuietMediaElementAsync();
		using var recorder = new MediaElementRecorder(mediaElement);

		await WithHandlerAsync(mediaElement, handler =>
		{
			SetCurrentState(mediaElement, MediaElementState.Opening);

			handler.Manager!.OnPlaybackStateChanged(playbackState);

			Assert.Equal(MediaElementState.Opening, mediaElement.CurrentState);
			Assert.Equal(0, recorder.EndedCount);
		});
	}

	[Fact]
	public async Task ReadyPlaybackStateCompletesAPendingSeek()
	{
		SetupProbeBuilder();
		var mediaElement = await CreateQuietMediaElementAsync();
		using var recorder = new MediaElementRecorder(mediaElement);

		await WithHandlerAsync(mediaElement, async handler =>
		{
			var seek = mediaElement.SeekTo(TimeSpan.FromSeconds(1));
			// Nothing is loaded, so ExoPlayer never reports ready: the seek waits for it.
			Assert.False(seek.IsCompleted);

			handler.Manager!.OnPlaybackStateChanged(playerStateReady);

			await seek.WaitAsync(TimeSpan.FromSeconds(5));
			Assert.Equal(1, recorder.SeekCompletedCount);
		});
	}

	[Fact]
	public async Task ReadyPlaybackStateWithoutAPendingSeekDoesNothing()
	{
		SetupProbeBuilder();
		var mediaElement = await CreateQuietMediaElementAsync();
		using var recorder = new MediaElementRecorder(mediaElement);

		await WithHandlerAsync(mediaElement, handler =>
		{
			SetCurrentState(mediaElement, MediaElementState.Opening);

			var exception = Record.Exception(() => handler.Manager!.OnPlaybackStateChanged(playerStateReady));

			Assert.Null(exception);
			Assert.Equal(MediaElementState.Opening, mediaElement.CurrentState);
			Assert.Equal(0, recorder.SeekCompletedCount);
		});
	}

	// The failure message is "<code name>, <code>, <message>" without the parts that are missing.
	[Theory]
	[InlineData("boom", PlaybackException.ErrorCodeIoUnspecified, "Error codename: ERROR_CODE_IO_UNSPECIFIED, Error code: 2000, Error message: boom")]
	[InlineData("   ", PlaybackException.ErrorCodeUnspecified, "Error codename: ERROR_CODE_UNSPECIFIED, Error code: 1000")]
	[InlineData("custom", 1234567, "Error codename: custom error code, Error code: 1234567, Error message: custom")]
	public async Task PlayerErrorRaisesMediaFailedWithTheDescriptionOfTheError(string message, int errorCode, string expectedMessage)
	{
		var logger = new CapturingLoggerFactory();
		SetupProbeBuilder(configureBuilder: builder => builder.Services.AddSingleton<ILoggerFactory>(logger));
		var mediaElement = await CreateQuietMediaElementAsync();
		using var recorder = new MediaElementRecorder(mediaElement);

		await WithHandlerAsync(mediaElement, handler =>
		{
			handler.Manager!.OnPlayerError(new PlaybackException(message, null, errorCode));

			Assert.Equal(MediaElementState.Failed, mediaElement.CurrentState);
			Assert.Equal(new[] { expectedMessage }, recorder.FailureMessages);
			Assert.Equal(new[] { (LogLevel.Error, "MediaManager", expectedMessage) }, logger.Entries);
		});
	}

	[Fact]
	public async Task PlayerErrorWithoutAMessageOmitsTheMessagePart()
	{
		var logger = new CapturingLoggerFactory();
		SetupProbeBuilder(configureBuilder: builder => builder.Services.AddSingleton<ILoggerFactory>(logger));
		var mediaElement = await CreateQuietMediaElementAsync();
		using var recorder = new MediaElementRecorder(mediaElement);

		await WithHandlerAsync(mediaElement, handler =>
		{
			handler.Manager!.OnPlayerError(new PlaybackException(null, null, PlaybackException.ErrorCodeTimeout));

			Assert.Equal(MediaElementState.Failed, mediaElement.CurrentState);
			Assert.Equal(new[] { "Error codename: ERROR_CODE_TIMEOUT, Error code: 1003" }, recorder.FailureMessages);
		});
	}

	[Fact]
	public async Task PlayerErrorWithoutDetailsRaisesMediaFailedWithAnEmptyMessage()
	{
		var logger = new CapturingLoggerFactory();
		SetupProbeBuilder(configureBuilder: builder => builder.Services.AddSingleton<ILoggerFactory>(logger));
		var mediaElement = await CreateQuietMediaElementAsync();
		using var recorder = new MediaElementRecorder(mediaElement);

		await WithHandlerAsync(mediaElement, handler =>
		{
			handler.Manager!.OnPlayerError(null);

			Assert.Equal(MediaElementState.Failed, mediaElement.CurrentState);
			Assert.Equal(new[] { string.Empty }, recorder.FailureMessages);
			Assert.Equal(new[] { (LogLevel.Error, "MediaManager", string.Empty) }, logger.Entries);
		});
	}

	[Fact]
	public async Task VideoSizeChangeUpdatesMediaWidthAndHeight()
	{
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync();

		await WithHandlerAsync(mediaElement, handler =>
		{
			handler.Manager!.OnVideoSizeChanged(new VideoSize(320, 200));

			Assert.Equal((320, 200), (mediaElement.MediaWidth, mediaElement.MediaHeight));
		});
	}

	[Fact]
	public async Task UnknownVideoSizeResetsMediaWidthAndHeightToZero()
	{
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync();

		await WithHandlerAsync(mediaElement, handler =>
		{
			handler.Manager!.OnVideoSizeChanged(new VideoSize(320, 200));

			handler.Manager.OnVideoSizeChanged(null);

			Assert.Equal((0, 0), (mediaElement.MediaWidth, mediaElement.MediaHeight));
		});
	}

	[Fact]
	public async Task VolumeChangeOfThePlayerUpdatesTheMediaElementVolume()
	{
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync();

		await WithHandlerAsync(mediaElement, handler =>
		{
			handler.Manager!.OnVolumeChanged(0.25f);

			Assert.Equal(0.25, mediaElement.Volume);
		});
	}

	[Fact]
	public async Task VolumeChangeOfThePlayerIsIgnoredWhileMuted()
	{
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync(element => element.ShouldMute = true);

		await WithHandlerAsync(mediaElement, handler =>
		{
			Assert.Equal(1, mediaElement.Volume);

			// A muted player reports volume 0: that must not overwrite the volume the user chose.
			handler.Manager!.OnVolumeChanged(0f);

			Assert.Equal(1, mediaElement.Volume);
		});
	}

	[Fact]
	public async Task VolumeChangeIsIgnoredWithoutAPlayer()
	{
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync();

		await InvokeOnMainThreadAsync(() =>
		{
			using var manager = new AndroidMediaManagerProbe(MauiContext, mediaElement, TestDispatcher.Current);

			manager.OnVolumeChanged(0.25f);

			Assert.Equal(1, mediaElement.Volume);
		});
	}

	[Fact]
	public async Task PlaybackParametersChangeWithoutParametersDoesNothing()
	{
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync(element => element.Speed = 1.5);

		await WithHandlerAsync(mediaElement, handler =>
		{
			var exception = Record.Exception(() => handler.Manager!.OnPlaybackParametersChanged(null));

			Assert.Null(exception);
			Assert.Equal(1.5, mediaElement.Speed);
			Assert.Equal(1.5f, GetPlayer(mediaElement).PlaybackParameters!.Speed);
		});
	}

	[Fact]
	public async Task PlaybackParametersChangeToTheCurrentSpeedKeepsTheSpeed()
	{
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync(element => element.Speed = 1.5);

		await WithHandlerAsync(mediaElement, handler =>
		{
			handler.Manager!.OnPlaybackParametersChanged(new PlaybackParameters(1.5f));

			Assert.Equal(1.5, mediaElement.Speed);
		});
	}

	// MediaManager.android.cs, OnPlaybackParametersChanged returns early when AreFloatingPointNumbersEqual says that the player's speed is the
	// MediaElement's. A speed that the player reports (lock-screen or notification controls, another controller of the media session) reaches
	// MediaElement.Speed (it never did while the helper returned true for numbers that differ).
	[Fact]
	public async Task PlaybackParametersChangedByThePlayerUpdateTheMediaElementSpeed()
	{
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync(element => element.Speed = 2);

		await WithHandlerAsync(mediaElement, async handler =>
		{
			var player = GetPlayer(mediaElement);

			// What a media-session controller does: it changes the speed of the player, not of the MediaElement.
			player.SetPlaybackSpeed(0.5f);
			await WaitUntilAsync(() => Math.Abs(mediaElement.Speed - 0.5) < 0.01, "MediaElement.Speed never followed the player's speed.", 2000);
		});
	}

	[Fact]
	public async Task UnusedPlayerListenerCallbacksDoNothing()
	{
		SetupProbeBuilder();
		var mediaElement = await CreateQuietMediaElementAsync();

		await WithHandlerAsync(mediaElement, handler =>
		{
			var manager = handler.Manager!;
			var player = GetPlayer(mediaElement);
			var before = Snapshot();

			manager.OnAudioAttributesChanged(null);
			manager.OnAudioSessionIdChanged(7);
			manager.OnAvailableCommandsChanged(null);
			manager.OnCues(null);
			manager.OnDeviceInfoChanged(null);
			manager.OnDeviceVolumeChanged(3, true);
			manager.OnEvents(player, null);
			manager.OnIsLoadingChanged(true);
			manager.OnIsPlayingChanged(true);
			manager.OnLoadingChanged(true);
			manager.OnMaxSeekToPreviousPositionChanged(3000);
			manager.OnMediaItemTransition(null, 1);
			manager.OnMediaMetadataChanged(null);
			manager.OnMetadata(null);
			manager.OnPlayWhenReadyChanged(true, 1);
			manager.OnPositionDiscontinuity(null, null, 1);
			manager.OnPlaybackSuppressionReasonChanged(1);
			manager.OnPlayerErrorChanged(null);
			manager.OnPlaylistMetadataChanged(null);
			manager.OnRenderedFirstFrame();
			manager.OnRepeatModeChanged(1);
			manager.OnSeekBackIncrementChanged(5000);
			manager.OnSeekForwardIncrementChanged(15000);
			manager.OnShuffleModeEnabledChanged(true);
			manager.OnSkipSilenceEnabledChanged(true);
			manager.OnSurfaceSizeChanged(10, 20);
			manager.OnTimelineChanged(null, 1);
			manager.OnTrackSelectionParametersChanged(null);
			manager.OnTracksChanged(null);

			Assert.Equal(before, Snapshot());

			(MediaElementState, double, double, TimeSpan, TimeSpan, int, int, float, bool, int, int) Snapshot() =>
				(mediaElement.CurrentState, mediaElement.Volume, mediaElement.Speed, mediaElement.Position, mediaElement.Duration, mediaElement.MediaWidth, mediaElement.MediaHeight,
					player.Volume, player.PlayWhenReady, player.PlaybackState, player.RepeatMode);
		});
	}

	// The fixture described at the top of the class.
	static Task<MediaElement> CreateQuietMediaElementAsync() =>
		CreateMediaElementAsync(element => element.Source = new UriMediaSource());
}