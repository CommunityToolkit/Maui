#pragma warning disable CS0618 // These tests pin the obsolete MetadataArtworkUrl, which keeps working
using System.Diagnostics;
using System.Reflection;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Views;
using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui.DeviceTests;

[Category(TestCategory.MediaElementAndroid)]
[Collection(TestCollections.DisableParallelization)]
public class MediaManagerArtworkSourceAndroidTests : MediaElementAndroidTestBase
{
	const string unableToRetrieveArtworkSource = $"Unable to retrieve {nameof(MediaElement.MetadataArtworkSource)} from ";

	[Fact]
	public async Task ArtworkSourceIsDownloadedFromAnHttpUri()
	{
		var artwork = await ReadAssetAsync(artworkAsset);
		using var server = new LoopbackHttpServer(new Dictionary<string, (byte[], string)> { ["/art.png"] = (artwork, "image/png") });

		await AssertLoadedAsync(element => element.MetadataArtworkSource = MediaSource.FromUri(server.GetUri("/art.png")), mediaElement =>
		{
			Assert.Equal(artwork, GetArtwork(mediaElement));
			Assert.True(server.HasRequest("GET", "/art.png"));
			Assert.False(server.HasRequest("HEAD", "/art.png"), "The typed path takes the content length from the response of its one GET.");
		});
	}

	[Fact]
	public async Task ArtworkSourceThatTheServerAnswersWithAnErrorStatusIsLeftOut()
	{
		using var server = new LoopbackHttpServer(new Dictionary<string, (byte[], string)>());
		using var trace = new TraceCapture();

		await AssertLoadedAsync(element => element.MetadataArtworkSource = MediaSource.FromUri(server.GetUri("/missing.png")), mediaElement =>
		{
			Assert.Empty(GetArtwork(mediaElement));
			Assert.True(server.HasRequest("GET", "/missing.png"));
			Assert.DoesNotContain(trace.Messages, message => message.Contains("Unable to retrieve", StringComparison.Ordinal));
		});
	}

	[Fact]
	public async Task ArtworkSourceFromAServerThatSendsNoContentLengthIsDownloaded()
	{
		var artwork = await ReadAssetAsync(artworkAsset);
		using var server = new LoopbackHttpServer(new Dictionary<string, (byte[], string)> { ["/art.png"] = (artwork, "image/png") }, new HashSet<string> { "/art.png" });

		await AssertLoadedAsync(element => element.MetadataArtworkSource = MediaSource.FromUri(server.GetUri("/art.png")), mediaElement =>
		{
			Assert.True(server.HasRequest("GET", "/art.png"));
			Assert.Equal(artwork, GetArtwork(mediaElement));
		});
	}

	[Fact]
	public async Task ArtworkSourceIsReadFromAFile()
	{
		var artwork = await ReadAssetAsync(artworkAsset);
		var path = await CopyAssetToCacheAsync(artworkAsset);

		await AssertLoadedAsync(element => element.MetadataArtworkSource = MediaSource.FromFile(path), mediaElement =>
		{
			Assert.Equal(artwork, GetArtwork(mediaElement));
			// The finally block closes the file again: no file descriptor of the process still points to it.
			Assert.DoesNotContain(GetOpenFileTargets(), target => target.EndsWith(Path.GetFileName(path), StringComparison.Ordinal));
		});
	}

	[Fact]
	public async Task ArtworkSourceIsReadFromAFileGivenAsAFileUri()
	{
		var artwork = await ReadAssetAsync(artworkAsset);
		var fileUri = new Uri(await CopyAssetToCacheAsync(artworkAsset)).AbsoluteUri;

		// The implicit conversion of a "file:" string creates a FileMediaSource whose path is the URI.
		await AssertLoadedAsync(element => element.MetadataArtworkSource = fileUri, mediaElement =>
		{
			Assert.StartsWith("file:///", fileUri, StringComparison.Ordinal);
			Assert.IsType<FileMediaSource>(mediaElement.MetadataArtworkSource);
			Assert.Equal(artwork, GetArtwork(mediaElement));
		});
	}

