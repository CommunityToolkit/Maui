using CommunityToolkit.Maui.Core;
using Windows.Storage.Streams;
using Buffer = Windows.Storage.Streams.Buffer;
using HttpClient = Windows.Web.Http.HttpClient;

namespace CommunityToolkit.Maui.DeviceTests;

// Primitives/HttpRandomAccessStream.windows.cs: the read-only IRandomAccessStream over HTTP Range requests. A WinMediaHttpServer on the loopback
// interface serves the bundled clip (and misbehaving variants of it) and records every request, so each branch of ReadAsync and CreateAsync
// is asserted on the bytes returned and on the request the stream really sent. No media is decoded.
[Category(TestCategory.HttpRandomAccessStreamWindows)]
[Collection(TestCollections.DisableParallelization)]
public class HttpRandomAccessStreamWindowsTests : WinMediaTestBase
{
	static readonly byte[] content = WinMediaAssets.ReadVideoBytes();

	static WinMediaResponse ServeContent(WinMediaRequest request) => WinMediaResponse.ServeBytes(request, content, "video/mp4");

	static WinMediaHttpServer StartServer(Func<WinMediaRequest, WinMediaResponse>? handler = null) =>
		new WinMediaHttpServer { Handler = handler ?? ServeContent }.Start();

	static Task<HttpRandomAccessStream> CreateStreamAsync(WinMediaHttpServer server, HttpClient? client = null) =>
		HttpRandomAccessStream.CreateAsync(client ?? new HttpClient(), server.UriFor("video.mp4"));

	static async Task<byte[]> ReadBytesAsync(HttpRandomAccessStream stream, uint count)
	{
		var result = await stream.ReadAsync(new Buffer(count), count, InputStreamOptions.None).AsTask();

		// Not IBuffer.ToArray: for a native buffer that a read filled only in part (Length below Capacity), the CsWinRT projection copies the
		// whole capacity and throws ArgumentException ("Destination is too short"). DataReader reads exactly Length bytes.
		var bytes = new byte[result.Length];
		using var reader = DataReader.FromBuffer(result);
		reader.ReadBytes(bytes);

		return bytes;
	}

	[Fact]
	public async Task CreateAsyncMeasuresTheContentWithAHeadRequest()
	{
		await using var server = StartServer();

		using var stream = await CreateStreamAsync(server);

		Assert.Equal((ulong)content.Length, stream.Size);
		Assert.Equal(0UL, stream.Position);
		Assert.True(stream.CanRead);
		Assert.False(stream.CanWrite);
		var request = Assert.Single(server.Requests);
		Assert.Equal("HEAD", request.Method);
		Assert.Equal("/video.mp4", request.Path);
	}

	[Fact]
	public async Task CreateAsyncSendsTheHeadersOfTheClient()
	{
		await using var server = StartServer();
		var client = new HttpClient();
		Assert.True(client.DefaultRequestHeaders.TryAppendWithoutValidation("X-Device-Test", "7"));

		using var stream = await CreateStreamAsync(server, client);
		await ReadBytesAsync(stream, 8);

		Assert.All(server.Requests, request => Assert.Equal("7", request.Header("X-Device-Test")));
		Assert.Equal(2, server.Requests.Count);
	}

	[Fact]
	public async Task CreateAsyncWithoutAContentLengthReportsASizeOfZero()
	{
		await using var server = StartServer(request => request.Method is "HEAD" ? new WinMediaResponse { OmitContentLength = true } : ServeContent(request));

		using var stream = await CreateStreamAsync(server);

		Assert.Equal(0UL, stream.Size);
	}

	// Windows.Web.Http's EnsureSuccessStatusCode reports the status in the HRESULT: 0x80190000 + the HTTP status code (MS Learn).
	static int HttpStatusHResult(int statusCode) => unchecked((int)(0x80190000u + (uint)statusCode));

	[Theory]
	[InlineData(404)]
	[InlineData(500)]
	public async Task CreateAsyncThrowsForAnErrorStatus(int statusCode)
	{
		await using var server = StartServer(_ => new WinMediaResponse { StatusCode = statusCode, ReasonPhrase = "Test" });

		var exception = await Assert.ThrowsAnyAsync<Exception>(() => CreateStreamAsync(server));

		Assert.Equal(HttpStatusHResult(statusCode), exception.HResult);
		Assert.Equal("HEAD", Assert.Single(server.Requests).Method);
	}

