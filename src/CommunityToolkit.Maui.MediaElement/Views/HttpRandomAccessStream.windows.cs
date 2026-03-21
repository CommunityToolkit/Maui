using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Storage.Streams;
using HttpClient = Windows.Web.Http.HttpClient;
using HttpRequestMessage = Windows.Web.Http.HttpRequestMessage;
using HttpMethod = Windows.Web.Http.HttpMethod;
using HttpCompletionOption = Windows.Web.Http.HttpCompletionOption;

namespace CommunityToolkit.Maui.Core.Views;

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
		using var response = await httpClient.SendRequestAsync(request).AsTask(cancellationToken).ConfigureAwait(false);
		response.EnsureSuccessStatusCode();

		var contentLength = response.Content.Headers.ContentLength ?? 0;
		return new HttpRandomAccessStream(httpClient, uri, contentLength);
	}

	/// <inheritdoc/>
	public IAsyncOperationWithProgress<IBuffer, uint> ReadAsync(IBuffer buffer, uint count, InputStreamOptions options)
	{
		return AsyncInfo.Run<IBuffer, uint>(async (cancellationToken, _) =>
		{
			using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
			var rangeEnd = Position + count - 1;
			request.Headers.TryAppendWithoutValidation("Range", $"bytes={Position}-{rangeEnd}");

			using var response = await httpClient.SendRequestAsync(request, HttpCompletionOption.ResponseHeadersRead).AsTask(cancellationToken).ConfigureAwait(false);
			response.EnsureSuccessStatusCode();

			var inputStream = await response.Content.ReadAsInputStreamAsync().AsTask(cancellationToken).ConfigureAwait(false);
			var result = await inputStream.ReadAsync(buffer, count, options).AsTask(cancellationToken).ConfigureAwait(false);

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
}
