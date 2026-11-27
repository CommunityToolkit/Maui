using AndroidX.Media3.Common;
using CommunityToolkit.Maui.Views;
using Microsoft.Extensions.Logging;
using MediaMetadata = AndroidX.Media3.Common.MediaMetadata;

namespace CommunityToolkit.Maui.Core.Views;

public partial class MediaManager
{
	// The artwork of the loaded media is updated in place: the source load that it belongs to, and the update that a newer change replaces
	CancellationTokenSource? metadataArtworkCancellationTokenSource;

	// The source load whose media item the player has: an artwork change that comes while a source loads is applied once it has loaded
	CancellationTokenSource? loadedSourceCancellationTokenSource;
	bool hasMetadataArtworkChangedWhileLoading;

	protected virtual async partial ValueTask PlatformUpdateMetadataArtwork()
	{
		if (Player is null || cancellationTokenSource is not CancellationTokenSource sourceCancellationTokenSource)
		{
			return;
		}

		// The source that is loading read its artwork when its load started
		if (!ReferenceEquals(sourceCancellationTokenSource, loadedSourceCancellationTokenSource))
		{
			hasMetadataArtworkChangedWhileLoading = true;
			return;
		}

		await UpdateArtworkOfTheLoadedMedia(sourceCancellationTokenSource.Token).ConfigureAwait(true);
	}

	// Called by PlatformUpdateSource once the media item of its source is in the player
	async Task OnSourceLoaded(CancellationTokenSource sourceCancellationTokenSource)
	{
		loadedSourceCancellationTokenSource = sourceCancellationTokenSource;

		if (hasMetadataArtworkChangedWhileLoading)
		{
			hasMetadataArtworkChangedWhileLoading = false;
			await UpdateArtworkOfTheLoadedMedia(sourceCancellationTokenSource.Token).ConfigureAwait(true);
		}
	}

	// Called by PlatformUpdateSource and Dispose: the artwork of the media that is replaced must not be applied to the next one
	void CancelMetadataArtworkUpdate()
	{
		metadataArtworkCancellationTokenSource?.Cancel();
		metadataArtworkCancellationTokenSource?.Dispose();
		metadataArtworkCancellationTokenSource = null;

		loadedSourceCancellationTokenSource = null;
		hasMetadataArtworkChangedWhileLoading = false;
	}

	async Task UpdateArtworkOfTheLoadedMedia(CancellationToken sourceToken)
	{
		metadataArtworkCancellationTokenSource?.Cancel();
		metadataArtworkCancellationTokenSource?.Dispose();
		metadataArtworkCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(sourceToken);
		var token = metadataArtworkCancellationTokenSource.Token;

		// The player is used on the main thread only
		var data = await GetMetadataArtworkBytes(token).ConfigureAwait(true);
		if (token.IsCancellationRequested || Player?.CurrentMediaItem is not MediaItem currentMediaItem)
		{
			return;
		}

		var hasArtwork = data.Length > 0;
		var mediaMetadata = (currentMediaItem.MediaMetadata ?? MediaMetadata.Empty)!.BuildUpon()!
			.SetArtworkData(hasArtwork ? data : null, hasArtwork ? (Java.Lang.Integer)MediaMetadata.PictureTypeFrontCover : null)!
			.Build();

		// The new item has the URI and the configuration of the current one: Media3 updates it in place (MediaSource.canUpdateMediaItem), so the
		// media goes on playing, and onMediaMetadataChanged updates the media notification. The handler cannot observe a failure, which would end
		// the app: it is logged, and the media keeps its artwork.
		try
		{
			Player.ReplaceMediaItem(Player.CurrentMediaItemIndex, currentMediaItem.BuildUpon()!.SetMediaMetadata(mediaMetadata)!.Build());
		}
		catch (Exception exception)
		{
			Logger.LogWarning(exception, "Unable to update the artwork of the loaded media");
		}
	}

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
				case UriMediaSource { Uri: Uri uri } uriSource when MetadataArtworkPaths.IsHttpUri(uri):
					return await DownloadArtwork(uri, uriSource.HttpHeaders, sendContentLengthRequest: metadataArtworkUrl is not null, token).ConfigureAwait(false);

				case UriMediaSource { Uri: Uri uri } when uri.IsAbsoluteUri && uri.IsFile:
					stream = File.OpenRead(uri.LocalPath);
					break;

				case FileMediaSource fileSource when MetadataArtworkPaths.GetLocalFilePath(fileSource.Path) is string localFilePath:
					stream = File.OpenRead(localFilePath);
					break;

				case ResourceMediaSource resourceSource when MetadataArtworkPaths.GetAndroidAssetPath(resourceSource.Path) is string assetPath:
					stream = Platform.AppContext.Assets?.Open(assetPath) ?? throw new InvalidOperationException("Assets cannot be null");
					break;

				case StreamMediaSource { Stream: Stream artworkStream } when MetadataArtworkLoader.IsStreamOfTheSource(artworkStream, MediaElement.Source):
					Logger.LogWarning("{ArtworkSource} is not used: its stream is that of {Source}, which the player reads", nameof(MediaElement.MetadataArtworkSource), nameof(MediaElement.Source));
					return [];

				// The stream belongs to the app, which may use it again: it is read, not disposed
				case StreamMediaSource { Stream: Stream artworkStream }:
					return await MetadataArtworkLoader.ReadStreamAsync(artworkStream, token).ConfigureAwait(false);

				default:
					return [];
			}

			// An asset stream cannot seek, so its length is only known once it has been read
			using var memoryStream = new MemoryStream();
			await stream.CopyToAsync(memoryStream, token).ConfigureAwait(false);
			return memoryStream.ToArray();
		}
		catch (OperationCanceledException) when (token.IsCancellationRequested)
		{
			// A newer source or artwork replaced this one: there is nothing to report
			return [];
		}
		catch (Exception e)
		{
			Logger.LogWarning(e, "Unable to retrieve {ArtworkDescription}", GetArtworkDescription(artworkSource, metadataArtworkUrl));
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

	// A MetadataArtworkUrl is named by the string the app set
	static string GetArtworkDescription(MediaSource artworkSource, string? metadataArtworkUrl) =>
		metadataArtworkUrl is null
			? $"{nameof(MediaElement.MetadataArtworkSource)} from {artworkSource}"
#pragma warning disable CS0618 // MetadataArtworkUrl stays supported: the log names it
			: $"{nameof(MediaElement.MetadataArtworkUrl)} for {metadataArtworkUrl}";
#pragma warning restore CS0618

	async Task<byte[]> DownloadArtwork(Uri uri, IDictionary<string, string> httpHeaders, bool sendContentLengthRequest, CancellationToken token)
	{
		// 17.0.0 asked for the content length with a HEAD request before the GET of a MetadataArtworkUrl, and a failing HEAD left the artwork out
		if (sendContentLengthRequest)
		{
			using var contentLengthRequest = new HttpRequestMessage(HttpMethod.Head, uri);
			MetadataArtworkLoader.AddHttpHeaders(contentLengthRequest, httpHeaders);
			using var contentLengthResponse = await client.SendAsync(contentLengthRequest, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false);
		}

		return await MetadataArtworkLoader.DownloadAsync(client, uri, httpHeaders, token).ConfigureAwait(false);
	}
}