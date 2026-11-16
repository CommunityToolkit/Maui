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

		// Only media with metadata becomes a system media session, like the Now Playing info on iOS and the media notification on Android.
		// Media without metadata leaves the controls off, as MediaManager.CreatePlatformView does.
		systemMediaControls.IsEnabled = HasMetadata(mp);

		// RandomAccessStreamReference.CreateFromUri supports the http, https, ms-appx and ms-appdata schemes only, so any other url (a relative
		// or malformed one, or a file path, for example) is ignored instead of passed on.
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
	/// Removes the button handler from the system media transport controls, which belong to the player and would otherwise keep this
	/// instance and its MediaElement reachable.
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