	[Fact]
	public async Task ArtworkSourceFromAMissingFileIsLeftOutAndTraced()
	{
		using var trace = new TraceCapture();
		var path = Path.Combine(FileSystem.CacheDirectory, $"missing_{Guid.NewGuid():N}.png");

		await AssertLoadedAsync(element => element.MetadataArtworkSource = MediaSource.FromFile(path), mediaElement =>
		{
			Assert.Empty(GetArtwork(mediaElement));
			Assert.Contains(trace.Messages, message => message.StartsWith($"{unableToRetrieveArtworkSource}File: {path}.", StringComparison.Ordinal));
			Assert.Equal(toneDuration, mediaElement.Duration);
		});
	}

	[Fact]
	public async Task ArtworkSourceIsReadFromAResourceCreatedByFromResource()
	{
		var artwork = await ReadAssetAsync(artworkAsset);

		await AssertLoadedAsync(element => element.MetadataArtworkSource = MediaSource.FromResource(artworkAsset), mediaElement =>
		{
			Assert.Equal($"Assets/{artworkAsset}", ((ResourceMediaSource)mediaElement.MetadataArtworkSource!).Path);
			Assert.Equal(artwork, GetArtwork(mediaElement));
		});
	}

	[Theory]
	[InlineData("exoplayer/art.png")]
	[InlineData("exoplayer\\art.png")]
	public async Task ArtworkSourceIsReadFromAResourcePathWithoutTheAssetsPrefix(string resourcePath)
	{
		var artwork = await ReadAssetAsync(artworkAsset);

		await AssertLoadedAsync(element => element.MetadataArtworkSource = new ResourceMediaSource { Path = resourcePath }, mediaElement =>
			Assert.Equal(artwork, GetArtwork(mediaElement)));
	}

	[Fact]
	public async Task ArtworkSourceFromAMissingResourceIsLeftOutAndTraced()
	{
		using var trace = new TraceCapture();

		await AssertLoadedAsync(element => element.MetadataArtworkSource = MediaSource.FromResource("exoplayer/no_such_artwork.png"), mediaElement =>
		{
			Assert.Empty(GetArtwork(mediaElement));
			Assert.Contains(trace.Messages, message => message.StartsWith($"{unableToRetrieveArtworkSource}Resource: Assets/exoplayer/no_such_artwork.png.", StringComparison.Ordinal));
			Assert.Equal(toneDuration, mediaElement.Duration);
		});
	}

	[Fact]
	public async Task ArtworkSourceFromAnEmptyResourceIsLeftOutWithoutAnError()
	{
		using var trace = new TraceCapture();

		await AssertLoadedAsync(element => element.MetadataArtworkSource = MediaSource.FromResource("exoplayer/empty.png"), mediaElement =>
		{
			Assert.Empty(GetArtwork(mediaElement));
			Assert.DoesNotContain(trace.Messages, message => message.Contains("Unable to retrieve", StringComparison.Ordinal));
		});
	}

	[Theory]
	[InlineData("ftp")]
	[InlineData("stream")]
	[InlineData("blankFile")]
	public async Task ArtworkSourceWithAnUnsupportedSchemeOrAStreamIsIgnored(string kind)
	{
		using var trace = new TraceCapture();
		using var stream = new MemoryStream(await ReadAssetAsync(artworkAsset));
		MediaSource unsupportedSource = kind switch
		{
			"ftp" => MediaSource.FromUri("ftp://127.0.0.1/art.png"),
			"stream" => MediaSource.FromStream(stream),
			_ => MediaSource.FromFile("   ")
		};

		await AssertLoadedAsync(element => element.MetadataArtworkSource = unsupportedSource, mediaElement =>
		{
			Assert.Empty(GetArtwork(mediaElement));
			Assert.DoesNotContain(trace.Messages, message => message.Contains("Unable to retrieve", StringComparison.Ordinal));
			Assert.Equal(toneDuration, mediaElement.Duration);
		});
	}

