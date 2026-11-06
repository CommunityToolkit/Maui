using System.Net;
using System.Net.Sockets;
using System.Reflection;
using AVFoundation;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;
using CoreGraphics;
using CoreMedia;
using Foundation;
using MediaPlayer;
using UIKit;

namespace CommunityToolkit.Maui.DeviceTests;

// The command handlers (private methods of Metadata) are called directly for the arguments the system never sends (no event, another
// kind of event), and the commands are also delivered the way the system delivers them (AppleRemoteCommands.Dispatch: a real command
// event through MediaPlayer) to check which handler each command reaches.
[Category(TestCategory.MediaElementApple)]
[Collection(TestCollections.DisableParallelization)]
public class MetadataAppleTests : AppleMediaElementTestBase
{
	const string longAudioName = "long";
	const string longAudioResource = "avplayer/long.m4a";

	static readonly MethodInfo getImage = typeof(Metadata).GetMethod("GetImage", BindingFlags.NonPublic | BindingFlags.Static)!;

	static UIImage DefaultImage => (UIImage)typeof(Metadata).GetField("defaultUIImage", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!;

	static MPRemoteCommandHandlerStatus Invoke(Metadata metadata, string handlerName, MPRemoteCommandEvent? commandEvent) =>
		(MPRemoteCommandHandlerStatus)typeof(Metadata).GetMethod(handlerName, BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(metadata, [commandEvent])!;

	static UIImage InvokeGetImage(string? url) => (UIImage)getImage.Invoke(null, [url])!;

	static void AssertDefaultNowPlayingInfo(MPNowPlayingInfo? info)
	{
		Assert.NotNull(info);
		Assert.Equal(string.Empty, info.Title);
		Assert.Equal(string.Empty, info.Artist);
		Assert.Equal(string.Empty, info.AlbumTitle);
		Assert.Equal(0d, info.PlaybackDuration);
		Assert.False(info.IsLiveStream);
		Assert.Equal(0f, info.PlaybackRate);
		Assert.Equal(0d, info.ElapsedPlaybackTime);
		Assert.Equal(CGSize.Empty, info.Artwork!.Bounds.Size);
	}

	static void ShowNowPlayingInfoOfAnotherApp() =>
		MPNowPlayingInfoCenter.DefaultCenter.NowPlaying = new MPNowPlayingInfo { Title = "Another app", Artist = "Someone else", PlaybackDuration = 99, PlaybackRate = 3, ElapsedPlaybackTime = 7 };

	static string UnreachableUrl()
	{
		var listener = new TcpListener(IPAddress.Loopback, 0);
		listener.Start();
		var port = ((IPEndPoint)listener.LocalEndpoint).Port;
		listener.Stop();
		return $"http://127.0.0.1:{port}/art.png";
	}

	// The command center keeps the targets of every Metadata ever created.
	async Task WithCleanRemoteCommandsAsync(Func<Task> action)
	{
		await InvokeOnMainThreadAsync(AppleRemoteCommands.RemoveAllTargets);
		try
		{
			await action();
		}
		finally
		{
			await InvokeOnMainThreadAsync(AppleRemoteCommands.RemoveAllTargets);
		}
	}

	// The remote commands start playback through the play callback (MediaManager.PlayAtSpeed, which starts at the Speed), not with
	// AVPlayer.Play(), so the tests count the calls of the callback.
	int playCallbackCount;

	Task WithPlayerAsync(Func<AVPlayer, Metadata, Task> action) =>
		WithCleanRemoteCommandsAsync(async () =>
		{
			AVPlayer player = null!;
			AVPlayerItem item = null!;
			Metadata metadata = null!;
			await InvokeOnMainThreadAsync(() =>
			{
				item = new AVPlayerItem(AVAsset.FromUrl(NSUrl.FromFilename(BundlePath(longAudioName, "m4a"))));
				player = new AVPlayer(item) { Muted = true };
				metadata = new Metadata(player, () =>
				{
					playCallbackCount++;
					player.Play();
				});
			});

			try
			{
				await WaitUntilAsync(() => item.Status is AVPlayerItemStatus.ReadyToPlay && item.SeekableTimeRanges.Length > 0, "The media did not become ready to play.");
				await action(player, metadata);
			}
			finally
			{
				await InvokeOnMainThreadAsync(() =>
				{
					player.Pause();
					player.Dispose();
					item.Dispose();
				});
			}
		});

	static async Task SeekToAsync(AVPlayer player, double seconds)
	{
		await InvokeOnMainThreadAsync(() => player.Seek(CMTime.FromSeconds(seconds, 1000)));
		await WaitUntilAsync(() => Math.Abs(player.CurrentTime.Seconds - seconds) < 0.1, $"The player did not reach {seconds} s.");
	}

	static Task<MPRemoteCommandHandlerStatus> InvokeHandlerAsync(Metadata metadata, string handlerName, MPRemoteCommandEvent? commandEvent) =>
		InvokeOnMainThreadAsync(() => Invoke(metadata, handlerName, commandEvent));

	[Fact]
	public async Task ConstructorPublishesTheDefaultNowPlayingInfo()
	{
		SetupApple();

		await WithCleanRemoteCommandsAsync(() => InvokeOnMainThreadAsync(() =>
		{
			ShowNowPlayingInfoOfAnotherApp();
			using var player = new AVPlayer();

			var metadata = new Metadata(player, player.Play);

			AssertDefaultNowPlayingInfo(MPNowPlayingInfoCenter.DefaultCenter.NowPlaying);
			Assert.Null(metadata.NowPlayingInfo.Title);
		}));
	}

	[Theory]
	[InlineData("togglePlayPause", true)]
	[InlineData("play", true)]
	[InlineData("pause", true)]
	[InlineData("changePlaybackPosition", true)]
	[InlineData("seekBackward", true)]
	[InlineData("seekForward", false)]
	public async Task ConstructorSetsTheEnabledStateOfTheRemoteCommand(string commandName, bool expectedEnabled)
	{
		SetupApple();

		await WithCleanRemoteCommandsAsync(() => InvokeOnMainThreadAsync(() =>
		{
			var command = AppleRemoteCommands.Get(commandName);
			command.Enabled = !expectedEnabled;
			using var player = new AVPlayer();

			_ = new Metadata(player, player.Play);

			Assert.Equal(expectedEnabled, command.Enabled);
		}));
	}

	[Theory]
	[InlineData("togglePlayPause")]
	[InlineData("play")]
	[InlineData("pause")]
	[InlineData("changePlaybackPosition")]
	[InlineData("seekBackward")]
	[InlineData("seekForward")]
	public async Task ConstructorRegistersATargetOnTheRemoteCommand(string commandName)
	{
		SetupApple();
		Skip.IfNot(AppleRemoteCommands.CanQueryTargets, "This OS version does not tell whether an MPRemoteCommand has targets (MPRemoteCommand.hasTargets).");

		await WithCleanRemoteCommandsAsync(() => InvokeOnMainThreadAsync(() =>
		{
			Assert.False(AppleRemoteCommands.HasTargets(commandName));
			using var player = new AVPlayer();

			_ = new Metadata(player, player.Play);

			Assert.True(AppleRemoteCommands.HasTargets(commandName));
		}));
	}

	[Fact]
	public async Task ConstructorLeavesTheOtherRemoteCommandsAlone()
	{
		SetupApple();
		Skip.IfNot(AppleRemoteCommands.CanQueryTargets, "This OS version does not tell whether an MPRemoteCommand has targets (MPRemoteCommand.hasTargets).");

		await WithCleanRemoteCommandsAsync(() => InvokeOnMainThreadAsync(() =>
		{
			using var player = new AVPlayer();

			_ = new Metadata(player, player.Play);

			Assert.False(AppleRemoteCommands.HasTargets("nextTrack"));
		}));
	}

	[Fact]
	public async Task ClearNowPlayingPublishesTheDefaultNowPlayingInfo()
	{
		SetupApple();

		await InvokeOnMainThreadAsync(() =>
		{
			ShowNowPlayingInfoOfAnotherApp();
			Assert.Equal("Another app", MPNowPlayingInfoCenter.DefaultCenter.NowPlaying?.Title);

			Metadata.ClearNowPlaying();

			AssertDefaultNowPlayingInfo(MPNowPlayingInfoCenter.DefaultCenter.NowPlaying);
		});
	}

	[Fact]
	public async Task SetMetadataPublishesTheValuesOfTheMediaElementAndThePlayerItem()
	{
		SetupApple();

		await WithCleanRemoteCommandsAsync(() => InvokeOnMainThreadAsync(() =>
		{
			using var player = new AVPlayer();
			var metadata = new Metadata(player, player.Play);
			var mediaElement = new MediaElement { MetadataTitle = "Song", MetadataArtist = "Artist", Speed = 1.5 };
			var item = new AppleFakeAVPlayerItem(AVAsset.FromUrl(NSUrl.FromFilename(BundlePath("audio", "m4a"))))
			{
				DurationOverride = CMTime.FromSeconds(200, 1000),
				CurrentTimeOverride = CMTime.FromSeconds(42.5, 1000)
			};

			metadata.SetMetadata(item, mediaElement);

			var info = MPNowPlayingInfoCenter.DefaultCenter.NowPlaying!;
			Assert.Equal("Song", info.Title);
			Assert.Equal("Artist", info.Artist);
			Assert.Equal(200d, info.PlaybackDuration!.Value, precision: 3);
			Assert.Equal(42.5, info.ElapsedPlaybackTime!.Value, precision: 3);
			Assert.Equal(1.5f, info.PlaybackRate);
			Assert.False(info.IsLiveStream);
			Assert.Equal(new CGSize(320, 240), info.Artwork!.Bounds.Size);
		}));
	}

	[Fact]
	public async Task SetMetadataWithoutPlayerItemPublishesNoDurationAndNoElapsedTime()
	{
		SetupApple();

		await WithCleanRemoteCommandsAsync(() => InvokeOnMainThreadAsync(() =>
		{
			using var player = new AVPlayer();
			var metadata = new Metadata(player, player.Play);
			var mediaElement = new MediaElement { MetadataTitle = "Song" };
			var item = new AppleFakeAVPlayerItem(AVAsset.FromUrl(NSUrl.FromFilename(BundlePath("audio", "m4a"))))
			{
				DurationOverride = CMTime.FromSeconds(200, 1000),
				CurrentTimeOverride = CMTime.FromSeconds(42.5, 1000)
			};
			metadata.SetMetadata(item, mediaElement);
			Assert.Equal(200d, MPNowPlayingInfoCenter.DefaultCenter.NowPlaying!.PlaybackDuration!.Value, precision: 3);

			metadata.SetMetadata(null, mediaElement);

			var info = MPNowPlayingInfoCenter.DefaultCenter.NowPlaying!;
			Assert.Equal("Song", info.Title);
			Assert.Equal(0d, info.PlaybackDuration);
			Assert.Equal(0d, info.ElapsedPlaybackTime);
		}));
	}

	[Fact]
	public async Task SetMetadataWithoutMediaElementPublishesTheDefaultNowPlayingInfo()
	{
		SetupApple();

		await WithCleanRemoteCommandsAsync(() => InvokeOnMainThreadAsync(() =>
		{
			using var player = new AVPlayer();
			var metadata = new Metadata(player, player.Play);
			metadata.SetMetadata(null, new MediaElement { MetadataTitle = "Song", MetadataArtist = "Artist" });
			Assert.Equal("Song", MPNowPlayingInfoCenter.DefaultCenter.NowPlaying?.Title);

			metadata.SetMetadata(null, null);

			AssertDefaultNowPlayingInfo(MPNowPlayingInfoCenter.DefaultCenter.NowPlaying);
		}));
	}

	[Fact]
	public async Task NowPlayingArtworkIsLoadedFromTheMetadataArtworkUrl()
	{
		SetupApple();
		using var server = new AppleLoopbackHttpServer();
		server.Add("/art.png", File.ReadAllBytes(BundlePath("poster", "png")), "image/png");

		await WithCleanRemoteCommandsAsync(() => InvokeOnMainThreadAsync(() =>
		{
			using var player = new AVPlayer();
			var metadata = new Metadata(player, player.Play);
			var mediaElement = new MediaElement { MetadataArtworkUrl = server.GetUri("/art.png").AbsoluteUri };

			metadata.SetMetadata(null, mediaElement);

			var artwork = MPNowPlayingInfoCenter.DefaultCenter.NowPlaying!.Artwork!;
			var image = artwork.ImageWithSize(new CGSize(320, 240));
			Assert.Equal(new CGSize(40, 30), image!.Size);
			Assert.Contains(server.Requests, request => request.Path == "/art.png");
		}));
	}

	[Fact]
	public async Task ArtworkOfAnHttpUrlIsLoaded()
	{
		SetupApple();
		using var server = new AppleLoopbackHttpServer();
		server.Add("/art.png", File.ReadAllBytes(BundlePath("poster", "png")), "image/png");

		var image = await InvokeOnMainThreadAsync(() => InvokeGetImage(server.GetUri("/art.png").AbsoluteUri));

		Assert.Equal(new CGSize(40, 30), image.Size);
		Assert.NotSame(DefaultImage, image);
	}

	[Fact]
	public async Task ArtworkUrlSchemeIsMatchedIgnoringCase()
	{
		SetupApple();
		using var server = new AppleLoopbackHttpServer();
		server.Add("/art.png", File.ReadAllBytes(BundlePath("poster", "png")), "image/png");
		var url = server.GetUri("/art.png").AbsoluteUri.Replace("http://", "HTTP://", StringComparison.Ordinal);

		var image = await InvokeOnMainThreadAsync(() => InvokeGetImage(url));

		Assert.Equal(new CGSize(40, 30), image.Size);
	}

	[Theory]
	[InlineData("")]
	[InlineData("art.png")]
	[InlineData("file:///art.png")]
	[InlineData("ftp://127.0.0.1/art.png")]
	[InlineData("ht://127.0.0.1/art.png")]
	public async Task ArtworkWithoutAnHttpUrlIsNotLoaded(string url)
	{
		SetupApple();

		var image = await InvokeOnMainThreadAsync(() => InvokeGetImage(url));

		Assert.Same(DefaultImage, image);
	}

	[Fact]
	public async Task ArtworkOfALoadableFileUrlIsNotLoaded()
	{
		SetupApple();

		// The rows above cannot be loaded anyway; this file URL can (NSData reads it and UIImage decodes the 40x30 poster), so only the
		// http(s) scheme check keeps GetImage from loading it.
		var url = BundleFileUrl("poster", "png");
		var (loadable, image) = await InvokeOnMainThreadAsync(() =>
		{
			using var data = NSData.FromUrl(new NSUrl(url));
			using var decoded = data is null ? null : UIImage.LoadFromData(data);
			return (decoded?.Size, InvokeGetImage(url));
		});

		Assert.StartsWith("file://", url, StringComparison.Ordinal);
		Assert.Equal(new CGSize(40, 30), loadable);
		Assert.Same(DefaultImage, image);
	}

	[Fact]
	public async Task ArtworkWithoutAnUrlFallsBackToTheDefaultImage()
	{
		SetupApple();

		var image = await InvokeOnMainThreadAsync(() => InvokeGetImage(null));

		Assert.Same(DefaultImage, image);
	}

	[Fact]
	public async Task ArtworkThatTheServerCannotDeliverFallsBackToTheDefaultImage()
	{
		SetupApple();

		// Nothing listens on the port: NSData.FromUrl returns null and UIImage.LoadFromData(null) throws.
		var image = await InvokeOnMainThreadAsync(() => InvokeGetImage(UnreachableUrl()));

		Assert.Same(DefaultImage, image);
	}

	[Fact]
	public async Task ArtworkOfAnUnknownPathFallsBackToTheDefaultImage()
	{
		SetupApple();
		using var server = new AppleLoopbackHttpServer();

		// The server answers 404 with an empty body, which UIImage cannot decode: LoadFromData returns null.
		var image = await InvokeOnMainThreadAsync(() => InvokeGetImage(server.GetUri("/missing.png").AbsoluteUri));

		Assert.Same(DefaultImage, image);
		Assert.Contains(server.Requests, request => request.Path == "/missing.png");
	}

	[Fact]
	public async Task ArtworkThatIsNotAnImageFallsBackToTheDefaultImage()
	{
		SetupApple();
		using var server = new AppleLoopbackHttpServer();
		server.Add("/art.png", "This is not an image."u8.ToArray(), "image/png");

		var image = await InvokeOnMainThreadAsync(() => InvokeGetImage(server.GetUri("/art.png").AbsoluteUri));

		Assert.Same(DefaultImage, image);
		Assert.Contains(server.Requests, request => request.Path == "/art.png");
	}

	[Fact]
	public async Task ArtworkWithAMalformedUrlFallsBackToTheDefaultImage()
	{
		SetupApple();

		var image = await InvokeOnMainThreadAsync(() => InvokeGetImage("http://[not a host/art.png"));

		Assert.Same(DefaultImage, image);
	}

	[Fact]
	public async Task PlayCommandStartsThePlayer()
	{
		SetupApple();

		await WithPlayerAsync(async (player, metadata) =>
		{
			Assert.Equal(0f, player.Rate);

			var status = await InvokeHandlerAsync(metadata, "PlayCommand", new AppleRemoteCommandEvent());

			Assert.Equal(MPRemoteCommandHandlerStatus.Success, status);
			Assert.Equal(1, playCallbackCount);
			Assert.Equal(1f, player.Rate);
		});
	}

	[Fact]
	public async Task PlayCommandWithoutEventFails()
	{
		SetupApple();

		await WithPlayerAsync(async (player, metadata) =>
		{
			var status = await InvokeHandlerAsync(metadata, "PlayCommand", null);

			Assert.Equal(MPRemoteCommandHandlerStatus.CommandFailed, status);
			Assert.Equal(0, playCallbackCount);
			Assert.Equal(0f, player.Rate);
		});
	}

	[Fact]
	public async Task PauseCommandPausesThePlayer()
	{
		SetupApple();

		await WithPlayerAsync(async (player, metadata) =>
		{
			await InvokeOnMainThreadAsync(player.Play);
			Assert.Equal(1f, player.Rate);

			var status = await InvokeHandlerAsync(metadata, "PauseCommand", new AppleRemoteCommandEvent());

			Assert.Equal(MPRemoteCommandHandlerStatus.Success, status);
			Assert.Equal(0f, player.Rate);
		});
	}

	[Fact]
	public async Task PauseCommandWithoutEventFails()
	{
		SetupApple();

		await WithPlayerAsync(async (player, metadata) =>
		{
			await InvokeOnMainThreadAsync(player.Play);

			var status = await InvokeHandlerAsync(metadata, "PauseCommand", null);

			Assert.Equal(MPRemoteCommandHandlerStatus.CommandFailed, status);
			Assert.Equal(1f, player.Rate);
		});
	}

	[Fact]
	public async Task SeekCommandMovesThePlayerToTheRequestedPosition()
	{
		SetupApple();

		await WithPlayerAsync(async (player, metadata) =>
		{
			var status = await InvokeHandlerAsync(metadata, "SeekCommand", new AppleChangePlaybackPositionEvent(12));

			Assert.Equal(MPRemoteCommandHandlerStatus.Success, status);
			await WaitUntilAsync(() => Math.Abs(player.CurrentTime.Seconds - 12) < 0.1, "The player did not seek to 12 s.");
		});
	}

	[Fact]
	public async Task SeekCommandWithAnotherKindOfEventFails()
	{
		SetupApple();

		await WithPlayerAsync(async (player, metadata) =>
		{
			var status = await InvokeHandlerAsync(metadata, "SeekCommand", new AppleRemoteCommandEvent());

			Assert.Equal(MPRemoteCommandHandlerStatus.CommandFailed, status);
			Assert.Equal(0.0, player.CurrentTime.Seconds, precision: 1);
		});
	}

	[Fact]
	public async Task SeekCommandWithoutEventFails()
	{
		SetupApple();

		await WithPlayerAsync(async (player, metadata) =>
		{
			var status = await InvokeHandlerAsync(metadata, "SeekCommand", null);

			Assert.Equal(MPRemoteCommandHandlerStatus.CommandFailed, status);
			Assert.Equal(0.0, player.CurrentTime.Seconds, precision: 1);
		});
	}

	[Fact]
	public async Task SeekBackwardCommandGoesBackTenSeconds()
	{
		SetupApple();

		await WithPlayerAsync(async (player, metadata) =>
		{
			await SeekToAsync(player, 20);

			var status = await InvokeHandlerAsync(metadata, "SeekBackwardCommand", new AppleRemoteCommandEvent());

			Assert.Equal(MPRemoteCommandHandlerStatus.Success, status);
			await WaitUntilAsync(() => Math.Abs(player.CurrentTime.Seconds - 10) < 0.1, "The player did not go back to 10 s.");
		});
	}

	[Fact]
	public async Task SeekBackwardCommandStopsAtTheStartOfTheMedia()
	{
		SetupApple();

		await WithPlayerAsync(async (player, metadata) =>
		{
			await SeekToAsync(player, 3);

			var status = await InvokeHandlerAsync(metadata, "SeekBackwardCommand", new AppleRemoteCommandEvent());

			Assert.Equal(MPRemoteCommandHandlerStatus.Success, status);
			await WaitUntilAsync(() => player.CurrentTime.Seconds < 0.1, "The player did not go back to the start.");
		});
	}

	[Fact]
	public async Task SeekBackwardCommandWithoutEventFails()
	{
		SetupApple();

		await WithPlayerAsync(async (player, metadata) =>
		{
			await SeekToAsync(player, 20);

			var status = await InvokeHandlerAsync(metadata, "SeekBackwardCommand", null);

			Assert.Equal(MPRemoteCommandHandlerStatus.CommandFailed, status);
			Assert.Equal(20.0, player.CurrentTime.Seconds, precision: 1);
		});
	}

	[Fact]
	public async Task SeekForwardCommandGoesForwardTenSeconds()
	{
		SetupApple();

		await WithPlayerAsync(async (player, metadata) =>
		{
			await SeekToAsync(player, 10);

			var status = await InvokeHandlerAsync(metadata, "SeekForwardCommand", new AppleRemoteCommandEvent());

			Assert.Equal(MPRemoteCommandHandlerStatus.Success, status);
			await WaitUntilAsync(() => Math.Abs(player.CurrentTime.Seconds - 20) < 0.1, "The player did not go forward to 20 s.");
		});
	}

	[Fact]
	public async Task SeekForwardCommandStopsAtTheEndOfTheMedia()
	{
		SetupApple();

		await WithPlayerAsync(async (player, metadata) =>
		{
			var duration = player.CurrentItem!.Duration.Seconds;
			await SeekToAsync(player, duration - 3);

			var status = await InvokeHandlerAsync(metadata, "SeekForwardCommand", new AppleRemoteCommandEvent());

			Assert.Equal(MPRemoteCommandHandlerStatus.Success, status);
			await WaitUntilAsync(() => Math.Abs(player.CurrentTime.Seconds - duration) < 0.1, "The player did not go to the end of the media.");
		});
	}

	[Fact]
	public async Task SeekForwardCommandWithoutEventFails()
	{
		SetupApple();

		await WithPlayerAsync(async (player, metadata) =>
		{
			await SeekToAsync(player, 10);

			var status = await InvokeHandlerAsync(metadata, "SeekForwardCommand", null);

			Assert.Equal(MPRemoteCommandHandlerStatus.CommandFailed, status);
			Assert.Equal(10.0, player.CurrentTime.Seconds, precision: 1);
		});
	}

	[Fact]
	public async Task ToggleCommandStartsAPausedPlayerAndPausesAPlayingOne()
	{
		SetupApple();

		await WithPlayerAsync(async (player, metadata) =>
		{
			var commandEvent = new AppleRemoteCommandEvent();
			Assert.Equal(0f, player.Rate);

			var started = await InvokeHandlerAsync(metadata, "ToggleCommand", commandEvent);
			var rateAfterStart = player.Rate;
			var paused = await InvokeHandlerAsync(metadata, "ToggleCommand", commandEvent);

			Assert.Equal(MPRemoteCommandHandlerStatus.Success, started);
			Assert.Equal(1f, rateAfterStart);
			Assert.Equal(MPRemoteCommandHandlerStatus.Success, paused);
			Assert.Equal(0f, player.Rate);
			Assert.Equal(1, playCallbackCount);
		});
	}

	[Fact]
	public async Task ToggleCommandWithoutEventFails()
	{
		SetupApple();

		await WithPlayerAsync(async (player, metadata) =>
		{
			var status = await InvokeHandlerAsync(metadata, "ToggleCommand", null);

			Assert.Equal(MPRemoteCommandHandlerStatus.CommandFailed, status);
			Assert.Equal(0f, player.Rate);
		});
	}

	[Fact]
	public async Task SeekCommandKeepsTheFractionOfASecond()
	{
		SetupApple();

		await WithPlayerAsync(async (player, metadata) =>
		{
			var status = await InvokeHandlerAsync(metadata, "SeekCommand", new AppleChangePlaybackPositionEvent(12.5));

			Assert.Equal(MPRemoteCommandHandlerStatus.Success, status);
			await WaitUntilAsync(() => Math.Abs(player.CurrentTime.Seconds - 12.5) < 0.1, "The player did not seek to 12.5 s.");
		});
	}

	[Fact]
	public async Task DisconnectingTheMediaElementRemovesItsRemoteCommandTargets()
	{
		SetupApple();
		Skip.IfNot(AppleRemoteCommands.CanQueryTargets, "This OS version does not tell whether an MPRemoteCommand has targets (MPRemoteCommand.hasTargets).");
		var mediaElement = CreateMediaElement();

		await WithCleanRemoteCommandsAsync(() => HostAsync(mediaElement, async handler =>
		{
			// Loading a source (even none) creates the Metadata of the manager.
			await handler.Manager!.TakeSourceUpdates();
			Assert.All(AppleRemoteCommands.Names, name => Assert.True(AppleRemoteCommands.HasTargets(name), name));

			handler.Disconnect();

			Assert.All(AppleRemoteCommands.Names, name => Assert.False(AppleRemoteCommands.HasTargets(name), name));
		}));
	}

	[Fact]
	public async Task MetadataOfADisconnectedMediaElementIsCollected()
	{
		SetupApple();
		WeakReference metadataReference = null!, playerReference = null!;

		await WithCleanRemoteCommandsAsync(async () =>
		{
			await InvokeOnMainThreadAsync(() =>
			{
				var mediaElement = CreateMediaElement();
				var page = new ContentPage { Content = new VerticalStackLayout { mediaElement } };
				CreateHandler(page);
				var handler = (AppleMediaElementHandler)mediaElement.Handler!;

				// Connecting the handler ran the first (synchronous) source update, which created the Metadata.
				metadataReference = new(GetMetadata(handler.Manager!));
				playerReference = new(handler.Manager!.NativePlayer);

				handler.Disconnect();
				page.Handler?.DisconnectHandler();
				mediaElement.Handler = null;
				page.Handler = null;
				mediaElement.Dispose();
			});

			await AssertionExtensions.WaitForGC(metadataReference, playerReference);
		});
	}

	[Fact]
	public async Task DisconnectingTheMediaElementClearsItsNowPlayingInfo()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();
		mediaElement.MetadataTitle = "Song";
		mediaElement.MetadataArtist = "Artist";

		await HostAsync(mediaElement, async handler =>
		{
			await LoadAsync(mediaElement, handler, ResourceSource(AudioResource));
			Assert.Equal("Song", MPNowPlayingInfoCenter.DefaultCenter.NowPlaying?.Title);

			handler.Disconnect();

			var nowPlaying = MPNowPlayingInfoCenter.DefaultCenter.NowPlaying;
			Assert.True(string.IsNullOrEmpty(nowPlaying?.Title), $"Now Playing title after the disconnect: '{nowPlaying?.Title}'.");
			Assert.True(string.IsNullOrEmpty(nowPlaying?.Artist), $"Now Playing artist after the disconnect: '{nowPlaying?.Artist}'.");
			Assert.True(nowPlaying?.PlaybackDuration is null or 0d, $"Now Playing duration after the disconnect: {nowPlaying?.PlaybackDuration}.");
		});
	}

	[Fact]
	public async Task TogglePlayPauseRemoteCommandStartsAPausedPlayerAndPausesAPlayingOne()
	{
		SetupApple();
		Skip.IfNot(AppleRemoteCommands.CanDispatch, "This OS version has no MPRemoteCommand.newCommandEvent / invokeCommandWithEvent:completion: to deliver a remote command.");

		await WithPlayerAsync(async (player, _) =>
		{
			Assert.Equal(0f, player.Rate);

			await DispatchAsync("togglePlayPause");
			var rateAfterFirstToggle = player.Rate;
			await DispatchAsync("togglePlayPause");

			Assert.Equal(1f, rateAfterFirstToggle);
			Assert.Equal(0f, player.Rate);
		});
	}

	[Fact]
	public async Task DisposingAMetadataLeavesTheRemoteCommandTargetsOfAnotherOne()
	{
		SetupApple();
		Skip.IfNot(AppleRemoteCommands.CanQueryTargets, "This OS version does not tell whether an MPRemoteCommand has targets (MPRemoteCommand.hasTargets).");

		await WithCleanRemoteCommandsAsync(() => InvokeOnMainThreadAsync(() =>
		{
			using var firstPlayer = new AVPlayer();
			using var secondPlayer = new AVPlayer();
			var first = new Metadata(firstPlayer, firstPlayer.Play);
			using var second = new Metadata(secondPlayer, secondPlayer.Play);

			// The command center is shared by every MediaElement of the app.
			first.Dispose();

			Assert.All(AppleRemoteCommands.Names, name => Assert.True(AppleRemoteCommands.HasTargets(name), name));
		}));
	}

	[Fact]
	public async Task DisposingAMetadataLeavesTheNowPlayingInfoOfAnotherOne()
	{
		SetupApple();

		await WithCleanRemoteCommandsAsync(() => InvokeOnMainThreadAsync(() =>
		{
			using var firstPlayer = new AVPlayer();
			using var secondPlayer = new AVPlayer();
			using var first = new Metadata(firstPlayer, firstPlayer.Play);
			using var second = new Metadata(secondPlayer, secondPlayer.Play);
			first.SetMetadata(null, new MediaElement { MetadataTitle = "First" });
			second.SetMetadata(null, new MediaElement { MetadataTitle = "Second" });

			// The Now Playing info is shared by every MediaElement of the app: the second one's media is shown.
			first.Dispose();
			Assert.Equal("Second", MPNowPlayingInfoCenter.DefaultCenter.NowPlaying?.Title);

			second.Dispose();
			AssertDefaultNowPlayingInfo(MPNowPlayingInfoCenter.DefaultCenter.NowPlaying);
		}));
	}

	[Theory]
	[InlineData(false, false)]
	[InlineData(true, false)]
	[InlineData(false, true)]
	public async Task DisconnectingAMediaElementLeavesTheNowPlayingInfoOfAnotherOne(bool disconnectedOneIsPlaying, bool otherOneIsPlaying)
	{
		SetupApple();
		var disconnected = CreateMediaElement();
		disconnected.MetadataTitle = "Disconnected";
		disconnected.ShouldMute = true;
		var other = CreateMediaElement();
		other.MetadataTitle = "Other";
		other.ShouldMute = true;
		var page = new ContentPage { Content = new VerticalStackLayout { disconnected, other } };

		try
		{
			await AttachAndRun(page, async _ =>
			{
				await page.OnLoadedAsync();
				var disconnectedHandler = (AppleMediaElementHandler)disconnected.Handler!;
				await LoadAndPlayAsync(disconnected, disconnectedHandler, disconnectedOneIsPlaying);
				await LoadAndPlayAsync(other, (AppleMediaElementHandler)other.Handler!, otherOneIsPlaying);
				await DrainMainQueueAsync();

				// The Now Playing info is shared by every MediaElement of the app: the one loaded last shows its media.
				Assert.Equal("Other", MPNowPlayingInfoCenter.DefaultCenter.NowPlaying?.Title);

				// Disposing the player pauses it, which raises its time control status observer (even when it is already paused).
				disconnectedHandler.Disconnect();
				await DrainMainQueueAsync();

				Assert.Equal("Other", MPNowPlayingInfoCenter.DefaultCenter.NowPlaying?.Title);
			});
		}
		finally
		{
			await InvokeOnMainThreadAsync(() =>
			{
				disconnected.Handler?.DisconnectHandler();
				other.Handler?.DisconnectHandler();
				page.Handler?.DisconnectHandler();
				disconnected.Dispose();
				other.Dispose();
			});
		}

		static async Task LoadAndPlayAsync(MediaElement mediaElement, AppleMediaElementHandler handler, bool play)
		{
			await LoadAsync(mediaElement, handler, MediaSource.FromResource(longAudioResource));
			if (play)
			{
				mediaElement.Play();
				await WaitUntilAsync(() => mediaElement.CurrentState is MediaElementState.Playing, $"The MediaElement '{mediaElement.MetadataTitle}' did not start playing.");
			}
		}
	}

	[Fact]
	public async Task DisconnectingAMediaElementWhoseStateChangedHandlerSetsASourceLeavesNoRemoteCommandTargets()
	{
		SetupApple();
		Skip.IfNot(AppleRemoteCommands.CanQueryTargets, "This OS version does not tell whether an MPRemoteCommand has targets (MPRemoteCommand.hasTargets).");
		var mediaElement = CreateMediaElement();
		mediaElement.ShouldMute = true;

		await WithCleanRemoteCommandsAsync(() => HostAsync(mediaElement, async handler =>
		{
			var manager = handler.Manager!;
			await LoadAsync(mediaElement, handler, MediaSource.FromResource(longAudioResource));
			mediaElement.Play();
			await WaitUntilAsync(() => mediaElement.CurrentState is MediaElementState.Playing, "The MediaElement did not start playing.");

			// Disposing the player pauses it, which raises StateChanged: a handler that sets a source then updates the source of the
			// player that is being disposed, which creates the Metadata (and its remote command targets) of the new source.
			AVPlayerItem? itemBeforeTheSourceChange = null, itemAfterTheSourceChange = null;
			void SetSource(object? sender, MediaStateChangedEventArgs e)
			{
				mediaElement.StateChanged -= SetSource;
				itemBeforeTheSourceChange = manager.NativePlayerItem;
				mediaElement.Source = ResourceSource(AudioResource);
				itemAfterTheSourceChange = manager.NativePlayerItem;
			}

			mediaElement.StateChanged += SetSource;
			handler.Disconnect();

			Assert.NotNull(itemAfterTheSourceChange);
			Assert.NotSame(itemBeforeTheSourceChange, itemAfterTheSourceChange);
			Assert.All(AppleRemoteCommands.Names, name => Assert.False(AppleRemoteCommands.HasTargets(name), name));
		}));
	}

	async Task DispatchAsync(string commandName)
	{
		var before = await InvokeOnMainThreadAsync(() => AppleRemoteCommands.Dispatch(AppleRemoteCommands.Get(commandName)));
		await WaitUntilAsync(() => AppleRemoteCommands.Completions > before, $"MediaPlayer did not report that the {commandName} command was handled.", timeout: 3000);
	}

	[Fact]
	public async Task PlayRemoteCommandReachesThePlayHandler()
	{
		SetupApple();
		Skip.IfNot(AppleRemoteCommands.CanDispatch, "This OS version has no MPRemoteCommand.newCommandEvent / invokeCommandWithEvent:completion: to deliver a remote command.");

		await WithPlayerAsync(async (player, _) =>
		{
			Assert.Equal(0f, player.Rate);

			await DispatchAsync("play");

			Assert.Equal(1, playCallbackCount);
			Assert.Equal(1f, player.Rate);
		});
	}

	[Fact]
	public async Task PauseRemoteCommandReachesThePauseHandler()
	{
		SetupApple();
		Skip.IfNot(AppleRemoteCommands.CanDispatch, "This OS version has no MPRemoteCommand.newCommandEvent / invokeCommandWithEvent:completion: to deliver a remote command.");

		await WithPlayerAsync(async (player, _) =>
		{
			await InvokeOnMainThreadAsync(player.Play);
			Assert.Equal(1f, player.Rate);

			await DispatchAsync("pause");

			Assert.Equal(0f, player.Rate);
		});
	}

	[Theory]
	[InlineData("changePlaybackPosition", 20, 0)]
	[InlineData("seekBackward", 20, 10)]
	[InlineData("seekForward", 10, 20)]
	public async Task SeekRemoteCommandReachesItsHandler(string commandName, double startInSeconds, double expectedInSeconds)
	{
		SetupApple();
		Skip.IfNot(AppleRemoteCommands.CanDispatch, "This OS version has no MPRemoteCommand.newCommandEvent / invokeCommandWithEvent:completion: to deliver a remote command.");

		await WithPlayerAsync(async (player, _) =>
		{
			await SeekToAsync(player, startInSeconds);

			// A new event of the change-playback-position command asks for position 0.
			await DispatchAsync(commandName);

			await WaitUntilAsync(() => Math.Abs(player.CurrentTime.Seconds - expectedInSeconds) < 0.1, $"The player did not move from {startInSeconds} s to {expectedInSeconds} s.");
			Assert.Equal(0f, player.Rate);
		});
	}
}