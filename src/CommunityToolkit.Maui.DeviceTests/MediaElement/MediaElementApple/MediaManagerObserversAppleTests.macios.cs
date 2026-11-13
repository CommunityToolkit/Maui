using AVFoundation;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Views;
using CommunityToolkit.Maui.Views;
using Foundation;
using MediaPlayer;
using Microsoft.Extensions.Logging;

namespace CommunityToolkit.Maui.DeviceTests;

// The real player will not produce some status values on demand (a failed player, an unknown status): the tests swap the manager's
// Player for a test double and poke the key-value observers of the real one (willChangeValue / didChangeValue).
[Category(TestCategory.MediaElementApple)]
[Collection(TestCollections.DisableParallelization)]
public class MediaManagerObserversAppleTests : AppleMediaElementTestBase
{
	static void SetState(MediaElement mediaElement, MediaElementState state) => ((IMediaElement)mediaElement).CurrentStateChanged(state);

	static NSString GetErrorNotification(string key) => key switch
	{
		"FailedToPlayToEnd" => AVPlayerItem.ItemFailedToPlayToEndTimeNotification,
		"PlaybackStalled" => AVPlayerItem.PlaybackStalledNotification,
		"NewErrorLogEntry" => AVPlayerItem.NewErrorLogEntryNotification,
		_ => throw new ArgumentOutOfRangeException(nameof(key), key, null)
	};

	// The manager's observers are registered on the real player and read the manager's Player when they run.
	static void Poke(AppleMediaManager manager, AVPlayer realPlayer, AVPlayer playerSeenByTheManager, string key)
	{
		manager.NativePlayer = playerSeenByTheManager;
		try
		{
			realPlayer.WillChangeValue(key);
			realPlayer.DidChangeValue(key);
		}
		finally
		{
			manager.NativePlayer = realPlayer;
		}
	}

	[Theory]
	[InlineData(0.25f)]
	[InlineData(0f)]
	[InlineData(0.75f)]
	public async Task NativeVolumeChangeUpdatesTheMediaElementVolume(float volume)
	{
		SetupApple();
		var mediaElement = CreateMediaElement();

		await HostAsync(mediaElement, async handler =>
		{
			var player = handler.Manager!.NativePlayer!;
			await handler.Manager.TakeSourceUpdates();
			Assert.Equal(1.0, mediaElement.Volume);

			player.Volume = volume;

			Assert.Equal(volume, mediaElement.Volume, precision: 3);
		});
	}

	[Fact]
	public async Task NativeVolumeChangeWithinTheToleranceDoesNotUpdateTheMediaElement()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();

