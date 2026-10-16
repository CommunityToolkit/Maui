using AVFoundation;
using CommunityToolkit.Maui.Core.Views;
using CommunityToolkit.Maui.DeviceTests.Runners;
using CommunityToolkit.Maui.Views;
using CoreMedia;
using Foundation;
using UIKit;

namespace CommunityToolkit.Maui.DeviceTests;

// MediaManager.macios.cs: the Platform* property updates (aspect, controls, volume, mute, screen-on, loop) and the position/duration
// reporting. Handler-based tests change the MediaElement property and read the native AVPlayer / AVPlayerViewController / UIApplication
// state; manager-only tests cover the guards for a manager that has no player.
[Category(TestCategory.MediaElementApple)]
[Collection(TestCollections.DisableParallelization)]
public class MediaManagerPropertiesAppleTests : AppleMediaElementTestBase
{
	AppleMediaManager CreateManager(MediaElement mediaElement) => new(MauiContext, mediaElement, TestDispatcher.Current);

	static NSValue Range(double startInSeconds, double durationInSeconds) =>
		NSValue.FromCMTimeRange(new CMTimeRange { Start = CMTime.FromSeconds(startInSeconds, 1000), Duration = CMTime.FromSeconds(durationInSeconds, 1000) });

	[Theory]
	[InlineData(Aspect.AspectFit, AVLayerVideoGravity.ResizeAspect)]
	[InlineData(Aspect.AspectFill, AVLayerVideoGravity.ResizeAspectFill)]
	[InlineData(Aspect.Fill, AVLayerVideoGravity.Resize)]
	[InlineData((Aspect)99, AVLayerVideoGravity.ResizeAspect)]
	public async Task AspectChangeSetsTheVideoGravityOfThePlayerViewController(Aspect aspect, AVLayerVideoGravity expectedGravity)
	{
		SetupApple();
		var mediaElement = CreateMediaElement();

		await HostAsync(mediaElement, async handler =>
		{
			var controller = handler.Manager!.NativePlayerViewController!;
			await handler.Manager.TakeSourceUpdates();

			// Start from a different aspect so that the property change reaches the handler.
			mediaElement.Aspect = aspect is Aspect.Fill ? Aspect.AspectFill : Aspect.Fill;
			Assert.NotEqual(expectedGravity, controller.VideoGravity);

			mediaElement.Aspect = aspect;

			Assert.Equal(expectedGravity, controller.VideoGravity);
		});
	}

	[Fact]
	public async Task AspectUpdateWithoutPlayerViewControllerDoesNothing()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();
		mediaElement.Aspect = Aspect.Fill;

		await InvokeOnMainThreadAsync(() =>
		{
			using var manager = CreateManager(mediaElement);

			Assert.Null(Record.Exception(manager.UpdateAspect));
			Assert.Null(manager.NativePlayerViewController);
		});
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task ShouldShowPlaybackControlsChangeShowsOrHidesTheNativeControls(bool shouldShow)
	{
		SetupApple();
		var mediaElement = CreateMediaElement();
		mediaElement.ShouldShowPlaybackControls = !shouldShow;

		await HostAsync(mediaElement, async handler =>
		{
			var controller = handler.Manager!.NativePlayerViewController!;
			await handler.Manager.TakeSourceUpdates();
			Assert.Equal(!shouldShow, controller.ShowsPlaybackControls);

			mediaElement.ShouldShowPlaybackControls = shouldShow;

			Assert.Equal(shouldShow, controller.ShowsPlaybackControls);
		});
	}

	[Fact]
	public async Task ShouldShowPlaybackControlsUpdateWithoutPlayerViewControllerDoesNothing()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();
		mediaElement.ShouldShowPlaybackControls = true;

