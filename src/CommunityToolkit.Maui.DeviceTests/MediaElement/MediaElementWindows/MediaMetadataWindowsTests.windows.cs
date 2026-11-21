#pragma warning disable CS0618 // These tests pin the obsolete MetadataArtworkUrl, which keeps working
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Primitives;
using CommunityToolkit.Maui.DeviceTests.Runners;
using CommunityToolkit.Maui.Views;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Media;
using Windows.Media.Control;
using Windows.Media.Playback;

namespace CommunityToolkit.Maui.DeviceTests;

// The artwork is served by a loopback server because RandomAccessStreamReference.CreateFromUri accepts http, https, ms-appx and ms-appdata only.
[Category(TestCategory.MediaMetadataWindows)]
[Collection(TestCollections.DisableParallelization)]
public class MediaMetadataWindowsTests : WinMediaTestBase
{
	const string title = "A Title";
	const string artist = "An Artist";

	static Task UpdateMetadataAsync(WinMediaManagerFixture fixture) =>
		InvokeOnMainThreadAsync(async () => await (ValueTask)WinMediaReflection.Invoke(fixture.Manager, "UpdateMetadata")!);

	static Metadata CreateMetadata(WinMediaManagerFixture fixture) =>
		new(fixture.Player!.SystemMediaTransportControls, fixture.View, TestDispatcher.Current);

	static Uri ArtworkUri(WinMediaHttpServer server) => server.UriFor(WinMediaAssets.ArtworkServerPath);

	static async Task AssertThumbnailIsTheArtworkAsync(WinMediaManagerFixture fixture, WinMediaHttpServer server)
	{
		await AssertThumbnailIsAsync(fixture, WinMediaAssets.ReadArtworkBytes());
		Assert.Contains(server.Requests, request => request.Method is "GET" && request.Path == "/" + WinMediaAssets.ArtworkServerPath);
	}

	static async Task AssertThumbnailIsAsync(WinMediaManagerFixture fixture, byte[] expectedBytes)
	{
		byte[] bytes;
		try
		{
			bytes = await WinMediaAssets.ReadThumbnailAsync(() => ReadThumbnailOnceAsync(fixture));
		}
		catch (Exception ex) when (WinMediaAssets.IsOutOfProcessServerGone(ex))
		{
			// That process can stay gone for the rest of the app run (CI runs 38089989724 and 38096555799): what fails is the machine's media
			// overlay, not the thumbnail the toolkit published. A FileNotFoundException that persists still fails, as it would for a wrong address.
			throw new SkipException($"The system process that serves this machine's media overlay thumbnails went away (HRESULT 0x{ex.HResult:X8}), so the thumbnail the toolkit published cannot be read back.");
		}

		Assert.Equal(expectedBytes, bytes);

		static async Task<byte[]> ReadThumbnailOnceAsync(WinMediaManagerFixture fixture)
		{
			var thumbnail = await InvokeOnMainThreadAsync(() => fixture.Player!.SystemMediaTransportControls.DisplayUpdater.Thumbnail);
			Assert.NotNull(thumbnail);
			return await WinMediaAssets.ReadThumbnailOnceAsync(thumbnail);
		}
	}

	[Fact]
	public async Task SetMetadataPublishesMusicWithBothTextsToTheDisplayUpdater()
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();
		await RequireDisplayUpdaterAsync();
		var fixture = await CreateManagerAsync(view =>
		{
			view.MetadataTitle = title;
			view.MetadataArtist = artist;
		});

