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
#pragma warning disable CS0618 // MetadataArtworkUrl stays supported: the test pins that the source wins over it
			element.MetadataArtworkUrl = server.GetUri("/other.png").ToString();
#pragma warning restore CS0618
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
#pragma warning disable CS0618 // MetadataArtworkUrl stays supported: the test pins that it is used when no source is set
			element.MetadataArtworkUrl = path;
#pragma warning restore CS0618
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
