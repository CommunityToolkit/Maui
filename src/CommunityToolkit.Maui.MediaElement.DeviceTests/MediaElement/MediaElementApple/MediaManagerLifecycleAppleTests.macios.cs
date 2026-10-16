using System.Reflection;
using AVFoundation;
using CommunityToolkit.Maui.Core.Views;
using CommunityToolkit.Maui.DeviceTests.Runners;
using CommunityToolkit.Maui.Views;
using Foundation;
using Microsoft.Extensions.Logging;
using ObjCRuntime;
using UIKit;

namespace CommunityToolkit.Maui.DeviceTests;

// MediaManager.macios.cs: CreatePlatformView, Dispose(bool) and the observer registration helpers. The manager is created
// directly (its public API) so that the tests see exactly what CreatePlatformView does, without the handler's property mapper.
[Category(TestCategory.MediaElementApple)]
[Collection(TestCollections.DisableParallelization)]
public class MediaManagerLifecycleAppleTests : AppleMediaElementTestBase
{
	AppleMediaManager CreateManager(MediaElement mediaElement) => new(MauiContext, mediaElement, TestDispatcher.Current);

	[Fact]
	public async Task CreatePlatformViewReturnsAnAvPlayerHostedByThePlayerViewController()
	{
		SetupApple();

		await InvokeOnMainThreadAsync(() =>
		{
			using var manager = CreateManager(CreateMediaElement());

			var (player, controller) = manager.CreatePlatformView();

			Assert.IsType<AVPlayer>(player);
			Assert.Same(player, controller.Player);
			Assert.Same(player, manager.NativePlayer);
			Assert.Same(controller, manager.NativePlayerViewController);
		});
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task CreatePlatformViewAppliesTheMutedStateOfTheMediaElement(bool shouldMute)
	{
		SetupApple();

		await InvokeOnMainThreadAsync(() =>
		{
			using var manager = CreateManager(new MediaElement { ShouldMute = shouldMute });

			var (player, _) = manager.CreatePlatformView();

			Assert.Equal(shouldMute, player.Muted);
		});
	}

	[Theory]
	[InlineData(0.0)]
	[InlineData(0.25)]
	[InlineData(0.98)]
	public async Task CreatePlatformViewAppliesTheVolumeOfTheMediaElement(double volume)
	{
		SetupApple();

		await InvokeOnMainThreadAsync(() =>
		{
			using var manager = CreateManager(new MediaElement { Volume = volume });

			var (player, _) = manager.CreatePlatformView();

			Assert.Equal(volume, player.Volume, precision: 3);
		});
	}

	[Theory]
	[InlineData(1.0)]
	[InlineData(0.995)]
	public async Task CreatePlatformViewLeavesThePlayerVolumeAloneWithinTheTolerance(double volume)
	{
		SetupApple();

		await InvokeOnMainThreadAsync(() =>
		{
			using var manager = CreateManager(new MediaElement { Volume = volume });

			var (player, _) = manager.CreatePlatformView();

			// A new AVPlayer is at full volume; a difference of at most 0.01 is not written to the player.
			Assert.Equal(1.0f, player.Volume);
		});
	}

	[Fact]
	public async Task CreatePlatformViewConfiguresTheNowPlayingInfoCenterPolicyOfThePlatform()
	{
		SetupApple();

		await InvokeOnMainThreadAsync(() =>
		{
			using var manager = CreateManager(CreateMediaElement());

			var (_, controller) = manager.CreatePlatformView();

#if IOS
			// iOS: the toolkit publishes the Now Playing info itself (Metadata), so the controller must not.
			Assert.False(controller.UpdatesNowPlayingInfoCenter);
#else
			// Mac Catalyst: the controller keeps publishing it. True is also AVPlayerViewController's default, so this arm documents the
			// platform's state rather than proving the assignment (the coverage file lists the assignment as not observable).
			Assert.True(controller.UpdatesNowPlayingInfoCenter);
#endif
		});
	}

	[Fact]
	public async Task CreatePlatformViewSwitchesTheAudioSessionToPlayback()
	{
		SetupApple();

		await InvokeOnMainThreadAsync(() =>
		{
			var session = AVAudioSession.SharedInstance();
			session.SetCategory(AVAudioSessionCategory.Ambient);
			Assert.Equal(AVAudioSessionCategory.Ambient.GetConstant()!.ToString(), session.Category.ToString());
			using var manager = CreateManager(CreateMediaElement());

			manager.CreatePlatformView();

			Assert.Equal(AVAudioSessionCategory.Playback.GetConstant()!.ToString(), session.Category.ToString());
		});
	}

	[Fact]
	public async Task CreatePlatformViewObservesTheErrorNotificationsOfPlayerItems()
	{
		SetupApple();

		await InvokeOnMainThreadAsync(() =>
		{
			using var manager = CreateManager(CreateMediaElement());
			manager.CreatePlatformView();

			NSNotificationCenter.DefaultCenter.PostNotificationName(AVPlayerItem.PlaybackStalledNotification, null);

			Assert.Single(Logs.Entries, entry => entry.Level is LogLevel.Warning);
		});
	}

	[Fact]
	public async Task CreatePlatformViewTwiceReplacesTheErrorObserversInsteadOfAddingMore()
	{
		SetupApple();

		await InvokeOnMainThreadAsync(() =>
		{
			using var manager = CreateManager(CreateMediaElement());
			manager.CreatePlatformView();
			manager.CreatePlatformView();

			NSNotificationCenter.DefaultCenter.PostNotificationName(AVPlayerItem.PlaybackStalledNotification, null);

			// Both calls registered a stalled-playback observer; the second call must have removed the first one's.
			Assert.Single(Logs.Entries, entry => entry.Level is LogLevel.Warning);
		});
	}

	[Fact]
	public async Task CreatePlatformViewTwiceReplacesThePlayedToEndObserverInsteadOfAddingAnother()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();
		var events = new MediaElementEvents(mediaElement);
		AppleMediaManager? manager = null;

		try
		{
			await InvokeOnMainThreadAsync(() =>
			{
				manager = CreateManager(mediaElement);
				manager.CreatePlatformView();
				manager.CreatePlatformView();

				// The notification names the item the (second) player is playing.
				var item = new AVPlayerItem(AVAsset.FromUrl(NSUrl.FromFilename(BundlePath("video", "mp4"))));
				manager.NativePlayerViewController!.Player!.ReplaceCurrentItemWithPlayerItem(item);
				NSNotificationCenter.DefaultCenter.PostNotificationName(AVPlayerItem.DidPlayToEndTimeNotification, item);
			});

			await WaitUntilAsync(() => events.Ended > 0, "The played-to-end notification was not handled.");
			await InvokeOnMainThreadAsync(() => { });

			Assert.Equal(1, events.Ended);
		}
		finally
		{
			await InvokeOnMainThreadAsync(() => manager?.Dispose());
		}
	}

