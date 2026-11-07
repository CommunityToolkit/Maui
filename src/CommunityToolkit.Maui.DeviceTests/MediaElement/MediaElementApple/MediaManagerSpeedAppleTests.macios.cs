using AVFoundation;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Views;
using CommunityToolkit.Maui.DeviceTests.Runners;
using CommunityToolkit.Maui.Views;
using Foundation;
using MediaPlayer;

namespace CommunityToolkit.Maui.DeviceTests;

[Category(TestCategory.MediaElementApple)]
[Collection(TestCollections.DisableParallelization)]
public class MediaManagerSpeedAppleTests : AppleMediaElementTestBase
{
	const string longAudioResource = "avplayer/long.m4a";

	AppleMediaManager CreateManager(MediaElement mediaElement) => new(MauiContext, mediaElement, TestDispatcher.Current);

	[Fact]
	public async Task SpeedChangeWhilePlayingSetsTheNativeRate()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();

		await HostAsync(mediaElement, async handler =>
		{
			var player = handler.Manager!.NativePlayer!;
			await LoadAsync(mediaElement, handler, ResourceSource(VideoResource));
			mediaElement.Play();
			await WaitUntilAsync(() => mediaElement.CurrentState is MediaElementState.Playing, "The player did not start playing.");

			mediaElement.Speed = 2;

			Assert.Equal(2f, player.Rate);
		});
	}

	// Only a Speed within the rounding margin (0.005) of the native rate is taken for the rate that the player already has.
	// Catches: a margin wide enough to swallow a real change of the Speed.
	[Fact]
	public async Task SmallSpeedChangesWhilePlayingSetTheNativeRate()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();

		await HostAsync(mediaElement, async handler =>
		{
			var player = handler.Manager!.NativePlayer!;
			await LoadAsync(mediaElement, handler, ResourceSource(VideoResource));
			mediaElement.Play();
			await WaitUntilAsync(() => mediaElement.CurrentState is MediaElementState.Playing, "The player did not start playing.");

			mediaElement.Speed = 1.25;
			Assert.Equal(1.25f, player.Rate);

			mediaElement.Speed = 1.1;
			Assert.Equal(1.1f, player.Rate);
		});
	}

	[Fact]
	public async Task SpeedChangeWhilePausedDoesNotStartPlayback()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();

		await HostAsync(mediaElement, async handler =>
		{
			var player = handler.Manager!.NativePlayer!;
			await LoadAsync(mediaElement, handler, ResourceSource(VideoResource));

			mediaElement.Speed = 2;

			Assert.Equal(0f, player.Rate);
		});
	}

	[Fact]
	public async Task SpeedChangeWhilePausedIsUsedWhenPlaybackStarts()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();

		await HostAsync(mediaElement, async handler =>
		{
			var player = handler.Manager!.NativePlayer!;
			await LoadAsync(mediaElement, handler, ResourceSource(VideoResource));
			mediaElement.Speed = 2;
			Assert.Equal(0f, player.Rate);

			mediaElement.Play();

			Assert.Equal(2f, player.Rate);
			await WaitUntilAsync(() => mediaElement.CurrentState is MediaElementState.Playing, "The player did not start playing.");
			Assert.Equal(2.0, mediaElement.Speed);
		});
	}

	[Fact]
	public async Task PositiveSpeedAfterASpeedOfZeroResumesPlayback()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();

		await HostAsync(mediaElement, async handler =>
		{
			var player = handler.Manager!.NativePlayer!;
			await LoadAsync(mediaElement, handler, ResourceSource(VideoResource));
			mediaElement.Play();
			await WaitUntilAsync(() => mediaElement.CurrentState is MediaElementState.Playing, "The player did not start playing.");

			// A Speed of 0 pauses the media, as on Android and Windows.
			mediaElement.Speed = 0;
			Assert.Equal(0f, player.Rate);

			// Play() and the playback controls still start at the last positive Speed: a default rate of 0 would not start playback at all.
			if (OperatingSystem.IsIOSVersionAtLeast(16) || OperatingSystem.IsMacCatalystVersionAtLeast(16))
			{
				Assert.Equal(1f, player.DefaultRate);
			}

			mediaElement.Speed = 1.5;

			Assert.Equal(1.5f, player.Rate);
			Assert.Equal(1.5, mediaElement.Speed);
		});
	}

	[Fact]
	public async Task FirstSpeedUpdateDoesNotApplyTheRateWhileAutoPlayIsOff()
	{
		SetupApple();
		using var mediaElement = CreateMediaElement();
		mediaElement.Speed = 2;

		await InvokeOnMainThreadAsync(async () =>
		{
			using var manager = CreateManager(mediaElement);
			var (player, _) = manager.CreatePlatformView();
			mediaElement.Source = ResourceSource(VideoResource);
			await manager.UpdateSource();
			await WaitUntilAsync(() => player.Status is AVPlayerStatus.ReadyToPlay, "The player did not become ready.");

			// The first speed a manager sees only configures it: setting the rate of a player starts it, which AutoPlay=false forbids.
			manager.UpdateSpeed();

			Assert.Equal(0f, player.Rate);
			Assert.Equal(AVPlayerTimeControlStatus.Paused, player.TimeControlStatus);
		});
	}

	[Fact]
	public async Task SpeedUpdatesBeforeAutoPlayLeaveTheStartToTheAutoPlay()
	{
		Skip.IfNot(OperatingSystem.IsIOSVersionAtLeast(16) || OperatingSystem.IsMacCatalystVersionAtLeast(16), "AVPlayer.DefaultRate needs iOS 16 or Mac Catalyst 16.");
		SetupApple();
		var mediaElement = CreateMediaElement();
		mediaElement.Speed = 2;
		mediaElement.ShouldAutoPlay = true;

		await InvokeOnMainThreadAsync(() =>
		{
			using var manager = CreateManager(mediaElement);
			var (player, _) = manager.CreatePlatformView();

			// Setting the rate starts the player: no update does before the item is ready to play. Each one sets the rate that Play() and the
			// auto play of the ready item start at (AutoPlayStartsPlaybackAtTheConfiguredSpeed).
			manager.UpdateSpeed();
			manager.UpdateSpeed();

			Assert.Equal(0f, player.Rate);
			Assert.Equal(2f, player.DefaultRate);
		});
	}

	[Fact]
	public async Task SpeedUpdateWithoutPlayerViewControllerDoesNothing()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();
		mediaElement.Speed = 2;

		await InvokeOnMainThreadAsync(() =>
		{
			using var manager = CreateManager(mediaElement);

			Assert.Null(Record.Exception(manager.UpdateSpeed));
			Assert.Null(manager.NativePlayerViewController);
		});
	}

	[Fact]
	public async Task NativeRateChangeIsReflectedInTheMediaElementSpeed()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();

		await HostAsync(mediaElement, async handler =>
		{
			var player = handler.Manager!.NativePlayer!;
			await LoadAsync(mediaElement, handler, ResourceSource(VideoResource));
			mediaElement.Play();
			await WaitUntilAsync(() => mediaElement.CurrentState is MediaElementState.Playing, "The player did not start playing.");

			// What the system playback controls do when the user picks a speed.
			player.Rate = 1.5f;

			await WaitUntilAsync(() => Math.Abs(mediaElement.Speed - 1.5) < 0.01, "MediaElement.Speed did not follow the native rate.", timeout: 2000);
			Assert.Equal(1.5f, GetMetadata(handler.Manager).NowPlayingInfo.PlaybackRate);
			Assert.Equal(1.5f, MPNowPlayingInfoCenter.DefaultCenter.NowPlaying?.PlaybackRate);
		});
	}

	[Fact]
	public async Task NativeRateChangeOffTheMainThreadIsReflectedInTheMediaElementSpeedOnTheMainThread()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();
		mediaElement.ShouldMute = true;
		var speedChangesOnTheMainThread = new List<bool>();
		mediaElement.PropertyChanged += (_, e) =>
		{
			if (e.PropertyName == nameof(MediaElement.Speed))
			{
				lock (speedChangesOnTheMainThread)
				{
					speedChangesOnTheMainThread.Add(NSThread.IsMain);
				}
			}
		};

		await HostAsync(mediaElement, async handler =>
		{
			var player = handler.Manager!.NativePlayer!;
			await LoadAsync(mediaElement, handler, MediaSource.FromResource(longAudioResource));
			mediaElement.Play();
			await WaitUntilAsync(() => mediaElement.CurrentState is MediaElementState.Playing, "The player did not start playing.");

			// AVPlayer posts its rate notification on the thread that changed the rate.
			await Task.Run(() => player.Rate = 1.5f);

			await WaitUntilAsync(() => Math.Abs(mediaElement.Speed - 1.5) < 0.01, "MediaElement.Speed did not follow the native rate.", timeout: 2000);
			Assert.Equal(1.5f, GetMetadata(handler.Manager).NowPlayingInfo.PlaybackRate);
			Assert.Equal(1.5f, MPNowPlayingInfoCenter.DefaultCenter.NowPlaying?.PlaybackRate);
			lock (speedChangesOnTheMainThread)
			{
				Assert.All(speedChangesOnTheMainThread, static isMainThread => Assert.True(isMainThread, "MediaElement.Speed changed off the main thread."));
			}

			mediaElement.Pause();
		});
	}

	[Fact]
	public async Task NativePauseDoesNotChangeTheSpeed()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();

		await HostAsync(mediaElement, async handler =>
		{
			var player = handler.Manager!.NativePlayer!;
			await LoadAsync(mediaElement, handler, ResourceSource(VideoResource));
			mediaElement.Play();
			await WaitUntilAsync(() => mediaElement.CurrentState is MediaElementState.Playing, "The player did not start playing.");
			mediaElement.Speed = 1.5;
			Assert.Equal(1.5f, player.Rate);

			// What the pause button of the system playback controls does: it sets the native rate to 0, which is not a speed.
			player.Pause();
			await DrainMainQueueAsync();

			Assert.Equal(0f, player.Rate);
			Assert.Equal(1.5, mediaElement.Speed);
		});
	}

	[Fact]
	public async Task RateChangeOfAnotherPlayerDoesNotChangeTheSpeed()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();

		await HostAsync(mediaElement, async handler =>
		{
			await LoadAsync(mediaElement, handler, ResourceSource(VideoResource));
			using var otherPlayer = new AVPlayer();
			var notifications = 0;
			var observer = NSNotificationCenter.DefaultCenter.AddObserver(AVPlayer.RateDidChangeNotification, _ => notifications++, otherPlayer);

			try
			{
				// The rate notification of every AVPlayer in the app reaches the manager's observer, for example that of another MediaElement.
				otherPlayer.Rate = 1.5f;
				await DrainMainQueueAsync();

				Assert.NotEqual(0, notifications);
				Assert.Equal(1.0, mediaElement.Speed);
			}
			finally
			{
				NSNotificationCenter.DefaultCenter.RemoveObserver(observer);
				otherPlayer.Pause();
			}
		});
	}

	[Fact]
	public async Task RateChangeIsIgnoredWhileTheManagerHasNoPlayer()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();

		await HostAsync(mediaElement, async handler =>
		{
			var manager = handler.Manager!;
			var player = manager.NativePlayer!;
			await LoadAsync(mediaElement, handler, ResourceSource(VideoResource));
			var metadata = GetMetadata(manager);
			metadata.NowPlayingInfo.PlaybackRate = 0.25f;
			manager.NativePlayer = null;

			NSNotificationCenter.DefaultCenter.PostNotificationName(AVPlayer.RateDidChangeNotification, player);

			Assert.Equal(0.25f, metadata.NowPlayingInfo.PlaybackRate);
			manager.NativePlayer = player;
		});
	}

	[Fact]
	public async Task RateChangeWithoutMetadataDoesNotThrow()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();
		mediaElement.Speed = 0;

		await InvokeOnMainThreadAsync(() =>
		{
			using var manager = CreateManager(mediaElement);
			manager.CreatePlatformView();

			// UpdateSource has not run, so there is no Metadata; the player's rate (0) equals the speed (0).
			var exception = Record.Exception(() => NSNotificationCenter.DefaultCenter.PostNotificationName(AVPlayer.RateDidChangeNotification, manager.NativePlayer));

			Assert.Null(exception);
		});
	}

	[Fact]
	public async Task AutoPlayStartsPlaybackAtTheConfiguredSpeed()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();
		mediaElement.ShouldAutoPlay = true;
		mediaElement.Speed = 2;

		await HostAsync(mediaElement, async handler =>
		{
			var player = handler.Manager!.NativePlayer!;

			await LoadAsync(mediaElement, handler, ResourceSource(VideoResource));

			await WaitUntilAsync(() => player.TimeControlStatus is AVPlayerTimeControlStatus.Playing, "The player did not start playing.");
			Assert.Equal(2f, player.Rate);
		});
	}

	[Fact]
	public async Task AutoPlayWithASpeedOfZeroLeavesTheMediaPausedAndKeepsTheSpeed()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();
		mediaElement.ShouldAutoPlay = true;
		mediaElement.Speed = 0;

		await HostAsync(mediaElement, async handler =>
		{
			var player = handler.Manager!.NativePlayer!;

			await LoadAsync(mediaElement, handler, ResourceSource(VideoResource));
			await DrainMainQueueAsync();

			Assert.Equal(0.0, mediaElement.Speed);
			Assert.Equal(0f, player.Rate);
			Assert.Equal(MediaElementState.Paused, mediaElement.CurrentState);

			// The default rate that the playback controls start at is not set to 0: it stays AVPlayer's own default of 1.
			if (OperatingSystem.IsIOSVersionAtLeast(16) || OperatingSystem.IsMacCatalystVersionAtLeast(16))
			{
				Assert.Equal(1f, player.DefaultRate);
			}

			mediaElement.Speed = 1.5;

			Assert.Equal(1.5f, player.Rate);
			mediaElement.Pause();
		});
	}

	[Fact]
	public async Task PlayAfterAPauseContinuesAtTheConfiguredSpeed()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();

		await HostAsync(mediaElement, async handler =>
		{
			var player = handler.Manager!.NativePlayer!;
			await LoadAsync(mediaElement, handler, ResourceSource(VideoResource));
			mediaElement.Play();
			await WaitUntilAsync(() => mediaElement.CurrentState is MediaElementState.Playing, "The player did not start playing.");
			mediaElement.Speed = 2;
			Assert.Equal(2f, player.Rate);
			mediaElement.Pause();
			Assert.Equal(0f, player.Rate);

			mediaElement.Play();

			Assert.Equal(2f, player.Rate);
		});
	}

	// Stop() pauses and rewinds the media; the next Play() starts at the Speed again, not at a rate of 1 (#3326).
	[Fact]
	public async Task PlayAfterStopStartsAtTheConfiguredSpeed()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();

		await HostAsync(mediaElement, async handler =>
		{
			var player = handler.Manager!.NativePlayer!;
			await LoadAsync(mediaElement, handler, ResourceSource(VideoResource));
			mediaElement.Play();
			await WaitUntilAsync(() => mediaElement.CurrentState is MediaElementState.Playing, "The player did not start playing.");
			mediaElement.Speed = 2;
			Assert.Equal(2f, player.Rate);
			mediaElement.Stop();
			Assert.Equal(0f, player.Rate);

			mediaElement.Play();

			Assert.Equal(2f, player.Rate);
			Assert.Equal(2.0, mediaElement.Speed);
		});
	}

	[Fact]
	public async Task LoopingRestartsPlaybackAtTheConfiguredSpeed()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();
		mediaElement.ShouldLoopPlayback = true;

		await HostAsync(mediaElement, async handler =>
		{
			var player = handler.Manager!.NativePlayer!;
			await LoadAsync(mediaElement, handler, ResourceSource(VideoResource));
			mediaElement.Speed = 2;
			await mediaElement.SeekTo(TimeSpan.FromMilliseconds(1700));

			mediaElement.Play();

			await WaitUntilAsync(() => player.CurrentTime.Seconds < 1.0 && player.Rate > 0, "The media did not loop.");
			Assert.Equal(2f, player.Rate);
			Assert.Equal(2.0, mediaElement.Speed);
			mediaElement.Pause();
		});
	}
}