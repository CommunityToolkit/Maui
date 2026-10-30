using System.Diagnostics;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui.DeviceTests;

[Category(TestCategory.MediaElementAndroid)]
[Collection(TestCollections.DisableParallelization)]
public class MediaManagerMetadataAndroidTests : MediaElementAndroidTestBase
{
	[Fact]
	public async Task TitleAndArtistReachTheMediaItemOfAResourceSource()
	{
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync(element =>
		{
			element.MetadataTitle = "A title";
			element.MetadataArtist = "An artist";
			element.Source = MediaSource.FromResource(toneAsset);
		});
		using var recorder = new MediaElementRecorder(mediaElement);

		await HostAsync(mediaElement, async _ =>
		{
			await WaitUntilLoadedAsync(mediaElement, recorder);

			var metadata = GetPlayer(mediaElement).CurrentMediaItem!.MediaMetadata!;
			Assert.Equal("A title", metadata.Title?.ToString());
			Assert.Equal("An artist", metadata.Artist?.ToString());
		});
	}

	[Fact]
	public async Task TitleAndArtistReachTheMediaItemOfAStreamSource()
	{
		var tone = await ReadAssetAsync(toneAsset);
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync(element =>
		{
			element.MetadataTitle = "A stream title";
			element.MetadataArtist = "A stream artist";
			element.Source = MediaSource.FromStream(new MemoryStream(tone));
		});
		using var recorder = new MediaElementRecorder(mediaElement);

		await HostAsync(mediaElement, async _ =>
		{
			await WaitUntilLoadedAsync(mediaElement, recorder);

			var metadata = GetPlayer(mediaElement).CurrentMediaItem!.MediaMetadata!;
			Assert.Equal("A stream title", metadata.Title?.ToString());
			Assert.Equal("A stream artist", metadata.Artist?.ToString());
			Assert.Empty(GetArtwork(mediaElement));
		});
	}

	[Fact]
	public async Task ArtworkIsDownloadedFromAnHttpUrl()
	{
		var artwork = await ReadAssetAsync(artworkAsset);
		using var server = new LoopbackHttpServer(new Dictionary<string, (byte[], string)> { ["/art.png"] = (artwork, "image/png") });
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync(element =>
		{
			element.MetadataArtworkUrl = server.GetUri("/art.png").ToString();
			element.Source = MediaSource.FromResource(toneAsset);
		});
		using var recorder = new MediaElementRecorder(mediaElement);

		await HostAsync(mediaElement, async _ =>
		{
			await WaitUntilLoadedAsync(mediaElement, recorder);

			Assert.Equal(artwork, GetArtwork(mediaElement));
			Assert.True(server.HasRequest("HEAD", "/art.png"), "The content length is asked with a HEAD request first.");
			Assert.True(server.HasRequest("GET", "/art.png"));
		});
	}

	[Fact]
	public async Task ArtworkThatTheServerAnswersWithAnErrorStatusIsLeftOut()
	{
		using var server = new LoopbackHttpServer(new Dictionary<string, (byte[], string)>());
		using var trace = new TraceCapture();
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync(element =>
		{
			element.MetadataArtworkUrl = server.GetUri("/missing.png").ToString();
			element.Source = MediaSource.FromResource(toneAsset);
		});
		using var recorder = new MediaElementRecorder(mediaElement);

		await HostAsync(mediaElement, async _ =>
		{
			await WaitUntilLoadedAsync(mediaElement, recorder);

			Assert.Empty(GetArtwork(mediaElement));
			Assert.True(server.HasRequest("GET", "/missing.png"));
			// A 404 is not an exception, so nothing is traced.
			Assert.DoesNotContain(trace.Messages, message => message.Contains("Unable to retrieve"));
			Assert.Empty(recorder.FailureMessages);
		});
	}

