using System.Reflection;
using AVFoundation;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Views;
using CommunityToolkit.Maui.DeviceTests.Runners;
using CommunityToolkit.Maui.Views;
using Foundation;
using Microsoft.Extensions.Logging;

namespace CommunityToolkit.Maui.DeviceTests;

// MediaManager.macios.cs PlatformUpdateSource: how every kind of MediaSource becomes an AVPlayerItem, what is reported for
// media that cannot be opened, and the media dimensions. The native AVPlayer / AVPlayerItem / AVAsset are what the tests read.
[Category(TestCategory.MediaElementApple)]
[Collection(TestCollections.DisableParallelization)]
public class MediaManagerSourceAppleTests : AppleMediaElementTestBase
{
	static string AssetPath(AVPlayerItem? item) => ((AVUrlAsset)item!.Asset).Url.Path!;

	[Fact]
	public async Task ResourceSourceLoadsTheFileFromTheAppBundle()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();
		var events = new MediaElementEvents(mediaElement);

		await HostAsync(mediaElement, async handler =>
		{
			await LoadAsync(mediaElement, handler, ResourceSource(VideoResource));

			var item = handler.Manager!.NativePlayer!.CurrentItem;
			Assert.EndsWith("/avplayer/video.mp4", AssetPath(item));
			Assert.Same(item, handler.Manager.NativePlayerItem);
			Assert.Equal(1, events.Opened);
			Assert.Equal((64, 48), (mediaElement.MediaWidth, mediaElement.MediaHeight));
		});
	}

	[Fact]
	public async Task ResourceSourceInTheBundleRootLoadsTheFile()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();

		await HostAsync(mediaElement, async handler =>
		{
			await LoadAsync(mediaElement, handler, ResourceSource(RootAudioResource));

			var item = handler.Manager!.NativePlayer!.CurrentItem;
			Assert.EndsWith("/root_audio.m4a", AssetPath(item));
			Assert.Single(item!.Asset.Tracks);
		});
	}

	[Fact]
	public async Task MissingResourceLogsAWarningAndClearsThePlayerItem()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();
		var events = new MediaElementEvents(mediaElement);

		await HostAsync(mediaElement, async handler =>
		{
			var manager = handler.Manager!;
			await LoadAsync(mediaElement, handler, ResourceSource(VideoResource));
			events.States.Clear();

			mediaElement.Source = ResourceSource("avplayer/does_not_exist.mp4");
			await manager.TakeSourceUpdates();

			Assert.Null(manager.NativePlayer!.CurrentItem);
			Assert.Null(manager.NativePlayerItem);
			Assert.Equal((0, 0), (mediaElement.MediaWidth, mediaElement.MediaHeight));
			Assert.Contains(events.States, state => state is MediaElementState.None);
			Assert.Equal(1, events.Opened);
			Assert.Contains(Logs.Entries, entry => entry.Level is LogLevel.Warning && entry.Message == "Resource file was not found.");
		});
	}

	[Theory]
	[InlineData("avplayer/no_extension")]
	[InlineData("")]
	[InlineData("   ")]
	public Task ResourcePathWithoutFileNameOrExtensionLogsAnInvalidPathWarning(string path) => AssertInvalidResourcePathAsync(path);

	[Fact]
	public Task ResourceSourceWithoutPathLogsAnInvalidPathWarning() => AssertInvalidResourcePathAsync(null);

	async Task AssertInvalidResourcePathAsync(string? path)
	{
		SetupApple();
		var mediaElement = CreateMediaElement();

		await HostAsync(mediaElement, async handler =>
		{
			var manager = handler.Manager!;
			await LoadAsync(mediaElement, handler, ResourceSource(VideoResource));

			mediaElement.Source = new ResourceMediaSource { Path = path };
			await manager.TakeSourceUpdates();

			Assert.Null(manager.NativePlayer!.CurrentItem);
			Assert.Contains(Logs.Entries, entry => entry.Level is LogLevel.Warning && entry.Message == "Invalid file path for ResourceMediaSource.");
			Assert.DoesNotContain(Logs.Entries, entry => entry.Message == "Resource file was not found.");
		});
	}

	[Fact]
	public async Task FileSourceLoadsALocalFile()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();

		await HostAsync(mediaElement, async handler =>
		{
			var path = BundlePath("video", "mp4");

			await LoadAsync(mediaElement, handler, MediaSource.FromFile(path));

			Assert.Equal(path, AssetPath(handler.Manager!.NativePlayer!.CurrentItem));
			Assert.Equal((64, 48), (mediaElement.MediaWidth, mediaElement.MediaHeight));
		});
	}

	[Theory]
	[InlineData("")]
	[InlineData("   ")]
	public Task FileSourceWithBlankPathClearsThePlayerItem(string path) => AssertFilePathClearsThePlayerItemAsync(path);

	[Fact]
	public Task FileSourceWithoutPathClearsThePlayerItem() => AssertFilePathClearsThePlayerItemAsync(null);

	async Task AssertFilePathClearsThePlayerItemAsync(string? path)
	{
		SetupApple();
		var mediaElement = CreateMediaElement();

		await HostAsync(mediaElement, async handler =>
		{
			var manager = handler.Manager!;
			await LoadAsync(mediaElement, handler, ResourceSource(VideoResource));
			Assert.NotNull(manager.NativePlayer!.CurrentItem);

			mediaElement.Source = new FileMediaSource { Path = path };
			await manager.TakeSourceUpdates();

			Assert.Null(manager.NativePlayer.CurrentItem);
			Assert.Equal((0, 0), (mediaElement.MediaWidth, mediaElement.MediaHeight));
		});
	}

	[Fact]
	public async Task UriSourceLoadsFromAnHttpServer()
	{
		SetupApple();
		using var server = new AppleLoopbackHttpServer();
		server.Add("/video.mp4", File.ReadAllBytes(BundlePath("video", "mp4")), "video/mp4");
		var mediaElement = CreateMediaElement();

		await HostAsync(mediaElement, async handler =>
		{
			await LoadAsync(mediaElement, handler, MediaSource.FromUri(server.GetUri("/video.mp4")));

			Assert.Equal("/video.mp4", AssetPath(handler.Manager!.NativePlayer!.CurrentItem));
			Assert.Equal((64, 48), (mediaElement.MediaWidth, mediaElement.MediaHeight));
			Assert.NotEmpty(server.Requests);
			Assert.All(server.Requests, request => Assert.False(request.Headers.ContainsKey("X-Device-Test")));
		});
	}

	[Fact]
	public async Task UriSourceSendsTheConfiguredHttpHeaders()
	{
		SetupApple();
		using var server = new AppleLoopbackHttpServer();
		server.Add("/video.mp4", File.ReadAllBytes(BundlePath("video", "mp4")), "video/mp4");
		var mediaElement = CreateMediaElement();
		var source = new UriMediaSource { Uri = server.GetUri("/video.mp4") };
		source.HttpHeaders["X-Device-Test"] = "first";
		source.HttpHeaders["Authorization"] = "Bearer token";

		await HostAsync(mediaElement, async handler =>
		{
			await LoadAsync(mediaElement, handler, source);

			Assert.Equal("/video.mp4", AssetPath(handler.Manager!.NativePlayer!.CurrentItem));
			Assert.NotEmpty(server.Requests);
			Assert.All(server.Requests, request =>
			{
				Assert.Equal("first", request.Headers["X-Device-Test"]);
				Assert.Equal("Bearer token", request.Headers["Authorization"]);
			});
		});
	}

	[Fact]
	public async Task ChangingTheHttpHeadersReloadsTheMedia()
	{
		SetupApple();
		using var server = new AppleLoopbackHttpServer();
		server.Add("/video.mp4", File.ReadAllBytes(BundlePath("video", "mp4")), "video/mp4");
		var mediaElement = CreateMediaElement();
		var source = new UriMediaSource { Uri = server.GetUri("/video.mp4") };
		source.HttpHeaders["X-Device-Test"] = "first";

		await HostAsync(mediaElement, async handler =>
		{
			await LoadAsync(mediaElement, handler, source);
			var firstItem = handler.Manager!.NativePlayerItem;

			source.HttpHeaders["X-Device-Test"] = "second";
			await handler.Manager.TakeSourceUpdates();
			await WaitUntilReadyAsync(handler);

			Assert.NotSame(firstItem, handler.Manager.NativePlayerItem);
			Assert.Contains(server.Requests, request => request.Headers.TryGetValue("X-Device-Test", out var value) && value == "second");
		});
	}

	[Fact]
	public async Task UriSourceWithoutUriClearsThePlayerItem()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();

		await HostAsync(mediaElement, async handler =>
		{
			var manager = handler.Manager!;
			await LoadAsync(mediaElement, handler, ResourceSource(VideoResource));

			mediaElement.Source = new UriMediaSource();
			await manager.TakeSourceUpdates();

			Assert.Null(manager.NativePlayer!.CurrentItem);
			Assert.Equal((0, 0), (mediaElement.MediaWidth, mediaElement.MediaHeight));
		});
	}

	// A kind of MediaSource that the manager does not know cannot be applied: it replaces the previous media and is reported through MediaFailed,
	// with the message of a NotSupportedException (as on Android), not as a source without data (None).
	[Fact]
	public async Task UnknownMediaSourceIsReportedAsFailedAndClearsThePlayerItem()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();
		var events = new MediaElementEvents(mediaElement);

		await HostAsync(mediaElement, async handler =>
		{
			var manager = handler.Manager!;
			await LoadAsync(mediaElement, handler, ResourceSource(VideoResource));

			var setterException = Record.Exception(() => mediaElement.Source = new AppleUnknownMediaSource());
			await manager.TakeSourceUpdates();
			await DrainMainQueueAsync();

			Assert.Null(setterException);
			Assert.Null(manager.NativePlayer!.CurrentItem);
			Assert.Null(manager.NativePlayerItem);
			var failure = Assert.Single(events.Failures);
			Assert.Contains(typeof(AppleUnknownMediaSource).FullName!, failure);
			Assert.Contains("is not yet supported", failure);
			Assert.Equal(MediaElementState.Failed, mediaElement.CurrentState);
			Assert.Equal((0, 0), (mediaElement.MediaWidth, mediaElement.MediaHeight));
		});
	}

	// A source without data empties the player and reports None, like a null Source, on every platform; it is not a failure.
	[Theory]
	[InlineData("a Uri source without Uri")]
	[InlineData("a file source without path")]
	[InlineData("a file source with a blank path")]
	[InlineData("a resource source without path")]
	[InlineData("a stream source without stream")]
	public async Task SourceWithoutDataAfterAValidSourceEmptiesThePlayerAndReportsNone(string source)
	{
		SetupApple();
		var mediaElement = CreateMediaElement();
		var events = new MediaElementEvents(mediaElement);

		await HostAsync(mediaElement, async handler =>
		{
			var manager = handler.Manager!;
			await LoadAsync(mediaElement, handler, ResourceSource(VideoResource));
			Assert.NotNull(manager.NativePlayer!.CurrentItem);

			mediaElement.Source = source switch
			{
				"a Uri source without Uri" => new UriMediaSource(),
				"a file source without path" => new FileMediaSource(),
				"a file source with a blank path" => new FileMediaSource { Path = "   " },
				"a resource source without path" => new ResourceMediaSource(),
				"a stream source without stream" => new StreamMediaSource(),
				_ => throw new ArgumentOutOfRangeException(nameof(source), source, null),
			};
			await manager.TakeSourceUpdates();
			await DrainMainQueueAsync();

			Assert.Null(manager.NativePlayer.CurrentItem);
			Assert.Null(manager.NativePlayerItem);
			Assert.Equal((0, 0), (mediaElement.MediaWidth, mediaElement.MediaHeight));
			Assert.Equal(MediaElementState.None, mediaElement.CurrentState);
			Assert.Empty(events.Failures);
		});
	}

	[Fact]
	public async Task NullSourceClearsThePreviousItemAndTheMediaSize()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();
		var events = new MediaElementEvents(mediaElement);

		await HostAsync(mediaElement, async handler =>
		{
			var manager = handler.Manager!;
			await LoadAsync(mediaElement, handler, ResourceSource(VideoResource));
			Assert.Equal((64, 48), (mediaElement.MediaWidth, mediaElement.MediaHeight));
			events.States.Clear();

			mediaElement.Source = null;
			await manager.TakeSourceUpdates();

			Assert.Null(manager.NativePlayer!.CurrentItem);
			Assert.Equal((0, 0), (mediaElement.MediaWidth, mediaElement.MediaHeight));
			Assert.Equal(MediaElementState.Opening, events.States[0]);
			Assert.Contains(events.States, state => state is MediaElementState.None);
		});
	}

	[Fact]
	public async Task NewSourceReportsTheOpeningStateFirst()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();
		var events = new MediaElementEvents(mediaElement);

		await HostAsync(mediaElement, async handler =>
		{
			events.States.Clear();

			await LoadAsync(mediaElement, handler, ResourceSource(VideoResource));

			Assert.Equal(MediaElementState.Opening, events.States[0]);
			Assert.Contains(MediaElementState.Paused, events.States);
		});
	}

	[Fact]
	public async Task AutoPlayStartsPlaybackWhenTheSourceIsLoaded()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();
		mediaElement.ShouldAutoPlay = true;

		await HostAsync(mediaElement, async handler =>
		{
			var player = handler.Manager!.NativePlayer!;

			await LoadAsync(mediaElement, handler, ResourceSource(VideoResource));

			await WaitUntilAsync(() => player.TimeControlStatus is AVPlayerTimeControlStatus.Playing, "The player did not start playing.");
			Assert.Equal(1f, player.Rate);
			await WaitUntilAsync(() => mediaElement.CurrentState is MediaElementState.Playing, "The state did not become Playing.");
		});
	}

	[Fact]
	public async Task LoadingASourceDoesNotStartPlaybackWithoutAutoPlay()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();

		await HostAsync(mediaElement, async handler =>
		{
			var player = handler.Manager!.NativePlayer!;

			await LoadAsync(mediaElement, handler, ResourceSource(VideoResource));

			Assert.Equal(0f, player.Rate);
			Assert.Equal(AVPlayerTimeControlStatus.Paused, player.TimeControlStatus);
		});
	}

	[Fact]
	public async Task RotatedVideoReportsTheSizeAfterTheTrackTransform()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();

		await HostAsync(mediaElement, async handler =>
		{
			await LoadAsync(mediaElement, handler, ResourceSource(RotatedVideoResource));

			var track = handler.Manager!.NativePlayerItem!.Asset.Tracks.Single();
			Assert.Equal((64, 48), (track.NaturalSize.Width, track.NaturalSize.Height));
			Assert.Equal((48, 64), (mediaElement.MediaWidth, mediaElement.MediaHeight));
		});
	}

	[Fact]
	public async Task AudioSourceOpensWithoutMediaSize()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();
		var events = new MediaElementEvents(mediaElement);

		await HostAsync(mediaElement, async handler =>
		{
			await LoadAsync(mediaElement, handler, ResourceSource(AudioResource));

			Assert.Equal(1, events.Opened);
			Assert.Empty(events.Failures);
			Assert.Equal((0, 0), (mediaElement.MediaWidth, mediaElement.MediaHeight));
			Assert.Equal(AVMediaTypes.Audio.GetConstant()!.ToString(), handler.Manager!.NativePlayerItem!.Asset.Tracks.Single().MediaType);
		});
	}

	[Fact]
	public async Task SourceWithoutTracksIsReportedAsFailed()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();
		var events = new MediaElementEvents(mediaElement);

		await HostAsync(mediaElement, async handler =>
		{
			var player = handler.Manager!.NativePlayer!;

			Assert.Null(await SetSourceAsync(mediaElement, handler, ResourceSource(TracklessResource)));

			// A well-formed file without any track: AVFoundation has nothing to play (AVAsset.Playable is false) and never makes the item
			// ReadyToPlay, so the media was not opened. It must be reported as failed, without a media size.
			var item = handler.Manager.NativePlayerItem!;
			Assert.Empty(item.Asset.Tracks);
			Assert.False(item.Asset.Playable);
			Assert.NotEqual(AVPlayerItemStatus.ReadyToPlay, item.Status);
			Assert.Equal(0, events.Opened);
			Assert.Single(events.Failures);
			Assert.Equal((0, 0), (mediaElement.MediaWidth, mediaElement.MediaHeight));

			// Bounded wait (it does not fail) for the player's own status, which must not hide the failure.
			await AssertHelpers.Wait(() => player.Status is not AVPlayerStatus.Unknown, timeout: 2000, interval: 20);
			await DrainMainQueueAsync();
			Assert.Equal(MediaElementState.Failed, mediaElement.CurrentState);
		});
	}

	[Fact]
	public async Task UnreadableMediaRaisesMediaFailedAndLogsTheError()
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
				mediaElement.Source = MediaSource.FromFile(path);
				await Record.ExceptionAsync(handler.Manager!.TakeSourceUpdates);

				await WaitUntilAsync(() => events.Failures.Count > 0, "MediaFailed was not raised.");
				var item = handler.Manager.NativePlayerItem!;
				Assert.Equal(AVPlayerItemStatus.Failed, item.Status);
				var message = Assert.Single(events.Failures);
				Assert.Equal($"{item.Error!.LocalizedDescription} - {item.Error.LocalizedFailureReason}", message);
				Assert.Equal(MediaElementState.Failed, mediaElement.CurrentState);
				Assert.Contains(Logs.Entries, entry => entry.Level is LogLevel.Error && entry.Message == message);
			});
		}
		finally
		{
			File.Delete(path);
		}
	}

	[Fact]
	public async Task ReplacingTheFailedSourceOpensTheNewMedia()
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
				mediaElement.Source = MediaSource.FromFile(path);
				await Record.ExceptionAsync(handler.Manager!.TakeSourceUpdates);
				await WaitUntilAsync(() => events.Failures.Count > 0, "MediaFailed was not raised.");

				await LoadAsync(mediaElement, handler, ResourceSource(VideoResource));

				Assert.Equal((64, 48), (mediaElement.MediaWidth, mediaElement.MediaHeight));
				Assert.Equal(AVPlayerItemStatus.ReadyToPlay, handler.Manager.NativePlayerItem!.Status);
				Assert.Single(events.Failures);
			});
		}
		finally
		{
			File.Delete(path);
		}
	}

	static string CreateCorruptMediaFile()
	{
		var path = Path.Combine(Path.GetTempPath(), $"corrupt_{Guid.NewGuid():N}.mp4");
		File.WriteAllText(path, "This is not a media file.");
		return path;
	}

	[Fact]
	public async Task UnreadableMediaIsNotReportedAsOpened()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();
		var events = new MediaElementEvents(mediaElement);
		var path = CreateCorruptMediaFile();

		try
		{
			await HostAsync(mediaElement, async handler =>
			{
				await SetSourceAsync(mediaElement, handler, MediaSource.FromFile(path));
				await WaitUntilAsync(() => events.Failures.Count > 0, "MediaFailed was not raised.");

				Assert.Equal(0, events.Opened);
			});
		}
		finally
		{
			File.Delete(path);
		}
	}

	[Fact]
	public async Task UnreadableMediaDoesNotKeepTheSizeOfThePreviousMedia()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();
		var events = new MediaElementEvents(mediaElement);
		var path = CreateCorruptMediaFile();

		try
		{
			await HostAsync(mediaElement, async handler =>
			{
				await LoadAsync(mediaElement, handler, ResourceSource(VideoResource));
				Assert.Equal((64, 48), (mediaElement.MediaWidth, mediaElement.MediaHeight));

				await SetSourceAsync(mediaElement, handler, MediaSource.FromFile(path));
				await WaitUntilAsync(() => events.Failures.Count > 0, "MediaFailed was not raised.");

				Assert.Equal((0, 0), (mediaElement.MediaWidth, mediaElement.MediaHeight));
			});
		}
		finally
		{
			File.Delete(path);
		}
	}

	[Fact]
	public async Task UnreadableMediaDoesNotFaultTheSourceUpdate()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();
		var path = CreateCorruptMediaFile();

		try
		{
			await HostAsync(mediaElement, async handler =>
			{
				await handler.Manager!.TakeSourceUpdates();

				var exception = await SetSourceAsync(mediaElement, handler, MediaSource.FromFile(path));

				Assert.Null(exception);
			});
		}
		finally
		{
			File.Delete(path);
		}
	}

	// Only a source that cannot be applied or played is reported through MediaFailed. An exception that an event handler throws while the source
	// changes is not a failure of the source: the update of iOS and Mac Catalyst runs synchronously, so it propagates to the code that sets Source.
	[Fact]
	public async Task ExceptionOfAnEventHandlerDuringASourceChangePropagatesAndIsNotReportedAsMediaFailed()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();
		var events = new MediaElementEvents(mediaElement);
		var handlerException = new InvalidOperationException("The StateChanged handler of the app failed.");

		await HostAsync(mediaElement, async handler =>
		{
			var manager = handler.Manager!;
			await manager.TakeSourceUpdates();
			mediaElement.StateChanged += (_, e) =>
			{
				if (e.NewState is MediaElementState.Opening)
				{
					throw handlerException;
				}
			};

			var setterException = Record.Exception(() => mediaElement.Source = ResourceSource(VideoResource));
			await Record.ExceptionAsync(manager.TakeSourceUpdates);
			await DrainMainQueueAsync();

			Assert.Same(handlerException, ExceptionOfTheHandler(setterException));

			// A failure of the source raises MediaFailed. CurrentState cannot show one here: MAUI applies no change of CurrentState after a StateChanged
			// handler threw (BindableObject queues the changes of a property while it is being set, and the exception never ends that set)
			Assert.Empty(events.Failures);
		});
	}

	// The part of a source update that runs after an await (the updates of Android and Windows do) has no caller left: an exception of an event
	// handler that it raises is rethrown on the dispatcher, as the exception of a handler of an event that the player raises, and is not reported
	// as MediaFailed either.
	[Fact]
	public async Task ExceptionOfAnEventHandlerAfterTheSourceUpdateAwaitedIsRethrownOnTheDispatcher()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();
		var events = new MediaElementEvents(mediaElement);
		var handlerException = new InvalidOperationException("The StateChanged handler of the app failed.");

		await HostAsync(mediaElement, async handler =>
		{
			var manager = handler.Manager!;
			await manager.TakeSourceUpdates();
			var resume = new TaskCompletionSource();
			manager.DelayNextSourceUpdate = resume.Task;
			mediaElement.StateChanged += (_, e) =>
			{
				if (e.NewState is MediaElementState.Opening)
				{
					throw handlerException;
				}
			};

			// The capturing context receives what the source mapping rethrows on the dispatcher (where it would end the app), and runs the rest of the
			// update inline on this thread.
			var context = new AppleCapturingSynchronizationContext();
			Exception? setterException = null;
			context.Run(() => setterException = Record.Exception(() => mediaElement.Source = ResourceSource(VideoResource)));
			Assert.Null(setterException);

			resume.SetResult();
			await Record.ExceptionAsync(manager.TakeSourceUpdates);

			Assert.Same(handlerException, ExceptionOfTheHandler(context.Captured));
			Assert.Empty(events.Failures);
		});
	}

	// The part of the update whose failures are failures of the source ends with the creation of the asset and its item. The events raised after it
	// are not in it, here None once a source without data has taken the previous media out of the player: an exception of its handler propagates to
	// the code that sets Source, and is not reported as MediaFailed.
	[Fact]
	public async Task ExceptionOfANoneHandlerAfterTheSourceIsAppliedPropagatesAndIsNotReportedAsMediaFailed()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();
		var events = new MediaElementEvents(mediaElement);
		var handlerException = new InvalidOperationException("The StateChanged handler of the app failed.");

		await HostAsync(mediaElement, async handler =>
		{
			var manager = handler.Manager!;
			await LoadAsync(mediaElement, handler, ResourceSource(VideoResource));
			var isSettingTheSource = false;
			mediaElement.StateChanged += (_, e) =>
			{
				if (isSettingTheSource && e.NewState is MediaElementState.None)
				{
					throw handlerException;
				}
			};

			isSettingTheSource = true;
			var setterException = Record.Exception(() => mediaElement.Source = new UriMediaSource());
			isSettingTheSource = false;
			await Record.ExceptionAsync(manager.TakeSourceUpdates);
			await DrainMainQueueAsync();

			Assert.Same(handlerException, ExceptionOfTheHandler(setterException));
			Assert.Empty(events.Failures);
			Assert.Null(manager.NativePlayer!.CurrentItem);
			Assert.Equal(MediaElementState.None, mediaElement.CurrentState);
		});
	}

	// MAUI's WeakEventManager, which raises the MediaElement's events, invokes the handlers through reflection: the exception of a handler reaches
	// the caller in a TargetInvocationException.
	static Exception? ExceptionOfTheHandler(Exception? exception) => exception is TargetInvocationException { InnerException: Exception inner } ? inner : exception;

	[Fact]
	public async Task ClearingTheSourceWhileTheMediaLoadsLeavesNoStaleState()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();

		await HostAsync(mediaElement, async handler =>
		{
			var manager = handler.Manager!;
			var player = manager.NativePlayer!;
			await manager.TakeSourceUpdates();

			// Same main-thread turn: the first update is still loading the video's tracks when the second one clears the source.
			mediaElement.Source = ResourceSource(VideoResource);
			mediaElement.Source = null;
			await manager.TakeSourceUpdates();

			// Bounded wait (it does not fail) for a late status of the player, which used to have an item.
			await AssertHelpers.Wait(() => player.Status is not AVPlayerStatus.Unknown, timeout: 2000, interval: 20);

			Assert.Null(player.CurrentItem);
			Assert.Equal((0, 0), (mediaElement.MediaWidth, mediaElement.MediaHeight));
			Assert.Equal(MediaElementState.None, mediaElement.CurrentState);
		});
	}

	[Fact]
	public async Task ClearingTheSourceWhileTheMediaLoadsNeitherAutoPlaysNorKeepsItsSize()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();

		await HostAsync(mediaElement, async handler =>
		{
			var manager = handler.Manager!;
			var player = manager.NativePlayer!;
			await manager.TakeSourceUpdates();
			Assert.Equal(0f, player.Rate);

			// Set after the handler's first mapping of Speed, which also reads ShouldAutoPlay before #3281.
			mediaElement.ShouldAutoPlay = true;

			// Same main-thread turn: the video's item is replaced before its status handler (or, before #3281, the rest of its update) runs.
			mediaElement.Source = ResourceSource(VideoResource);
			mediaElement.Source = null;
			await manager.TakeSourceUpdates();
			await DrainMainQueueAsync();

			// Bounded wait (it does not fail) for a late status of the player, which used to have an item.
			await AssertHelpers.Wait(() => player.Status is not AVPlayerStatus.Unknown, timeout: 2000, interval: 20);
			await DrainMainQueueAsync();

			Assert.Null(player.CurrentItem);
			Assert.Equal(0f, player.Rate);
			Assert.Equal((0, 0), (mediaElement.MediaWidth, mediaElement.MediaHeight));
		});
	}

	[Fact]
	public async Task SourceUpdateWithoutPlayerCompletesWithoutAnItem()
	{
		SetupApple();
		using var mediaElement = CreateMediaElement();

		await InvokeOnMainThreadAsync(async () =>
		{
			using var manager = new AppleMediaManager(MauiContext, mediaElement, TestDispatcher.Current);
			mediaElement.Source = ResourceSource(VideoResource);

			await manager.UpdateSource();

			Assert.Null(manager.NativePlayer);
			Assert.Null(manager.NativePlayerItem);
		});
	}

	[Fact]
	public async Task NowPlayingMetadataIsCreatedOnceAndReused()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();

		await HostAsync(mediaElement, async handler =>
		{
			var manager = handler.Manager!;
			await LoadAsync(mediaElement, handler, ResourceSource(VideoResource));
			var metadata = GetMetadata(manager);

			await LoadAsync(mediaElement, handler, ResourceSource(AudioResource));

			Assert.Same(metadata, GetMetadata(manager));
		});
	}

	[Fact]
	public async Task GetVideoDimensionsUsesThePresentationSizeWhenThereIsNoVideoTrack()
	{
		SetupApple();
		var method = typeof(MediaManager).GetMethod("GetVideoDimensions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
		var audioAsset = AVAsset.FromUrl(NSUrl.FromFilename(BundlePath("audio", "m4a")));

		// Before #3281 GetVideoDimensions(item) loads the item's video track itself (the audio asset has none); since #3281
		// GetVideoDimensions(item, track) gets the track that the caller loaded, here none.
		async Task<(int Width, int Height)> GetVideoDimensions(AVPlayerItem item) => method.GetParameters().Length == 1
			? await (Task<(int Width, int Height)>)method.Invoke(null, [item])!
			: ((int Width, int Height))method.Invoke(null, [item, null])!;

		await InvokeOnMainThreadAsync(async () =>
		{
			// An HLS stream has no tracks to ask, but the item knows how large it presents the video.
			var sized = new AppleFakeAVPlayerItem(audioAsset) { PresentationSizeOverride = new CoreGraphics.CGSize(320.7, 180.2) };
			Assert.Equal((320, 180), await GetVideoDimensions(sized));

			var unsized = new AppleFakeAVPlayerItem(audioAsset) { PresentationSizeOverride = CoreGraphics.CGSize.Empty };
			Assert.Equal((0, 0), await GetVideoDimensions(unsized));
		});
	}

	[Fact]
	public async Task VideoTrackIsFoundWithTheSynchronousApiBeforeIos18()
	{
		SetupApple();
		Skip.If(
			OperatingSystem.IsIOSVersionAtLeast(18) || OperatingSystem.IsMacCatalystVersionAtLeast(18),
			"GetTrack takes its pre-iOS 18 / Mac Catalyst 18 branch (AVAsset.TracksWithMediaType) only on older OS versions; the oldest runtime available here is iOS 18.6, so this device runs the LoadTracksWithMediaType branch instead.");
		var mediaElement = CreateMediaElement();

		await HostAsync(mediaElement, async handler =>
		{
			await LoadAsync(mediaElement, handler, ResourceSource(VideoResource));

			Assert.Equal((64, 48), (mediaElement.MediaWidth, mediaElement.MediaHeight));
		});
	}
}