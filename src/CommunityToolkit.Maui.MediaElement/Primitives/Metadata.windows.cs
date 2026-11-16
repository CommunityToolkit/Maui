using CommunityToolkit.Maui.Views;
using Microsoft.Maui.Dispatching;
using Windows.Media;
using Windows.Storage.Streams;

namespace CommunityToolkit.Maui.Core.Primitives;

sealed partial class Metadata : IDisposable
{
	readonly IMediaElement? mediaElement;
	readonly SystemMediaTransportControls? systemMediaControls;
	readonly IDispatcher dispatcher;
	/// <summary>
	/// Initializes a new instance of the <see cref="Metadata"/> class.
	/// </summary>
	public Metadata(SystemMediaTransportControls systemMediaTransportControls, IMediaElement MediaElement, IDispatcher Dispatcher)
	{
		mediaElement = MediaElement;
		this.dispatcher = Dispatcher;
		systemMediaControls = systemMediaTransportControls;
		systemMediaControls.ButtonPressed += OnSystemMediaControlsButtonPressed;
	}

	/// <summary>
	/// Sets the metadata for the given MediaElement.
	/// </summary>
	/// <param name="mp">The MediaElement whose metadata is published.</param>
	public void SetMetadata(IMediaElement mp) =>
		SetMetadata(mp, MetadataArtworkResolver.Resolve(mp).Source, loadedThumbnail: null);

	/// <summary>
	/// Sets the metadata for the given MediaElement, with the artwork the caller resolved.
	/// </summary>
	/// <param name="mp">The MediaElement whose metadata is published.</param>
	/// <param name="artworkSource">The artwork the caller resolved from the MediaElement, so the thumbnail and the poster come from the same artwork.</param>
	/// <param name="loadedThumbnail">
	/// The thumbnail loaded from <paramref name="artworkSource"/> (a local file or a file of the app package). Without it, an http, https,
	/// ms-appx or ms-appdata artwork URI becomes the thumbnail, and any other artwork clears it.
	/// </param>
	public void SetMetadata(IMediaElement mp, MediaSource? artworkSource, RandomAccessStreamReference? loadedThumbnail)
	{
		if (systemMediaControls is null || mediaElement is null)
		{
			return;
		}

		// Only media with metadata becomes a system media session, like the Now Playing info on iOS and the media notification on Android.
		// Media without metadata leaves the controls off, as MediaManager.CreatePlatformView does.
		systemMediaControls.IsEnabled = HasMetadata(mp);

		systemMediaControls.DisplayUpdater.Thumbnail = loadedThumbnail ?? CreateThumbnailFromUri(artworkSource);
		systemMediaControls.DisplayUpdater.Type = MediaPlaybackType.Music;
		systemMediaControls.DisplayUpdater.MusicProperties.Title = mp.MetadataTitle;
		systemMediaControls.DisplayUpdater.MusicProperties.Artist = mp.MetadataArtist;
		systemMediaControls.DisplayUpdater.Update();
	}

	/// <summary>
	/// Removes the button handler from the system media transport controls, which belong to the player and would otherwise keep this
	/// instance and its MediaElement reachable.
	/// </summary>
	public void Dispose()
	{
		systemMediaControls?.ButtonPressed -= OnSystemMediaControlsButtonPressed;
	}

	// Null clears the thumbnail: the controls are the player's own and outlive the media, so a thumbnail left in place would show the artwork
	// of the previous media.
	static RandomAccessStreamReference? CreateThumbnailFromUri(MediaSource? artworkSource) =>
		artworkSource is UriMediaSource { Uri: Uri artworkUri } && IsSupportedThumbnailUri(artworkUri)
			? RandomAccessStreamReference.CreateFromUri(artworkUri)
			: null;

	// RandomAccessStreamReference.CreateFromUri supports these schemes only. System.Uri lowercases the scheme.
	internal static bool IsSupportedThumbnailUri(Uri artworkUri) =>
		artworkUri.IsAbsoluteUri && artworkUri.Scheme is "http" or "https" or "ms-appx" or "ms-appdata";

	static bool HasMetadata(IMediaElement mediaElement) =>
		!string.IsNullOrWhiteSpace(mediaElement.MetadataTitle)
		|| !string.IsNullOrWhiteSpace(mediaElement.MetadataArtist)
		|| mediaElement.MetadataArtworkSource is not null
		|| HasArtworkUrl(mediaElement);

	static bool HasArtworkUrl(IMediaElement mediaElement)
	{
#pragma warning disable CS0618 // MetadataArtworkUrl stays supported: it is used when MetadataArtworkSource is not set
		return !string.IsNullOrWhiteSpace(mediaElement.MetadataArtworkUrl);
#pragma warning restore CS0618
	}

	void OnSystemMediaControlsButtonPressed(SystemMediaTransportControls sender, SystemMediaTransportControlsButtonPressedEventArgs args)
	{
		if (mediaElement is null)
		{
			return;
		}

		if (args.Button == SystemMediaTransportControlsButton.Play)
		{
			if (dispatcher.IsDispatchRequired)
			{
				dispatcher.Dispatch(() => mediaElement.Play());
			}
			else
			{
				mediaElement.Play();
			}
		}
		else if (args.Button == SystemMediaTransportControlsButton.Pause)
		{
			if (dispatcher.IsDispatchRequired)
			{
				dispatcher.Dispatch(() => mediaElement.Pause());
			}
			else
			{
				mediaElement.Pause();
			}
		}
	}
}