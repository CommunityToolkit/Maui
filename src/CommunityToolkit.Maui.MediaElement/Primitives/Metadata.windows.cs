using Microsoft.Maui.Dispatching;
using Windows.Media;

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
	public void SetMetadata(IMediaElement mp)
	{
		if (systemMediaControls is null || mediaElement is null)
		{
			return;
		}

		// Media with metadata becomes a system media session, which the system media overlay shows and the media keys control, like the Now
		// Playing info on iOS and the media notification on Android. Media without metadata does not: the controls stay off, as
		// MediaManager.CreatePlatformView leaves them.
		systemMediaControls.IsEnabled = HasMetadata(mp);

		// RandomAccessStreamReference.CreateFromUri supports the http, https, ms-appx and ms-appdata schemes only: any other url (a relative or
		// malformed one, or a file path, for example) cannot be the thumbnail and is ignored rather than passed on
		if (Uri.TryCreate(mp.MetadataArtworkUrl, UriKind.Absolute, out var artworkUri)
			&& artworkUri.Scheme is "http" or "https" or "ms-appx" or "ms-appdata")
		{
			systemMediaControls.DisplayUpdater.Thumbnail = Windows.Storage.Streams.RandomAccessStreamReference.CreateFromUri(artworkUri);
		}
		systemMediaControls.DisplayUpdater.Type = MediaPlaybackType.Music;
		systemMediaControls.DisplayUpdater.MusicProperties.Title = mp.MetadataTitle;
		systemMediaControls.DisplayUpdater.MusicProperties.Artist = mp.MetadataArtist;
		systemMediaControls.DisplayUpdater.Update();
	}

	/// <summary>
	/// Stops handling the buttons of the system media transport controls. The controls belong to the player: until their handler is
	/// removed, they keep this instance and its MediaElement reachable.
	/// </summary>
	public void Dispose()
	{
		systemMediaControls?.ButtonPressed -= OnSystemMediaControlsButtonPressed;
	}

	static bool HasMetadata(IMediaElement mediaElement) =>
		!string.IsNullOrWhiteSpace(mediaElement.MetadataTitle)
		|| !string.IsNullOrWhiteSpace(mediaElement.MetadataArtist)
		|| !string.IsNullOrWhiteSpace(mediaElement.MetadataArtworkUrl);

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