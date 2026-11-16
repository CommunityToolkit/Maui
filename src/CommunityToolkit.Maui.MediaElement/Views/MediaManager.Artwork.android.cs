using System.Diagnostics;
using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui.Core.Views;

public partial class MediaManager
{
	Task<byte[]> GetMetadataArtworkBytes(CancellationToken token)
	{
		var (artworkSource, metadataArtworkUrl) = MetadataArtworkResolver.Resolve(MediaElement);
		if (artworkSource is null)
		{
			return Task.FromResult<byte[]>([]);
		}

		return GetBytesFromMetadataArtworkSource(artworkSource, metadataArtworkUrl, token);
	}

	async Task<byte[]> GetBytesFromMetadataArtworkSource(MediaSource artworkSource, string? metadataArtworkUrl, CancellationToken token)
	{
		Stream? stream = null;

		try
		{
			switch (artworkSource)
			{
				case UriMediaSource { Uri: Uri uri } when MetadataArtworkPaths.IsHttpUri(uri):
					return await DownloadArtwork(uri, sendContentLengthRequest: metadataArtworkUrl is not null, token).ConfigureAwait(false);

				case UriMediaSource { Uri: Uri uri } when uri.IsAbsoluteUri && uri.IsFile:
					stream = File.OpenRead(uri.LocalPath);
					break;

				case FileMediaSource fileSource when MetadataArtworkPaths.GetLocalFilePath(fileSource.Path) is string localFilePath:
					stream = File.OpenRead(localFilePath);
					break;

				case ResourceMediaSource resourceSource when MetadataArtworkPaths.GetAndroidAssetPath(resourceSource.Path) is string assetPath:
					stream = Platform.AppContext.Assets?.Open(assetPath) ?? throw new InvalidOperationException("Assets cannot be null");
					break;

				default:
					return [];
			}

			// An asset stream cannot seek, so its length is only known once it has been read
			using var memoryStream = new MemoryStream();
			await stream.CopyToAsync(memoryStream, token).ConfigureAwait(false);
			return memoryStream.ToArray();
		}
		catch (Exception e)
		{
			Trace.WriteLine($"Unable to retrieve {GetArtworkDescription(artworkSource, metadataArtworkUrl)}.{e}\n");
			return [];
		}
		finally
		{
			if (stream is not null)
			{
				await stream.DisposeAsync().ConfigureAwait(false);
			}
		}
	}

	// A MetadataArtworkUrl keeps the trace text of 17.0.0, which names the string the app set
	static string GetArtworkDescription(MediaSource artworkSource, string? metadataArtworkUrl) =>
		metadataArtworkUrl is null
			? $"{nameof(MediaElement.MetadataArtworkSource)} from {artworkSource}"
#pragma warning disable CS0618 // MetadataArtworkUrl stays supported: its trace text names it
			: $"{nameof(MediaElement.MetadataArtworkUrl)} for {metadataArtworkUrl}";
#pragma warning restore CS0618

	async Task<byte[]> DownloadArtwork(Uri uri, bool sendContentLengthRequest, CancellationToken token)
	{
		// 17.0.0 asked for the content length with a HEAD request before the GET of a MetadataArtworkUrl, and a failing HEAD left the artwork out
		if (sendContentLengthRequest)
		{
			using var contentLengthRequest = new HttpRequestMessage(HttpMethod.Head, uri);
			using var contentLengthResponse = await client.SendAsync(contentLengthRequest, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false);
		}

		// The whole body is read before this returns, so HttpClient.Timeout also ends a server that stalls halfway: the media item waits for the artwork
		using var response = await client.GetAsync(uri, HttpCompletionOption.ResponseContentRead, token).ConfigureAwait(false);
		if (!response.IsSuccessStatusCode)
		{
			return [];
		}

		return await response.Content.ReadAsByteArrayAsync(token).ConfigureAwait(false);
	}
}