	[Fact]
	public async Task CreatePlatformViewRegistersTheObserversOfThePlayer()
	{
		SetupApple();

		await InvokeOnMainThreadAsync(() =>
		{
			using var manager = CreateManager(CreateMediaElement());

			manager.CreatePlatformView();

			Assert.Equal(NSKeyValueObservingOptions.Initial | NSKeyValueObservingOptions.New, manager.NativeValueObserverOptions);
			Assert.NotNull(manager.NativeMutedObserver);
			Assert.NotNull(manager.NativeVolumeObserver);
			Assert.NotNull(manager.NativeStatusObserver);
			Assert.NotNull(manager.NativeTimeControlStatusObserver);
			Assert.NotNull(manager.NativeRateObserver);
			Assert.NotNull(manager.NativePlayedToEndObserver);
			Assert.NotNull(manager.NativeErrorObserver);
			Assert.NotNull(manager.NativeItemFailedToPlayToEndTimeObserver);
			Assert.NotNull(manager.NativePlaybackStalledObserver);

			// There is no item to observe yet.
			Assert.Null(manager.NativeCurrentItemErrorObserver);
		});
	}

	[Fact]
	public async Task AddStatusObserversWithoutPlayerRegistersNothing()
	{
		SetupApple();

		await InvokeOnMainThreadAsync(() =>
		{
			using var manager = CreateManager(CreateMediaElement());

			typeof(MediaManager).GetMethod("AddStatusObservers", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(manager, null);

			Assert.Null(manager.NativeMutedObserver);
			Assert.Null(manager.NativeVolumeObserver);
			Assert.Null(manager.NativeStatusObserver);
			Assert.Null(manager.NativeTimeControlStatusObserver);
			Assert.Null(manager.NativeRateObserver);
		});
	}

	[Fact]
	public async Task LoadingASourceObservesTheErrorOfItsItem()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();

		await HostAsync(mediaElement, async handler =>
		{
			var manager = handler.Manager!;
			await manager.TakeSourceUpdates();
			Assert.Null(manager.NativeCurrentItemErrorObserver);

			await LoadAsync(mediaElement, handler, ResourceSource(VideoResource));
			var firstObserver = manager.NativeCurrentItemErrorObserver;
			Assert.NotNull(firstObserver);

			await LoadAsync(mediaElement, handler, ResourceSource(AudioResource));
			var secondObserver = manager.NativeCurrentItemErrorObserver;
			Assert.NotNull(secondObserver);
			Assert.NotSame(firstObserver, secondObserver);

			// The observer of the replaced item was disposed: AddObserver returns an NSObject that removes the observation and releases its
			// native handle in Dispose. The test keeps a reference, so only Dispose can clear the handle.
			Assert.Equal(NativeHandle.Zero, Assert.IsType<NSObject>(firstObserver, exactMatch: false).Handle);
			Assert.NotEqual(NativeHandle.Zero, Assert.IsType<NSObject>(secondObserver, exactMatch: false).Handle);

			// Without an item there is nothing to observe, and the observer of the cleared item was disposed too.
			mediaElement.Source = null;
			await manager.TakeSourceUpdates();
			Assert.Null(manager.NativeCurrentItemErrorObserver);
			Assert.Equal(NativeHandle.Zero, ((NSObject)secondObserver).Handle);
		});
	}

