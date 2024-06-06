using Windows.Media;

namespace CommunityToolkit.Maui.Core.Primitives;

/// <summary>
/// A class that provides methods to update the system UI for media transport controls to display media metadata.
/// </summary>
public class Metadata
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

	/// <summary>
	/// Sets the metadata for the given MediaElement.
	/// </summary>
	public void SetMetadata(IMediaElement mp)
	{
		if (systemMediaControls is null || mediaElement is null)
		{
			return;
		}

		if (!string.IsNullOrEmpty(mp.MetadataArtworkUrl))
		{
			systemMediaControls.DisplayUpdater.Thumbnail = Windows.Storage.Streams.RandomAccessStreamReference.CreateFromUri(new Uri(mp.MetadataArtworkUrl ?? string.Empty));
		}
		systemMediaControls.DisplayUpdater.Type = MediaPlaybackType.Music;
		systemMediaControls.DisplayUpdater.MusicProperties.Artist = mp.MetadataTitle;
		systemMediaControls.DisplayUpdater.MusicProperties.Title = mp.MetadataArtist;
		systemMediaControls.DisplayUpdater.Update();
	}
}