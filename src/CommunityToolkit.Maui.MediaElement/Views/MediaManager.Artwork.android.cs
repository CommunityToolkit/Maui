using System.Diagnostics;
using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui.Core.Views;

public partial class MediaManager
{
	Task<byte[]> GetMetadataArtworkBytes(CancellationToken token)
	{
		if (MediaElement.MetadataArtworkSource is MediaSource artworkSource)
		{
			return GetBytesFromMetadataArtworkSource(artworkSource, token);
		}

#pragma warning disable CS0618 // MetadataArtworkUrl stays supported: it is used when MetadataArtworkSource is not set
		return GetBytesFromMetadataArtworkUrl(MediaElement.MetadataArtworkUrl, token);
#pragma warning restore CS0618
	}

	async Task<byte[]> GetBytesFromMetadataArtworkSource(MediaSource artworkSource, CancellationToken token)
	{
		Stream? stream = null;

		try
		{
			switch (artworkSource)
			{
				case UriMediaSource { Uri: Uri uri } when MetadataArtworkPaths.IsHttpUri(uri):
					return await DownloadArtwork(uri, token).ConfigureAwait(false);

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
			Trace.WriteLine($"Unable to retrieve {nameof(MediaElement.MetadataArtworkSource)} from {artworkSource}.{e}\n");
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

	async Task<byte[]> DownloadArtwork(Uri uri, CancellationToken token)
	{
		using var response = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false);
		if (!response.IsSuccessStatusCode)
		{
			return [];
		}

		// A length announced by the server only sizes the buffer at first: a server can announce none (a chunked response) or a wrong one
		var contentLength = response.Content.Headers.ContentLength;
		using var memoryStream = new MemoryStream(contentLength is > 0 and <= int.MaxValue ? (int)contentLength.Value : 0);
		await using var contentStream = await response.Content.ReadAsStreamAsync(token).ConfigureAwait(false);
		await contentStream.CopyToAsync(memoryStream, token).ConfigureAwait(false);
		return memoryStream.ToArray();
	}
}