	[Fact]
	public async Task CreateAsyncRejectsNullArguments()
	{
		await using var server = StartServer();

		var noClient = await Assert.ThrowsAsync<ArgumentNullException>(() => HttpRandomAccessStream.CreateAsync(null!, server.UriFor("video.mp4")));
		var noUri = await Assert.ThrowsAsync<ArgumentNullException>(() => HttpRandomAccessStream.CreateAsync(new HttpClient(), null!));

		Assert.Equal("httpClient", noClient.ParamName);
		Assert.Equal("uri", noUri.ParamName);
		Assert.Empty(server.Requests);
	}

	[Fact]
	public async Task CreateAsyncWithACancelledTokenThrowsOperationCanceled()
	{
		await using var server = StartServer();
		using var cancellation = new CancellationTokenSource();
		await cancellation.CancelAsync();

		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => HttpRandomAccessStream.CreateAsync(new HttpClient(), server.UriFor("video.mp4"), cancellation.Token));
	}

	[Fact]
	public async Task ReadAsyncReadsTheRequestedRangesAndAdvancesThePosition()
	{
		await using var server = StartServer();
		using var stream = await CreateStreamAsync(server);

		var first = await ReadBytesAsync(stream, 16);
		Assert.Equal(content[..16], first);
		Assert.Equal(16UL, stream.Position);

		var second = await ReadBytesAsync(stream, 16);
		Assert.Equal(content[16..32], second);
		Assert.Equal(32UL, stream.Position);

		var gets = server.Requests.Where(request => request.Method is "GET").ToList();
		Assert.Equal(["bytes=0-15", "bytes=16-31"], gets.Select(request => request.Header("Range")));
	}

	[Fact]
	public async Task ReadAsyncPastTheEndClampsTheRangeToTheLastByte()
	{
		await using var server = StartServer();
		using var stream = await CreateStreamAsync(server);
		stream.Seek((ulong)content.Length - 4);

		var tail = await ReadBytesAsync(stream, 100);

		Assert.Equal(content[^4..], tail);
		Assert.Equal((ulong)content.Length, stream.Position);
		var request = Assert.Single(server.Requests, request => request.Method is "GET");
		Assert.Equal($"bytes={content.Length - 4}-{content.Length - 1}", request.Header("Range"));
	}

	[Fact]
	public async Task ReadAsyncAtTheEndReturnsAnEmptyBufferWithoutARequest()
	{
		await using var server = StartServer();
		using var stream = await CreateStreamAsync(server);
		stream.Seek((ulong)content.Length);
		var requestsBefore = server.Requests.Count;

		var result = await stream.ReadAsync(new Buffer(16) { Length = 16 }, 16, InputStreamOptions.None).AsTask();

		Assert.Equal(0u, result.Length);
		Assert.Equal(requestsBefore, server.Requests.Count);
		Assert.Equal((ulong)content.Length, stream.Position);
	}

	[Fact]
	public async Task ReadAsyncOfZeroBytesReturnsTheSameBufferWithoutARequest()
	{
		await using var server = StartServer();
		using var stream = await CreateStreamAsync(server);
		var requestsBefore = server.Requests.Count;
		var buffer = new Buffer(8);

		var result = await stream.ReadAsync(buffer, 0, InputStreamOptions.None).AsTask();

		Assert.Same(buffer, result);
		Assert.Equal(requestsBefore, server.Requests.Count);
		Assert.Equal(0UL, stream.Position);
	}

	[Fact]
	public async Task ReadAsyncWithAnUnknownSizeDoesNotClampTheRange()
	{
		await using var server = StartServer(request => request.Method is "HEAD" ? new WinMediaResponse { OmitContentLength = true } : ServeContent(request));
		using var stream = await CreateStreamAsync(server);
		Assert.Equal(0UL, stream.Size);

		var bytes = await ReadBytesAsync(stream, 8);

		// Size == 0 means "unknown": neither the end-of-stream shortcut nor the clamp applies.
		Assert.Equal(content[..8], bytes);
		Assert.Equal(8UL, stream.Position);
		var request = Assert.Single(server.Requests, request => request.Method is "GET");
		Assert.Equal("bytes=0-7", request.Header("Range"));
	}

	[Fact]
	public async Task ReadAsyncThrowsWhenTheServerIgnoresTheRange()
	{
		await using var server = StartServer(request => request.Method is "HEAD" ? ServeContent(request) : new WinMediaResponse { Body = content });
		using var stream = await CreateStreamAsync(server);

		var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => ReadBytesAsync(stream, 16));

		Assert.Contains("did not honor the HTTP Range request", exception.Message, StringComparison.Ordinal);
		Assert.Equal(0UL, stream.Position);
	}

	[Fact]
	public async Task ReadAsyncAcceptsAContentRangeUnitInAnotherCase()
	{
		// HTTP range units are case-insensitive (RFC 9110): "BYTES 0-15/n" answers a request for bytes 0-15.
		await using var server = StartServer(request => request.Method is "HEAD"
			? ServeContent(request)
			: new WinMediaResponse { StatusCode = 206, ReasonPhrase = "Partial Content", Headers = [("Content-Range", $"BYTES 0-15/{content.Length}")], Body = content[..16] });
		using var stream = await CreateStreamAsync(server);

		var bytes = await ReadBytesAsync(stream, 16);

		Assert.Equal(content[..16], bytes);
		Assert.Equal(16UL, stream.Position);
	}

	[Fact]
	public async Task ReadAsyncThrowsWhenAPartialAnswerHasNoContentRange()
	{
		await using var server = StartServer(request => request.Method is "HEAD" ? ServeContent(request) : new WinMediaResponse { StatusCode = 206, ReasonPhrase = "Partial Content", Body = content[..16] });
		using var stream = await CreateStreamAsync(server);

		var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => ReadBytesAsync(stream, 16));

		Assert.Contains("unexpected Content-Range", exception.Message, StringComparison.Ordinal);
	}

	[Fact]
	public async Task ReadAsyncThrowsWhenTheContentRangeStartsAtAnotherPosition()
	{
		await using var server = StartServer(request => request.Method is "HEAD"
			? ServeContent(request)
			: new WinMediaResponse { StatusCode = 206, ReasonPhrase = "Partial Content", Headers = [("Content-Range", $"bytes 5-20/{content.Length}")], Body = content[5..21] });
		using var stream = await CreateStreamAsync(server);

		var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => ReadBytesAsync(stream, 16));

		Assert.Contains("unexpected Content-Range", exception.Message, StringComparison.Ordinal);
	}

	[Fact]
	public async Task ReadAsyncThrowsTheHttpErrorBeforeLookingAtTheRange()
	{
		await using var server = StartServer(request => request.Method is "HEAD" ? ServeContent(request) : WinMediaResponse.ServerError());
		using var stream = await CreateStreamAsync(server);

		var exception = await Assert.ThrowsAnyAsync<Exception>(() => ReadBytesAsync(stream, 16));

		// EnsureSuccessStatusCode comes first: the range checks (InvalidOperationException) never ran.
		Assert.IsNotType<InvalidOperationException>(exception);
		Assert.Equal(HttpStatusHResult(500), exception.HResult);
		Assert.Equal(0UL, stream.Position);
	}

	[Fact]
	public async Task ReadAsyncCanBeCancelledWhileTheRequestIsInFlight()
	{
		await using var server = StartServer(request => request.Method is "HEAD" ? ServeContent(request) : new WinMediaResponse { Delay = TimeSpan.FromSeconds(60), Body = content });
		using var stream = await CreateStreamAsync(server);

		var operation = stream.ReadAsync(new Buffer(16), 16, InputStreamOptions.None);
		var read = operation.AsTask();
		operation.Cancel();

		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => read);
	}

	[Fact]
	public async Task SeekAndGetInputStreamAtMoveThePosition()
	{
		await using var server = StartServer();
		using var stream = await CreateStreamAsync(server);

		stream.Seek(10);
		Assert.Equal(10UL, stream.Position);

		var input = stream.GetInputStreamAt(20);
		Assert.Same(stream, input);
		Assert.Equal(20UL, stream.Position);

		// A read starts where the position is.
		var bytes = await ReadBytesAsync(stream, 4);
		Assert.Equal(content[20..24], bytes);
		Assert.Equal("bytes=20-23", server.Requests.Last(request => request.Method is "GET").Header("Range"));
	}

	[Fact]
	public async Task WriteAndCloneMembersAreNotSupported()
	{
		await using var server = StartServer();
		using var stream = await CreateStreamAsync(server);

		Assert.Throws<NotSupportedException>(() => stream.Size = 1);
		Assert.Throws<NotSupportedException>(() => stream.CloneStream());
		Assert.Throws<NotSupportedException>(() => stream.GetOutputStreamAt(0));
		Assert.Throws<NotSupportedException>(() => stream.WriteAsync(new Buffer(1)));
		Assert.Throws<NotSupportedException>(() => stream.FlushAsync());
		Assert.Equal((ulong)content.Length, stream.Size);
	}

	[Fact]
	public async Task DisposeLeavesTheCallersHttpClientUsable()
	{
		await using var server = StartServer();
		var client = new HttpClient();
		var stream = await CreateStreamAsync(server, client);

		stream.Dispose();

		// The client belongs to the caller (the manager reuses it for the next source): another stream over it must still work.
		using var second = await CreateStreamAsync(server, client);
		Assert.Equal((ulong)content.Length, second.Size);
	}
}