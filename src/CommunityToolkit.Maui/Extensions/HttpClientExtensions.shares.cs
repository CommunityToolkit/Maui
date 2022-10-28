using System.Diagnostics;

namespace CommunityToolkit.Maui.Extensions;

/// <summary>HttpClient extensions.</summary>
static partial class HttpClientExtensions
{
	/// <summary>Get stream from Uri.</summary>
	/// <param name="client"><see href="HttpClient" />.</param>
	/// <param name="uri">Target Uri.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Task <see cref="Task{TResult}"/> result.</returns>
	public static async Task<Stream> DownloadStreamAsync(this HttpClient client, Uri uri, CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(client);
		ArgumentNullException.ThrowIfNull(uri);

		try
		{
			return await StreamWrapper.GetStreamAsync(uri, cancellationToken, client).ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"Error getting stream for {uri}: {ex}");
			return Stream.Null;
		}
	}
}