	[Fact]
	public async Task ArtworkFromAServerThatSendsNoContentLengthIsDownloaded()
	{
		var artwork = await ReadAssetAsync(artworkAsset);
		using var server = new LoopbackHttpServer(new Dictionary<string, (byte[], string)> { ["/art.png"] = (artwork, "image/png") }, new HashSet<string> { "/art.png" });
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync(element =>
		{
			element.MetadataArtworkUrl = server.GetUri("/art.png").ToString();
			element.Source = MediaSource.FromResource(toneAsset);
		});
		using var recorder = new MediaElementRecorder(mediaElement);

		await HostAsync(mediaElement, async _ =>
		{
			await WaitUntilLoadedAsync(mediaElement, recorder);

			Assert.True(server.HasRequest("GET", "/art.png"));
			Assert.Equal(artwork, GetArtwork(mediaElement));
		});
	}

	[Fact]
	public async Task EmptyArtworkFromAServerThatSendsNoContentLengthIsLeftOutWithoutAnError()
	{
		using var server = new LoopbackHttpServer(new Dictionary<string, (byte[], string)> { ["/empty.png"] = ([], "image/png") }, new HashSet<string> { "/empty.png" });
		using var trace = new TraceCapture();
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync(element =>
		{
			element.MetadataArtworkUrl = server.GetUri("/empty.png").ToString();
			element.Source = MediaSource.FromResource(toneAsset);
		});
		using var recorder = new MediaElementRecorder(mediaElement);

		await HostAsync(mediaElement, async _ =>
		{
			await WaitUntilLoadedAsync(mediaElement, recorder);

			Assert.True(server.HasRequest("HEAD", "/empty.png"));
			Assert.True(server.HasRequest("GET", "/empty.png"));
			Assert.Empty(GetArtwork(mediaElement));
			Assert.DoesNotContain(trace.Messages, message => message.Contains("Unable to retrieve"));
		});
	}

	[Fact]
	public async Task ArtworkFromAnHttpsUrlThatCannotBeReachedIsLeftOutAndTraced()
	{
		using var trace = new TraceCapture();
		SetupProbeBuilder();
		const string url = "https://127.0.0.1:1/art.png";
		var mediaElement = await CreateMediaElementAsync(element =>
		{
			element.MetadataArtworkUrl = url;
			element.Source = MediaSource.FromResource(toneAsset);
		});
		using var recorder = new MediaElementRecorder(mediaElement);

		await HostAsync(mediaElement, async _ =>
		{
			await WaitUntilLoadedAsync(mediaElement, recorder);

			Assert.Empty(GetArtwork(mediaElement));
			Assert.Contains(trace.Messages, message => message.StartsWith($"Unable to retrieve {nameof(MediaElement.MetadataArtworkUrl)} for {url}.", StringComparison.Ordinal));
			Assert.Equal(toneDuration, mediaElement.Duration);
		});
	}

	[Fact]
	public async Task ArtworkWithAnUnsupportedSchemeIsLeftOutWithoutAnError()
	{
		using var trace = new TraceCapture();
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync(element =>
		{
			element.MetadataArtworkUrl = "ftp://127.0.0.1/art.png";
			element.Source = MediaSource.FromResource(toneAsset);
		});
		using var recorder = new MediaElementRecorder(mediaElement);

		await HostAsync(mediaElement, async _ =>
		{
			await WaitUntilLoadedAsync(mediaElement, recorder);

			Assert.Empty(GetArtwork(mediaElement));
			Assert.DoesNotContain(trace.Messages, message => message.Contains("Unable to retrieve"));
			Assert.Empty(recorder.FailureMessages);
		});
	}