	[Fact]
	public async Task ArtworkSourceReplacesMetadataArtworkUrl()
	{
		var artwork = await ReadAssetAsync(artworkAsset);
		byte[] otherArtwork = [.. artwork, 0];
		var path = await CopyAssetToCacheAsync(artworkAsset);
		using var server = new LoopbackHttpServer(new Dictionary<string, (byte[], string)> { ["/other.png"] = (otherArtwork, "image/png") });

		await AssertLoadedAsync(element =>
		{
			element.MetadataArtworkSource = MediaSource.FromFile(path);
			element.MetadataArtworkUrl = server.GetUri("/other.png").ToString();
		}, mediaElement =>
		{
			Assert.Equal(artwork, GetArtwork(mediaElement));
			Assert.False(server.HasRequest("GET", "/other.png"), "The legacy URL is not read when a source is set.");
		});
	}

	[Fact]
	public async Task WithoutArtworkSourceMetadataArtworkUrlIsStillUsed()
	{
		var artwork = await ReadAssetAsync(artworkAsset);
		var path = await CopyAssetToCacheAsync(artworkAsset);

		await AssertLoadedAsync(element =>
		{
			element.MetadataArtworkUrl = path;
		}, mediaElement =>
		{
			Assert.Null(mediaElement.MetadataArtworkSource);
			Assert.Equal(artwork, GetArtwork(mediaElement));
		});
	}

	[Fact]
	public async Task ArtworkSourceOfAStreamSourceIsLoadedToo()
	{
		var artwork = await ReadAssetAsync(artworkAsset);
		var tone = await ReadAssetAsync(toneAsset);
		var path = await CopyAssetToCacheAsync(artworkAsset);

		await AssertLoadedAsync(element =>
		{
			element.MetadataArtworkSource = MediaSource.FromFile(path);
			element.Source = MediaSource.FromStream(new MemoryStream(tone));
		}, mediaElement => Assert.Equal(artwork, GetArtwork(mediaElement)));
	}

	// Every load has a token source of its own, which the next source cancels, and a load that was replaced applies nothing: a source that
	// waits for a slow artwork cannot replace the source that replaced it.
	[Fact]
	public async Task ChangingTheSourceWhileTheArtworkSourceDownloadsKeepsTheNewSource()
	{
		var artwork = await ReadAssetAsync(artworkAsset);
		using var server = new LoopbackHttpServer(new Dictionary<string, (byte[], string)> { ["/art.png"] = (artwork, "image/png") });
		var heldArtworkResponse = server.HoldResponses("/art.png");
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync(element =>
		{
			element.MetadataArtworkSource = MediaSource.FromUri(server.GetUri("/art.png"));
			element.Source = MediaSource.FromResource(toneAsset);
		});
		using var recorder = new MediaElementRecorder(mediaElement);

		await HostAsync(mediaElement, async _ =>
		{
			await WaitUntilAsync(() => server.HasRequest("GET", "/art.png"), "The artwork of the first source was never requested.");
			Assert.Equal(0, recorder.OpenedCount);

			// The second source has no artwork, so it loads at once.
			mediaElement.MetadataArtworkSource = null;
			mediaElement.Source = MediaSource.FromResource(shortToneAsset);
			await WaitUntilLoadedAsync(mediaElement, recorder);
			Assert.EndsWith(shortToneAsset, GetPlayer(mediaElement).CurrentMediaItem!.MediaId, StringComparison.Ordinal);

			heldArtworkResponse.SetResult();
			await AssertHelpers.Wait(() => recorder.OpenedCount >= 2, timeout: 3000);

			Assert.EndsWith(shortToneAsset, GetPlayer(mediaElement).CurrentMediaItem!.MediaId, StringComparison.Ordinal);
			Assert.Equal(1, recorder.OpenedCount);
		});
	}

