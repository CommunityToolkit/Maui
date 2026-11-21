using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui.Core;

static class MetadataArtworkLoader
{
	// Two loads can read the same artwork stream at once (a load of the media and a change of the artwork, or Now Playing and the poster on
	// iOS): each one moves the position of the stream, so they read it one after the other
	static readonly SemaphoreSlim streamReadLock = new(1, 1);

	/// <summary>
	/// Whether <paramref name="artworkStream"/> is the stream that <paramref name="source"/> plays: the player reads that stream itself, so the
	/// artwork must not move its position.
	/// </summary>
	public static bool IsStreamOfTheSource(Stream artworkStream, MediaSource? source) =>
		source is StreamMediaSource { Stream: Stream sourceStream } && ReferenceEquals(sourceStream, artworkStream);

	/// <summary>
	/// Reads the whole artwork stream: from its start when it can seek, leaving its position as it was, else from its position to its end.
	/// </summary>
	public static async Task<byte[]> ReadStreamAsync(Stream stream, CancellationToken token)
	{
		await streamReadLock.WaitAsync(token).ConfigureAwait(false);
		try
		{
			var position = stream.CanSeek ? stream.Position : 0;
			try
			{
				if (stream.CanSeek)
				{
					stream.Position = 0;
				}

				using var memoryStream = new MemoryStream();
				await stream.CopyToAsync(memoryStream, token).ConfigureAwait(false);
				return memoryStream.ToArray();
			}
			finally
			{
				if (stream.CanSeek)
				{
					stream.Position = position;
				}
			}
		}
		finally
		{
			streamReadLock.Release();
		}
	}

	/// <summary>
	/// Reads the whole artwork stream, like <see cref="ReadStreamAsync"/>, on the calling thread.
	/// </summary>
	public static byte[] ReadStream(Stream stream)
	{
		streamReadLock.Wait();
		try
		{
			var position = stream.CanSeek ? stream.Position : 0;
			try
			{
				if (stream.CanSeek)
				{
					stream.Position = 0;
				}

				using var memoryStream = new MemoryStream();
				stream.CopyTo(memoryStream);
				return memoryStream.ToArray();
			}
			finally
			{
				if (stream.CanSeek)
				{
					stream.Position = position;
				}
			}
		}
		finally
		{
			streamReadLock.Release();
		}
	}

	/// <summary>
	/// Downloads the artwork with the <see cref="UriMediaSource.HttpHeaders"/> of its source, as the player sends them for <see cref="MediaElement.Source"/>.
	/// </summary>
	/// <returns>The body, or an empty array when the server does not answer with a success status.</returns>
	/// <exception cref="InvalidOperationException">A header cannot be sent (its name is empty or not valid).</exception>
	public static async Task<byte[]> DownloadAsync(HttpClient client, Uri uri, IEnumerable<KeyValuePair<string, string>> httpHeaders, CancellationToken token)
	{
		using var request = new HttpRequestMessage(HttpMethod.Get, uri);
		AddHttpHeaders(request, httpHeaders);

		// The whole body is read before this returns, so HttpClient.Timeout also ends a server that stalls halfway: the media waits for the artwork
		using var response = await client.SendAsync(request, HttpCompletionOption.ResponseContentRead, token).ConfigureAwait(false);
		if (!response.IsSuccessStatusCode)
		{
			return [];
		}

		return await response.Content.ReadAsByteArrayAsync(token).ConfigureAwait(false);
	}

	/// <summary>
	/// Adds the <see cref="UriMediaSource.HttpHeaders"/> of an artwork source to a request.
	/// </summary>
	/// <exception cref="InvalidOperationException">A header cannot be sent (its name is empty or not valid).</exception>
	public static void AddHttpHeaders(HttpRequestMessage request, IEnumerable<KeyValuePair<string, string>> httpHeaders)
	{
		foreach (var (name, value) in httpHeaders)
		{
			// Same message as the Windows player gives for a header of Source that it cannot send
			if (string.IsNullOrWhiteSpace(name) || !request.Headers.TryAddWithoutValidation(name, value))
			{
				throw new InvalidOperationException($"Failed to append HTTP header '{name}'. The header name may be empty or contain invalid characters.");
			}
		}
	}
}