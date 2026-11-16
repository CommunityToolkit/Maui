#pragma warning disable CS0618 // These tests pin that the obsolete MetadataArtworkUrl keeps working when MetadataArtworkSource is not set
using System.Reflection;
using AVFoundation;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;
using CoreGraphics;
using Foundation;
using MediaPlayer;
using UIKit;

namespace CommunityToolkit.Maui.DeviceTests;

// Files live in FileSystem.CacheDirectory, so no test asks for a permission.
[Category(TestCategory.MediaElementApple)]
[Collection(TestCollections.DisableParallelization)]
public class MediaManagerArtworkSourceAppleTests : AppleMediaElementTestBase
{
	const string posterResource = "avplayer/poster.png";

	static readonly CGSize posterSize = new(40, 30);

	static readonly MethodInfo loadArtworkImage = typeof(Metadata).GetMethod("LoadArtworkImage", BindingFlags.NonPublic | BindingFlags.Static)!;

	static UIImage DefaultImage => (UIImage)typeof(Metadata).GetField("defaultUIImage", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!;

	static UIView Overlay(AppleMediaElementHandler handler) => handler.Manager!.NativePlayerViewController!.ContentOverlayView!;

	static string CopyToCache(string fileName, byte[] content)
	{
		var path = Path.Combine(FileSystem.CacheDirectory, $"{Guid.NewGuid():N}_{fileName}");
		File.WriteAllBytes(path, content);
		return path;
	}

	static string CopyPosterToCache() => CopyToCache("poster.png", File.ReadAllBytes(BundlePath("poster", "png")));

	static void AssertIsTheDefaultImage(UIImage? image)
	{
		// The artwork handler hands back the zero-sized default image, which the system may return as nothing
		Assert.True(image is null || image.Size == CGSize.Empty, $"Expected the default image but got {image?.Size}.");
	}

	static Task<UIImage?> ReadNowPlayingArtworkAsync(MediaElement mediaElement) =>
		InvokeOnMainThreadAsync(() =>
		{
			using var player = new AVPlayer();
			using var metadata = new Metadata(player, player.Play);

			metadata.SetMetadata(null, mediaElement);

			return MPNowPlayingInfoCenter.DefaultCenter.NowPlaying!.Artwork!.ImageWithSize(new CGSize(320, 240));
		});

	static string CreateDataUrl(byte[] image) => $"data:image/png;base64,{Convert.ToBase64String(image)}";

	// Noise does not compress, so the PNG (and its base64 form) is long enough to pass the 65,519 characters that mark a long data URL
	static byte[] CreateNoisePng(int side)
	{
		var pixels = new byte[side * side * 4];
		new Random(1).NextBytes(pixels);
		using var colorSpace = CGColorSpace.CreateDeviceRGB();
		using var context = new CGBitmapContext(pixels, side, side, 8, side * 4, colorSpace, CGImageAlphaInfo.NoneSkipLast);
		using var cgImage = context.ToImage()!;
		using var image = UIImage.FromImage(cgImage);
		return image.AsPNG()!.ToArray();
	}

	static AppleLoopbackHttpServer CreateServerWithPoster()
	{
		var server = new AppleLoopbackHttpServer();
		server.Add("/art.png", File.ReadAllBytes(BundlePath("poster", "png")), "image/png");
		return server;
	}

	[Fact]
	public async Task NowPlayingArtworkIsLoadedFromAUriArtworkSource()
	{
		SetupApple();
		using var server = CreateServerWithPoster();
		var mediaElement = new MediaElement { MetadataArtworkSource = MediaSource.FromUri(server.GetUri("/art.png")) };

		var image = await ReadNowPlayingArtworkAsync(mediaElement);

		Assert.Equal(posterSize, image!.Size);
		Assert.Contains(server.Requests, request => request.Method is "GET" && request.Path == "/art.png");
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task NowPlayingArtworkIsLoadedFromAFileArtworkSource(bool givenAsFileUri)
	{
		SetupApple();
		var path = CopyPosterToCache();
		try
		{
			var mediaElement = new MediaElement { MetadataArtworkSource = MediaSource.FromFile(givenAsFileUri ? new Uri(path).AbsoluteUri : path) };

			var image = await ReadNowPlayingArtworkAsync(mediaElement);

			Assert.Equal(posterSize, image!.Size);
		}
		finally
		{
			File.Delete(path);
		}
	}

	[Fact]
	public async Task NowPlayingArtworkIsLoadedFromAResourceArtworkSource()
	{
		SetupApple();
		var mediaElement = new MediaElement { MetadataArtworkSource = MediaSource.FromResource(posterResource) };

		var image = await ReadNowPlayingArtworkAsync(mediaElement);

		Assert.Equal(posterSize, image!.Size);
	}

	public enum UnusableArtwork
	{
		MissingFile,
		MissingResource,
		UndecodableData
	}

	[Theory]
	[InlineData(UnusableArtwork.MissingFile)]
	[InlineData(UnusableArtwork.MissingResource)]
	[InlineData(UnusableArtwork.UndecodableData)]
	public async Task NowPlayingArtworkFallsBackToTheDefaultImageForAMissingFileResourceOrUndecodableData(UnusableArtwork unusableArtwork)
	{
		SetupApple();
		var undecodablePath = CopyToCache("not_an_image.png", "This is not an image."u8.ToArray());
		try
		{
			MediaSource artworkSource = unusableArtwork switch
			{
				UnusableArtwork.MissingFile => MediaSource.FromFile(Path.Combine(FileSystem.CacheDirectory, $"missing_{Guid.NewGuid():N}.png")),
				UnusableArtwork.MissingResource => MediaSource.FromResource("avplayer/does_not_exist.png"),
				_ => MediaSource.FromFile(undecodablePath)
			};
			var mediaElement = new MediaElement { MetadataArtworkSource = artworkSource };

			var image = await ReadNowPlayingArtworkAsync(mediaElement);
			var loaded = await InvokeOnMainThreadAsync(() => (UIImage)loadArtworkImage.Invoke(null, [Metadata.GetArtworkUrl(artworkSource)])!);

			AssertIsTheDefaultImage(image);
			Assert.Same(DefaultImage, loaded);
		}
		finally
		{
			File.Delete(undecodablePath);
		}
	}

	[Fact]
	public async Task NowPlayingArtworkOfAStreamArtworkSourceIsTheDefaultImage()
	{
		SetupApple();
		using var stream = new MemoryStream(File.ReadAllBytes(BundlePath("poster", "png")));
		var mediaElement = new MediaElement { MetadataArtworkSource = MediaSource.FromStream(stream) };

		var image = await ReadNowPlayingArtworkAsync(mediaElement);

		AssertIsTheDefaultImage(image);
	}

	[Fact]
	public async Task ArtworkSourceReplacesMetadataArtworkUrlInNowPlaying()
	{
		SetupApple();
		using var server = CreateServerWithPoster();
		var path = CopyPosterToCache();
		try
		{
			var mediaElement = new MediaElement
			{
				MetadataArtworkUrl = server.GetUri("/art.png").AbsoluteUri,
				MetadataArtworkSource = MediaSource.FromFile(path)
			};

			var image = await ReadNowPlayingArtworkAsync(mediaElement);

			Assert.Equal(posterSize, image!.Size);
			Assert.DoesNotContain(server.Requests, request => request.Path == "/art.png");

			// Without a source the URL is read through the same rules, so a file URL loads too
			var legacyFileUrl = new MediaElement { MetadataArtworkUrl = new Uri(path).AbsoluteUri };
			Assert.Equal(posterSize, (await ReadNowPlayingArtworkAsync(legacyFileUrl))!.Size);

			var legacyHttpUrl = new MediaElement { MetadataArtworkUrl = server.GetUri("/art.png").AbsoluteUri };
			Assert.Equal(posterSize, (await ReadNowPlayingArtworkAsync(legacyHttpUrl))!.Size);
			Assert.Contains(server.Requests, request => request.Path == "/art.png");
		}
		finally
		{
			File.Delete(path);
		}
	}

	[Fact]
	public async Task GetArtworkUrlResolvesEachArtworkSourceType()
	{
		SetupApple();
		var path = CopyPosterToCache();
		try
		{
			await InvokeOnMainThreadAsync(() =>
			{
				var fileUrl = Metadata.GetArtworkUrl(MediaSource.FromFile(path));
				Assert.True(fileUrl!.IsFileUrl);
				Assert.Equal(path, fileUrl.Path);

				var fileUriUrl = Metadata.GetArtworkUrl(MediaSource.FromFile(new Uri(path).AbsoluteUri));
				Assert.True(fileUriUrl!.IsFileUrl);
				Assert.Equal(path, fileUriUrl.Path);

				var uriFileUrl = Metadata.GetArtworkUrl(MediaSource.FromUri(new Uri(path)));
				Assert.True(uriFileUrl!.IsFileUrl);
				Assert.Equal(path, uriFileUrl.Path);

				var httpUrl = Metadata.GetArtworkUrl(MediaSource.FromUri(new Uri("http://127.0.0.1:1/art.png")));
				Assert.Equal("http://127.0.0.1:1/art.png", httpUrl!.AbsoluteString);

				var resourceUrl = Metadata.GetArtworkUrl(MediaSource.FromResource(posterResource));
				Assert.True(File.Exists(resourceUrl!.Path), $"{resourceUrl.Path} is not in the app bundle.");
				Assert.Equal(BundlePath("poster", "png"), resourceUrl.Path);

				Assert.Null(Metadata.GetArtworkUrl(MediaSource.FromFile(string.Empty)));
				Assert.Null(Metadata.GetArtworkUrl(MediaSource.FromFile("   ")));
				Assert.Null(Metadata.GetArtworkUrl(MediaSource.FromResource("avplayer/poster")));
				Assert.Null(Metadata.GetArtworkUrl(MediaSource.FromUri(new Uri("ftp://127.0.0.1/art.png"))));
				Assert.Null(Metadata.GetArtworkUrl(MediaSource.FromStream(new MemoryStream())));
				Assert.Null(Metadata.GetArtworkUrl(null));
			});
		}
		finally
		{
			File.Delete(path);
		}
	}

	[Fact]
	public async Task ResolveArtworkUrlKeepsThe17RulesForEachMetadataArtworkUrlForm()
	{
		SetupApple();
		var path = CopyPosterToCache();
		var posterDataUrl = CreateDataUrl(File.ReadAllBytes(BundlePath("poster", "png")));
		var longDataUrl = $"data:image/png;base64,{new string('A', 70_000)}";
		const string unparseableUrl = "http://[not a host/art.png";
		try
		{
			await InvokeOnMainThreadAsync(() =>
			{
				static NSUrl? Resolve(string? url) => Metadata.ResolveArtworkUrl(new MediaElement { MetadataArtworkUrl = url! });

				const string httpUrl = "http://127.0.0.1:1/Art.png?size=2";
				Assert.Equal(new Uri(httpUrl).AbsoluteUri, Resolve(httpUrl)!.AbsoluteString);
				Assert.Equal("http", Resolve("HTTP://127.0.0.1:1/art.png")!.Scheme);

				var fileUrl = Resolve(new Uri(path).AbsoluteUri)!;
				Assert.True(fileUrl.IsFileUrl);
				Assert.Equal(path, fileUrl.Path);

				var absolutePathUrl = Resolve(path)!;
				Assert.True(absolutePathUrl.IsFileUrl);
				Assert.Equal(path, absolutePathUrl.Path);

				Assert.Equal(
					NSBundle.MainBundle.GetUrlForResource("poster", "png", "avplayer")!.AbsoluteString,
					Resolve(posterResource)!.AbsoluteString);

				Assert.Equal(posterDataUrl, Resolve(posterDataUrl)!.AbsoluteString);
				Assert.Equal(longDataUrl, Resolve(longDataUrl)!.AbsoluteString);
				Assert.Equal("ftp://127.0.0.1/art.png", Resolve("ftp://127.0.0.1/art.png")!.AbsoluteString);
				Assert.Equal(NSUrl.FromString(unparseableUrl)?.AbsoluteString, Resolve(unparseableUrl)?.AbsoluteString);

				Assert.Null(Resolve(null));
				Assert.Null(Resolve(string.Empty));
				Assert.Null(Resolve("   "));

				using var stream = new MemoryStream();
				var sourceWins = new MediaElement { MetadataArtworkUrl = new Uri(path).AbsoluteUri, MetadataArtworkSource = MediaSource.FromStream(stream) };
				Assert.Null(Metadata.ResolveArtworkUrl(sourceWins));
			});
		}
		finally
		{
			File.Delete(path);
		}
	}

	public enum MetadataArtworkUrlForm
	{
		FileUrl,
		AbsolutePath,
		AppPackagePath
	}

	[Theory]
	[InlineData(MetadataArtworkUrlForm.FileUrl)]
	[InlineData(MetadataArtworkUrlForm.AbsolutePath)]
	[InlineData(MetadataArtworkUrlForm.AppPackagePath)]
	public async Task NowPlayingArtworkIsLoadedFromAMetadataArtworkUrlFilePathOrResource(MetadataArtworkUrlForm form)
	{
		SetupApple();
		var path = CopyPosterToCache();
		try
		{
			var url = form switch
			{
				MetadataArtworkUrlForm.FileUrl => new Uri(path).AbsoluteUri,
				MetadataArtworkUrlForm.AbsolutePath => path,
				_ => posterResource
			};
			var mediaElement = new MediaElement { MetadataArtworkUrl = url };

			var image = await ReadNowPlayingArtworkAsync(mediaElement);

			Assert.Equal(posterSize, image!.Size);
		}
		finally
		{
			File.Delete(path);
		}
	}

	[Fact]
	public async Task NowPlayingArtworkIsLoadedFromAMetadataArtworkUrlDataUrl()
	{
		SetupApple();
		var mediaElement = new MediaElement { MetadataArtworkUrl = CreateDataUrl(File.ReadAllBytes(BundlePath("poster", "png"))) };

		var image = await ReadNowPlayingArtworkAsync(mediaElement);

		Assert.Equal(posterSize, image!.Size);
	}

	public enum PosterUrlForm
	{
		AbsolutePath,
		AppPackagePath,
		DataUrl,
		LongDataUrl,
		SingleSlashFileUrl
	}

	// 17.0.0 showed the poster of any URL that NSData reads, so each form that worked then has to keep working
	[Theory]
	[InlineData(PosterUrlForm.AbsolutePath)]
	[InlineData(PosterUrlForm.AppPackagePath)]
	[InlineData(PosterUrlForm.DataUrl)]
	[InlineData(PosterUrlForm.LongDataUrl)]
	[InlineData(PosterUrlForm.SingleSlashFileUrl)]
	public async Task AudioShowsAPosterFromEachMetadataArtworkUrlForm(PosterUrlForm form)
	{
		SetupApple();
		var path = CopyPosterToCache();
		var noisePng = CreateNoisePng(200);
		var expectedSize = form is PosterUrlForm.LongDataUrl ? new CGSize(200, 200) : posterSize;
		try
		{
			var url = form switch
			{
				PosterUrlForm.AbsolutePath => path,
				PosterUrlForm.AppPackagePath => posterResource,
				PosterUrlForm.DataUrl => CreateDataUrl(File.ReadAllBytes(BundlePath("poster", "png"))),
				PosterUrlForm.LongDataUrl => CreateDataUrl(noisePng),
				// .NET reads "file:/path" as a relative path, so only the raw string handed to NSURL finds the file
				_ => $"file:{BundlePath("poster", "png")}"
			};
			var mediaElement = CreateMediaElement();
			mediaElement.MetadataArtworkUrl = url;

			await HostAsync(mediaElement, async handler =>
			{
				await LoadAsync(mediaElement, handler, ResourceSource(AudioResource));

				Assert.True(form is not PosterUrlForm.LongDataUrl || url.Length > 65_519, $"The data URL has {url.Length} characters.");
				var poster = Assert.IsType<UIImageView>(Assert.Single(Overlay(handler).Subviews));
				Assert.Equal(expectedSize, poster.Image!.Size);
			});
		}
		finally
		{
			File.Delete(path);
		}
	}

	[Fact]
	public async Task ClearingTheArtworkSourceFallsBackToMetadataArtworkUrlInNowPlaying()
	{
		SetupApple();
		using var server = CreateServerWithPoster();
		server.Add("/source.png", File.ReadAllBytes(BundlePath("poster", "png")), "image/png");
		server.Add("/legacy.png", File.ReadAllBytes(BundlePath("poster", "png")), "image/png");
		var mediaElement = new MediaElement
		{
			MetadataArtworkUrl = server.GetUri("/legacy.png").AbsoluteUri,
			MetadataArtworkSource = MediaSource.FromUri(server.GetUri("/source.png"))
		};
		mediaElement.MetadataArtworkSource = null;

		var image = await ReadNowPlayingArtworkAsync(mediaElement);

		Assert.Equal(posterSize, image!.Size);
		Assert.Contains(server.Requests, request => request.Path == "/legacy.png");
		Assert.DoesNotContain(server.Requests, request => request.Path == "/source.png");
	}

	[Fact]
	public async Task AudioWithAFileArtworkSourceShowsAPoster()
	{
		SetupApple();
		var path = CopyPosterToCache();
		var mediaElement = CreateMediaElement();
		mediaElement.MetadataArtworkSource = MediaSource.FromFile(path);
		try
		{
			await HostAsync(mediaElement, async handler =>
			{
				await LoadAsync(mediaElement, handler, ResourceSource(AudioResource));

				var poster = Assert.IsType<UIImageView>(Assert.Single(Overlay(handler).Subviews));
				Assert.Equal(posterSize, poster.Image!.Size);
			});
		}
		finally
		{
			File.Delete(path);
		}
	}

	[Fact]
	public async Task AudioWithAResourceArtworkSourceShowsAPoster()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();
		mediaElement.MetadataArtworkSource = MediaSource.FromResource(posterResource);

		await HostAsync(mediaElement, async handler =>
		{
			await LoadAsync(mediaElement, handler, ResourceSource(AudioResource));

			var poster = Assert.IsType<UIImageView>(Assert.Single(Overlay(handler).Subviews));
			Assert.Equal(posterSize, poster.Image!.Size);
		});
	}

	[Fact]
	public async Task AudioWithAUriArtworkSourceShowsAPoster()
	{
		SetupApple();
		using var server = CreateServerWithPoster();
		var mediaElement = CreateMediaElement();
		mediaElement.MetadataArtworkSource = MediaSource.FromUri(server.GetUri("/art.png"));

		await HostAsync(mediaElement, async handler =>
		{
			await LoadAsync(mediaElement, handler, ResourceSource(AudioResource));

			var poster = Assert.IsType<UIImageView>(Assert.Single(Overlay(handler).Subviews));
			Assert.Equal(posterSize, poster.Image!.Size);
			Assert.Contains(server.Requests, request => request.Method is "GET" && request.Path == "/art.png");
		});
	}

	[Fact]
	public async Task AudioWithAMissingArtworkSourceShowsNoPoster()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();
		mediaElement.MetadataArtworkSource = MediaSource.FromFile(Path.Combine(FileSystem.CacheDirectory, $"missing_{Guid.NewGuid():N}.png"));

		await HostAsync(mediaElement, async handler =>
		{
			var exception = await SetSourceAsync(mediaElement, handler, ResourceSource(AudioResource));
			await DrainMainQueueAsync();

			Assert.Null(exception);
			Assert.NotNull(handler.Manager!.NativePlayerItem);
			Assert.Empty(Overlay(handler).Subviews);
			Assert.DoesNotContain(Logs.Entries, entry => entry.Level is Microsoft.Extensions.Logging.LogLevel.Error);
		});
	}

