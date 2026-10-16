using System.Reflection;
using AVFoundation;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Views;
using CommunityToolkit.Maui.DeviceTests.Runners;
using CommunityToolkit.Maui.Views;
using Foundation;
using Microsoft.Extensions.Logging;
using UIKit;

namespace CommunityToolkit.Maui.DeviceTests;

// MediaManager.macios.cs since #3281: PlatformUpdateSource observes the new AVPlayerItem's "status", and the observer queues
// PlayerItemStatusChanged on the main queue. For ReadyToPlay it opens the media once (video track, media size, position, auto play at the
// configured Speed, state, MediaOpened, poster); for Failed it raises MediaFailed. Every item has a cancellation token that a newer source
// or Dispose cancels, so the handler of a replaced item stops, also when an event handler of the opening replaces it. The tests use the real
// observer where AVFoundation produces the state, and run the handler directly with test doubles where it cannot (a failed item without an
// error, a track load that fails or is still running, a player state at an exact moment). Together with the MediaOpened tests of
// MediaManagerSourceAppleTests they verify the fix of #3249: MediaOpened is raised only once the item is ReadyToPlay.
[Category(TestCategory.MediaElementApple)]
[Collection(TestCollections.DisableParallelization)]
public class MediaManagerItemStatusAppleTests : AppleMediaElementTestBase
{
	AppleMediaManager CreateManager(MediaElement mediaElement) => new(MauiContext, mediaElement, TestDispatcher.Current);

	static AVAsset VideoAsset() => AVAsset.FromUrl(NSUrl.FromFilename(BundlePath("video", "mp4")));

	static NSError ItemError(string description, string? reason = null)
	{
		var userInfo = reason is null
			? NSDictionary.FromObjectAndKey(new NSString(description), NSError.LocalizedDescriptionKey)
			: NSDictionary.FromObjectsAndKeys(
				new NSObject[] { new NSString(description), new NSString(reason) },
				new NSObject[] { NSError.LocalizedDescriptionKey, NSError.LocalizedFailureReasonErrorKey });
		return new NSError(new NSString("test-domain"), 7, userInfo);
	}

	static UIView Overlay(AppleMediaElementHandler handler) => handler.Manager!.NativePlayerViewController!.ContentOverlayView!;

	/// <summary>Puts a <see cref="AppleFakeAVPlayer"/> in place of the manager's player and gives the real one back afterwards (Dispose needs it).</summary>
	static async Task WithFakePlayerAsync(AppleMediaManager manager, AppleFakeAVPlayer fake, Func<Task> action)
	{
		var player = manager.NativePlayer;
		manager.NativePlayer = fake;
		try
		{
			await action();
		}
		finally
		{
			manager.NativePlayer = player;
			fake.AfterNextReplace = fake.AfterPlay = fake.AfterRateSet = null;
			fake.Pause();
			fake.ReplaceCurrentItemWithPlayerItem(null);
			fake.Dispose();
		}
	}

	[Fact]
	public async Task ReadyToPlayStatusOpensTheMediaOnlyOnce()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();
		var events = new MediaElementEvents(mediaElement);