	[Theory]
	[InlineData("exoplayer/art.png", "exoplayer/art.png")]
	[InlineData("exoplayer\\art.png", "exoplayer/art.png")]
	[InlineData("Assets/art.png", "Assets/art.png")]
	[InlineData("Assets\\art.png", "Assets/art.png")]
	[InlineData("art.png", "art.png")]
	public void MetadataArtworkUrlOpensTheAssetPathThat17Opened(string metadataArtworkUrl, string expectedAssetPath)
	{
		var artworkSource = MetadataArtworkResolver.FromMetadataArtworkUrl(metadataArtworkUrl);

		var resourceSource = Assert.IsType<ResourceMediaSource>(artworkSource);
		Assert.Equal(expectedAssetPath, MetadataArtworkPaths.GetAndroidAssetPath(resourceSource.Path));
	}

	// 17.0.0 asked the asset manager for "/exoplayer/art.png" and found nothing: only the shared loader trims the slash
	[Fact]
	public async Task MetadataArtworkUrlWithALeadingBackslashOpensTheAsset()
	{
		var artwork = await ReadAssetAsync(artworkAsset);

		await AssertLoadedAsync(element => element.MetadataArtworkUrl = "\\exoplayer\\art.png", mediaElement =>
			Assert.Equal(artwork, GetArtwork(mediaElement)));
	}

	[Fact]
	public async Task MetadataArtworkUrlWithAHashInAnAbsolutePathReadsThatFile()
	{
		var artwork = await ReadAssetAsync(artworkAsset);
		var path = await CopyAssetToCacheAsync(artworkAsset, $"art#1_{Guid.NewGuid():N}");

		await AssertLoadedAsync(element => element.MetadataArtworkUrl = path, mediaElement =>
		{
			Assert.Contains('#', Path.GetFileName(path));
			Assert.Equal(artwork, GetArtwork(mediaElement));
		});
	}

	[Fact]
	public async Task MetadataArtworkUrlFileUriWithAnEscapedHashReadsThatFile()
	{
		var artwork = await ReadAssetAsync(artworkAsset);
		var path = await CopyAssetToCacheAsync(artworkAsset, $"art#1_{Guid.NewGuid():N}");
		var fileUri = new Uri(path).AbsoluteUri;

		await AssertLoadedAsync(element => element.MetadataArtworkUrl = fileUri, mediaElement =>
		{
			Assert.Contains("%23", fileUri, StringComparison.Ordinal);
			Assert.Equal(artwork, GetArtwork(mediaElement));
		});
	}

	[Fact]
	public async Task MetadataArtworkUrlWithABackslashInAnAbsolutePathIsNormalized()
	{
		var artwork = await ReadAssetAsync(artworkAsset);
		var path = await CopyAssetToCacheAsync(artworkAsset);
		var pathWithBackslash = $"{Path.GetDirectoryName(path)}\\{Path.GetFileName(path)}";

		await AssertLoadedAsync(element => element.MetadataArtworkUrl = pathWithBackslash, mediaElement =>
		{
			Assert.Contains('\\', pathWithBackslash);
			Assert.Equal(artwork, GetArtwork(mediaElement));
		});
	}

	[Theory]
	[InlineData("content://media/external/images/media/1")]
	[InlineData("data")]
	[InlineData("ms-appx:///art.png")]
	public async Task MetadataArtworkUrlOfAnotherSchemeIsLeftOutWithoutATrace(string metadataArtworkUrl)
	{
		using var trace = new TraceCapture();
		var artwork = await ReadAssetAsync(artworkAsset);
		var url = metadataArtworkUrl is "data" ? $"data:image/png;base64,{Convert.ToBase64String(artwork)}" : metadataArtworkUrl;

		await AssertLoadedAsync(element => element.MetadataArtworkUrl = url, mediaElement =>
		{
			Assert.Empty(GetArtwork(mediaElement));
			Assert.DoesNotContain(trace.Messages, message => message.Contains("Unable to retrieve", StringComparison.Ordinal));
		});
	}