	[Fact]
	public async Task VideoWithAnArtworkSourceShowsNoPoster()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();
		mediaElement.MetadataArtworkSource = MediaSource.FromResource(posterResource);

		await HostAsync(mediaElement, async handler =>
		{
			await LoadAsync(mediaElement, handler, ResourceSource(VideoResource));

			Assert.Empty(Overlay(handler).Subviews);
		});
	}

	[Fact]
	public async Task QuickSourceChangesWithAnArtworkSourceLeaveNoStalePoster()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();
		mediaElement.MetadataArtworkSource = MediaSource.FromResource(posterResource);

		await HostAsync(mediaElement, async handler =>
		{
			await handler.Manager!.TakeSourceUpdates();

			// Two audio sources in one main-thread turn: the first update is still loading the tracks when the second one starts.
			var opened = WhenOpenedOrFailedAsync(mediaElement);
			mediaElement.Source = ResourceSource(AudioResource);
			mediaElement.Source = ResourceSource(RootAudioResource);
			await handler.Manager.TakeSourceUpdates();
			await WaitAsync(opened, "MediaOpened was not raised.");
			await DrainMainQueueAsync();

			Assert.Single(Overlay(handler).Subviews);

			await LoadAsync(mediaElement, handler, ResourceSource(VideoResource));

			Assert.Empty(Overlay(handler).Subviews);
		});
	}
}
