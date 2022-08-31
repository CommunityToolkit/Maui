namespace CommunityToolkit.Maui.Extensions;

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

/// <summary>HttpClient extensions.</summary>
public static partial class HttpClientExtensions
{
	/// <summary>Get stream from uri.</summary>
	/// <param name="client">this HttpClient.</param>
	/// <param name="uri">Target uri.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Task <see cref="Task{TResult}"/> result.</returns>
	public static async Task<Stream> DownloadStreamAsync(this HttpClient client, Uri? uri, CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(client, nameof(client));
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