	[Theory]
	[InlineData("http://")]
	[InlineData("http://[not a host/art.png")]
	public async Task MetadataArtworkUrlThatIsNeitherAbsoluteNorRelativeIsLeftOutWithoutATrace(string metadataArtworkUrl)
	{
		using var trace = new TraceCapture();

		await AssertLoadedAsync(element => element.MetadataArtworkUrl = metadataArtworkUrl, mediaElement =>
		{
			Assert.Empty(GetArtwork(mediaElement));
			Assert.DoesNotContain(trace.Messages, message => message.Contains("Unable to retrieve", StringComparison.Ordinal));
		});
	}

	[Fact]
	public async Task ClearingTheArtworkSourceFallsBackToMetadataArtworkUrl()
	{
		var artwork = await ReadAssetAsync(artworkAsset);
		byte[] legacyArtwork = [.. artwork, 0];
		var path = await CopyAssetToCacheAsync(artworkAsset);
		using var server = new LoopbackHttpServer(new Dictionary<string, (byte[], string)> { ["/legacy.png"] = (legacyArtwork, "image/png") });

		await AssertLoadedAsync(element =>
		{
			element.MetadataArtworkSource = MediaSource.FromFile(path);
			element.MetadataArtworkUrl = server.GetUri("/legacy.png").ToString();
			element.MetadataArtworkSource = null;
		}, mediaElement =>
		{
			Assert.Equal(legacyArtwork, GetArtwork(mediaElement));
			Assert.True(server.HasRequest("GET", "/legacy.png"));
		});
	}

	// 17.0.0 read the whole body inside HttpClient.Timeout and opened the media without artwork when the server stalled halfway
	[Fact]
	public async Task ArtworkThatStallsHalfwayIsGivenUpOnAfterTheTimeoutAndTheMediaOpens()
	{
		var artwork = await ReadAssetAsync(artworkAsset);
		using var server = new LoopbackHttpServer(new Dictionary<string, (byte[], string)> { ["/stalled.png"] = (artwork, "image/png") });
		server.HoldBody("/stalled.png");
		using var trace = new TraceCapture();
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync();
		using var recorder = new MediaElementRecorder(mediaElement);

		await HostAsync(mediaElement, async handler =>
		{
			var client = (HttpClient)typeof(MediaManager).GetField("client", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(handler.Manager)!;
			client.Timeout = TimeSpan.FromSeconds(2);
			var stopwatch = Stopwatch.StartNew();

			mediaElement.MetadataArtworkSource = MediaSource.FromUri(server.GetUri("/stalled.png"));
			mediaElement.Source = MediaSource.FromResource(toneAsset);
			await WaitUntilLoadedAsync(mediaElement, recorder, 10_000);

			Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(10), $"The media opened after {stopwatch.Elapsed}.");
			Assert.True(server.HasRequest("GET", "/stalled.png"));
			Assert.Empty(GetArtwork(mediaElement));
			Assert.Contains(trace.Messages, message => message.StartsWith(unableToRetrieveArtworkSource, StringComparison.Ordinal));
		});
	}

	async Task AssertLoadedAsync(Action<MediaElement> configure, Action<MediaElement> assertWhenLoaded)
	{
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync(element =>
		{
			element.Source = MediaSource.FromResource(toneAsset);
			configure(element);
		});
		using var recorder = new MediaElementRecorder(mediaElement);

		await HostAsync(mediaElement, async _ =>
		{
			await WaitUntilLoadedAsync(mediaElement, recorder);

			assertWhenLoaded(mediaElement);
			Assert.Empty(recorder.FailureMessages);
		});
	}
}
