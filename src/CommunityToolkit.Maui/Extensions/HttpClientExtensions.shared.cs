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
	/// <see cref="Stream.Null"/> when the whole response does not arrive within <see cref="HttpClient.Timeout"/>,
	/// the response has an error status code, or the download fails.
	/// </returns>
	/// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> is canceled.</exception>
	public static async Task<Stream> DownloadStreamAsync(this HttpClient client, Uri uri, CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(client);
		ArgumentNullException.ThrowIfNull(uri);

		try
		{
			// With ResponseHeadersRead, HttpClient.Timeout ends once the headers are in: this deadline keeps it covering the body that is read here.
			using var downloadTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
			downloadTimeout.CancelAfter(client.Timeout);

			// Headers first, so that the body of an error response is never downloaded.
			using var response = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, downloadTimeout.Token).ConfigureAwait(false);
			if (!response.IsSuccessStatusCode)
			{
				Trace.WriteLine($"Could not retrieve {uri}, status code {response.StatusCode}");
				return Stream.Null;
			}

			// Not the network stream of the response: .NET MAUI on Windows decodes an image from a stream through AsRandomAccessStream,
			// which throws for a stream that cannot seek.
			var content = await response.Content.ReadAsByteArrayAsync(downloadTimeout.Token).ConfigureAwait(false);
			return new MemoryStream(content, writable: false);
		}
		// A canceled load must stay a cancellation: .NET MAUI clears the picture of a view when a load fails, even a load that a newer one replaced.
		catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
		{
			throw;
		}
		catch (Exception ex)
		{
			Trace.WriteLine($"Error getting stream for {uri}: {ex}");
			return Stream.Null;
		}
	}
}