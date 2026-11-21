using System.Net;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;
using FluentAssertions;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Views;

public class MediaElementMetadataArtworkLoaderTests : BaseTest
{
	static readonly byte[] artwork = [1, 2, 3, 4, 5, 6];

	[Fact]
	public async Task ReadStreamAsyncReadsASeekableStreamFromItsStartAndLeavesItsPosition()
	{
		using var stream = new MemoryStream(artwork) { Position = 4 };

		var data = await MetadataArtworkLoader.ReadStreamAsync(stream, TestContext.Current.CancellationToken);

		data.Should().Equal(artwork);
		stream.Position.Should().Be(4);
	}

	[Fact]
	public void ReadStreamReadsASeekableStreamFromItsStartAndLeavesItsPosition()
	{
		using var stream = new MemoryStream(artwork) { Position = 4 };

		var data = MetadataArtworkLoader.ReadStream(stream);

		data.Should().Equal(artwork);
		stream.Position.Should().Be(4);
	}

	[Fact]
	public async Task ReadStreamAsyncReadsAStreamThatCannotSeekFromItsPosition()
	{
		using var inner = new MemoryStream(artwork) { Position = 2 };
		using var stream = new UnseekableStream(inner);

		var data = await MetadataArtworkLoader.ReadStreamAsync(stream, TestContext.Current.CancellationToken);

		data.Should().Equal(artwork[2..]);
	}

	[Fact]
	public void ReadStreamReadsAStreamThatCannotSeekFromItsPosition()
	{
		using var inner = new MemoryStream(artwork) { Position = 2 };
		using var stream = new UnseekableStream(inner);

		MetadataArtworkLoader.ReadStream(stream).Should().Equal(artwork[2..]);
	}

	[Fact]
	public async Task ReadStreamAsyncLeavesThePositionOfAStreamThatFails()
	{
		using var stream = new FailingReadStream(artwork) { Position = 3 };

		var read = () => MetadataArtworkLoader.ReadStreamAsync(stream, TestContext.Current.CancellationToken);

		await read.Should().ThrowAsync<IOException>();
		stream.Position.Should().Be(3);
	}

	[Fact]
	public async Task ReadStreamAsyncCanReadAgainAfterItWasCancelled()
	{
		using var stream = new MemoryStream(artwork);
		using var cancelled = new CancellationTokenSource();
		await cancelled.CancelAsync();

		var cancelledRead = () => MetadataArtworkLoader.ReadStreamAsync(stream, cancelled.Token);

		await cancelledRead.Should().ThrowAsync<OperationCanceledException>();
		(await MetadataArtworkLoader.ReadStreamAsync(stream, TestContext.Current.CancellationToken)).Should().Equal(artwork);
	}

	[Fact]
	public void IsStreamOfTheSourceIsTrueOnlyForTheStreamThatTheSourcePlays()
	{
		using var played = new MemoryStream(artwork);
		using var other = new MemoryStream(artwork);

		MetadataArtworkLoader.IsStreamOfTheSource(played, MediaSource.FromStream(played)).Should().BeTrue();
		MetadataArtworkLoader.IsStreamOfTheSource(other, MediaSource.FromStream(played)).Should().BeFalse();
		MetadataArtworkLoader.IsStreamOfTheSource(played, MediaSource.FromUri("https://www.example.com/video.mp4")).Should().BeFalse();
		MetadataArtworkLoader.IsStreamOfTheSource(played, null).Should().BeFalse();
	}

	[Fact]
	public async Task DownloadAsyncSendsTheHttpHeadersOfTheSource()
	{
		var handler = new RecordingHandler(HttpStatusCode.OK, artwork);
		using var client = new HttpClient(handler);
		var source = (UriMediaSource)MediaSource.FromUri("https://www.example.com/art.png");
		source.HttpHeaders["Authorization"] = "Bearer token";
		source.HttpHeaders["X-Custom"] = "value";

		var data = await MetadataArtworkLoader.DownloadAsync(client, source.Uri!, source.HttpHeaders, TestContext.Current.CancellationToken);

		data.Should().Equal(artwork);
		handler.Request.Should().NotBeNull();
		handler.Request!.Method.Should().Be(HttpMethod.Get);
		handler.Request.RequestUri.Should().Be(source.Uri);
		handler.Request.Headers.GetValues("Authorization").Should().Equal("Bearer token");
		handler.Request.Headers.GetValues("X-Custom").Should().Equal("value");
	}

	[Fact]
	public async Task DownloadAsyncWithoutHeadersSendsNone()
	{
		var handler = new RecordingHandler(HttpStatusCode.OK, artwork);
		using var client = new HttpClient(handler);

		await MetadataArtworkLoader.DownloadAsync(client, new Uri("https://www.example.com/art.png"), new Dictionary<string, string>(), TestContext.Current.CancellationToken);

		handler.Request!.Headers.Should().BeEmpty();
	}

	[Theory]
	[InlineData(HttpStatusCode.NotFound)]
	[InlineData(HttpStatusCode.Unauthorized)]
	[InlineData(HttpStatusCode.InternalServerError)]
	public async Task DownloadAsyncOfAFailedResponseHasNoData(HttpStatusCode statusCode)
	{
		using var client = new HttpClient(new RecordingHandler(statusCode, artwork));

		var data = await MetadataArtworkLoader.DownloadAsync(client, new Uri("https://www.example.com/art.png"), new Dictionary<string, string>(), TestContext.Current.CancellationToken);

		data.Should().BeEmpty();
	}

	[Theory]
	[InlineData("")]
	[InlineData(" ")]
	[InlineData("Bad Header")]
	public async Task DownloadAsyncThrowsForAHeaderThatCannotBeSent(string headerName)
	{
		var handler = new RecordingHandler(HttpStatusCode.OK, artwork);
		using var client = new HttpClient(handler);

		var download = () => MetadataArtworkLoader.DownloadAsync(client, new Uri("https://www.example.com/art.png"),
			new Dictionary<string, string> { [headerName] = "value" }, TestContext.Current.CancellationToken);

		await download.Should().ThrowAsync<InvalidOperationException>();
		handler.Request.Should().BeNull();
	}

	sealed class RecordingHandler(HttpStatusCode statusCode, byte[] body) : HttpMessageHandler
	{
		public HttpRequestMessage? Request { get; private set; }

		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			Request = request;
			return Task.FromResult(new HttpResponseMessage(statusCode) { Content = new ByteArrayContent(body) });
		}
	}

	sealed class UnseekableStream(Stream inner) : Stream
	{
		public override bool CanRead => true;
		public override bool CanSeek => false;
		public override bool CanWrite => false;
		public override long Length => throw new NotSupportedException();
		public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
		public override void Flush() { }
		public override int Read(byte[] buffer, int offset, int count) => inner.Read(buffer, offset, count);
		public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
		public override void SetLength(long value) => throw new NotSupportedException();
		public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
	}

	sealed class FailingReadStream(byte[] data) : MemoryStream(data)
	{
		public override int Read(byte[] buffer, int offset, int count) => throw new IOException("The stream cannot be read.");

		public override int Read(Span<byte> buffer) => throw new IOException("The stream cannot be read.");

		public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => throw new IOException("The stream cannot be read.");

		public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) => throw new IOException("The stream cannot be read.");

		public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken) => throw new IOException("The stream cannot be read.");
	}
}