		await InvokeOnMainThreadAsync(() =>
		{
			CreateMetadata(fixture).SetMetadata(fixture.View);

			var updater = fixture.Player!.SystemMediaTransportControls.DisplayUpdater;
			Assert.Equal(MediaPlaybackType.Music, updater.Type);
			// Which property holds which text is the next test. Both sides are sorted the same way, so the comparison holds for either assignment
			// and for no other pair of texts.
			Assert.Equal(new[] { title, artist }.Order(StringComparer.Ordinal), new[] { updater.MusicProperties.Title, updater.MusicProperties.Artist }.Order(StringComparer.Ordinal));
		});
	}

	[Fact]
	public async Task SetMetadataMapsTheTitleToTheTitleAndTheArtistToTheArtist()
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();
		await RequireDisplayUpdaterAsync();
		var fixture = await CreateManagerAsync(view =>
		{
			view.MetadataTitle = title;
			view.MetadataArtist = artist;
		});

		await InvokeOnMainThreadAsync(() =>
		{
			CreateMetadata(fixture).SetMetadata(fixture.View);

			var music = fixture.Player!.SystemMediaTransportControls.DisplayUpdater.MusicProperties;
			Assert.Equal(title, music.Title);
			Assert.Equal(artist, music.Artist);
		});
	}

	[Fact]
	public async Task SetMetadataWithAnAbsoluteArtworkUrlSetsTheThumbnail()
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();
		await RequireDisplayUpdaterAsync();
		await using var server = WinMediaAssets.StartArtworkServer();
		var fixture = await CreateManagerAsync(view => view.MetadataArtworkUrl = ArtworkUri(server).AbsoluteUri);

		await InvokeOnMainThreadAsync(() => CreateMetadata(fixture).SetMetadata(fixture.View));

		await AssertThumbnailIsTheArtworkAsync(fixture, server);
	}

	[Fact]
	public async Task SetMetadataWithoutArtworkHasNoThumbnail()
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();
		await RequireDisplayUpdaterAsync();
		var fixture = await CreateManagerAsync();

		await InvokeOnMainThreadAsync(() =>
		{
			CreateMetadata(fixture).SetMetadata(fixture.View);

			Assert.Null(fixture.Player!.SystemMediaTransportControls.DisplayUpdater.Thumbnail);
		});
	}

	// RandomAccessStreamReference.CreateFromUri accepts http, https, ms-appx and ms-appdata only, and the Android MediaElement takes a file path as
	// the artwork. SetMetadata runs inside the async void MediaOpened handler, where an exception ends the app.
	[Theory]
	[InlineData(@"C:\Users\Public\Pictures\cover.png")]
	[InlineData("file:///C:/Users/Public/Pictures/cover.png")]
	[InlineData("ftp://localhost/cover.png")]
	public async Task SetMetadataWithAnArtworkUrlOfAnUnsupportedSchemeHasNoThumbnail(string artworkUrl)
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();
		await RequireDisplayUpdaterAsync();
		var fixture = await CreateManagerAsync(view =>
		{
			view.MetadataTitle = title;
			view.MetadataArtworkUrl = artworkUrl;
		});

		await InvokeOnMainThreadAsync(() =>
		{
			var exception = Record.Exception(() => CreateMetadata(fixture).SetMetadata(fixture.View));

			Assert.Null(exception);
			var updater = fixture.Player!.SystemMediaTransportControls.DisplayUpdater;
			Assert.Null(updater.Thumbnail);
			Assert.Equal(title, updater.MusicProperties.Title);
		});
	}

	// The constructor only stores the MediaElement (it dereferences the controls, never the element), so a Metadata built with a null element
	// reaches the element guards of SetMetadata and of the media-key handler. The manager always passes its own element; this is the only way in.
	[Fact]
	public async Task MetadataWithoutAMediaElementPublishesNothingAndIgnoresTheMediaKeys()
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();
		await RequireDisplayUpdaterAsync();
		var fixture = await CreateManagerAsync(view =>
		{
			view.MetadataTitle = title;
			view.MetadataArtist = artist;
		});

		await InvokeOnMainThreadAsync(() =>
		{
			var controls = fixture.Player!.SystemMediaTransportControls;
			Assert.Equal(MediaPlaybackType.Unknown, controls.DisplayUpdater.Type);
			var metadata = new Metadata(controls, null!, TestDispatcher.Current);

			// The element passed to SetMetadata has texts to publish; the guard on the stored element must stop it first.
			metadata.SetMetadata(fixture.View);

			Assert.Equal(MediaPlaybackType.Unknown, controls.DisplayUpdater.Type);
			Assert.Null(controls.DisplayUpdater.Thumbnail);
			Assert.False(controls.IsEnabled);

			// The handler returns before it reads its arguments: without the guard, args.Button would throw NullReferenceException.
			Assert.Null(Record.Exception(() => WinMediaReflection.Invoke(metadata, "OnSystemMediaControlsButtonPressed", controls, null)));
		});
	}

	[Theory]
	[InlineData("images/cover.png")]
	[InlineData("not a uri at all")]
	public async Task UpdateMetadataWithAnUnusableArtworkUrlIsIgnoredWithoutThrowing(string artworkUrl)
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();
		var fixture = await CreateManagerAsync(view => view.MetadataArtworkUrl = artworkUrl);

		var exception = await Record.ExceptionAsync(() => UpdateMetadataAsync(fixture));

		Assert.Null(exception);
	}

	[Fact]
	public async Task UpdateMetadataPublishesToTheDisplayAndSetsThePosterOnTheUiThread()
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();
		await RequireDisplayUpdaterAsync();
		await using var server = WinMediaAssets.StartArtworkServer();
		var fixture = await CreateManagerAsync(view =>
		{
			view.MetadataTitle = title;
			view.MetadataArtist = artist;
			view.MetadataArtworkUrl = ArtworkUri(server).AbsoluteUri;
		}, recordDispatches: true);

		await UpdateMetadataAsync(fixture);

		Assert.Equal(0, fixture.Dispatcher!.DispatchCount);
		var metadata = WinMediaReflection.GetField<object?>(fixture.Manager, "metadata");
		Assert.NotNull(metadata);
		Assert.Equal(MediaPlaybackType.Music, fixture.Player!.SystemMediaTransportControls.DisplayUpdater.Type);
		await AssertThumbnailIsTheArtworkAsync(fixture, server);
		await InvokeOnMainThreadAsync(() =>
		{
			var poster = Assert.IsType<BitmapImage>(fixture.Element!.PosterSource);
			Assert.Equal(ArtworkUri(server).AbsoluteUri, poster.UriSource.AbsoluteUri, ignoreCase: true);
		});

		await UpdateMetadataAsync(fixture);
		Assert.Same(metadata, WinMediaReflection.GetField<object?>(fixture.Manager, "metadata"));
	}

	[Fact]
	public async Task UpdateMetadataThatMustBeDispatchedQueuesThePosterUpdate()
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();

		await using var server = WinMediaAssets.StartArtworkServer();

		var fixture = await CreateManagerAsync(view => view.MetadataArtworkUrl = ArtworkUri(server).AbsoluteUri, dispatchRequired: true);

		await UpdateMetadataAsync(fixture);

		Assert.Equal(1, fixture.Dispatcher!.DispatchCount);
		await InvokeOnMainThreadAsync(() =>
		{
			var poster = Assert.IsType<BitmapImage>(fixture.Element!.PosterSource);
			Assert.Equal(ArtworkUri(server).AbsoluteUri, poster.UriSource.AbsoluteUri, ignoreCase: true);
		});
	}

	[Fact]
	public async Task UpdateMetadataFromABackgroundThreadDispatchesThePosterUpdate()
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();
		await using var server = WinMediaAssets.StartArtworkServer();
		var fixture = await CreateManagerAsync(view => view.MetadataArtworkUrl = ArtworkUri(server).AbsoluteUri);

		// BitmapImage is a XAML object, so only the dispatched arm can create it.
		await Task.Run(async () => await (ValueTask)WinMediaReflection.Invoke(fixture.Manager, "UpdateMetadata")!);

		await WaitUntilOnUiAsync(() => fixture.Element!.PosterSource is BitmapImage { UriSource: not null }, "The poster was never set from the background-thread UpdateMetadata.");
		await InvokeOnMainThreadAsync(() => Assert.Equal(ArtworkUri(server).AbsoluteUri, ((BitmapImage)fixture.Element!.PosterSource).UriSource.AbsoluteUri, ignoreCase: true));
	}

	[Fact]
	public async Task UpdateMetadataWithoutArtworkLeavesThePoster()
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();
		await RequireDisplayUpdaterAsync();
		var fixture = await CreateManagerAsync(view =>
		{
			view.MetadataTitle = title;
			view.MetadataArtist = artist;
		});

		await UpdateMetadataAsync(fixture);

		await InvokeOnMainThreadAsync(() => Assert.Null(fixture.Element!.PosterSource));
		Assert.Equal(MediaPlaybackType.Music, fixture.Player!.SystemMediaTransportControls.DisplayUpdater.Type);
	}

	[Fact]
	public async Task UpdateMetadataWithoutPlatformViewDoesNothing()
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();
		var fixture = await CreateManagerAsync(view => view.MetadataTitle = title, createPlatformView: false);

		await UpdateMetadataAsync(fixture);

		Assert.Null(WinMediaReflection.GetField<object?>(fixture.Manager, "metadata"));
	}

	// The native MediaOpened arrives on a worker thread, so its handler can still be running when the MediaElement's handler disconnects and
	// disposes the manager, and reach UpdateMetadata after Dispose released the Metadata helper. A new helper would subscribe to the system media
	// controls of the disposed player again, and they would keep the MediaElement reachable.
	[Fact]
	public async Task UpdateMetadataAfterDisposeCreatesNoNewMetadataHelper()
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();
		var fixture = await CreateManagerAsync(view => view.MetadataTitle = title);
		await UpdateMetadataAsync(fixture);
		Assert.NotNull(WinMediaReflection.GetField<object?>(fixture.Manager, "metadata"));

		await InvokeOnMainThreadAsync(() => fixture.Manager.Dispose());
		Assert.Null(WinMediaReflection.GetField<object?>(fixture.Manager, "metadata"));

		await UpdateMetadataAsync(fixture);

		Assert.Null(WinMediaReflection.GetField<object?>(fixture.Manager, "metadata"));
	}

	// The artwork file lives in the cache folder, as a file the user picked would: not in the app package, where the resource source reads.
	string CopyArtworkToCacheFolder(string fileName = "artwork.png")
	{
		var folder = Directory.CreateDirectory(Path.Combine(FileSystem.CacheDirectory, $"artwork_{Guid.NewGuid():N}")).FullName;
		RegisterCleanup(() => Directory.Delete(folder, recursive: true));
		var path = Path.Combine(folder, fileName);
		File.WriteAllBytes(path, WinMediaAssets.ReadArtworkBytes());
		return path;
	}

	static Task AssertPosterIsTheDecodedArtworkAsync(WinMediaManagerFixture fixture) =>
		WaitUntilOnUiAsync(() => fixture.Element!.PosterSource is BitmapImage { PixelWidth: 16, PixelHeight: 16 }, "The poster never showed the 16 by 16 artwork.");

	static async Task AssertNoArtworkAsync(WinMediaManagerFixture fixture)
	{
		await InvokeOnMainThreadAsync(() =>
		{
			Assert.Null(fixture.Player!.SystemMediaTransportControls.DisplayUpdater.Thumbnail);
			Assert.Null(fixture.Element!.PosterSource);
		});
	}

	[Fact]
	public async Task UpdateMetadataWithAUriArtworkSourceSetsTheThumbnailAndThePoster()
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();
		await RequireDisplayUpdaterAsync();
		await using var server = WinMediaAssets.StartArtworkServer();
		var fixture = await CreateManagerAsync(view => view.MetadataArtworkSource = MediaSource.FromUri(ArtworkUri(server)));

		await UpdateMetadataAsync(fixture);

		await AssertThumbnailIsTheArtworkAsync(fixture, server);
		await InvokeOnMainThreadAsync(() =>
		{
			var poster = Assert.IsType<BitmapImage>(fixture.Element!.PosterSource);
			Assert.Equal(ArtworkUri(server).AbsoluteUri, poster.UriSource.AbsoluteUri, ignoreCase: true);
		});
	}

	[Fact]
	public async Task UpdateMetadataWithAFileArtworkSourceSetsTheThumbnailAndThePoster()
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();
		await RequireDisplayUpdaterAsync();
		var path = CopyArtworkToCacheFolder();
		var fixture = await CreateManagerAsync(view => view.MetadataArtworkSource = MediaSource.FromFile(path));

		await UpdateMetadataAsync(fixture);

		await AssertThumbnailIsAsync(fixture, WinMediaAssets.ReadArtworkBytes());
		await AssertPosterIsTheDecodedArtworkAsync(fixture);
	}

	[Theory]
	[InlineData("a Uri source")]
	[InlineData("a File source")]
	public async Task UpdateMetadataWithAFileUriArtworkSourceSetsTheThumbnail(string sourceType)
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();
		await RequireDisplayUpdaterAsync();
		var fileUri = new Uri(CopyArtworkToCacheFolder());
		var fixture = await CreateManagerAsync(view => view.MetadataArtworkSource = sourceType switch
		{
			"a Uri source" => MediaSource.FromUri(fileUri),
			"a File source" => MediaSource.FromFile(fileUri.AbsoluteUri),
			_ => throw new ArgumentOutOfRangeException(nameof(sourceType), sourceType, null),
		});

		await UpdateMetadataAsync(fixture);

		await AssertThumbnailIsAsync(fixture, WinMediaAssets.ReadArtworkBytes());
		await AssertPosterIsTheDecodedArtworkAsync(fixture);
	}

	[Fact]
	public async Task UpdateMetadataWithAResourceArtworkSourceSetsTheThumbnailAndThePoster()
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();
		await RequireDisplayUpdaterAsync();
		var fixture = await CreateManagerAsync(view => view.MetadataArtworkSource = MediaSource.FromResource(WinMediaAssets.ArtworkRelativePath));

		await UpdateMetadataAsync(fixture);

		await AssertThumbnailIsAsync(fixture, WinMediaAssets.ReadArtworkBytes());
		await AssertPosterIsTheDecodedArtworkAsync(fixture);
	}

	// UpdateMetadata runs inside the async void MediaOpened handler, where an exception ends the app.
	[Theory]
	[InlineData("a missing file")]
	[InlineData("a missing resource")]
	public async Task UpdateMetadataWithAMissingArtworkSourceHasNoThumbnailAndKeepsTheEmptyPosterWithoutThrowing(string missing)
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();
		await RequireDisplayUpdaterAsync();
		var missingFile = Path.Combine(FileSystem.CacheDirectory, $"missing_{Guid.NewGuid():N}.png");
		var fixture = await CreateManagerAsync(view => view.MetadataArtworkSource = missing switch
		{
			"a missing file" => MediaSource.FromFile(missingFile),
			"a missing resource" => MediaSource.FromResource("mediaplayer/missing.png"),
			_ => throw new ArgumentOutOfRangeException(nameof(missing), missing, null),
		});

		var exception = await Record.ExceptionAsync(() => UpdateMetadataAsync(fixture));

		Assert.Null(exception);
		await AssertNoArtworkAsync(fixture);
		await InvokeOnMainThreadAsync(() => Assert.True(fixture.Player!.SystemMediaTransportControls.IsEnabled, "An artwork source counts as metadata, loaded or not."));
	}

	[Fact]
	public async Task UpdateMetadataIgnoresAStreamArtworkSource()
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();
		await RequireDisplayUpdaterAsync();
		using var stream = new MemoryStream(WinMediaAssets.ReadArtworkBytes());
		var fixture = await CreateManagerAsync(view => view.MetadataArtworkSource = MediaSource.FromStream(stream));

		var exception = await Record.ExceptionAsync(() => UpdateMetadataAsync(fixture));

		Assert.Null(exception);
		await AssertNoArtworkAsync(fixture);
		Assert.Equal(0, stream.Position);
	}

	// The artwork source wins over MetadataArtworkUrl even when the file is the only one of the two that loads: the url is never read.
	[Fact]
	public async Task ArtworkSourceReplacesMetadataArtworkUrl()
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();
		await RequireDisplayUpdaterAsync();
		await using var server = WinMediaAssets.StartArtworkServer();
		var path = CopyArtworkToCacheFolder();
		var fixture = await CreateManagerAsync(view =>
		{
			view.MetadataArtworkUrl = ArtworkUri(server).AbsoluteUri;
			view.MetadataArtworkSource = MediaSource.FromFile(path);
		});

		await UpdateMetadataAsync(fixture);

		await AssertThumbnailIsAsync(fixture, WinMediaAssets.ReadArtworkBytes());
		await AssertPosterIsTheDecodedArtworkAsync(fixture);
		await InvokeOnMainThreadAsync(() => Assert.Null(((BitmapImage)fixture.Element!.PosterSource).UriSource));
		Assert.Empty(server.Requests);
	}

	// The system media controls belong to the player and outlive the media: the thumbnail of the previous media must not stay behind.
	[Theory]
	[InlineData("a MetadataArtworkUrl")]
	[InlineData("a MetadataArtworkSource")]
	public async Task MediaWithoutArtworkAfterMediaWithArtworkClearsTheThumbnail(string firstArtwork)
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();
		await RequireDisplayUpdaterAsync();
		await using var server = WinMediaAssets.StartArtworkServer();
		var path = CopyArtworkToCacheFolder();
		var fixture = await CreateManagerAsync(view =>
		{
			view.MetadataTitle = title;
			switch (firstArtwork)
			{
				case "a MetadataArtworkUrl":
					view.MetadataArtworkUrl = ArtworkUri(server).AbsoluteUri;
					break;
				case "a MetadataArtworkSource":
					view.MetadataArtworkSource = MediaSource.FromFile(path);
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(firstArtwork), firstArtwork, null);
			}
		});
		await UpdateMetadataAsync(fixture);
		await InvokeOnMainThreadAsync(() => Assert.NotNull(fixture.Player!.SystemMediaTransportControls.DisplayUpdater.Thumbnail));

		await InvokeOnMainThreadAsync(() =>
		{
			fixture.View.MetadataArtworkUrl = string.Empty;
			fixture.View.MetadataArtworkSource = null;
		});
		await UpdateMetadataAsync(fixture);

		await InvokeOnMainThreadAsync(() =>
		{
			var updater = fixture.Player!.SystemMediaTransportControls.DisplayUpdater;
			Assert.Null(updater.Thumbnail);
			Assert.Equal(title, updater.MusicProperties.Title);
		});
	}

	// A newer source update (or Dispose) changes sourceUpdateVersion while the artwork loads: the artwork of the superseded update must not be
	// applied. The superseded update is started with the version before the current one, so that nothing depends on the timing of the load.
	[Fact]
	public async Task ArtworkThatLoadsWhileANewerSourceStartsIsNotApplied()
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();
		await RequireDisplayUpdaterAsync();
		var path = CopyArtworkToCacheFolder();
		var fixture = await CreateManagerAsync(view =>
		{
			view.MetadataTitle = title;
			view.MetadataArtworkSource = MediaSource.FromFile(path);
		});
		var supersededVersion = WinMediaReflection.GetField<int>(fixture.Manager, "sourceUpdateVersion") - 1;

		await InvokeOnMainThreadAsync(async () => await (ValueTask)WinMediaReflection.Invoke(fixture.Manager, "UpdateMetadataForSourceUpdate", supersededVersion, CancellationToken.None)!);

		await AssertNoArtworkAsync(fixture);
		Assert.Null(WinMediaReflection.GetField<object?>(fixture.Manager, "metadata"));
	}

	[Fact]
	public async Task UpdateMetadataWithAnArtworkSourceAfterDisposeDoesNothing()
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();
		var path = CopyArtworkToCacheFolder();
		var fixture = await CreateManagerAsync(view =>
		{
			view.MetadataTitle = title;
			view.MetadataArtworkSource = MediaSource.FromFile(path);
		});
		await InvokeOnMainThreadAsync(() => fixture.Manager.Dispose());

		var exception = await Record.ExceptionAsync(() => UpdateMetadataAsync(fixture));

		Assert.Null(exception);
		Assert.Null(WinMediaReflection.GetField<object?>(fixture.Manager, "metadata"));
		await InvokeOnMainThreadAsync(() => Assert.Null(fixture.Element!.PosterSource));
	}

	// 17.0.0 showed a poster for a local file but no thumbnail: the same artwork now reaches the system media controls too
	[Theory]
	[InlineData("a path")]
	[InlineData("a path with forward slashes")]
	[InlineData("a file URI")]
	public async Task UpdateMetadataWithAMetadataArtworkUrlFilePathSetsTheThumbnailAndThePoster(string form)
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();
		await RequireDisplayUpdaterAsync();
		var path = CopyArtworkToCacheFolder();
		var artworkUrl = form switch
		{
			"a path" => path,
			"a path with forward slashes" => path.Replace('\\', '/'),
			"a file URI" => new Uri(path).AbsoluteUri,
			_ => throw new ArgumentOutOfRangeException(nameof(form), form, null),
		};
		var fixture = await CreateManagerAsync(view => view.MetadataArtworkUrl = artworkUrl);

		await UpdateMetadataAsync(fixture);

		await AssertThumbnailIsAsync(fixture, WinMediaAssets.ReadArtworkBytes());
		await AssertPosterIsTheDecodedArtworkAsync(fixture);
	}

	// 17.0.0 trimmed the string with Uri.TryCreate before it built the poster, so a padded path or file URI kept its poster
	[Theory]
	[InlineData("a path")]
	[InlineData("a file URI")]
	public async Task UpdateMetadataWithAWhitespacePaddedMetadataArtworkUrlFileKeepsThe17Poster(string form)
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();
		await RequireDisplayUpdaterAsync();
		var path = CopyArtworkToCacheFolder();
		var artworkUrl = form switch
		{
			"a path" => $"  {path}",
			"a file URI" => $" {new Uri(path).AbsoluteUri}",
			_ => throw new ArgumentOutOfRangeException(nameof(form), form, null),
		};
		var fixture = await CreateManagerAsync(view => view.MetadataArtworkUrl = artworkUrl);

		await UpdateMetadataAsync(fixture);

		await AssertPosterIsTheDecodedArtworkAsync(fixture);
	}

	[Theory]
	[InlineData("mediaplayer/artwork.png")]
	[InlineData(@"mediaplayer\artwork.png")]
	public async Task UpdateMetadataWithARelativeMetadataArtworkUrlLoadsTheAppPackageFile(string artworkUrl)
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();
		await RequireDisplayUpdaterAsync();
		var fixture = await CreateManagerAsync(view => view.MetadataArtworkUrl = artworkUrl);

		await UpdateMetadataAsync(fixture);

		await AssertThumbnailIsAsync(fixture, WinMediaAssets.ReadArtworkBytes());
		await AssertPosterIsTheDecodedArtworkAsync(fixture);
	}

	[Fact]
	public async Task UpdateMetadataWithAnMsAppxMetadataArtworkUrlKeepsThe17ThumbnailAndPoster()
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();
		await RequireDisplayUpdaterAsync();
		var artworkUri = new Uri($"ms-appx:///{WinMediaAssets.ArtworkRelativePath}");
		var fixture = await CreateManagerAsync(view => view.MetadataArtworkUrl = artworkUri.AbsoluteUri);

		await UpdateMetadataAsync(fixture);

		await InvokeOnMainThreadAsync(() =>
		{
			Assert.NotNull(fixture.Player!.SystemMediaTransportControls.DisplayUpdater.Thumbnail);
			var poster = Assert.IsType<BitmapImage>(fixture.Element!.PosterSource);
			Assert.Equal(artworkUri.AbsoluteUri, poster.UriSource.AbsoluteUri, ignoreCase: true);
		});
	}

	// The system media controls take only http, https, ms-appx and ms-appdata, but 17.0.0 set the poster from any absolute URI, best effort
	[Theory]
	[InlineData("a data URI in MetadataArtworkUrl")]
	[InlineData("an ftp URI in MetadataArtworkUrl")]
	[InlineData("a data URI in MetadataArtworkSource")]
	public async Task UpdateMetadataWithAnArtworkOfAnotherSchemeSetsOnlyThePoster(string form)
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();
		await RequireDisplayUpdaterAsync();
		var dataUri = new Uri($"data:image/png;base64,{Convert.ToBase64String(WinMediaAssets.ReadArtworkBytes())}");
		var ftpUri = new Uri("ftp://localhost/cover.png");
		var fixture = await CreateManagerAsync(view =>
		{
			switch (form)
			{
				case "a data URI in MetadataArtworkUrl":
					view.MetadataArtworkUrl = dataUri.OriginalString;
					break;
				case "an ftp URI in MetadataArtworkUrl":
					view.MetadataArtworkUrl = ftpUri.OriginalString;
					break;
				case "a data URI in MetadataArtworkSource":
					view.MetadataArtworkSource = MediaSource.FromUri(dataUri);
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(form), form, null);
			}
		});
		var expectedUri = form.StartsWith("an ftp", StringComparison.Ordinal) ? ftpUri : dataUri;

		var exception = await Record.ExceptionAsync(() => UpdateMetadataAsync(fixture));

		Assert.Null(exception);
		await InvokeOnMainThreadAsync(() =>
		{
			Assert.Null(fixture.Player!.SystemMediaTransportControls.DisplayUpdater.Thumbnail);
			// BitmapImage may refuse a scheme it cannot load: the empty poster then stays, as when the load fails
			if (fixture.Element!.PosterSource is BitmapImage { UriSource: not null } poster)
			{
				Assert.Equal(expectedUri.AbsoluteUri, poster.UriSource.AbsoluteUri, ignoreCase: true);
			}
		});
	}

	[Theory]
	[InlineData("a missing path")]
	[InlineData("a missing app package path")]
	public async Task UpdateMetadataWithAMissingMetadataArtworkUrlFileHasNoThumbnailWithoutThrowing(string form)
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();
		await RequireDisplayUpdaterAsync();
		var artworkUrl = form switch
		{
			"a missing path" => Path.Combine(FileSystem.CacheDirectory, $"missing_{Guid.NewGuid():N}.png"),
			"a missing app package path" => "images/missing.png",
			_ => throw new ArgumentOutOfRangeException(nameof(form), form, null),
		};
		var fixture = await CreateManagerAsync(view => view.MetadataArtworkUrl = artworkUrl);

		var exception = await Record.ExceptionAsync(() => UpdateMetadataAsync(fixture));

		Assert.Null(exception);
		await AssertNoArtworkAsync(fixture);
	}

	[Fact]
	public async Task ClearingTheArtworkSourceFallsBackToMetadataArtworkUrl()
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();
		await RequireDisplayUpdaterAsync();
		await using var server = WinMediaAssets.StartArtworkServer();
		var path = CopyArtworkToCacheFolder();
		var fixture = await CreateManagerAsync(view =>
		{
			view.MetadataArtworkSource = MediaSource.FromFile(path);
			view.MetadataArtworkUrl = ArtworkUri(server).AbsoluteUri;
			view.MetadataArtworkSource = null;
		});

		await UpdateMetadataAsync(fixture);

		await AssertThumbnailIsTheArtworkAsync(fixture, server);
	}

	// UpdateMetadata resolves the artwork once and hands it to SetMetadata, so the thumbnail and the poster cannot come from two artworks
	[Fact]
	public async Task SetMetadataWithAnArtworkSourceUsesThatSourceForTheThumbnail()
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();
		await RequireDisplayUpdaterAsync();
		await using var server = WinMediaAssets.StartArtworkServer();
		var fixture = await CreateManagerAsync();

		await InvokeOnMainThreadAsync(() => CreateMetadata(fixture).SetMetadata(fixture.View, MediaSource.FromUri(ArtworkUri(server)), loadedThumbnail: null));

		await AssertThumbnailIsTheArtworkAsync(fixture, server);
	}

	[Fact]
	public async Task OpeningMediaPublishesTheMetadata()
	{
		SetupWindowsBuilder();
		await RequireDecoderAsync();
		await RequireDisplayUpdaterAsync();
		var fixture = await CreateManagerAsync(view =>
		{
			view.MetadataTitle = title;
			view.MetadataArtist = artist;
		});

		await OpenAsync(fixture.View, MediaSource.FromFile(WinMediaAssets.VideoPath), fixture.Manager);

		// OnMediaElementMediaOpened awaits UpdateMetadata after raising MediaOpened.
		await WaitUntilAsync(() => WinMediaReflection.GetField<object?>(fixture.Manager, "metadata") is not null, "MediaOpened never created the Metadata helper.");
		Assert.Equal(MediaPlaybackType.Music, fixture.Player!.SystemMediaTransportControls.DisplayUpdater.Type);
	}

	// What users get: a playing MediaElement with metadata, in the configuration the toolkit leaves its player in (nothing reconfigured here).
	// The system media overlay, the media keys and the lock screen find an app through its system media session.
	[Fact]
	public async Task OpenedMediaWithMetadataAppearsAsASystemMediaSession()
	{
		SetupWindowsBuilder();
		await RequireDecoderAsync();
		await RequireMediaSessionAsync();
		var token = $"test-{Guid.NewGuid():N}";
		var view = NewMediaElement(view =>
		{
			view.ShouldLoopPlayback = true;
			view.MetadataTitle = token;
			view.MetadataArtist = token;
		});
		await ConnectAsync(view);
		await OpenAsync(view, MediaSource.FromFile(WinMediaAssets.VideoPath));

		await InvokeOnMainThreadAsync(view.Play);

		var session = await WinMediaMediaSessions.FindAsync(token, timeoutMilliseconds: 10000);

		// RequireMediaSessionAsync saw the session of a plain MediaPlayer on this machine: a missing session is the toolkit's.
		Assert.True(session is not null, "This machine shows the system media session of a plain MediaPlayer, but none for the playing MediaElement whose metadata the toolkit published.");
		Assert.Equal(token, (await WinMediaMediaSessions.Bounded(session.TryGetMediaPropertiesAsync())).Title);
	}

	// While the media plays, its system media session plays at the Speed of the MediaElement, the rate at which the system media overlay advances
	// the position it shows. Playing takes a machine that renders the video.
	[Fact]
	public async Task TheSystemMediaSessionPublishesTheSpeedWhileTheMediaPlays()
	{
		SetupWindowsBuilder();
		await RequireVideoRenderingAsync();
		var playback = await PlayInASystemMediaSessionAsync(speed: 1.5);
		await WaitForSystemMediaSessionAsync(playback, info => IsPlayingAt(info, 1.5), "The system media session never showed the media as playing at a speed of 1.5.");

		await InvokeOnMainThreadAsync(() => playback.View.Speed = 2);

		await WaitForSystemMediaSessionAsync(playback, info => IsPlayingAt(info, 2), "The system media session never showed the new speed.");
	}

	// While the media is paused, its system media session is paused: the system media overlay and the lock screen show the media as paused, at a
	// position that does not advance. The session keeps the playback rate of the player, which a pause doesn't change on Windows. Where the video
	// doesn't render (the Windows CI jobs), Play() leaves the player paused, and the session shows the paused media the same way.
	[Fact]
	public async Task PauseShowsTheMediaAsPausedInTheSystemMediaSession()
	{
		SetupWindowsBuilder();
		var playback = await PlayInASystemMediaSessionAsync(speed: 1.5);

		await InvokeOnMainThreadAsync(playback.View.Pause);

		await WaitForSystemMediaSessionAsync(playback,
			info => info.PlaybackStatus is GlobalSystemMediaTransportControlsSessionPlaybackStatus.Paused && info.PlaybackRate is double rate && Math.Abs(rate - 1.5) < 0.001,
			"The system media session never showed the media as paused at the playback rate of the player.");
	}

	static bool IsPlayingAt(GlobalSystemMediaTransportControlsSessionPlaybackInfo info, double speed) =>
		info.PlaybackStatus is GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing && info.PlaybackRate is double rate && Math.Abs(rate - speed) < 0.001;

	// A failure reports what the session showed, also through the session found again, and what the player told its system media transport controls.
	static async Task WaitForSystemMediaSessionAsync(SystemMediaSessionPlayback playback, Func<GlobalSystemMediaTransportControlsSessionPlaybackInfo, bool> condition, string message)
	{
		var deadline = DateTime.UtcNow.AddSeconds(15);
		GlobalSystemMediaTransportControlsSessionPlaybackInfo info;
		while (!condition(info = playback.Session.GetPlaybackInfo()))
		{
			if (DateTime.UtcNow > deadline)
			{
				var foundAgain = (await WinMediaMediaSessions.FindAsync(playback.Token))?.GetPlaybackInfo();
				var player = await InvokeOnMainThreadAsync(() =>
				{
					var controls = playback.Player.SystemMediaTransportControls;
					return $"{playback.View.CurrentState}, its player {playback.Player.PlaybackSession.PlaybackState} at a rate of {playback.Player.PlaybackSession.PlaybackRate}, "
						+ $"its system media transport controls {controls.PlaybackStatus} at a rate of {controls.PlaybackRate} (enabled: {controls.IsEnabled}, command manager enabled: {playback.Player.CommandManager.IsEnabled})";
				});
				Assert.Fail($"{message} The session showed {Describe(info)}, and {(foundAgain is null ? "was not found again" : $"{Describe(foundAgain)} when found again")}. The MediaElement was {player}.");
			}

			await Task.Delay(100);
		}

		static string Describe(GlobalSystemMediaTransportControlsSessionPlaybackInfo info) => $"{info.PlaybackStatus} at a rate of {info.PlaybackRate?.ToString() ?? "none"}";
	}

	// A MediaElement with metadata that plays in the configuration the toolkit leaves its player in, as in OpenedMediaWithMetadataAppearsAsASystemMediaSession.
	async Task<SystemMediaSessionPlayback> PlayInASystemMediaSessionAsync(double speed)
	{
		await RequireDecoderAsync();
		await RequireMediaSessionAsync();
		var token = $"test-{Guid.NewGuid():N}";
		var view = NewMediaElement(view =>
		{
			view.ShouldLoopPlayback = true;
			view.MetadataTitle = token;
			view.MetadataArtist = token;
		});
		var handler = await ConnectAsync(view);
		await OpenAsync(view, MediaSource.FromFile(WinMediaAssets.VideoPath));

		await InvokeOnMainThreadAsync(() =>
		{
			view.Speed = speed;
			view.Play();
		});

		var session = await WinMediaMediaSessions.FindAsync(token, timeoutMilliseconds: 10000);
		Assert.True(session is not null, "This machine shows the system media session of a plain MediaPlayer, but none for the playing MediaElement whose metadata the toolkit published.");
		return new(view, await GetPlayerAsync(handler), session, token);
	}

	sealed record SystemMediaSessionPlayback(MediaElement View, MediaPlayer Player, GlobalSystemMediaTransportControlsSession Session, string Token);

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task OpeningMediaSwitchesTheSystemMediaControlsOnOnlyForMediaWithMetadata(bool hasMetadata)
	{
		SetupWindowsBuilder();
		await RequireDecoderAsync();
		await RequireDisplayUpdaterAsync();
		var fixture = await CreateManagerAsync(view =>
		{
			if (hasMetadata)
			{
				view.MetadataTitle = title;
				view.MetadataArtist = artist;
			}
		});

		await OpenAsync(fixture.View, MediaSource.FromFile(WinMediaAssets.VideoPath), fixture.Manager);

		// SetMetadata runs on the thread that raised MediaOpened, and sets the display type after the state of the controls.
		var controls = fixture.Player!.SystemMediaTransportControls;
		await WaitUntilAsync(() => controls.DisplayUpdater.Type is MediaPlaybackType.Music, "MediaOpened never published the metadata.");
		Assert.Equal(hasMetadata, controls.IsEnabled);
	}

	[Theory]
	[InlineData(nameof(MediaElement.MetadataTitle))]
	[InlineData(nameof(MediaElement.MetadataArtist))]
	[InlineData(nameof(MediaElement.MetadataArtworkUrl))]
	[InlineData(nameof(MediaElement.MetadataArtworkSource))]
	public async Task SetMetadataWithMetadataSwitchesTheSystemMediaControlsOn(string metadata)
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();
		await using var server = WinMediaAssets.StartArtworkServer();
		var fixture = await CreateManagerAsync(view =>
		{
			switch (metadata)
			{
				case nameof(MediaElement.MetadataTitle):
					view.MetadataTitle = title;
					break;
				case nameof(MediaElement.MetadataArtist):
					view.MetadataArtist = artist;
					break;
				case nameof(MediaElement.MetadataArtworkUrl):
					view.MetadataArtworkUrl = ArtworkUri(server).AbsoluteUri;
					break;
				case nameof(MediaElement.MetadataArtworkSource):
					view.MetadataArtworkSource = MediaSource.FromFile(WinMediaAssets.ArtworkPath);
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(metadata), metadata, null);
			}
		});

		await InvokeOnMainThreadAsync(() =>
		{
			var controls = fixture.Player!.SystemMediaTransportControls;
			Assert.False(controls.IsEnabled);

			CreateMetadata(fixture).SetMetadata(fixture.View);

			Assert.True(controls.IsEnabled);
		});
	}

	[Theory]
	[InlineData("")]
	[InlineData("   ")]
	public async Task SetMetadataWithoutMetadataSwitchesTheSystemMediaControlsOff(string blank)
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();
		var fixture = await CreateManagerAsync(view =>
		{
			view.MetadataTitle = blank;
			view.MetadataArtist = blank;
			view.MetadataArtworkUrl = blank;
		});

		await InvokeOnMainThreadAsync(() =>
		{
			var controls = fixture.Player!.SystemMediaTransportControls;
			controls.IsEnabled = true;

			CreateMetadata(fixture).SetMetadata(fixture.View);

			Assert.False(controls.IsEnabled);
		});
	}

	[Theory]
	[InlineData("no source")]
	[InlineData("a source that cannot be applied")]
	public async Task RemovingTheMediaSwitchesTheSystemMediaControlsOff(string replacement)
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();
		var fixture = await CreateManagerAsync(view => view.MetadataTitle = title);
		await UpdateMetadataAsync(fixture);
		var controls = fixture.Player!.SystemMediaTransportControls;
		Assert.True(controls.IsEnabled);

		switch (replacement)
		{
			case "no source":
				await SetSourceAsync(fixture.View, null, fixture.Manager);
				break;
			case "a source that cannot be applied":
				var missing = Path.Combine(Path.GetDirectoryName(WinMediaAssets.VideoPath)!, "missing.mp4");
				await SetSourceAsync(fixture.View, MediaSource.FromFile(missing), fixture.Manager);
				Assert.Equal(MediaElementState.Failed, fixture.View.CurrentState);
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(replacement), replacement, null);
		}

		Assert.False(controls.IsEnabled);
	}

	// The hardware media keys (and the system media overlay) raise SystemMediaTransportControls.ButtonPressed on a background thread. The app cannot
	// raise it itself, but the system media sessions can: asking this app's own session to pause, play or skip is what a key press does. The
	// session is found by a title that the test publishes itself, so no other app is touched; the tests skip, visibly, on a machine that shows
	// no session for the app.
	[Fact]
	public async Task SystemPlayAndPauseCommandsRaiseThePlayAndPauseRequestsOnTheUiThread()
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();
		var token = $"test-{Guid.NewGuid():N}";
		var fixture = await CreateManagerAsync(view =>
		{
			view.MetadataTitle = token;
			view.MetadataArtist = token;
		});
		var session = await PublishSystemMediaSessionAsync(fixture, token);
		var uiThread = await InvokeOnMainThreadAsync(() => Environment.CurrentManagedThreadId);
		var played = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
		var paused = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
		fixture.View.PlayRequested += (_, _) => played.TrySetResult(Environment.CurrentManagedThreadId);
		fixture.View.PauseRequested += (_, _) => paused.TrySetResult(Environment.CurrentManagedThreadId);

		Assert.True(await WinMediaMediaSessions.Bounded(session.TryPauseAsync()), "The system refused the pause command.");
		Assert.Equal(uiThread, await paused.Task.WaitAsync(TimeSpan.FromSeconds(15)));
		Assert.False(played.Task.IsCompleted);

		// What an app does when it paused: tell the system, so that Play is a meaningful request.
		fixture.Player!.SystemMediaTransportControls.PlaybackStatus = MediaPlaybackStatus.Paused;
		Assert.True(await WinMediaMediaSessions.Bounded(session.TryPlayAsync()), "The system refused the play command.");
		Assert.Equal(uiThread, await played.Task.WaitAsync(TimeSpan.FromSeconds(15)));
	}

	[Fact]
	public async Task SystemPlayAndPauseCommandsRunInlineWhenNoDispatchIsRequired()
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();
		var token = $"test-{Guid.NewGuid():N}";

		var fixture = await CreateManagerAsync(view =>
		{
			view.MetadataTitle = token;
			view.MetadataArtist = token;
		}, dispatchRequired: false);
		var session = await PublishSystemMediaSessionAsync(fixture, token);
		var uiThread = await InvokeOnMainThreadAsync(() => Environment.CurrentManagedThreadId);
		var played = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
		var paused = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
		fixture.View.PlayRequested += (_, _) => played.TrySetResult(Environment.CurrentManagedThreadId);
		fixture.View.PauseRequested += (_, _) => paused.TrySetResult(Environment.CurrentManagedThreadId);

		Assert.True(await WinMediaMediaSessions.Bounded(session.TryPauseAsync()), "The system refused the pause command.");
		Assert.NotEqual(uiThread, await paused.Task.WaitAsync(TimeSpan.FromSeconds(15)));
		fixture.Player!.SystemMediaTransportControls.PlaybackStatus = MediaPlaybackStatus.Paused;
		Assert.True(await WinMediaMediaSessions.Bounded(session.TryPlayAsync()), "The system refused the play command.");
		Assert.NotEqual(uiThread, await played.Task.WaitAsync(TimeSpan.FromSeconds(15)));
		Assert.Equal(0, fixture.Dispatcher!.DispatchCount);
	}

	[Fact]
	public async Task SystemNextCommandRaisesNeitherPlayNorPauseRequest()
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();
		var token = $"test-{Guid.NewGuid():N}";
		var fixture = await CreateManagerAsync(view =>
		{
			view.MetadataTitle = token;
			view.MetadataArtist = token;
		});
		var session = await PublishSystemMediaSessionAsync(fixture, token);
		var playRequests = 0;
		var pauseRequests = 0;
		var played = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		fixture.View.PlayRequested += (_, _) =>
		{
			Interlocked.Increment(ref playRequests);
			played.TrySetResult();
		};
		fixture.View.PauseRequested += (_, _) => Interlocked.Increment(ref pauseRequests);

		// The play command that follows is the fence: ButtonPressed events are raised in order, so once the play request arrived, a request caused by
		// Next would have arrived before it.
		Assert.True(await WinMediaMediaSessions.Bounded(session.TrySkipNextAsync()), "The system refused the next-track command.");
		fixture.Player!.SystemMediaTransportControls.PlaybackStatus = MediaPlaybackStatus.Paused;
		Assert.True(await WinMediaMediaSessions.Bounded(session.TryPlayAsync()), "The system refused the play command.");
		await played.Task.WaitAsync(TimeSpan.FromSeconds(15));
		await FlushDispatcherAsync();

		Assert.Equal(1, playRequests);
		Assert.Equal(0, pauseRequests);
	}

	// The same handlers with the real keys: nothing in the app can press them, so the tests below wait for a person. They run only when the app
	// is started without arguments (visual runner). The player's command manager is switched off: otherwise it would act on the keys itself and
	// the tests would pass without the toolkit's handler.
	async Task<(MediaElement View, MediaPlayer Player)> StartPlaybackForButtonTestAsync()
	{
		SetupWindowsBuilder();
		await RequireDecoderAsync();
		var view = NewMediaElement(view =>
		{
			view.ShouldLoopPlayback = true;
			view.MetadataTitle = title;
			view.MetadataArtist = artist;
		});
		var handler = await ConnectAsync(view);
		await OpenAsync(view, MediaSource.FromFile(WinMediaAssets.VideoPath));
		var player = await GetPlayerAsync(handler);
		await InvokeOnMainThreadAsync(() =>
		{
			player.CommandManager.IsEnabled = false;
			var controls = player.SystemMediaTransportControls;
			controls.IsEnabled = true;
			controls.IsPlayEnabled = true;
			controls.IsPauseEnabled = true;
			controls.IsNextEnabled = true;
			controls.IsStopEnabled = true;
		});
		return (view, player);
	}

	static async Task PlayAndPublishAsync(MediaElement view, MediaPlayer player)
	{
		await InvokeOnMainThreadAsync(() =>
		{
			view.Play();
			player.SystemMediaTransportControls.PlaybackStatus = MediaPlaybackStatus.Playing;
		});
		await WaitUntilAsync(() => player.PlaybackSession.PlaybackState is MediaPlaybackState.Playing, "The media never started playing.");
	}

	[Fact]
	[Category(TestCategory.RequiresUserInteraction)]
	public async Task PressingThePauseMediaKeyPausesTheMediaElement()
	{
		var (view, player) = await StartPlaybackForButtonTestAsync();
		await PlayAndPublishAsync(view, player);
		var pauseRequested = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		view.PauseRequested += (_, _) => pauseRequested.TrySetResult();
		TestRunnerLogger?.LogWarning("MANUAL STEP: press the Pause (or Play/Pause) media key or use the system media overlay within 60 seconds.");

		await pauseRequested.Task.WaitAsync(TimeSpan.FromSeconds(60));

		await WaitUntilAsync(() => view.CurrentState is MediaElementState.Paused, "ButtonPressed(Pause) requested a pause, but the MediaElement did not pause.");
	}

	[Fact]
	[Category(TestCategory.RequiresUserInteraction)]
	public async Task PressingThePlayMediaKeyPlaysTheMediaElement()
	{
		var (view, player) = await StartPlaybackForButtonTestAsync();
		await PlayAndPublishAsync(view, player);
		await InvokeOnMainThreadAsync(() =>
		{
			view.Pause();
			player.SystemMediaTransportControls.PlaybackStatus = MediaPlaybackStatus.Paused;
		});
		await WaitUntilAsync(() => player.PlaybackSession.PlaybackState is MediaPlaybackState.Paused, "The media never paused.");
		var playRequested = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		view.PlayRequested += (_, _) => playRequested.TrySetResult();
		TestRunnerLogger?.LogWarning("MANUAL STEP: press the Play (or Play/Pause) media key or use the system media overlay within 60 seconds.");

		await playRequested.Task.WaitAsync(TimeSpan.FromSeconds(60));

		await WaitUntilAsync(() => view.CurrentState is MediaElementState.Playing, "ButtonPressed(Play) requested playback, but the MediaElement did not play.");
	}

	[Fact]
	[Category(TestCategory.RequiresUserInteraction)]
	public async Task PressingAnyOtherMediaKeyLeavesThePlaybackAlone()
	{
		var (view, player) = await StartPlaybackForButtonTestAsync();
		await PlayAndPublishAsync(view, player);
		var requests = 0;
		view.PlayRequested += (_, _) => Interlocked.Increment(ref requests);
		view.PauseRequested += (_, _) => Interlocked.Increment(ref requests);

		// Registered after the toolkit's handler (the Metadata helper subscribed when the media opened): when it runs, the toolkit's ran.
		var buttonPressed = new TaskCompletionSource<SystemMediaTransportControlsButton>(TaskCreationOptions.RunContinuationsAsynchronously);
		player.SystemMediaTransportControls.ButtonPressed += (_, args) => buttonPressed.TrySetResult(args.Button);
		TestRunnerLogger?.LogWarning("MANUAL STEP: press the Next track or Stop media key within 60 seconds (not Play, Pause or Play/Pause).");

		var button = await buttonPressed.Task.WaitAsync(TimeSpan.FromSeconds(60));
		await FlushDispatcherAsync();

		Assert.True(button is not (SystemMediaTransportControlsButton.Play or SystemMediaTransportControlsButton.Pause), $"A {button} press arrived; press another key.");
		Assert.Equal(0, requests);
		Assert.Equal(MediaElementState.Playing, view.CurrentState);
	}
}