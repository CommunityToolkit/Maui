using System.Net;
using CommunityToolkit.Maui.Extensions;
using FluentAssertions;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Extensions;

public class HttpClientExtensionsTests : BaseTest
{
	static readonly Uri avatarUri = new("https://www.gravatar.com/avatar/b65a519785f69fbe7236dd0fd6396094?s=80&d=mp");

	[Fact]
	public async Task DownloadStreamAsync_NullClient_ThrowsArgumentNullException()
	{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		var exception = await Assert.ThrowsAsync<ArgumentNullException>(() => HttpClientExtensions.DownloadStreamAsync(null, avatarUri, TestContext.Current.CancellationToken));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

		exception.ParamName.Should().Be("client");
	}

	[Fact]
	public async Task DownloadStreamAsync_NullUri_ThrowsArgumentNullException()
	{
		using var client = new HttpClient(RespondWith(new ServerStream([])));

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		var exception = await Assert.ThrowsAsync<ArgumentNullException>(() => client.DownloadStreamAsync(null, TestContext.Current.CancellationToken));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

		exception.ParamName.Should().Be("uri");
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task DownloadStreamAsync_ServerStreamCannotSeek_ReturnsSeekableStreamWithEveryByte()
	{
		var body = CreateBody(length: 20_000);
		var serverStream = new ServerStream(body, maxBytesPerRead: 997);
		using var client = new HttpClient(RespondWith(serverStream));

		await using var stream = await client.DownloadStreamAsync(avatarUri, TestContext.Current.CancellationToken);

		serverStream.CanSeek.Should().BeFalse();
		stream.CanRead.Should().BeTrue();
		stream.CanSeek.Should().BeTrue();
		stream.Position.Should().Be(0);
		stream.Length.Should().Be(body.Length);
		(await ReadToEndAsync(stream)).Should().Equal(body);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task DownloadStreamAsync_ReturnedStream_CanBeReadAgainAfterSeekingToTheStart()
	{
		var body = CreateBody(length: 5_000);
		using var client = new HttpClient(RespondWith(new ServerStream(body, maxBytesPerRead: 512)));
		await using var stream = await client.DownloadStreamAsync(avatarUri, TestContext.Current.CancellationToken);
		await ReadToEndAsync(stream);

		stream.Seek(0, SeekOrigin.Begin);

		(await ReadToEndAsync(stream)).Should().Equal(body);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task DownloadStreamAsync_Success_ReleasesTheServerStreamAndTheReturnedStreamStaysReadable()
	{
		var body = CreateBody(length: 3_000);
		var serverStream = new ServerStream(body, maxBytesPerRead: 400);
		using var client = new HttpClient(RespondWith(serverStream));

		await using var stream = await client.DownloadStreamAsync(avatarUri, TestContext.Current.CancellationToken);

		serverStream.IsDisposed.Should().BeTrue();
		(await ReadToEndAsync(stream)).Should().Equal(body);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task DownloadStreamAsync_EmptyBody_ReturnsEmptySeekableStream()
	{
		using var client = new HttpClient(RespondWith(new ServerStream([])));

		await using var stream = await client.DownloadStreamAsync(avatarUri, TestContext.Current.CancellationToken);

		stream.Should().NotBeSameAs(Stream.Null);
		stream.CanSeek.Should().BeTrue();
		stream.Length.Should().Be(0);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task DownloadStreamAsync_RequestsTheUriWithGet()
	{
		var handler = RespondWith(new ServerStream(CreateBody(length: 10)));
		using var client = new HttpClient(handler);

		await using var stream = await client.DownloadStreamAsync(avatarUri, TestContext.Current.CancellationToken);

		handler.RequestUri.Should().Be(avatarUri);
		handler.Method.Should().Be(HttpMethod.Get);
	}

	[Theory(Timeout = (int)TestDuration.Short)]
	[InlineData(HttpStatusCode.NotFound)]
	[InlineData(HttpStatusCode.Forbidden)]
	[InlineData(HttpStatusCode.InternalServerError)]
	public async Task DownloadStreamAsync_ErrorStatusCode_ReturnsStreamNullWithoutDownloadingTheBody(HttpStatusCode statusCode)
	{
		var serverStream = new ServerStream(CreateBody(length: 1_000));
		using var client = new HttpClient(RespondWith(serverStream, statusCode));

		var stream = await client.DownloadStreamAsync(avatarUri, TestContext.Current.CancellationToken);

		stream.Should().BeSameAs(Stream.Null);
		serverStream.ReadCount.Should().Be(0);
		serverStream.IsDisposed.Should().BeTrue();
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task DownloadStreamAsync_HeadersDoNotArriveWithinTheTimeout_ReturnsStreamNull()
	{
		var handler = new StubHttpMessageHandler(WaitUntilCanceledAsync);
		using var client = new HttpClient(handler) { Timeout = TimeSpan.FromMilliseconds(100) };

		var stream = await client.DownloadStreamAsync(avatarUri, TestContext.Current.CancellationToken);

		stream.Should().BeSameAs(Stream.Null);
		handler.WasCanceled.Should().BeTrue();
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task DownloadStreamAsync_CanceledWhileWaitingForTheHeaders_ReturnsStreamNull()
	{
		using var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
		var handler = new StubHttpMessageHandler(WaitUntilCanceledAsync);
		using var client = new HttpClient(handler);
		var download = client.DownloadStreamAsync(avatarUri, cancellationTokenSource.Token);
		await handler.RequestStarted.WaitAsync(TestContext.Current.CancellationToken);

		await cancellationTokenSource.CancelAsync();

		(await download).Should().BeSameAs(Stream.Null);
		handler.WasCanceled.Should().BeTrue();
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task DownloadStreamAsync_CanceledWhileTheBodyIsDownloading_ReturnsStreamNullAndReleasesTheServerStream()
	{
		using var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
		var serverStream = new ServerStream(CreateBody(length: 1_000), maxBytesPerRead: 100, blockAfterBytes: 300);
		using var client = new HttpClient(RespondWith(serverStream));
		var download = client.DownloadStreamAsync(avatarUri, cancellationTokenSource.Token);
		await serverStream.ReadIsBlocked.WaitAsync(TestContext.Current.CancellationToken);

		await cancellationTokenSource.CancelAsync();

		(await download).Should().BeSameAs(Stream.Null);
		serverStream.IsDisposed.Should().BeTrue();
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task DownloadStreamAsync_TokenIsAlreadyCanceled_ReturnsStreamNull()
	{
		using var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
		await cancellationTokenSource.CancelAsync();
		using var client = new HttpClient(new StubHttpMessageHandler(WaitUntilCanceledAsync));

		var stream = await client.DownloadStreamAsync(avatarUri, cancellationTokenSource.Token);

		stream.Should().BeSameAs(Stream.Null);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task DownloadStreamAsync_RequestFails_ReturnsStreamNull()
	{
		using var client = new HttpClient(new StubHttpMessageHandler((_, _) => throw new HttpRequestException("The name could not be resolved.")));

		var stream = await client.DownloadStreamAsync(avatarUri, TestContext.Current.CancellationToken);

		stream.Should().BeSameAs(Stream.Null);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task DownloadStreamAsync_ConnectionIsLostWhileTheBodyIsDownloading_ReturnsStreamNullInsteadOfATruncatedBody()
	{
		var serverStream = new ServerStream(CreateBody(length: 1_000), maxBytesPerRead: 100, failAfterBytes: 300);
		using var client = new HttpClient(RespondWith(serverStream));

		var stream = await client.DownloadStreamAsync(avatarUri, TestContext.Current.CancellationToken);

		stream.Should().BeSameAs(Stream.Null);
		serverStream.IsDisposed.Should().BeTrue();
	}

	static StubHttpMessageHandler RespondWith(ServerStream body, HttpStatusCode statusCode = HttpStatusCode.OK) =>
		new((_, _) => Task.FromResult(new HttpResponseMessage(statusCode) { Content = new StreamContent(body) }));

	static async Task<HttpResponseMessage> WaitUntilCanceledAsync(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
		throw new InvalidOperationException("The wait for the response only ends when the request is canceled.");
	}

	static byte[] CreateBody(int length)
	{
		var body = new byte[length];
		for (var index = 0; index < body.Length; index++)
		{
			// 251 is prime, so the pattern never lines up with the chunks a test reads.
			body[index] = (byte)(index % 251);
		}

		return body;
	}

	static async Task<byte[]> ReadToEndAsync(Stream stream)
	{
		using var copy = new MemoryStream();
		await stream.CopyToAsync(copy, TestContext.Current.CancellationToken);
		return copy.ToArray();
	}

	sealed class StubHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
	{
		readonly TaskCompletionSource requestStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);

		public Task RequestStarted => requestStarted.Task;

		public Uri? RequestUri { get; private set; }

		public HttpMethod? Method { get; private set; }

		public bool WasCanceled { get; private set; }

		protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			RequestUri = request.RequestUri;
			Method = request.Method;
			requestStarted.TrySetResult();

			try
			{
				return await send(request, cancellationToken);
			}
			catch (OperationCanceledException)
			{
				WasCanceled = true;
				throw;
			}
		}
	}

	// The body of a response that is still arriving: it cannot seek, hands out small chunks and, like a network stream, is read asynchronously.
	sealed class ServerStream(byte[] content, int maxBytesPerRead = int.MaxValue, int blockAfterBytes = int.MaxValue, int failAfterBytes = int.MaxValue) : Stream
	{
		readonly TaskCompletionSource readIsBlocked = new(TaskCreationOptions.RunContinuationsAsynchronously);

		int position;

		public Task ReadIsBlocked => readIsBlocked.Task;

		public bool IsDisposed { get; private set; }

		public int ReadCount { get; private set; }

		public override bool CanRead => !IsDisposed;

		public override bool CanSeek => false;

		public override bool CanWrite => false;

		public override long Length => throw new NotSupportedException();

		public override long Position
		{
			get => throw new NotSupportedException();
			set => throw new NotSupportedException();
		}

		public override void Flush()
		{
		}

		public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException("The server stream is read asynchronously.");

		public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
		{
			cancellationToken.ThrowIfCancellationRequested();
			ReadCount++;

			if (position >= failAfterBytes)
			{
				throw new IOException("The connection was closed before the whole body arrived.");
			}

			if (position >= blockAfterBytes)
			{
				readIsBlocked.TrySetResult();
				await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
			}

			var end = Math.Min(content.Length, Math.Min(blockAfterBytes, failAfterBytes));
			var count = Math.Min(Math.Min(buffer.Length, maxBytesPerRead), end - position);
			content.AsMemory(position, count).CopyTo(buffer);
			position += count;

			return count;
		}

		public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

		public override void SetLength(long value) => throw new NotSupportedException();

		public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

		protected override void Dispose(bool disposing)
		{
			IsDisposed = true;
			base.Dispose(disposing);
		}
	}
}