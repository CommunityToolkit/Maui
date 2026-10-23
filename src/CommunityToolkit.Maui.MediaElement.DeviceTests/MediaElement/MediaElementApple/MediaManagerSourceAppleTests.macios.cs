using AVFoundation;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Views;
using CommunityToolkit.Maui.DeviceTests.Runners;
using CommunityToolkit.Maui.Views;
using Foundation;
using Microsoft.Extensions.Logging;

namespace CommunityToolkit.Maui.DeviceTests;

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

	// As on Android, an unknown kind of source fails with the message of a NotSupportedException instead of being treated as a source without
	// data (None).
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

	// Not a failure: like a null Source, a source without data reports None on every platform.
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
			// ReadyToPlay, so the media is not opened and must be reported as failed.
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

	// Paused shows that the set of CurrentState whose handler threw has ended: MAUI applies no later change of a property whose setting threw.
	[Fact]
	public async Task ExceptionOfAnOpeningHandlerIsReportedThroughMediaFailedAndTheSourceIsApplied()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();
		var events = new MediaElementEvents(mediaElement);
		var handlerException = new InvalidOperationException("The StateChanged handler of the app failed.");

		await HostAsync(mediaElement, async handler =>
		{
			var manager = handler.Manager;
			Assert.NotNull(manager);
			await manager.TakeSourceUpdates();
			mediaElement.StateChanged += (_, e) =>
			{
				if (e.NewState is MediaElementState.Opening)
				{
					throw handlerException;
				}
			};

			var opened = WhenOpenedOrFailedAsync(mediaElement, openedOnly: true);
			var setterException = Record.Exception(() => mediaElement.Source = ResourceSource(VideoResource));
			await manager.TakeSourceUpdates();

			Assert.Null(setterException);
			AssertReportsTheExceptionOfTheHandler(events, handlerException);

			await WaitAsync(opened, "The media of the source was not opened.");
			Assert.NotNull(manager.NativePlayer?.CurrentItem);
			Assert.Equal(MediaElementState.Paused, mediaElement.CurrentState);
			Assert.Single(events.Failures);
		});
	}

	// MAUI raises PropertyChanging before it stores the value: StateChanged for Opening shows that the value was stored all the same, and Paused
	// that the set whose handler threw has ended (MAUI applies no later change of a property whose setting threw).
	[Fact]
	public async Task ExceptionOfAPropertyChangingHandlerIsReportedThroughMediaFailedAndTheSourceIsApplied()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();
		var events = new MediaElementEvents(mediaElement);
		var handlerException = new InvalidOperationException("The PropertyChanging handler of the app failed.");

		await HostAsync(mediaElement, async handler =>
		{
			var manager = handler.Manager;
			Assert.NotNull(manager);
			await manager.TakeSourceUpdates();
			var isSettingTheSource = false;
			mediaElement.PropertyChanging += (_, e) =>
			{
				if (isSettingTheSource && e.PropertyName is nameof(MediaElement.CurrentState))
				{
					throw handlerException;
				}
			};

			var opened = WhenOpenedOrFailedAsync(mediaElement, openedOnly: true);
			var statesBeforeTheSource = events.States.Count;
			isSettingTheSource = true;
			var setterException = Record.Exception(() => mediaElement.Source = ResourceSource(VideoResource));
			isSettingTheSource = false;
			await manager.TakeSourceUpdates();

			Assert.Null(setterException);
			AssertReportsTheExceptionOfTheHandler(events, handlerException);
			Assert.Contains(MediaElementState.Opening, events.States.Skip(statesBeforeTheSource));

			await WaitAsync(opened, "The media of the source was not opened.");
			Assert.NotNull(manager.NativePlayer?.CurrentItem);
			Assert.Equal(MediaElementState.Paused, mediaElement.CurrentState);
			Assert.Single(events.Failures);
		});
	}

	// The update of iOS and Mac Catalyst is synchronous, so the test double makes it continue after an await, as the updates of Android and Windows
	// do: what is thrown from then on can only reach the dispatcher, which ends the app.
	[Fact]
	public async Task ExceptionOfAnEventHandlerAfterTheSourceUpdateAwaitedIsReportedThroughMediaFailed()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();
		var events = new MediaElementEvents(mediaElement);
		var handlerException = new InvalidOperationException("The StateChanged handler of the app failed.");

		await HostAsync(mediaElement, async handler =>
		{
			var manager = handler.Manager;
			Assert.NotNull(manager);
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

			// The context receives what would be rethrown on the dispatcher, and runs the rest of the update inline on this thread.
			var context = new AppleCapturingSynchronizationContext();
			Exception? setterException = null;
			context.Run(() => setterException = Record.Exception(() => mediaElement.Source = ResourceSource(VideoResource)));
			Assert.Null(setterException);
			Assert.Empty(events.Failures);

			resume.SetResult();
			await manager.TakeSourceUpdates();
			await WaitUntilAsync(() => events.Failures.Count > 0, "The exception of the handler was not reported.");

			Assert.Null(context.Captured);
			AssertReportsTheExceptionOfTheHandler(events, handlerException);
			Assert.NotNull(manager.NativePlayer?.CurrentItem);
			Assert.NotEqual(MediaElementState.Failed, mediaElement.CurrentState);
		});
	}

	// None is raised after the part of the update whose failures are failures of the source, once a source without data has taken the previous
	// media out of the player.
	[Fact]
	public async Task ExceptionOfANoneHandlerAfterTheSourceIsAppliedIsReportedThroughMediaFailed()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();
		var events = new MediaElementEvents(mediaElement);
		var handlerException = new InvalidOperationException("The StateChanged handler of the app failed.");

		await HostAsync(mediaElement, async handler =>
		{
			var manager = handler.Manager;
			Assert.NotNull(manager);
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
			await manager.TakeSourceUpdates();
			await DrainMainQueueAsync();

			Assert.Null(setterException);
			AssertReportsTheExceptionOfTheHandler(events, handlerException);
			var player = manager.NativePlayer;
			Assert.NotNull(player);
			Assert.Null(player.CurrentItem);
			Assert.Equal(MediaElementState.None, mediaElement.CurrentState);
		});
	}

	// MediaFailed resets Duration before it raises the event, so the exception of the Duration handler must not stop the report of the failure. The
	// Duration of the next source shows that the set whose handler threw has ended: MAUI applies no later change of a property whose setting threw.
	[Fact]
	public async Task ExceptionOfADurationHandlerWhileAFailedSourceResetsTheDurationIsReportedAfterTheFailure()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();
		var events = new MediaElementEvents(mediaElement);
		var handlerException = new InvalidOperationException("The PropertyChanged handler of the app failed.");

		await HostAsync(mediaElement, async handler =>
		{
			var manager = handler.Manager;
			Assert.NotNull(manager);
			await LoadAsync(mediaElement, handler, ResourceSource(VideoResource));
			var isSettingTheSource = false;
			mediaElement.PropertyChanged += (_, e) =>
			{
				if (isSettingTheSource && e.PropertyName is nameof(MediaElement.Duration))
				{
					throw handlerException;
				}
			};

			isSettingTheSource = true;
			var setterException = Record.Exception(() => mediaElement.Source = new AppleUnknownMediaSource());
			isSettingTheSource = false;
			await manager.TakeSourceUpdates();

			Assert.Null(setterException);
			Assert.Equal(2, events.Failures.Count);
			Assert.Equal($"{typeof(AppleUnknownMediaSource).FullName} is not yet supported for {nameof(MediaElement.Source)}", events.Failures[0]);
			Assert.EndsWith($": {handlerException.GetType().FullName}: {handlerException.Message}", events.Failures[1]);
			Assert.Equal(MediaElementState.Failed, mediaElement.CurrentState);
			Assert.Equal(TimeSpan.Zero, mediaElement.Duration);

			await LoadAsync(mediaElement, handler, ResourceSource(VideoResource));
			Assert.Equal(VideoDuration.TotalSeconds, mediaElement.Duration.TotalSeconds, precision: 1);
		});
	}

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

	void AssertReportsTheExceptionOfTheHandler(MediaElementEvents events, Exception handlerException)
	{
		var failure = Assert.Single(events.Failures);
		Assert.EndsWith($": {handlerException.GetType().FullName}: {handlerException.Message}", failure);
		Assert.Contains(Logs.Entries, entry => entry.Level is LogLevel.Error && ReferenceEquals(entry.Exception, handlerException));
	}
}