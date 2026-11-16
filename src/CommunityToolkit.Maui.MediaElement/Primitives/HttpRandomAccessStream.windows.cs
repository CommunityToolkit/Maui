using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Storage.Streams;
using HttpClient = Windows.Web.Http.HttpClient;
using HttpCompletionOption = Windows.Web.Http.HttpCompletionOption;
using HttpMethod = Windows.Web.Http.HttpMethod;
using HttpRequestMessage = Windows.Web.Http.HttpRequestMessage;

namespace CommunityToolkit.Maui.Core;

/// <summary>
/// An <see cref="IRandomAccessStream"/> implementation backed by HTTP Range requests,
/// enabling progressive streaming of media content with custom HTTP headers without buffering the entire file in memory.
/// </summary>
sealed partial class HttpRandomAccessStream : IRandomAccessStream
{
	readonly HttpClient httpClient;
	readonly Uri requestUri;
	readonly ulong size;

	HttpRandomAccessStream(HttpClient httpClient, Uri requestUri, ulong size)
	{
		this.httpClient = httpClient;
		this.requestUri = requestUri;
		this.size = size;
	}

	/// <inheritdoc/>
	public ulong Size
	{
		get => size;
		set => throw new NotSupportedException("Cannot set size on a read-only HTTP stream.");
	}

	/// <inheritdoc/>
	public ulong Position { get; private set; }

	/// <inheritdoc/>
	public bool CanRead => true;

	/// <inheritdoc/>
	public bool CanWrite => false;

	/// <inheritdoc/>
	public IAsyncOperationWithProgress<IBuffer, uint> ReadAsync(IBuffer buffer, uint count, InputStreamOptions options)
	{
		return AsyncInfo.Run<IBuffer, uint>(async (cancellationToken, _) =>
		{
			if (count is 0)
			{
				return buffer;
			}

			using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
			var requestedPosition = Position;

			if (Size > 0 && requestedPosition >= Size)
			{
				buffer.Length = 0;
				return buffer;
			}

			var rangeEnd = requestedPosition + count - 1;

			if (Size > 0 && rangeEnd >= Size)
			{
				rangeEnd = Size - 1;
			}
			request.Headers.TryAppendWithoutValidation("Range", $"bytes={requestedPosition}-{rangeEnd}");

			using var response = await httpClient.SendRequestAsync(request, HttpCompletionOption.ResponseHeadersRead).AsTask(cancellationToken).ConfigureAwait(ConfigureAwaitOptions.ForceYielding);
			response.EnsureSuccessStatusCode();

			if (response.StatusCode is not Windows.Web.Http.HttpStatusCode.PartialContent)
			{
				throw new InvalidOperationException("The server did not honor the HTTP Range request.");
			}
			if (!response.Content.Headers.TryGetValue("Content-Range", out var contentRangeHeader) ||
				!contentRangeHeader.StartsWith($"bytes {requestedPosition}-", StringComparison.OrdinalIgnoreCase))
			{
				throw new InvalidOperationException("The server returned an unexpected Content-Range header.");
			}

			var inputStream = await response.Content.ReadAsInputStreamAsync().AsTask(cancellationToken);
			var result = await inputStream.ReadAsync(buffer, count, options).AsTask(cancellationToken);

			Position += result.Length;
			return result;
		});
	}

	/// <inheritdoc/>
	public void Seek(ulong position) => Position = position;

	/// <inheritdoc/>
	public IRandomAccessStream CloneStream() => throw new NotSupportedException();

	/// <inheritdoc/>
	public IInputStream GetInputStreamAt(ulong position)
	{
		Position = position;
		return this;
	}

	/// <inheritdoc/>
	public IOutputStream GetOutputStreamAt(ulong position) => throw new NotSupportedException();

	/// <inheritdoc/>
	public IAsyncOperationWithProgress<uint, uint> WriteAsync(IBuffer buffer) => throw new NotSupportedException();

	/// <inheritdoc/>
	public IAsyncOperation<bool> FlushAsync() => throw new NotSupportedException();

	/// <inheritdoc/>
	public void Dispose()
	{
		// HttpClient is owned by the caller; do not dispose it here.
	}

	/// <summary>
	/// Creates an <see cref="HttpRandomAccessStream"/> by issuing a HEAD request to determine the content length.
	/// </summary>
	/// <param name="httpClient">The <see cref="HttpClient"/> configured with the desired HTTP headers.</param>
	/// <param name="uri">The URI of the media resource.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
	/// <returns>A new <see cref="HttpRandomAccessStream"/> instance.</returns>
	internal static async Task<HttpRandomAccessStream> CreateAsync(HttpClient httpClient, Uri uri, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(httpClient);
		ArgumentNullException.ThrowIfNull(uri);

		using var request = new HttpRequestMessage(HttpMethod.Head, uri);
		using var response = await httpClient.SendRequestAsync(request).AsTask(cancellationToken).ConfigureAwait(ConfigureAwaitOptions.ForceYielding);
		response.EnsureSuccessStatusCode();

		var contentLength = response.Content.Headers.ContentLength ?? 0;
		return new HttpRandomAccessStream(httpClient, uri, contentLength);
	}
}