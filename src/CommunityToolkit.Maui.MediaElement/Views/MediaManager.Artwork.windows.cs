using CommunityToolkit.Maui.Views;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Dispatching;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;

namespace CommunityToolkit.Maui.Core.Views;

partial class MediaManager
{
	// The thumbnail goes to the system media controls from any thread; the poster is a XAML object, so it is created by the caller of CreatePoster
	// on the UI thread.
	sealed record MetadataArtwork(RandomAccessStreamReference Thumbnail, Func<CancellationToken, Task<BitmapImage>> CreatePoster);

	// Runs inside the async void MediaOpened handler, where an exception would end the app: every failure is logged and means "no artwork".
	async Task<MetadataArtwork?> LoadMetadataArtwork(MediaSource artworkSource, CancellationToken token)
	{
		try
		{
			switch (artworkSource)
			{
				case UriMediaSource uriSource when uriSource.Uri is Uri artworkUri && artworkUri.IsAbsoluteUri && artworkUri.IsFile:
					return await LoadMetadataArtworkFromFile(artworkUri.LocalPath, token);

				case UriMediaSource uriSource when uriSource.Uri is Uri artworkUri && IsSupportedThumbnailUri(artworkUri):
					return new MetadataArtwork(RandomAccessStreamReference.CreateFromUri(artworkUri), _ => Task.FromResult(new BitmapImage(artworkUri)));

				case FileMediaSource fileSource when MetadataArtworkPaths.GetLocalFilePath(fileSource.Path) is string localFilePath:
					return await LoadMetadataArtworkFromFile(localFilePath, token);

				case ResourceMediaSource resourceSource when !string.IsNullOrWhiteSpace(resourceSource.Path):
					return await LoadMetadataArtworkFromFile(GetFullAppPackageFilePath(resourceSource.Path), token);

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

	static bool IsSupportedThumbnailUri(Uri artworkUri)
	{
		// RandomAccessStreamReference.CreateFromUri supports these schemes only. System.Uri lowercases the scheme.
		return artworkUri.IsAbsoluteUri && artworkUri.Scheme is "http" or "https" or "ms-appx" or "ms-appdata";
	}

	static async Task<MetadataArtwork> LoadMetadataArtworkFromFile(string path, CancellationToken token)
	{
		var file = await StorageFile.GetFileFromPathAsync(path).AsTask(token);
		return new MetadataArtwork(RandomAccessStreamReference.CreateFromFile(file), posterToken => CreatePosterFromFile(file, posterToken));
	}

	static async Task<BitmapImage> CreatePosterFromFile(StorageFile file, CancellationToken token)
	{
		var poster = new BitmapImage();
		using var stream = await file.OpenReadAsync().AsTask(token);
		await poster.SetSourceAsync(stream).AsTask(token);
		return poster;
	}

	async Task SetPosterFromArtwork(MetadataArtwork artwork, int version, CancellationToken token)
	{
		if (Dispatcher.IsDispatchRequired)
		{
			await Dispatcher.DispatchAsync(() => SetPosterOnUiThread(artwork, version, token));
		}
		else
		{
			await SetPosterOnUiThread(artwork, version, token);
		}
	}

	async Task SetPosterOnUiThread(MetadataArtwork artwork, int version, CancellationToken token)
	{
		try
		{
			var poster = await artwork.CreatePoster(token);

			// Loading the poster awaits: a newer source update has reset the poster meanwhile, and this one would bring the previous media's back.
			if (IsCurrentSourceUpdate(version) && Player is not null)
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
