using System.Diagnostics;

namespace CommunityToolkit.Maui.Extensions;

/// <summary>HttpClient extensions.</summary>
static partial class HttpClientExtensions
{
	/// <summary>Downloads the response body of a Uri into a stream.</summary>
	/// <param name="client"><see href="HttpClient" />.</param>
	/// <param name="uri">Target Uri.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>
	/// A readable, seekable stream that holds the whole response body, positioned at its start.
	/// <see cref="Stream.Null"/> when the response headers do not arrive within <see cref="HttpClient.Timeout"/>,
	/// the response has an error status code, the request is canceled, or the download fails.
	/// </returns>
	public static async Task<Stream> DownloadStreamAsync(this HttpClient client, Uri uri, CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(client);
		ArgumentNullException.ThrowIfNull(uri);

		try
		{
			// Headers first: HttpClient.Timeout covers the wait for the response, and the body of an error response is never downloaded.
			using var response = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
			if (!response.IsSuccessStatusCode)
			{
				Trace.WriteLine($"Could not retrieve {uri}, status code {response.StatusCode}");
				return Stream.Null;
			}

			// Not the network stream of the response: .NET MAUI on Windows decodes an image from a stream through AsRandomAccessStream,
			// which throws for a stream that cannot seek.
			var content = await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
			return new MemoryStream(content, writable: false);
		}
		catch (Exception ex)
		{
			Trace.WriteLine($"Error getting stream for {uri}: {ex}");
			return Stream.Null;
		}
	}
}