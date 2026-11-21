using CommunityToolkit.Maui.Core.Primitives;
using CommunityToolkit.Maui.Views;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Dispatching;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;

namespace CommunityToolkit.Maui.Core.Views;

partial class MediaManager
{
	// Every load of the artwork changes it: a load that a newer one (of a newer change of the artwork) replaced while it awaited applies nothing
	int metadataArtworkVersion;

	// The source update whose media has opened: a change of the artwork before then needs no update, the opening media reads its artwork itself
	int openedSourceUpdateVersion = -1;

	// For an artwork downloaded with the HttpHeaders of its source; without headers the system media controls and the poster download it themselves
	System.Net.Http.HttpClient? artworkHttpClient;

	// The thumbnail goes to the system media controls from any thread; the poster is a XAML object, so it is created by the caller of CreatePoster
	// on the UI thread.
	sealed record MetadataArtwork(RandomAccessStreamReference? Thumbnail, Func<CancellationToken, Task<BitmapImage>> CreatePoster);

	protected virtual async partial ValueTask PlatformUpdateMetadataArtwork()
	{
		var version = Volatile.Read(ref sourceUpdateVersion);
		if (Player is null || Volatile.Read(ref openedSourceUpdateVersion) != version)
		{
			return;
		}

		// The handler cannot observe a failure, which would end the app: it is logged, and the media keeps the artwork it has
		try
		{
			await UpdateMetadataOfTheOpenedMedia(version, isArtworkChange: true, CancellationToken.None);
		}
		catch (Exception exception)
		{
			Logger.LogWarning(exception, "Unable to update the artwork of the opened media");
		}
	}

	bool IsCurrentMetadataArtwork(int artworkVersion) => Volatile.Read(ref metadataArtworkVersion) == artworkVersion;

	// Runs inside the async void MediaOpened handler, where an exception would end the app: every failure is logged and means "no artwork".
	async Task<MetadataArtwork?> LoadMetadataArtwork(MediaSource artworkSource, CancellationToken token)
	{
		try
		{
			switch (artworkSource)
			{
				case UriMediaSource uriSource when uriSource.Uri is Uri artworkUri && artworkUri.IsAbsoluteUri && artworkUri.IsFile:
					return await LoadMetadataArtworkFromFile(artworkUri.LocalPath, token);

				// The system media controls and BitmapImage cannot send headers: the artwork is downloaded with them, as the player downloads Source
				case UriMediaSource { HttpHeaders.Count: > 0 } uriSource when uriSource.Uri is Uri artworkUri && MetadataArtworkPaths.IsHttpUri(artworkUri):
					artworkHttpClient ??= new System.Net.Http.HttpClient();
					return await LoadMetadataArtworkFromBytes(
						await MetadataArtworkLoader.DownloadAsync(artworkHttpClient, artworkUri, uriSource.HttpHeaders, token), artworkSource, token);

				case UriMediaSource uriSource when uriSource.Uri is Uri artworkUri && Metadata.IsSupportedThumbnailUri(artworkUri):
					return new MetadataArtwork(RandomAccessStreamReference.CreateFromUri(artworkUri), _ => Task.FromResult(new BitmapImage(artworkUri)));

				case FileMediaSource fileSource when MetadataArtworkPaths.GetLocalFilePath(fileSource.Path) is string localFilePath:
					return await LoadMetadataArtworkFromFile(localFilePath, token);

				case ResourceMediaSource resourceSource when !string.IsNullOrWhiteSpace(resourceSource.Path):
					return await LoadMetadataArtworkFromFile(GetFullAppPackageFilePath(resourceSource.Path), token);

				case StreamMediaSource { Stream: Stream artworkStream } when MetadataArtworkLoader.IsStreamOfTheSource(artworkStream, MediaElement.Source):
					Logger.LogWarning("{ArtworkSource} is not used: its stream is that of {Source}, which the player reads", nameof(MediaElement.MetadataArtworkSource), nameof(MediaElement.Source));
					return null;

				// The stream belongs to the app, which may use it again: it is read, not disposed
				case StreamMediaSource { Stream: Stream artworkStream }:
					return await LoadMetadataArtworkFromBytes(await MetadataArtworkLoader.ReadStreamAsync(artworkStream, token), artworkSource, token);

				// 17.0.0 set the poster from any absolute MetadataArtworkUrl; the system media controls take only the schemes above
				case UriMediaSource uriSource when uriSource.Uri is Uri artworkUri && artworkUri.IsAbsoluteUri:
					return new MetadataArtwork(null, _ => Task.FromResult(new BitmapImage(artworkUri)));

				default:
					return null;
			}
		}
		catch (Exception ex)
		{
			Logger.LogWarning(ex, "Unable to load the metadata artwork {ArtworkSource}", artworkSource);
			return null;
		}
	}