	[Fact]
	public async Task DisposeClearsTheObserversOfThePlayerAndOfTheItem()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();

		await HostAsync(mediaElement, async handler =>
		{
			var manager = handler.Manager!;
			await LoadAsync(mediaElement, handler, ResourceSource(VideoResource));
			Assert.NotNull(manager.NativeCurrentItemErrorObserver);

			manager.Dispose();

			Assert.Null(manager.NativeMutedObserver);
			Assert.Null(manager.NativeVolumeObserver);
			Assert.Null(manager.NativeStatusObserver);
			Assert.Null(manager.NativeTimeControlStatusObserver);
			Assert.Null(manager.NativeRateObserver);
			Assert.Null(manager.NativeCurrentItemErrorObserver);
		});
	}

	[Fact]
	public async Task DisposeReleasesThePlayerAndThePlayerViewController()
	{
		SetupApple();

		await InvokeOnMainThreadAsync(() =>
		{
			var manager = CreateManager(CreateMediaElement());
			var (player, controller) = manager.CreatePlatformView();
			Assert.NotEqual(NativeHandle.Zero, player.Handle);

			manager.Dispose();

			Assert.Equal(NativeHandle.Zero, player.Handle);
			Assert.Equal(NativeHandle.Zero, controller.Handle);
			Assert.Null(manager.NativePlayer);
			Assert.Null(manager.NativePlayerViewController);
		});
	}

	[Fact]
	public async Task DisposePausesThePlayerAndRemovesItsItem()
	{
		SetupApple();

		await InvokeOnMainThreadAsync(() =>
		{
			var manager = CreateManager(CreateMediaElement());
			manager.CreatePlatformView();
			var fake = new AppleFakeAVPlayer();
			manager.NativePlayer = fake;

			manager.Dispose();

			Assert.Equal(1, fake.PauseCount);
			Assert.Null(Assert.Single(fake.ReplacedItems));
		});
	}

	[Fact]
	public async Task DisposeTurnsTheIdleTimerBackOn()
	{
		SetupApple();

		await InvokeOnMainThreadAsync(() =>
		{
			var manager = CreateManager(CreateMediaElement());
			manager.CreatePlatformView();
			UIApplication.SharedApplication.IdleTimerDisabled = true;

			try
			{
				manager.Dispose();

				Assert.False(UIApplication.SharedApplication.IdleTimerDisabled);
			}
			finally
			{
				UIApplication.SharedApplication.IdleTimerDisabled = false;
			}
		});
	}

	[Fact]
	public async Task DisposeStopsObservingErrorNotifications()
	{
		SetupApple();

		await InvokeOnMainThreadAsync(() =>
		{
			var manager = CreateManager(CreateMediaElement());
			manager.CreatePlatformView();
			NSNotificationCenter.DefaultCenter.PostNotificationName(AVPlayerItem.PlaybackStalledNotification, null);
			Assert.Single(Logs.Entries, entry => entry.Level is LogLevel.Warning);

			manager.Dispose();
			NSNotificationCenter.DefaultCenter.PostNotificationName(AVPlayerItem.PlaybackStalledNotification, null);
			NSNotificationCenter.DefaultCenter.PostNotificationName(AVPlayerItem.ItemFailedToPlayToEndTimeNotification, null);
			NSNotificationCenter.DefaultCenter.PostNotificationName(AVPlayerItem.NewErrorLogEntryNotification, null);

			Assert.Single(Logs.Entries, entry => entry.Level is LogLevel.Warning);
		});
	}

	[Fact]
	public async Task DisposeOfAPlayerThatTheManagerNeverObservedDoesNotThrow()
	{
		SetupApple();

		await InvokeOnMainThreadAsync(() =>
		{
			var manager = CreateManager(CreateMediaElement());
			var player = new AVPlayer();
			manager.NativePlayer = player;

			var exception = Record.Exception(manager.Dispose);

			Assert.Null(exception);
			Assert.Equal(NativeHandle.Zero, player.Handle);
			Assert.Null(manager.NativePlayer);
		});
	}

	[Fact]
	public async Task DisposeWithoutCreatePlatformViewDoesNothing()
	{
		SetupApple();

		await InvokeOnMainThreadAsync(() =>
		{
			var manager = CreateManager(CreateMediaElement());

			manager.Dispose();

			Assert.Null(manager.NativePlayer);
			Assert.Null(manager.NativePlayerViewController);
		});
	}

	[Fact]
	public async Task DisposeTwiceIsHarmless()
	{
		SetupApple();

		await InvokeOnMainThreadAsync(() =>
		{
			var manager = CreateManager(CreateMediaElement());
			var (player, _) = manager.CreatePlatformView();
			manager.Dispose();

			var exception = Record.Exception(manager.Dispose);

			Assert.Null(exception);
			Assert.Equal(NativeHandle.Zero, player.Handle);
		});
	}

	[Fact]
	public async Task DisposeWithoutDisposingReleasesNothing()
	{
		SetupApple();

		await InvokeOnMainThreadAsync(() =>
		{
			var manager = CreateManager(CreateMediaElement());
			var (player, controller) = manager.CreatePlatformView();

			try
			{
				manager.DisposeCore(disposing: false);

				Assert.Same(player, manager.NativePlayer);
				Assert.Same(controller, manager.NativePlayerViewController);
				Assert.NotEqual(NativeHandle.Zero, player.Handle);
				Assert.NotEqual(NativeHandle.Zero, controller.Handle);
			}
			finally
			{
				manager.Dispose();
			}
		});
	}
}