		await InvokeOnMainThreadAsync(() =>
		{
			using var manager = CreateManager(mediaElement);

			Assert.Null(Record.Exception(manager.UpdateShouldShowPlaybackControls));
		});
	}

	[Theory]
	[InlineData(0.0)]
	[InlineData(0.25)]
	[InlineData(1.0)]
	public async Task VolumeChangeSetsTheVolumeOfThePlayer(double volume)
	{
		SetupApple();
		var mediaElement = CreateMediaElement();
		mediaElement.Volume = 0.6;

		await HostAsync(mediaElement, async handler =>
		{
			var player = handler.Manager!.NativePlayer!;
			await handler.Manager.TakeSourceUpdates();
			Assert.Equal(0.6, player.Volume, precision: 3);

			mediaElement.Volume = volume;

			Assert.Equal(volume, player.Volume, precision: 3);
		});
	}

	[Fact]
	public async Task VolumeChangeWithinTheToleranceLeavesThePlayerVolumeAlone()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();
		mediaElement.Volume = 0.5;

		await HostAsync(mediaElement, async handler =>
		{
			var player = handler.Manager!.NativePlayer!;
			await handler.Manager.TakeSourceUpdates();
			Assert.Equal(0.5, player.Volume, precision: 3);

			mediaElement.Volume = 0.505;

			// A difference of at most 0.01 is not written to the player.
			Assert.Equal(0.5, player.Volume, precision: 3);
			Assert.Equal(0.505, mediaElement.Volume, precision: 3);
		});
	}

	[Fact]
	public async Task VolumeUpdateWithoutPlayerDoesNothing()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();
		mediaElement.Volume = 0.3;

		await InvokeOnMainThreadAsync(() =>
		{
			using var manager = CreateManager(mediaElement);

			Assert.Null(Record.Exception(manager.UpdateVolume));
		});
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task ShouldMuteChangeMutesOrUnmutesThePlayer(bool shouldMute)
	{
		SetupApple();
		var mediaElement = CreateMediaElement();
		mediaElement.ShouldMute = !shouldMute;

		await HostAsync(mediaElement, async handler =>
		{
			var player = handler.Manager!.NativePlayer!;
			await handler.Manager.TakeSourceUpdates();
			Assert.Equal(!shouldMute, player.Muted);

			mediaElement.ShouldMute = shouldMute;

			Assert.Equal(shouldMute, player.Muted);
		});
	}

	[Fact]
	public async Task ShouldMuteUpdateWithoutPlayerDoesNothing()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();
		mediaElement.ShouldMute = true;

		await InvokeOnMainThreadAsync(() =>
		{
			using var manager = CreateManager(mediaElement);

			Assert.Null(Record.Exception(manager.UpdateShouldMute));
		});
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task ShouldKeepScreenOnChangeDisablesOrEnablesTheIdleTimer(bool shouldKeepScreenOn)
	{
		SetupApple();
		var mediaElement = CreateMediaElement();

		try
		{
			await HostAsync(mediaElement, async handler =>
			{
				await handler.Manager!.TakeSourceUpdates();
				UIApplication.SharedApplication.IdleTimerDisabled = !shouldKeepScreenOn;
				mediaElement.ShouldKeepScreenOn = !shouldKeepScreenOn;
				Assert.Equal(!shouldKeepScreenOn, UIApplication.SharedApplication.IdleTimerDisabled);

				mediaElement.ShouldKeepScreenOn = shouldKeepScreenOn;

				Assert.Equal(shouldKeepScreenOn, UIApplication.SharedApplication.IdleTimerDisabled);
			});
		}
		finally
		{
			await InvokeOnMainThreadAsync(() => UIApplication.SharedApplication.IdleTimerDisabled = false);
		}
	}

	[Fact]
	public async Task ShouldKeepScreenOnUpdateWithoutPlayerLeavesTheIdleTimerAlone()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();
		mediaElement.ShouldKeepScreenOn = true;

		await InvokeOnMainThreadAsync(() =>
		{
			using var manager = CreateManager(mediaElement);
			UIApplication.SharedApplication.IdleTimerDisabled = false;

			manager.UpdateShouldKeepScreenOn();

			Assert.False(UIApplication.SharedApplication.IdleTimerDisabled);
		});
	}

	[Fact]
	public async Task ShouldLoopPlaybackChangeLeavesTheNativePlayerAlone()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();

		await HostAsync(mediaElement, async handler =>
		{
			var player = handler.Manager!.NativePlayer!;
			await LoadAsync(mediaElement, handler, ResourceSource(VideoResource));
			var actionAtItemEnd = player.ActionAtItemEnd;

			mediaElement.ShouldLoopPlayback = true;
			handler.Manager.UpdateShouldLoopPlayback();

			// Looping is done by the played-to-end handler, not by a player setting.
			Assert.Equal(actionAtItemEnd, player.ActionAtItemEnd);
			Assert.Equal(0f, player.Rate);
			Assert.Same(handler.Manager.NativePlayer, player);
		});
	}

	[Fact]
	public async Task UpdateStatusReportsTheDurationAndPositionOfTheLoadedMedia()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();

		await HostAsync(mediaElement, async handler =>
		{
			await LoadAsync(mediaElement, handler, ResourceSource(VideoResource));
			await mediaElement.SeekTo(TimeSpan.FromSeconds(1));

			handler.Manager!.UpdateStatus();

			Assert.Equal(VideoDuration.TotalSeconds, mediaElement.Duration.TotalSeconds, precision: 1);
			Assert.Equal(1.0, mediaElement.Position.TotalSeconds, precision: 1);
		});
	}

	[Fact]
	public async Task UpdateStatusWithoutItemResetsDurationAndPosition()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();

		await HostAsync(mediaElement, async handler =>
		{
			var manager = handler.Manager!;
			await LoadAsync(mediaElement, handler, ResourceSource(VideoResource));
			await mediaElement.SeekTo(TimeSpan.FromSeconds(1));
			manager.UpdateStatus();
			Assert.NotEqual(TimeSpan.Zero, mediaElement.Duration);
			Assert.NotEqual(TimeSpan.Zero, mediaElement.Position);

			mediaElement.Source = null;
			await manager.TakeSourceUpdates();
			manager.UpdateStatus();

			Assert.Equal(TimeSpan.Zero, mediaElement.Duration);
			Assert.Equal(TimeSpan.Zero, mediaElement.Position);
		});
	}

	[Fact]
	public async Task UpdateStatusWithoutItemPausesThePlayer()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();

		await HostAsync(mediaElement, async handler =>
		{
			var manager = handler.Manager!;
			var player = manager.NativePlayer!;
			await manager.TakeSourceUpdates();
			Assert.Null(manager.NativePlayerItem);
			player.Play();
			Assert.NotEqual(0f, player.Rate);

			manager.UpdateStatus();

			Assert.Equal(0f, player.Rate);
		});
	}

	[Fact]
	public async Task UpdateStatusOfLiveMediaUsesTheLastSeekableRange()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();

		await InvokeOnMainThreadAsync(() =>
		{
			using var manager = CreateManager(mediaElement);
			manager.CreatePlatformView();
			manager.NativePlayerItem = new AppleFakeAVPlayerItem(AVAsset.FromUrl(NSUrl.FromFilename(BundlePath("video", "mp4"))))
			{
				// A live stream has no duration, only a window of seekable time.
				DurationOverride = CMTime.Indefinite,
				CurrentTimeOverride = CMTime.FromSeconds(12, 1000),
				SeekableTimeRangesOverride = [Range(0, 5), Range(10, 30)]
			};

			manager.UpdateStatus();

			Assert.Equal(TimeSpan.FromSeconds(30), mediaElement.Duration);
			Assert.Equal(TimeSpan.FromSeconds(12), mediaElement.Position);
		});
	}

	[Fact]
	public async Task UpdateStatusOfLiveMediaWithoutSeekableRangeKeepsTheLastValues()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();

		await InvokeOnMainThreadAsync(() =>
		{
			using var manager = CreateManager(mediaElement);
			manager.CreatePlatformView();
			var asset = AVAsset.FromUrl(NSUrl.FromFilename(BundlePath("video", "mp4")));
			manager.NativePlayerItem = new AppleFakeAVPlayerItem(asset)
			{
				DurationOverride = CMTime.FromSeconds(100, 1000),
				CurrentTimeOverride = CMTime.FromSeconds(50, 1000)
			};
			manager.UpdateStatus();
			Assert.Equal(TimeSpan.FromSeconds(100), mediaElement.Duration);

			manager.NativePlayerItem = new AppleFakeAVPlayerItem(asset)
			{
				DurationOverride = CMTime.Indefinite,
				CurrentTimeOverride = CMTime.FromSeconds(7, 1000),
				SeekableTimeRangesOverride = []
			};
			manager.UpdateStatus();

			Assert.Equal(TimeSpan.FromSeconds(100), mediaElement.Duration);
			Assert.Equal(TimeSpan.FromSeconds(50), mediaElement.Position);
		});
	}

	[Fact]
	public async Task UpdateStatusTurnsInvalidTimesIntoZero()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();

		await InvokeOnMainThreadAsync(() =>
		{
			using var manager = CreateManager(mediaElement);
			manager.CreatePlatformView();
			var asset = AVAsset.FromUrl(NSUrl.FromFilename(BundlePath("video", "mp4")));
			manager.NativePlayerItem = new AppleFakeAVPlayerItem(asset)
			{
				DurationOverride = CMTime.FromSeconds(100, 1000),
				CurrentTimeOverride = CMTime.FromSeconds(50, 1000)
			};
			manager.UpdateStatus();
			Assert.Equal(TimeSpan.FromSeconds(50), mediaElement.Position);

			manager.NativePlayerItem = new AppleFakeAVPlayerItem(asset)
			{
				DurationOverride = CMTime.Invalid,
				CurrentTimeOverride = CMTime.Invalid
			};
			manager.UpdateStatus();

			// An invalid CMTime has NaN seconds.
			Assert.Equal(TimeSpan.Zero, mediaElement.Duration);
			Assert.Equal(TimeSpan.Zero, mediaElement.Position);
		});
	}

	[Fact]
	public async Task UpdateStatusWithoutPlayerKeepsTheValues()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();

		await InvokeOnMainThreadAsync(() =>
		{
			using var manager = CreateManager(mediaElement);
			manager.CreatePlatformView();
			var asset = AVAsset.FromUrl(NSUrl.FromFilename(BundlePath("video", "mp4")));
			manager.NativePlayerItem = new AppleFakeAVPlayerItem(asset)
			{
				DurationOverride = CMTime.FromSeconds(100, 1000),
				CurrentTimeOverride = CMTime.FromSeconds(50, 1000)
			};
			manager.UpdateStatus();
			var player = manager.NativePlayer;
			manager.NativePlayer = null;
			manager.NativePlayerItem = new AppleFakeAVPlayerItem(asset)
			{
				DurationOverride = CMTime.FromSeconds(7, 1000),
				CurrentTimeOverride = CMTime.FromSeconds(3, 1000)
			};

			manager.UpdateStatus();

			Assert.Equal(TimeSpan.FromSeconds(100), mediaElement.Duration);
			Assert.Equal(TimeSpan.FromSeconds(50), mediaElement.Position);
			manager.NativePlayer = player;
		});
	}
}