	static async Task<MetadataArtwork> LoadMetadataArtworkFromFile(string path, CancellationToken token)
	{
		var file = await StorageFile.GetFileFromPathAsync(path).AsTask(token);
		return new MetadataArtwork(RandomAccessStreamReference.CreateFromFile(file), posterToken => CreatePosterFromFile(file, posterToken));
	}

	async Task<MetadataArtwork?> LoadMetadataArtworkFromBytes(byte[] data, MediaSource artworkSource, CancellationToken token)
	{
		if (data.Length is 0)
		{
			Logger.LogWarning("Unable to load the metadata artwork {ArtworkSource}: it has no data", artworkSource);
			return null;
		}

		// The system media controls read the thumbnail when they show it, so its stream stays with them
		var thumbnailStream = await CreateRandomAccessStream(data, token);
		return new MetadataArtwork(RandomAccessStreamReference.CreateFromStream(thumbnailStream), posterToken => CreatePosterFromBytes(data, posterToken));
	}

	static async Task<InMemoryRandomAccessStream> CreateRandomAccessStream(byte[] data, CancellationToken token)
	{
		var stream = new InMemoryRandomAccessStream();
		using (var writer = new DataWriter(stream.GetOutputStreamAt(0)))
		{
			writer.WriteBytes(data);
			await writer.StoreAsync().AsTask(token);
			await writer.FlushAsync().AsTask(token);

			// The stream outlives the writer, which would close it
			writer.DetachStream();
		}

		stream.Seek(0);
		return stream;
	}

	static async Task<BitmapImage> CreatePosterFromBytes(byte[] data, CancellationToken token)
	{
		var poster = new BitmapImage();
		using var stream = await CreateRandomAccessStream(data, token);
		await poster.SetSourceAsync(stream).AsTask(token);
		return poster;
	}

	static async Task<BitmapImage> CreatePosterFromFile(StorageFile file, CancellationToken token)
	{
		var poster = new BitmapImage();
		using var stream = await file.OpenReadAsync().AsTask(token);
		await poster.SetSourceAsync(stream).AsTask(token);
		return poster;
	}

	async Task SetPoster(MetadataArtwork? artwork, int version, int artworkVersion, CancellationToken token)
	{
		if (Dispatcher.IsDispatchRequired)
		{
			await Dispatcher.DispatchAsync(() => SetPosterOnUiThread(artwork, version, artworkVersion, token));
		}
		else
		{
			await SetPosterOnUiThread(artwork, version, artworkVersion, token);
		}
	}

	async Task SetPosterOnUiThread(MetadataArtwork? artwork, int version, int artworkVersion, CancellationToken token)
	{
		try
		{
			// Without artwork the poster is empty, as PlatformUpdateSource leaves it for new media
			var poster = artwork is null ? new BitmapImage() : await artwork.CreatePoster(token);

			// Loading the poster awaits: a newer source update has reset the poster meanwhile, or a newer artwork set it, and this one would bring
			// the previous one back.
			if (IsCurrentSourceUpdate(version) && IsCurrentMetadataArtwork(artworkVersion) && Player is not null)
			{
				Player.PosterSource = poster;
			}
		}
		catch (Exception ex)
		{
			Logger.LogWarning(ex, "Unable to set the poster from the metadata artwork");
		}
	}
}