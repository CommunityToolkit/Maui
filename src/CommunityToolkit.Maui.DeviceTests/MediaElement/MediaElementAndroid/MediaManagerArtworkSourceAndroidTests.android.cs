#pragma warning disable CS0618 // These tests pin the obsolete MetadataArtworkUrl, which keeps working
using System.Diagnostics;
using System.Reflection;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Views;
using CommunityToolkit.Maui.Views;
using Microsoft.Extensions.Logging;

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
			AotSafeAssert.SequenceEqual(artwork, GetArtwork(mediaElement));
			Assert.True(server.HasRequest("GET", "/art.png"));
			Assert.False(server.HasRequest("HEAD", "/art.png"), "The typed path takes the content length from the response of its one GET.");
		});
	}

	[Fact]
	public async Task ArtworkSourceThatTheServerAnswersWithAnErrorStatusIsLeftOut()
	{
		using var server = new LoopbackHttpServer(new Dictionary<string, (byte[], string)>());
		var log = new CapturingLoggerFactory();

		await AssertLoadedAsync(log, element => element.MetadataArtworkSource = MediaSource.FromUri(server.GetUri("/missing.png")), mediaElement =>
		{
			AotSafeAssert.Empty(GetArtwork(mediaElement));
			Assert.True(server.HasRequest("GET", "/missing.png"));
			Assert.DoesNotContain(log.Messages, message => message.Contains("Unable to retrieve", StringComparison.Ordinal));
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
			AotSafeAssert.SequenceEqual(artwork, GetArtwork(mediaElement));
		});
	}

	[Fact]
	public async Task ArtworkSourceIsReadFromAFile()
	{
		var artwork = await ReadAssetAsync(artworkAsset);
		var path = await CopyAssetToCacheAsync(artworkAsset);

		await AssertLoadedAsync(element => element.MetadataArtworkSource = MediaSource.FromFile(path), mediaElement =>
		{
			AotSafeAssert.SequenceEqual(artwork, GetArtwork(mediaElement));
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
			AotSafeAssert.SequenceEqual(artwork, GetArtwork(mediaElement));
		});
	}

	[Fact]
	public async Task ArtworkSourceFromAMissingFileIsLeftOutAndLogged()
	{
		var log = new CapturingLoggerFactory();
		var path = Path.Combine(FileSystem.CacheDirectory, $"missing_{Guid.NewGuid():N}.png");

		await AssertLoadedAsync(log, element => element.MetadataArtworkSource = MediaSource.FromFile(path), mediaElement =>
		{
			AotSafeAssert.Empty(GetArtwork(mediaElement));
			Assert.Contains($"{unableToRetrieveArtworkSource}File: {path}", log.Messages);
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
			AotSafeAssert.SequenceEqual(artwork, GetArtwork(mediaElement));
		});
	}

	[Theory]
	[InlineData("exoplayer/art.png")]
	[InlineData("exoplayer\\art.png")]
	public async Task ArtworkSourceIsReadFromAResourcePathWithoutTheAssetsPrefix(string resourcePath)
	{
		var artwork = await ReadAssetAsync(artworkAsset);

		await AssertLoadedAsync(element => element.MetadataArtworkSource = new ResourceMediaSource { Path = resourcePath }, mediaElement =>
			AotSafeAssert.SequenceEqual(artwork, GetArtwork(mediaElement)));
	}

	[Fact]
	public async Task ArtworkSourceFromAMissingResourceIsLeftOutAndLogged()
	{
		var log = new CapturingLoggerFactory();

		await AssertLoadedAsync(log, element => element.MetadataArtworkSource = MediaSource.FromResource("exoplayer/no_such_artwork.png"), mediaElement =>
		{
			AotSafeAssert.Empty(GetArtwork(mediaElement));
			Assert.Contains($"{unableToRetrieveArtworkSource}Resource: Assets/exoplayer/no_such_artwork.png", log.Messages);
			Assert.Equal(toneDuration, mediaElement.Duration);
		});
	}

	[Fact]
	public async Task ArtworkSourceFromAnEmptyResourceIsLeftOutWithoutAnError()
	{
		var log = new CapturingLoggerFactory();

		await AssertLoadedAsync(log, element => element.MetadataArtworkSource = MediaSource.FromResource("exoplayer/empty.png"), mediaElement =>
		{
			AotSafeAssert.Empty(GetArtwork(mediaElement));
			Assert.DoesNotContain(log.Messages, message => message.Contains("Unable to retrieve", StringComparison.Ordinal));
		});
	}

	[Theory]
	[InlineData("ftp")]
	[InlineData("blankFile")]
	public async Task ArtworkSourceWithAnUnsupportedSchemeOrWithoutAFileIsIgnored(string kind)
	{
		var log = new CapturingLoggerFactory();
		MediaSource unsupportedSource = kind switch
		{
			"ftp" => MediaSource.FromUri("ftp://127.0.0.1/art.png"),
			_ => MediaSource.FromFile("   ")
		};

		await AssertLoadedAsync(log, element => element.MetadataArtworkSource = unsupportedSource, mediaElement =>
		{
			AotSafeAssert.Empty(GetArtwork(mediaElement));
			Assert.DoesNotContain(log.Messages, message => message.Contains("Unable to retrieve", StringComparison.Ordinal));
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
			AotSafeAssert.SequenceEqual(artwork, GetArtwork(mediaElement));
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
			AotSafeAssert.SequenceEqual(artwork, GetArtwork(mediaElement));
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
		}, mediaElement => AotSafeAssert.SequenceEqual(artwork, GetArtwork(mediaElement)));
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
			AotSafeAssert.SequenceEqual(artwork, GetArtwork(mediaElement)));
	}

	[Fact]
	public async Task MetadataArtworkUrlWithAHashInAnAbsolutePathReadsThatFile()
	{
		var artwork = await ReadAssetAsync(artworkAsset);
		var path = await CopyAssetToCacheAsync(artworkAsset, $"art#1_{Guid.NewGuid():N}");

		await AssertLoadedAsync(element => element.MetadataArtworkUrl = path, mediaElement =>
		{
			AotSafeAssert.Contains('#', Path.GetFileName(path));
			AotSafeAssert.SequenceEqual(artwork, GetArtwork(mediaElement));
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
			AotSafeAssert.SequenceEqual(artwork, GetArtwork(mediaElement));
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
			AotSafeAssert.Contains('\\', pathWithBackslash);
			AotSafeAssert.SequenceEqual(artwork, GetArtwork(mediaElement));
		});
	}

	[Theory]
	[InlineData("content://media/external/images/media/1")]
	[InlineData("data")]
	[InlineData("ms-appx:///art.png")]
	public async Task MetadataArtworkUrlOfAnotherSchemeIsLeftOutWithoutALog(string metadataArtworkUrl)
	{
		var log = new CapturingLoggerFactory();
		var artwork = await ReadAssetAsync(artworkAsset);
		var url = metadataArtworkUrl is "data" ? $"data:image/png;base64,{Convert.ToBase64String(artwork)}" : metadataArtworkUrl;

		await AssertLoadedAsync(log, element => element.MetadataArtworkUrl = url, mediaElement =>
		{
			AotSafeAssert.Empty(GetArtwork(mediaElement));
			Assert.DoesNotContain(log.Messages, message => message.Contains("Unable to retrieve", StringComparison.Ordinal));
		});
	}

	[Theory]
	[InlineData("http://")]
	[InlineData("http://[not a host/art.png")]
	public async Task MetadataArtworkUrlThatIsNeitherAbsoluteNorRelativeIsLeftOutWithoutALog(string metadataArtworkUrl)
	{
		var log = new CapturingLoggerFactory();

		await AssertLoadedAsync(log, element => element.MetadataArtworkUrl = metadataArtworkUrl, mediaElement =>
		{
			AotSafeAssert.Empty(GetArtwork(mediaElement));
			Assert.DoesNotContain(log.Messages, message => message.Contains("Unable to retrieve", StringComparison.Ordinal));
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
			AotSafeAssert.SequenceEqual(legacyArtwork, GetArtwork(mediaElement));
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
		var log = new CapturingLoggerFactory();
		SetupProbeBuilder(log);
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
			AotSafeAssert.Empty(GetArtwork(mediaElement));
			Assert.Contains(log.Messages, message => message.StartsWith(unableToRetrieveArtworkSource, StringComparison.Ordinal));
		});
	}

	[Fact]
	public async Task ArtworkSourceIsReadFromAStreamWhichKeepsItsPositionAndStaysOpen()
	{
		var artwork = await ReadAssetAsync(artworkAsset);
		var stream = new MemoryStream(artwork) { Position = 5 };

		await AssertLoadedAsync(element => element.MetadataArtworkSource = MediaSource.FromStream(stream), mediaElement =>
		{
			AotSafeAssert.SequenceEqual(artwork, GetArtwork(mediaElement));
			Assert.Equal(5, stream.Position);
			Assert.True(stream.CanRead, "The stream belongs to the app: the artwork does not dispose it.");
		});
	}

	[Fact]
	public async Task ArtworkSourceWithTheStreamOfTheSourceIsLeftOutAndLogged()
	{
		var log = new CapturingLoggerFactory();
		var tone = new MemoryStream(await ReadAssetAsync(toneAsset));

		await AssertLoadedAsync(log, element =>
		{
			element.Source = MediaSource.FromStream(tone);
			element.MetadataArtworkSource = MediaSource.FromStream(tone);
		}, mediaElement =>
		{
			AotSafeAssert.Empty(GetArtwork(mediaElement));
			Assert.Contains(log.Entries, entry => entry.Level is LogLevel.Warning && entry.Message.Contains("its stream is that of Source", StringComparison.Ordinal));
		});
	}

	[Fact]
	public async Task ArtworkSourceIsDownloadedWithTheHttpHeadersOfItsSource()
	{
		var artwork = await ReadAssetAsync(artworkAsset);
		using var server = new LoopbackHttpServer(new Dictionary<string, (byte[], string)> { ["/art.png"] = (artwork, "image/png") });
		var artworkSource = (UriMediaSource)MediaSource.FromUri(server.GetUri("/art.png"));
		artworkSource.HttpHeaders["Authorization"] = "Bearer artwork-token";
		artworkSource.HttpHeaders["X-Artwork"] = "1";

		await AssertLoadedAsync(element => element.MetadataArtworkSource = artworkSource, mediaElement =>
		{
			AotSafeAssert.SequenceEqual(artwork, GetArtwork(mediaElement));
			Assert.Equal("Bearer artwork-token", server.GetRequestHeader("GET", "/art.png", "Authorization"));
			Assert.Equal("1", server.GetRequestHeader("GET", "/art.png", "X-Artwork"));
		});
	}

	[Fact]
	public async Task ChangingTheArtworkSourceUpdatesTheArtworkOfTheLoadedMediaWithoutReloadingIt()
	{
		var artwork = await ReadAssetAsync(artworkAsset);
		byte[] newArtwork = [.. artwork, 0];
		var path = await CopyAssetToCacheAsync(artworkAsset);
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync(element =>
		{
			element.MetadataTitle = "A title";
			element.MetadataArtworkSource = MediaSource.FromFile(path);
			element.Source = MediaSource.FromResource(toneAsset);
		});
		using var recorder = new MediaElementRecorder(mediaElement);

		await HostAsync(mediaElement, async _ =>
		{
			await WaitUntilLoadedAsync(mediaElement, recorder);
			var player = GetPlayer(mediaElement);
			var mediaId = player.CurrentMediaItem!.MediaId;

			mediaElement.MetadataArtworkSource = MediaSource.FromStream(new MemoryStream(newArtwork));
			await WaitUntilAsync(() => GetArtwork(mediaElement).SequenceEqual(newArtwork), "The loaded media never showed the new artwork.");

			Assert.Equal(mediaId, player.CurrentMediaItem!.MediaId);
			Assert.Equal("A title", player.CurrentMediaItem.MediaMetadata!.Title?.ToString());
			Assert.Equal(1, recorder.OpenedCount);
			Assert.Empty(recorder.FailureMessages);
		});
	}

	[Fact]
	public async Task ChangingAPropertyOfTheArtworkSourceUpdatesTheArtworkOfTheLoadedMedia()
	{
		var artwork = await ReadAssetAsync(artworkAsset);
		byte[] newArtwork = [.. artwork, 0];
		var path = await CopyAssetToCacheAsync(artworkAsset);
		var newPath = Path.Combine(FileSystem.CacheDirectory, $"{Guid.NewGuid():N}_new_art.png");
		await File.WriteAllBytesAsync(newPath, newArtwork);
		var artworkSource = (FileMediaSource)MediaSource.FromFile(path);
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync(element =>
		{
			element.MetadataArtworkSource = artworkSource;
			element.Source = MediaSource.FromResource(toneAsset);
		});
		using var recorder = new MediaElementRecorder(mediaElement);

		try
		{
			await HostAsync(mediaElement, async _ =>
			{
				await WaitUntilLoadedAsync(mediaElement, recorder);
				AotSafeAssert.SequenceEqual(artwork, GetArtwork(mediaElement));

				artworkSource.Path = newPath;
				await WaitUntilAsync(() => GetArtwork(mediaElement).SequenceEqual(newArtwork), "The loaded media never showed the artwork of the new path.");
				Assert.Equal(1, recorder.OpenedCount);
			});
		}
		finally
		{
			File.Delete(newPath);
		}
	}

	[Fact]
	public async Task ClearingTheArtworkSourceOfTheLoadedMediaRemovesItsArtwork()
	{
		var path = await CopyAssetToCacheAsync(artworkAsset);
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync(element =>
		{
			element.MetadataArtworkSource = MediaSource.FromFile(path);
			element.Source = MediaSource.FromResource(toneAsset);
		});
		using var recorder = new MediaElementRecorder(mediaElement);

		await HostAsync(mediaElement, async _ =>
		{
			await WaitUntilLoadedAsync(mediaElement, recorder);
			Assert.NotEmpty(GetArtwork(mediaElement));

			mediaElement.MetadataArtworkSource = null;
			await WaitUntilAsync(() => GetArtwork(mediaElement).Length is 0, "The loaded media kept the artwork that was cleared.");
		});
	}

	// The load read the artwork when it started: a change while it downloads is applied once the media has loaded
	[Fact]
	public async Task ChangingTheArtworkSourceWhileTheMediaLoadsIsAppliedOnceItHasLoaded()
	{
		var artwork = await ReadAssetAsync(artworkAsset);
		byte[] newArtwork = [.. artwork, 0];
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
			await WaitUntilAsync(() => server.HasRequest("GET", "/art.png"), "The artwork of the source was never requested.");

			mediaElement.MetadataArtworkSource = MediaSource.FromStream(new MemoryStream(newArtwork));
			heldArtworkResponse.SetResult();
			await WaitUntilLoadedAsync(mediaElement, recorder);

			await WaitUntilAsync(() => GetArtwork(mediaElement).SequenceEqual(newArtwork), "The artwork set while the media loaded was never applied.");
			Assert.Equal(1, recorder.OpenedCount);
		});
	}

	// 17.0.0 read MetadataArtworkUrl when the media loaded only, which it still does
	[Fact]
	public async Task ChangingMetadataArtworkUrlOfTheLoadedMediaKeepsItsArtwork()
	{
		var artwork = await ReadAssetAsync(artworkAsset);
		var path = await CopyAssetToCacheAsync(artworkAsset);
		using var server = new LoopbackHttpServer(new Dictionary<string, (byte[], string)> { ["/other.png"] = ([.. artwork, 0], "image/png") });
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync(element =>
		{
			element.MetadataArtworkUrl = path;
			element.Source = MediaSource.FromResource(toneAsset);
		});
		using var recorder = new MediaElementRecorder(mediaElement);

		await HostAsync(mediaElement, async _ =>
		{
			await WaitUntilLoadedAsync(mediaElement, recorder);

			mediaElement.MetadataArtworkUrl = server.GetUri("/other.png").ToString();
			await Task.Delay(500);

			AotSafeAssert.SequenceEqual(artwork, GetArtwork(mediaElement));
			Assert.False(server.HasRequest("GET", "/other.png"));
		});
	}

	// A change of the artwork that is still loading when the next source loads must not give that source the artwork of the previous change
	[Fact]
	public async Task AnArtworkUpdateThatTheNextSourceReplacedAppliesNothing()
	{
		var artwork = await ReadAssetAsync(artworkAsset);
		byte[] staleArtwork = [.. artwork, 0];
		var path = await CopyAssetToCacheAsync(artworkAsset);
		using var server = new LoopbackHttpServer(new Dictionary<string, (byte[], string)> { ["/stale.png"] = (staleArtwork, "image/png") });
		var heldStaleArtwork = server.HoldResponses("/stale.png");
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync(element => element.Source = MediaSource.FromResource(toneAsset));
		using var recorder = new MediaElementRecorder(mediaElement);

		await HostAsync(mediaElement, async _ =>
		{
			await WaitUntilLoadedAsync(mediaElement, recorder);

			mediaElement.MetadataArtworkSource = MediaSource.FromUri(server.GetUri("/stale.png"));
			await WaitUntilAsync(() => server.HasRequest("GET", "/stale.png"), "The artwork of the change was never requested.");

			mediaElement.MetadataArtworkSource = MediaSource.FromFile(path);
			mediaElement.Source = MediaSource.FromResource(shortToneAsset);
			await WaitUntilAsync(() => recorder.OpenedCount >= 2, "The next source never loaded.");
			AotSafeAssert.SequenceEqual(artwork, GetArtwork(mediaElement));

			heldStaleArtwork.SetResult();
			await Task.Delay(500);

			AotSafeAssert.SequenceEqual(artwork, GetArtwork(mediaElement));
		});
	}

	Task AssertLoadedAsync(Action<MediaElement> configure, Action<MediaElement> assertWhenLoaded) =>
		AssertLoadedAsync(new CapturingLoggerFactory(), configure, assertWhenLoaded);

	async Task AssertLoadedAsync(CapturingLoggerFactory log, Action<MediaElement> configure, Action<MediaElement> assertWhenLoaded)
	{
		SetupProbeBuilder(log);
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