		await HostAsync(mediaElement, async handler =>
		{
			var player = handler.Manager!.NativePlayer!;
			await handler.Manager.TakeSourceUpdates();
			player.Volume = 0.5f;
			Assert.Equal(0.5, mediaElement.Volume, precision: 3);

			player.Volume = 0.505f;

			Assert.Equal(0.5, mediaElement.Volume, precision: 3);
		});
	}

	[Fact]
	public async Task NativeVolumeChangeIsIgnoredWhileTheManagerHasNoPlayer()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();

		await HostAsync(mediaElement, async handler =>
		{
			var manager = handler.Manager!;
			var player = manager.NativePlayer!;
			await manager.TakeSourceUpdates();
			manager.NativePlayer = null;

			player.Volume = 0.2f;

			Assert.Equal(1.0, mediaElement.Volume);
			manager.NativePlayer = player;
		});
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task NativeMutedChangeUpdatesTheMediaElement(bool muted)
	{
		SetupApple();
		var mediaElement = CreateMediaElement();
		mediaElement.ShouldMute = !muted;

		await HostAsync(mediaElement, async handler =>
		{
			var player = handler.Manager!.NativePlayer!;
			await handler.Manager.TakeSourceUpdates();
			Assert.Equal(!muted, mediaElement.ShouldMute);

			player.Muted = muted;

			Assert.Equal(muted, mediaElement.ShouldMute);
		});
	}

	[Fact]
	public async Task NativeMutedChangeIsIgnoredWhileTheManagerHasNoPlayer()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();

		await HostAsync(mediaElement, async handler =>
		{
			var manager = handler.Manager!;
			var player = manager.NativePlayer!;
			await manager.TakeSourceUpdates();
			manager.NativePlayer = null;

			player.Muted = true;

			Assert.False(mediaElement.ShouldMute);
			manager.NativePlayer = player;
		});
	}

	[Theory]
	[InlineData(AVPlayerStatus.Unknown, MediaElementState.Stopped)]
	[InlineData(AVPlayerStatus.ReadyToPlay, MediaElementState.Paused)]
	[InlineData(AVPlayerStatus.Failed, MediaElementState.Failed)]
	[InlineData((AVPlayerStatus)99, MediaElementState.Buffering)]
	public async Task PlayerStatusChangeUpdatesTheMediaElementState(AVPlayerStatus status, MediaElementState expectedState)
	{
		SetupApple();
		var mediaElement = CreateMediaElement();

		await HostAsync(mediaElement, async handler =>
		{
			var manager = handler.Manager!;
			await LoadAsync(mediaElement, handler, ResourceSource(VideoResource));
			SetState(mediaElement, MediaElementState.Buffering);

			// (AVPlayerStatus)99 is not a known status: the state stays what it was (Buffering).
			Poke(manager, manager.NativePlayer!, new AppleFakeAVPlayer { StatusOverride = status }, "status");

			Assert.Equal(expectedState, mediaElement.CurrentState);
		});
	}

	[Fact]
	public async Task PlayerReadyToPlayStatusDoesNotHideAFailedItem()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();
		var events = new MediaElementEvents(mediaElement);
		var path = Path.Combine(Path.GetTempPath(), $"corrupt_{Guid.NewGuid():N}.mp4");
		File.WriteAllText(path, "This is not a media file.");

		try
		{
			await HostAsync(mediaElement, async handler =>
			{
				var manager = handler.Manager!;
				mediaElement.Source = MediaSource.FromFile(path);
				await Record.ExceptionAsync(manager.TakeSourceUpdates);
				await WaitUntilAsync(() => events.Failures.Count > 0, "MediaFailed was not raised.");

				// The status of the player says nothing about its item, which has failed.
				Poke(manager, manager.NativePlayer!, new AppleFakeAVPlayer { StatusOverride = AVPlayerStatus.ReadyToPlay }, "status");

				Assert.Equal(MediaElementState.Failed, mediaElement.CurrentState);
			});
		}
		finally
		{
			File.Delete(path);
		}
	}

	[Fact]
	public async Task PlayerStatusChangeIsIgnoredWhileTheManagerHasNoPlayer()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();

		await HostAsync(mediaElement, async handler =>
		{
			var manager = handler.Manager!;
			var player = manager.NativePlayer!;
			await manager.TakeSourceUpdates();
			SetState(mediaElement, MediaElementState.Buffering);
			manager.NativePlayer = null;

			player.WillChangeValue("status");
			player.DidChangeValue("status");

			Assert.Equal(MediaElementState.Buffering, mediaElement.CurrentState);
			manager.NativePlayer = player;
		});
	}

	[Fact]
	public async Task PlayerReadyToPlayStatusMakesAnOpenedMediaPaused()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();
		var events = new MediaElementEvents(mediaElement);

		await HostAsync(mediaElement, async handler =>
		{
			await LoadAsync(mediaElement, handler, ResourceSource(VideoResource));

			Assert.Equal(AVPlayerStatus.ReadyToPlay, handler.Manager!.NativePlayer!.Status);
			Assert.Equal(MediaElementState.Paused, mediaElement.CurrentState);
			Assert.True(events.States.IndexOf(MediaElementState.Opening) < events.States.IndexOf(MediaElementState.Paused));
		});
	}

	[Theory]
	[InlineData(AVPlayerTimeControlStatus.Paused, MediaElementState.Paused)]
	[InlineData(AVPlayerTimeControlStatus.Playing, MediaElementState.Playing)]
	[InlineData(AVPlayerTimeControlStatus.WaitingToPlayAtSpecifiedRate, MediaElementState.Buffering)]
	[InlineData((AVPlayerTimeControlStatus)99, MediaElementState.Stopped)]
	public async Task TimeControlStatusChangeUpdatesTheMediaElementState(AVPlayerTimeControlStatus timeControlStatus, MediaElementState expectedState)
	{
		SetupApple();
		var mediaElement = CreateMediaElement();

		await HostAsync(mediaElement, async handler =>
		{
			var manager = handler.Manager!;
			await manager.TakeSourceUpdates();
			SetState(mediaElement, MediaElementState.Stopped);
			var fake = new AppleFakeAVPlayer { StatusOverride = AVPlayerStatus.ReadyToPlay, TimeControlStatusOverride = timeControlStatus };

			// (AVPlayerTimeControlStatus)99 is not a known status: the state stays what it was (Stopped).
			Poke(manager, manager.NativePlayer!, fake, "timeControlStatus");

			Assert.Equal(expectedState, mediaElement.CurrentState);
		});
	}

	[Fact]
	public async Task TimeControlStatusChangeIsIgnoredWhileThePlayerStatusIsUnknown()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();

		await HostAsync(mediaElement, async handler =>
		{
			var manager = handler.Manager!;
			await manager.TakeSourceUpdates();
			SetState(mediaElement, MediaElementState.Stopped);
			var fake = new AppleFakeAVPlayer { StatusOverride = AVPlayerStatus.Unknown, TimeControlStatusOverride = AVPlayerTimeControlStatus.Playing };

			Poke(manager, manager.NativePlayer!, fake, "timeControlStatus");

			Assert.Equal(MediaElementState.Stopped, mediaElement.CurrentState);
		});
	}

	[Fact]
	public async Task TimeControlStatusChangeIsIgnoredWhileTheItemHasAnError()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();

		await HostAsync(mediaElement, async handler =>
		{
			var manager = handler.Manager!;
			await manager.TakeSourceUpdates();
			SetState(mediaElement, MediaElementState.Stopped);
			var failedItem = new AppleFakeAVPlayerItem(AVAsset.FromUrl(NSUrl.FromFilename(BundlePath("video", "mp4"))))
			{
				ErrorOverride = new NSError(new NSString("test-domain"), 1)
			};
			var fake = new AppleFakeAVPlayer
			{
				StatusOverride = AVPlayerStatus.ReadyToPlay,
				TimeControlStatusOverride = AVPlayerTimeControlStatus.Playing,
				UseCurrentItemOverride = true,
				CurrentItemOverride = failedItem
			};

			Poke(manager, manager.NativePlayer!, fake, "timeControlStatus");

			Assert.Equal(MediaElementState.Stopped, mediaElement.CurrentState);
		});
	}

	[Fact]
	public async Task TimeControlStatusChangeIsIgnoredWhileTheManagerHasNoPlayer()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();

		await HostAsync(mediaElement, async handler =>
		{
			var manager = handler.Manager!;
			var player = manager.NativePlayer!;
			await manager.TakeSourceUpdates();
			SetState(mediaElement, MediaElementState.Stopped);
			manager.NativePlayer = null;

			player.WillChangeValue("timeControlStatus");
			player.DidChangeValue("timeControlStatus");

			Assert.Equal(MediaElementState.Stopped, mediaElement.CurrentState);
			manager.NativePlayer = player;
		});
	}

	[Fact]
	public async Task TimeControlStatusChangeRefreshesTheNowPlayingInfo()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();

		await HostAsync(mediaElement, async handler =>
		{
			await LoadAsync(mediaElement, handler, ResourceSource(VideoResource));
			mediaElement.MetadataTitle = "Song";
			mediaElement.MetadataArtist = "Artist";
			Assert.NotEqual("Song", MPNowPlayingInfoCenter.DefaultCenter.NowPlaying?.Title);

			mediaElement.Play();
			await WaitUntilAsync(() => mediaElement.CurrentState is MediaElementState.Playing, "The player did not start playing.");

			var nowPlaying = MPNowPlayingInfoCenter.DefaultCenter.NowPlaying!;
			Assert.Equal("Song", nowPlaying.Title);
			Assert.Equal("Artist", nowPlaying.Artist);
			Assert.Equal(1f, nowPlaying.PlaybackRate);
			Assert.Equal(VideoDuration.TotalSeconds, nowPlaying.PlaybackDuration!.Value, precision: 1);
		});
	}

	[Fact]
	public async Task TimeControlStatusChangeWithoutNowPlayingMetadataStillUpdatesTheState()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();

		await InvokeOnMainThreadAsync(() =>
		{
			using var manager = new AppleMediaManager(MauiContext, mediaElement, CommunityToolkit.Maui.DeviceTests.Runners.TestDispatcher.Current);
			var (player, _) = manager.CreatePlatformView();
			SetState(mediaElement, MediaElementState.Stopped);
			var fake = new AppleFakeAVPlayer { StatusOverride = AVPlayerStatus.ReadyToPlay, TimeControlStatusOverride = AVPlayerTimeControlStatus.Playing };

			// UpdateSource has not run, so there is no Metadata to refresh.
			Poke(manager, player, fake, "timeControlStatus");

			Assert.Equal(MediaElementState.Playing, mediaElement.CurrentState);
		});
	}

	[Theory]
	[InlineData("FailedToPlayToEnd")]
	[InlineData("PlaybackStalled")]
	[InlineData("NewErrorLogEntry")]
	public async Task ErrorNotificationWithoutItemErrorIsOnlyLogged(string notificationKey)
	{
		SetupApple();
		var mediaElement = CreateMediaElement();
		var events = new MediaElementEvents(mediaElement);

		await HostAsync(mediaElement, async handler =>
		{
			await LoadAsync(mediaElement, handler, ResourceSource(VideoResource));
			var stateBefore = mediaElement.CurrentState;
			var notification = GetErrorNotification(notificationKey);

			NSNotificationCenter.DefaultCenter.PostNotificationName(notification, handler.Manager!.NativePlayer!.CurrentItem);

			var warning = Assert.Single(Logs.Entries, entry => entry.Level is LogLevel.Warning);
			Assert.Contains(notification.ToString(), warning.Message);
			Assert.Empty(events.Failures);
			Assert.Equal(stateBefore, mediaElement.CurrentState);
		});
	}

	[Fact]
	public async Task ErrorNotificationWithoutNotificationLogsAnUnknownReason()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();
		var events = new MediaElementEvents(mediaElement);

		await InvokeOnMainThreadAsync(() =>
		{
			using var manager = new AppleMediaManager(MauiContext, mediaElement, CommunityToolkit.Maui.DeviceTests.Runners.TestDispatcher.Current);
			manager.CreatePlatformView();
			var errorOccurred = typeof(MediaManager).GetMethod("ErrorOccurred", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;

			// AVFoundation always delivers a notification; the handler still has a message for the case that it does not.
			errorOccurred.Invoke(manager, [null, new NSNotificationEventArgs(null!)]);

			var warning = Assert.Single(Logs.Entries, entry => entry.Level is LogLevel.Warning);
			Assert.Equal("Media playback failed for an unknown reason.", warning.Message);
			Assert.Empty(events.Failures);
		});
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task ErrorNotificationIsOnlyLoggedWhileThereIsNoItemToAskForAnError(bool managerHasNoPlayer)
	{
		SetupApple();
		var mediaElement = CreateMediaElement();
		var events = new MediaElementEvents(mediaElement);

		await HostAsync(mediaElement, async handler =>
		{
			var manager = handler.Manager!;
			await LoadAsync(mediaElement, handler, ResourceSource(VideoResource));
			var player = manager.NativePlayer!;
			manager.NativePlayer = managerHasNoPlayer ? null : new AppleFakeAVPlayer { UseCurrentItemOverride = true, CurrentItemOverride = null };

			// A notification that names no item, so that it concerns this manager whatever item it plays.
			NSNotificationCenter.DefaultCenter.PostNotificationName(AVPlayerItem.PlaybackStalledNotification, null);

			manager.NativePlayer = player;
			Assert.Single(Logs.Entries, entry => entry.Level is LogLevel.Warning);
			Assert.Empty(events.Failures);
		});
	}

	[Theory]
	[InlineData("FailedToPlayToEnd")]
	[InlineData("PlaybackStalled")]
	[InlineData("NewErrorLogEntry")]
	public async Task ErrorNotificationWithItemErrorRaisesMediaFailed(string notificationKey)
	{
		SetupApple();
		var mediaElement = CreateMediaElement();
		var events = new MediaElementEvents(mediaElement);

		await HostAsync(mediaElement, async handler =>
		{
			var manager = handler.Manager!;
			await manager.TakeSourceUpdates();
			var failedItem = new AppleFakeAVPlayerItem(AVAsset.FromUrl(NSUrl.FromFilename(BundlePath("video", "mp4"))))
			{
				ErrorOverride = new NSError(new NSString("test-domain"), 7, NSDictionary.FromObjectAndKey(new NSString("The item failed."), NSError.LocalizedDescriptionKey))
			};
			var player = manager.NativePlayer!;
			manager.NativePlayer = new AppleFakeAVPlayer { UseCurrentItemOverride = true, CurrentItemOverride = failedItem };

			NSNotificationCenter.DefaultCenter.PostNotificationName(GetErrorNotification(notificationKey), failedItem);

			manager.NativePlayer = player;
			Assert.Equal("The item failed.", Assert.Single(events.Failures));
			Assert.Equal(MediaElementState.Failed, mediaElement.CurrentState);
			Assert.Contains(Logs.Entries, entry => entry.Level is LogLevel.Error && entry.Message == "The item failed.");
			Assert.DoesNotContain(Logs.Entries, entry => entry.Level is LogLevel.Warning);
		});
	}

	[Theory]
	[InlineData("FailedToPlayToEnd")]
	[InlineData("PlaybackStalled")]
	[InlineData("NewErrorLogEntry")]
	public async Task ErrorNotificationOfAnotherItemIsIgnored(string notificationKey)
	{
		SetupApple();
		var mediaElement = CreateMediaElement();
		var events = new MediaElementEvents(mediaElement);

		await HostAsync(mediaElement, async handler =>
		{
			var manager = handler.Manager!;
			await manager.TakeSourceUpdates();
			var asset = AVAsset.FromUrl(NSUrl.FromFilename(BundlePath("video", "mp4")));
			var failedItem = new AppleFakeAVPlayerItem(asset) { ErrorOverride = new NSError(new NSString("test-domain"), 7) };
			var player = manager.NativePlayer!;
			manager.NativePlayer = new AppleFakeAVPlayer { UseCurrentItemOverride = true, CurrentItemOverride = failedItem };

			NSNotificationCenter.DefaultCenter.PostNotificationName(GetErrorNotification(notificationKey), new AVPlayerItem(asset));

			manager.NativePlayer = player;
			Assert.Empty(events.Failures);
			Assert.DoesNotContain(Logs.Entries, entry => entry.Level is LogLevel.Error);
		});
	}
}