	[Fact]
	public async Task ArtworkIsReadFromAnAbsoluteFilePath()
	{
		var artwork = await ReadAssetAsync(artworkAsset);
		var path = await CopyAssetToCacheAsync(artworkAsset);
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

			Assert.Equal(artwork, GetArtwork(mediaElement));
			// The finally block closes the file again: no file descriptor of the process still points to it.
			Assert.DoesNotContain(GetOpenFileTargets(), target => target.EndsWith(Path.GetFileName(path), StringComparison.Ordinal));
		});
	}

	[Fact]
	public async Task ArtworkFromAFileUriIsReadFromTheFile()
	{
		var artwork = await ReadAssetAsync(artworkAsset);
		var fileUri = new Uri(await CopyAssetToCacheAsync(artworkAsset)).AbsoluteUri;
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync(element =>
		{
			element.MetadataArtworkUrl = fileUri;
			element.Source = MediaSource.FromResource(toneAsset);
		});
		using var recorder = new MediaElementRecorder(mediaElement);

		await HostAsync(mediaElement, async _ =>
		{
			await WaitUntilLoadedAsync(mediaElement, recorder);

			Assert.StartsWith("file:///", fileUri);
			Assert.Equal(artwork, GetArtwork(mediaElement));
		});
	}

	[Fact]
	public async Task ArtworkFromAMissingFileIsLeftOutAndTraced()
	{
		using var trace = new TraceCapture();
		SetupProbeBuilder();
		var path = Path.Combine(FileSystem.CacheDirectory, $"missing_{Guid.NewGuid():N}.png");
		var mediaElement = await CreateMediaElementAsync(element =>
		{
			element.MetadataArtworkUrl = path;
			element.Source = MediaSource.FromResource(toneAsset);
		});
		using var recorder = new MediaElementRecorder(mediaElement);

		await HostAsync(mediaElement, async _ =>
		{
			await WaitUntilLoadedAsync(mediaElement, recorder);

			Assert.Empty(GetArtwork(mediaElement));
			Assert.Contains(trace.Messages, message => message.StartsWith($"Unable to retrieve {nameof(MediaElement.MetadataArtworkUrl)} for {path}.", StringComparison.Ordinal));
			Assert.Equal(toneDuration, mediaElement.Duration);
		});
	}

	// NormalizeFilePath turns the backslashes into the platform's separator, so both spellings find the asset.
	[Theory]
	[InlineData("exoplayer/art.png")]
	[InlineData("exoplayer\\art.png")]
	public async Task ArtworkFromAnAssetPathHasTheSizeOfTheAsset(string assetPath)
	{
		var artwork = await ReadAssetAsync(artworkAsset);
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync(element =>
		{
			element.MetadataArtworkUrl = assetPath;
			element.Source = MediaSource.FromResource(toneAsset);
		});
		using var recorder = new MediaElementRecorder(mediaElement);

		await HostAsync(mediaElement, async _ =>
		{
			await WaitUntilLoadedAsync(mediaElement, recorder);

			Assert.Equal(artwork.Length, GetArtwork(mediaElement).Length);
		});
	}

	// An asset stream cannot seek, so its length is only known once it has been read: it is copied into a buffer that grows with it, not
	// counted first (which would leave nothing to copy).
	[Fact]
	public async Task ArtworkFromAnAssetPathHasTheContentOfTheAsset()
	{
		var artwork = await ReadAssetAsync(artworkAsset);
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync(element =>
		{
			element.MetadataArtworkUrl = "exoplayer/art.png";
			element.Source = MediaSource.FromResource(toneAsset);
		});
		using var recorder = new MediaElementRecorder(mediaElement);

		await HostAsync(mediaElement, async _ =>
		{
			await WaitUntilLoadedAsync(mediaElement, recorder);

			Assert.Equal(artwork, GetArtwork(mediaElement));
		});
	}

	[Fact]
	public async Task ArtworkFromAnEmptyAssetIsLeftOutWithoutAnError()
	{
		using var trace = new TraceCapture();
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync(element =>
		{
			element.MetadataArtworkUrl = "exoplayer/empty.png";
			element.Source = MediaSource.FromResource(toneAsset);
		});
		using var recorder = new MediaElementRecorder(mediaElement);

		await HostAsync(mediaElement, async _ =>
		{
			await WaitUntilLoadedAsync(mediaElement, recorder);

			Assert.Empty(GetArtwork(mediaElement));
			Assert.DoesNotContain(trace.Messages, message => message.Contains("Unable to retrieve"));
		});
	}

	[Fact]
	public async Task ArtworkFromAMissingAssetIsLeftOutAndTraced()
	{
		using var trace = new TraceCapture();
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync(element =>
		{
			element.MetadataArtworkUrl = "exoplayer/no_such_artwork.png";
			element.Source = MediaSource.FromResource(toneAsset);
		});
		using var recorder = new MediaElementRecorder(mediaElement);

		await HostAsync(mediaElement, async _ =>
		{
			await WaitUntilLoadedAsync(mediaElement, recorder);

			Assert.Empty(GetArtwork(mediaElement));
			Assert.Contains(trace.Messages, message => message.StartsWith($"Unable to retrieve {nameof(MediaElement.MetadataArtworkUrl)} for exoplayer/no_such_artwork.png.", StringComparison.Ordinal));
			Assert.Equal(toneDuration, mediaElement.Duration);
		});
	}

	[Theory]
	[InlineData("")]
	[InlineData("   ")]
	public async Task BlankArtworkUrlMeansNoArtworkAndNothingIsTraced(string url)
	{
		using var trace = new TraceCapture();
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync(element =>
		{
			element.MetadataArtworkUrl = url;
			element.Source = MediaSource.FromResource(toneAsset);
		});
		using var recorder = new MediaElementRecorder(mediaElement);

		await HostAsync(mediaElement, async _ =>
		{
			await WaitUntilLoadedAsync(mediaElement, recorder);

			Assert.Empty(GetArtwork(mediaElement));
			Assert.DoesNotContain(trace.Messages, message => message.Contains("Unable to retrieve"));
		});
	}

	[Fact]
	public async Task ArtworkOfAStreamSourceIsLoadedToo()
	{
		var artwork = await ReadAssetAsync(artworkAsset);
		var tone = await ReadAssetAsync(toneAsset);
		var path = await CopyAssetToCacheAsync(artworkAsset);
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync(element =>
		{
			element.MetadataArtworkUrl = path;
			element.Source = MediaSource.FromStream(new MemoryStream(tone));
		});
		using var recorder = new MediaElementRecorder(mediaElement);

		await HostAsync(mediaElement, async _ =>
		{
			await WaitUntilLoadedAsync(mediaElement, recorder);

			Assert.Equal(artwork, GetArtwork(mediaElement));
		});
	}

	static string[] GetOpenFileTargets() =>
		Directory.GetFiles("/proc/self/fd")
			.Select(descriptor =>
			{
				try
				{
					return new FileInfo(descriptor).LinkTarget;
				}
				catch (IOException)
				{
					// The descriptor was closed while the directory was read.
					return null;
				}
			})
			.OfType<string>()
			.ToArray();

	static byte[] GetArtwork(MediaElement mediaElement) =>
		GetPlayer(mediaElement).CurrentMediaItem!.MediaMetadata!.ArtworkData?.ToArray() ?? [];

	// The manager reports an artwork it could not load with Trace.WriteLine, which is how the failure branch becomes observable.
	sealed class TraceCapture : IDisposable
	{
		readonly CapturingTraceListener listener = new();

		public TraceCapture() => Trace.Listeners.Add(listener);

		public IReadOnlyList<string> Messages => listener.Messages;

		public void Dispose() => Trace.Listeners.Remove(listener);
	}

	sealed class CapturingTraceListener : TraceListener
	{
		readonly object gate = new();
		readonly List<string> messages = [];

		public IReadOnlyList<string> Messages
		{
			get
			{
				lock (gate)
				{
					return messages.ToArray();
				}
			}
		}

		public override void Write(string? message) => Add(message);

		public override void WriteLine(string? message) => Add(message);

		void Add(string? message)
		{
			if (message is null)
			{
				return;
			}

			lock (gate)
			{
				messages.Add(message);
			}
		}
	}
}