		await HostAsync(mediaElement, async handler =>
		{
			var manager = handler.Manager!;
			await LoadAsync(mediaElement, handler, ResourceSource(VideoResource));
			Assert.Equal(1, events.Opened);

			// The item reports ReadyToPlay again (for example after it has buffered): the media is open already.
			await manager.RunItemStatusHandler(manager.NativePlayerItem!, CancellationToken.None);

			Assert.Equal(1, events.Opened);
		});
	}

	[Fact]
	public async Task OpeningReportsTheDurationBeforeMediaOpened()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();
		TimeSpan? durationAtMediaOpened = null;
		mediaElement.MediaOpened += (_, _) => durationAtMediaOpened = mediaElement.Duration;

		await HostAsync(mediaElement, async handler =>
		{
			await LoadAsync(mediaElement, handler, ResourceSource(VideoResource));

			Assert.NotNull(durationAtMediaOpened);
			Assert.Equal(VideoDuration.TotalSeconds, durationAtMediaOpened.Value.TotalSeconds, precision: 1);
		});
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task FailedStatusRaisesMediaFailedAndLogsTheError(bool itemHasError)
	{
		SetupApple();
		var mediaElement = CreateMediaElement();
		var events = new MediaElementEvents(mediaElement);

		await InvokeOnMainThreadAsync(async () =>
		{
			using var manager = CreateManager(mediaElement);
			manager.CreatePlatformView();

			// AVFoundation always gives a failed item an error (UnreadableMediaRaisesMediaFailedAndLogsTheError); the handler has a message
			// for one without.
			var item = new AppleFakeAVPlayerItem(VideoAsset())
			{
				StatusOverride = AVPlayerItemStatus.Failed,
				ErrorOverride = itemHasError ? ItemError("The item failed.", "It is broken.") : null
			};

			await manager.RunItemStatusHandler(item, CancellationToken.None);

			var expected = itemHasError ? "The item failed. - It is broken." : "AVPlayerItem failed.";
			Assert.Equal(expected, Assert.Single(events.Failures));
			Assert.Equal(MediaElementState.Failed, mediaElement.CurrentState);
			Assert.Contains(Logs.Entries, entry => entry.Level is LogLevel.Error && entry.Message == expected);
			Assert.Equal(0, events.Opened);
		});
	}

	[Fact]
	public async Task UnknownStatusChangesNothing()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();
		var events = new MediaElementEvents(mediaElement);

		await InvokeOnMainThreadAsync(async () =>
		{
			using var manager = CreateManager(mediaElement);
			manager.CreatePlatformView();
			var stateBefore = mediaElement.CurrentState;
			var logsBefore = Logs.Entries.Count;

			// The observer's initial notification for a new item.
			await manager.RunItemStatusHandler(new AppleFakeAVPlayerItem(VideoAsset()) { StatusOverride = AVPlayerItemStatus.Unknown }, CancellationToken.None);

			Assert.Equal(0, events.Opened);
			Assert.Empty(events.Failures);
			Assert.Equal(stateBefore, mediaElement.CurrentState);
			Assert.Equal(logsBefore, Logs.Entries.Count);
		});
	}

	[Fact]
	public async Task HandlerOfAReplacedItemDoesNothing()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();
		var events = new MediaElementEvents(mediaElement);

		await InvokeOnMainThreadAsync(async () =>
		{
			using var manager = CreateManager(mediaElement);
			manager.CreatePlatformView();
			using var replaced = new CancellationTokenSource();
			replaced.Cancel();

			// A status that the handler would report, of an item whose token a newer source has cancelled.
			var item = new AppleFakeAVPlayerItem(VideoAsset()) { StatusOverride = AVPlayerItemStatus.Failed, ErrorOverride = ItemError("The item failed.") };

			await Assert.ThrowsAnyAsync<OperationCanceledException>(() => manager.RunItemStatusHandler(item, replaced.Token));

			Assert.Empty(events.Failures);
			Assert.NotEqual(MediaElementState.Failed, mediaElement.CurrentState);
		});
	}

	[Theory]
	[InlineData(AVPlayerTimeControlStatus.Playing, MediaElementState.Playing)]
	[InlineData(AVPlayerTimeControlStatus.WaitingToPlayAtSpecifiedRate, MediaElementState.Buffering)]
	[InlineData(AVPlayerTimeControlStatus.Paused, MediaElementState.Paused)]
	public async Task OpenedStateFollowsTheTimeControlStatusOfThePlayer(AVPlayerTimeControlStatus timeControlStatus, MediaElementState expectedState)
	{
		SetupApple();
		var mediaElement = CreateMediaElement();
		var events = new MediaElementEvents(mediaElement);

		await InvokeOnMainThreadAsync(async () =>
		{
			using var manager = CreateManager(mediaElement);
			manager.CreatePlatformView();
			var fake = new AppleFakeAVPlayer { TimeControlStatusOverride = timeControlStatus };
			await WithFakePlayerAsync(manager, fake, async () =>
			{
				((IMediaElement)mediaElement).CurrentStateChanged(MediaElementState.Opening);
				var item = new AppleFakeAVPlayerItem(VideoAsset()) { StatusOverride = AVPlayerItemStatus.ReadyToPlay };

				await manager.RunItemStatusHandler(item, CancellationToken.None);

				Assert.Equal(expectedState, mediaElement.CurrentState);
				Assert.Equal(1, events.Opened);
				Assert.Equal((64, 48), (mediaElement.MediaWidth, mediaElement.MediaHeight));
				Assert.Equal(0, fake.PlayCount);
			});
		});
	}

	[Fact]
	public async Task AutoPlayOfTheReadyItemStartsThePlayerAtTheSpeedWithOneRateChange()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();
		mediaElement.ShouldAutoPlay = true;
		mediaElement.Speed = 1.5;
		var events = new MediaElementEvents(mediaElement);

		await InvokeOnMainThreadAsync(async () =>
		{
			using var manager = CreateManager(mediaElement);
			manager.CreatePlatformView();
			var fake = new AppleFakeAVPlayer { TimeControlStatusOverride = AVPlayerTimeControlStatus.Playing };
			await WithFakePlayerAsync(manager, fake, async () =>
			{
				var item = new AppleFakeAVPlayerItem(VideoAsset()) { StatusOverride = AVPlayerItemStatus.ReadyToPlay };

				await manager.RunItemStatusHandler(item, CancellationToken.None);

				// Setting the rate starts playback at the configured speed: one rate change and no Play(), which would start at the default rate
				// first, so that RateChanged, which copies the player's rate into Speed, would see two rates (#3327).
				Assert.Equal(0, fake.PlayCount);
				Assert.Equal(new[] { 1.5f }, fake.RatesSet);
				Assert.Equal(1.5f, fake.Rate);
				Assert.Equal(MediaElementState.Playing, mediaElement.CurrentState);
				Assert.Equal(1, events.Opened);
			});
		});
	}

	[Fact]
	public async Task AutoPlayKeepsAPauseThatWasMadeWhilePlaybackStarted()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();
		mediaElement.ShouldAutoPlay = true;
		mediaElement.Speed = 1.5;
		var events = new MediaElementEvents(mediaElement);

		await InvokeOnMainThreadAsync(async () =>
		{
			using var manager = CreateManager(mediaElement);
			manager.CreatePlatformView();
			var fake = new AppleFakeAVPlayer();

			// Like a handler of the change that starting playback raises, which pauses the media again.
			fake.AfterRateSet = fake.Pause;
			await WithFakePlayerAsync(manager, fake, async () =>
			{
				var item = new AppleFakeAVPlayerItem(VideoAsset()) { StatusOverride = AVPlayerItemStatus.ReadyToPlay };

				await manager.RunItemStatusHandler(item, CancellationToken.None);

				// Playback is started once, at the speed; nothing starts the player again after the pause.
				Assert.Equal(0, fake.PlayCount);
				Assert.Equal(new[] { 1.5f }, fake.RatesSet);
				Assert.Equal(0f, fake.Rate);
				Assert.Equal(1, events.Opened);
			});
		});
	}

	[Theory]
	[InlineData("Duration")]
	[InlineData("Rate")]
	[InlineData("State")]
	public async Task ReplacingTheItemFromAnEventOfTheOpeningStopsTheRestOfIt(string replacedOn)
	{
		SetupApple();
		var mediaElement = CreateMediaElement();
		mediaElement.ShouldAutoPlay = true;
		mediaElement.Speed = 1.5;
		var events = new MediaElementEvents(mediaElement);

		await InvokeOnMainThreadAsync(async () =>
		{
			using var manager = CreateManager(mediaElement);
			manager.CreatePlatformView();
			var fake = new AppleFakeAVPlayer { TimeControlStatusOverride = AVPlayerTimeControlStatus.Playing };
			await WithFakePlayerAsync(manager, fake, async () =>
			{
				((IMediaElement)mediaElement).CurrentStateChanged(MediaElementState.Opening);
				var item = new AppleFakeAVPlayerItem(VideoAsset())
				{
					StatusOverride = AVPlayerItemStatus.ReadyToPlay,
					DurationOverride = CoreMedia.CMTime.FromSeconds(2, 1000),
					CurrentTimeOverride = CoreMedia.CMTime.Zero
				};
				manager.NativePlayerItem = item;

				// An event handler that sets another source (or disconnects the handler) cancels the token of this item's handler.
				using var replaced = new CancellationTokenSource();
				switch (replacedOn)
				{
					case "Duration":
						mediaElement.PropertyChanged += (_, args) =>
						{
							if (args.PropertyName == nameof(MediaElement.Duration))
							{
								replaced.Cancel();
							}
						};
						break;
					case "Rate":
						fake.AfterRateSet = replaced.Cancel;
						break;
					case "State":
						mediaElement.StateChanged += (_, args) =>
						{
							if (args.NewState is MediaElementState.Playing)
							{
								replaced.Cancel();
							}
						};
						break;
				}

				await Assert.ThrowsAnyAsync<OperationCanceledException>(() => manager.RunItemStatusHandler(item, replaced.Token));

				// Auto play starts playback with one rate change at the speed (no Play())
				Assert.Equal(0, fake.PlayCount);
				Assert.Equal(replacedOn is "Duration" ? 0 : 1, fake.RatesSet.Count);
				Assert.Equal(replacedOn is "State" ? MediaElementState.Playing : MediaElementState.Opening, mediaElement.CurrentState);
				Assert.Equal(0, events.Opened);
				manager.NativePlayerItem = null;
			});
		});
	}

	[Fact]
	public async Task ReplacingTheSourceFromMediaOpenedLeavesNoPosterOfTheReplacedItem()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();
		mediaElement.MetadataArtworkUrl = BundleFileUrl("poster", "png");

		await HostAsync(mediaElement, async handler =>
		{
			var manager = handler.Manager!;
			await manager.TakeSourceUpdates();
			var opened = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
			mediaElement.MediaOpened += OnMediaOpened;

			mediaElement.Source = ResourceSource(AudioResource);
			await manager.TakeSourceUpdates();
			await WaitAsync(opened.Task, "MediaOpened was not raised.");
			await DrainMainQueueAsync();

			Assert.Null(manager.NativePlayerItem);
			Assert.Empty(Overlay(handler).Subviews);
			Assert.DoesNotContain(Logs.Entries, entry => entry.Level is LogLevel.Error);

			void OnMediaOpened(object? sender, EventArgs args)
			{
				mediaElement.MediaOpened -= OnMediaOpened;
				mediaElement.Source = null;
				opened.TrySetResult();
			}
		});
	}

	[Fact]
	public async Task VideoTrackLoadFailureIsLoggedAndTheMediaStillOpensWithThePresentationSizeAndWithoutPoster()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();
		mediaElement.MetadataArtworkUrl = BundleFileUrl("poster", "png");
		var events = new MediaElementEvents(mediaElement);

		await HostAsync(mediaElement, async handler =>
		{
			var manager = handler.Manager!;
			await manager.TakeSourceUpdates();

			// Audio with artwork would get a poster, but only when the track load has shown that there is no video track.
			var asset = new AppleTrackLoadingAsset(NSUrl.FromFilename(BundlePath("audio", "m4a")));
			var item = new AppleFakeAVPlayerItem(asset) { StatusOverride = AVPlayerItemStatus.ReadyToPlay, PresentationSizeOverride = new CoreGraphics.CGSize(320, 180) };
			var error = ItemError("The tracks cannot be loaded.");

			var opening = manager.RunItemStatusHandler(item, CancellationToken.None);
			Assert.Equal(1, asset.TrackLoads);
			asset.FailTrackLoad(error);
			await opening;

			Assert.Equal(1, events.Opened);
			Assert.Empty(events.Failures);
			Assert.Equal((320, 180), (mediaElement.MediaWidth, mediaElement.MediaHeight));
			var warning = Assert.Single(Logs.Entries, entry => entry.Level is LogLevel.Warning);
			Assert.Equal("Failed to load the video track; using the presentation size instead.", warning.Message);
			Assert.Same(error, Assert.IsType<NSErrorException>(warning.Exception).Error);
			Assert.NotEmpty(item.Asset.Tracks);
			Assert.Empty(Overlay(handler).Subviews);
		});
	}

	[Fact]
	public async Task ReadyItemWithoutTracksShowsNoPoster()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();
		mediaElement.MetadataArtworkUrl = BundleFileUrl("poster", "png");
		var events = new MediaElementEvents(mediaElement);

		await HostAsync(mediaElement, async handler =>
		{
			var manager = handler.Manager!;
			await manager.TakeSourceUpdates();

			// AVFoundation never reports such an item as ReadyToPlay; if it did, there would be no video track and no track at all.
			var item = new AppleFakeAVPlayerItem(AVAsset.FromUrl(NSUrl.FromFilename(BundlePath("trackless", "mp4")))) { StatusOverride = AVPlayerItemStatus.ReadyToPlay };

			await manager.RunItemStatusHandler(item, CancellationToken.None);

			Assert.Equal(1, events.Opened);
			Assert.Empty(item.Asset.Tracks);
			Assert.Empty(Overlay(handler).Subviews);
			Assert.DoesNotContain(Logs.Entries, entry => entry.Level is LogLevel.Warning or LogLevel.Error);
		});
	}

	[Fact]
	public async Task ReplacingTheItemWhileItsTracksLoadStopsTheHandlerWithoutAWarning()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();
		var events = new MediaElementEvents(mediaElement);

		await InvokeOnMainThreadAsync(async () =>
		{
			using var manager = CreateManager(mediaElement);
			manager.CreatePlatformView();
			var asset = new AppleTrackLoadingAsset(NSUrl.FromFilename(BundlePath("video", "mp4")));
			var item = new AppleFakeAVPlayerItem(asset) { StatusOverride = AVPlayerItemStatus.ReadyToPlay };
			using var replaced = new CancellationTokenSource();

			// The track load never finishes; the token of the item is cancelled while the handler waits for it.
			var opening = manager.RunItemStatusHandler(item, replaced.Token);
			Assert.Equal(1, asset.TrackLoads);
			replaced.Cancel();

			await Assert.ThrowsAnyAsync<OperationCanceledException>(() => opening);
			Assert.Equal(0, events.Opened);
			Assert.DoesNotContain(Logs.Entries, entry => entry.Level is LogLevel.Warning or LogLevel.Error);
		});
	}

	[Fact]
	public async Task ReplacingTheItemDuringATrackLoadThatEndsAtOnceStopsTheHandlerBeforeTheMediaSize()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();
		var events = new MediaElementEvents(mediaElement);

		await InvokeOnMainThreadAsync(async () =>
		{
			using var manager = CreateManager(mediaElement);
			manager.CreatePlatformView();
			var asset = new AppleTrackLoadingAsset(NSUrl.FromFilename(BundlePath("video", "mp4")));
			var item = new AppleFakeAVPlayerItem(asset) { StatusOverride = AVPlayerItemStatus.ReadyToPlay, PresentationSizeOverride = new CoreGraphics.CGSize(320, 180) };
			using var replaced = new CancellationTokenSource();

			// The tracks are known already, so the load ends at once (the wait for it does not look at the token); the item is replaced
			// while it runs.
			asset.CompleteTrackLoad();
			asset.DuringTrackLoad = replaced.Cancel;

			await Assert.ThrowsAnyAsync<OperationCanceledException>(() => manager.RunItemStatusHandler(item, replaced.Token));

			Assert.Equal(1, asset.TrackLoads);
			Assert.Equal(0, events.Opened);
			Assert.Equal((0, 0), (mediaElement.MediaWidth, mediaElement.MediaHeight));
		});
	}

	[Fact]
	public async Task ItemThatFailsWhileItsTracksLoadIsNotOpened()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();
		var events = new MediaElementEvents(mediaElement);

		await InvokeOnMainThreadAsync(async () =>
		{
			using var manager = CreateManager(mediaElement);
			manager.CreatePlatformView();
			var asset = new AppleTrackLoadingAsset(NSUrl.FromFilename(BundlePath("video", "mp4")));

			// ReadyToPlay when the handler starts; Failed once the tracks have loaded (its own Failed notification reports the failure).
			var item = new AppleFakeAVPlayerItem(asset)
			{
				StatusReads = new Queue<AVPlayerItemStatus>([AVPlayerItemStatus.ReadyToPlay]),
				StatusOverride = AVPlayerItemStatus.Failed
			};

			var opening = manager.RunItemStatusHandler(item, CancellationToken.None);
			asset.CompleteTrackLoad();
			await opening;

			Assert.Equal(0, events.Opened);
			Assert.Empty(events.Failures);
			Assert.Equal((0, 0), (mediaElement.MediaWidth, mediaElement.MediaHeight));
		});
	}

	[Fact]
	public async Task MediaIsNotOpenedWhenThePlayerIsGoneOnceTheTracksHaveLoaded()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();
		var events = new MediaElementEvents(mediaElement);

		await InvokeOnMainThreadAsync(async () =>
		{
			using var manager = CreateManager(mediaElement);
			manager.CreatePlatformView();
			var player = manager.NativePlayer;
			var asset = new AppleTrackLoadingAsset(NSUrl.FromFilename(BundlePath("video", "mp4")));
			var item = new AppleFakeAVPlayerItem(asset) { StatusOverride = AVPlayerItemStatus.ReadyToPlay };

			var opening = manager.RunItemStatusHandler(item, CancellationToken.None);

			// What Dispose does to the player (for example when the handler is disconnected) while the tracks load.
			manager.NativePlayer = null;
			asset.CompleteTrackLoad();
			await opening;
			manager.NativePlayer = player;

			Assert.Equal(0, events.Opened);
			Assert.Equal((0, 0), (mediaElement.MediaWidth, mediaElement.MediaHeight));
		});
	}

	[Fact]
	public async Task ExceptionOfAMediaOpenedHandlerIsLogged()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();
		var failure = new InvalidOperationException("A MediaOpened handler failed.");
		mediaElement.MediaOpened += (_, _) => throw failure;

		await HostAsync(mediaElement, async handler =>
		{
			mediaElement.Source = ResourceSource(VideoResource);
			await handler.Manager!.TakeSourceUpdates();

			await WaitUntilAsync(() => Logs.Entries.Any(entry => entry.Level is LogLevel.Error), "The exception was not logged.");

			var error = Assert.Single(Logs.Entries, entry => entry.Level is LogLevel.Error);
			Assert.Equal("Failed to handle AVPlayerItem status change.", error.Message);

			// MAUI's WeakEventManager calls the handler through reflection.
			Assert.Same(failure, error.Exception is TargetInvocationException { InnerException: Exception inner } ? inner : error.Exception);
		});
	}

	[Fact]
	public async Task HandlerOfAnItemThatANewerSourceReplacedStopsQuietly()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();
		var events = new MediaElementEvents(mediaElement);

		await HostAsync(mediaElement, async handler =>
		{
			var manager = handler.Manager!;
			await manager.TakeSourceUpdates();
			var opened = WhenOpenedOrFailedAsync(mediaElement, openedOnly: true);

			// One main-thread turn: the audio item's observer queues its handler, then the video replaces the item and cancels it.
			mediaElement.Source = ResourceSource(AudioResource);
			var audioToken = manager.NativeItemCancellationTokenSource!.Token;
			mediaElement.Source = ResourceSource(VideoResource);
			await manager.TakeSourceUpdates();
			await WaitAsync(opened, "MediaOpened was not raised.");

			// The audio item's queued handler ran first (the main queue keeps the order). Its token was cancelled, but the item was also
			// replaced before it loaded, so the handler would read the Unknown status and do nothing even without the token check: this
			// test checks that the replaced item leaves no trace, HandlerOfAReplacedItemDoesNothing checks the token check itself.
			Assert.True(audioToken.IsCancellationRequested);
			Assert.Equal(1, events.Opened);
			Assert.Equal((64, 48), (mediaElement.MediaWidth, mediaElement.MediaHeight));
			Assert.DoesNotContain(Logs.Entries, entry => entry.Level is LogLevel.Error);
		});
	}

	[Fact]
	public async Task SourceSetByAnEventOfTheItemReplacementWinsOverTheReplacedSource()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();
		var events = new MediaElementEvents(mediaElement);

		await HostAsync(mediaElement, async handler =>
		{
			var manager = handler.Manager!;
			mediaElement.Source = ResourceSource(AudioResource);
			await manager.TakeSourceUpdates();
			var fake = new AppleFakeAVPlayer();
			await WithFakePlayerAsync(manager, fake, async () =>
			{
				IDisposable? videoObserver = null;
				AVPlayerItem? videoItem = null;

				// A handler of an event that the replacement raises sets another source.
				fake.AfterNextReplace = () =>
				{
					mediaElement.Source = ResourceSource(VideoResource);
					videoObserver = manager.NativeCurrentItemErrorObserver;
					videoItem = manager.NativePlayerItem;
				};
				var opened = WhenOpenedOrFailedAsync(mediaElement, openedOnly: true);

				// The update runs again for the audio, as when its MediaSource raises SourceChanged (a set of Source itself would delay the
				// nested set of Source until it has finished: MAUI's BindableObject queues it).
				handler.UpdateValue(nameof(IMediaElement.Source));
				await manager.TakeSourceUpdates();

				Assert.Equal(2, fake.ReplacedItems.Count);
				Assert.NotNull(videoItem);
				Assert.Same(videoItem, fake.ReplacedItems[1]);
				Assert.Same(videoItem, manager.NativePlayerItem);
				Assert.NotNull(videoObserver);
				Assert.Same(videoObserver, manager.NativeCurrentItemErrorObserver);

				await WaitAsync(opened, "The video was not opened.");
				Assert.Equal(1, events.Opened);
				Assert.Equal((64, 48), (mediaElement.MediaWidth, mediaElement.MediaHeight));
			});
		});
	}

	[Fact]
	public async Task PlayerRemovedByAnEventOfTheItemReplacementLeavesTheItemUnobserved()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();

		await HostAsync(mediaElement, async handler =>
		{
			var manager = handler.Manager!;
			await manager.TakeSourceUpdates();
			var fake = new AppleFakeAVPlayer();
			await WithFakePlayerAsync(manager, fake, async () =>
			{
				// What disconnecting the handler (Dispose) does to the player, from a handler of an event that the replacement raises.
				fake.AfterNextReplace = () => manager.NativePlayer = null;

				mediaElement.Source = ResourceSource(VideoResource);
				await manager.TakeSourceUpdates();

				Assert.Single(fake.ReplacedItems);
				Assert.Null(manager.NativeCurrentItemErrorObserver);
				Assert.Null(manager.NativeItemCancellationTokenSource);
			});
		});
	}

	[Fact]
	public async Task DisposeCancelsTheHandlerOfTheItem()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();

		await HostAsync(mediaElement, async handler =>
		{
			var manager = handler.Manager!;
			await LoadAsync(mediaElement, handler, ResourceSource(VideoResource));
			var token = manager.NativeItemCancellationTokenSource!.Token;
			Assert.False(token.IsCancellationRequested);

			manager.Dispose();

			Assert.True(token.IsCancellationRequested);
			Assert.Null(manager.NativeItemCancellationTokenSource);
			Assert.Null(manager.NativeCurrentItemErrorObserver);
		});
	}
}