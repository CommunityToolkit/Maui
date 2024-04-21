using Windows.Media;
using CommunityToolkit.Maui.Core;

namespace CommunityToolkit.Maui.Extensions;

/// <summary>
/// A class that provides methods to update the system UI for media transport controls to display media metadata.
/// </summary>
public class MetaDataExtensions
{
	readonly IMediaElement? mediaElement;
	readonly SystemMediaTransportControls? systemMediaControls;
	readonly IDispatcher dispatcher;
	/// <summary>
	/// Initializes a new instance of the <see cref="MetaDataExtensions"/> class.
	/// </summary>
	public MetaDataExtensions(SystemMediaTransportControls systemMediaTransportControls, IMediaElement MediaElement, IDispatcher Dispatcher)
	{
		mediaElement = MediaElement;
		this.dispatcher = Dispatcher;
		systemMediaControls = systemMediaTransportControls;
		systemMediaControls.ButtonPressed += OnSystemMediaControlsButtonPressed;
	}


	void SystemMediaControls_ButtonPressed(SystemMediaTransportControls sender, SystemMediaTransportControlsButtonPressedEventArgs args)
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
	public void SetMetaData(IMediaElement mp)
	{
		if (systemMediaControls is null || mediaElement is null)
		{
			return;
		}

		if (!string.IsNullOrEmpty(mp.MetaDataArtworkUrl))
		{
			systemMediaControls.DisplayUpdater.Thumbnail = Windows.Storage.Streams.RandomAccessStreamReference.CreateFromUri(new Uri(mp.MetaDataArtworkUrl ?? string.Empty));
		}		
		systemMediaControls.DisplayUpdater.Type = MediaPlaybackType.Music;
		systemMediaControls.DisplayUpdater.MusicProperties.Artist = mp.MetaDataTitle;
		systemMediaControls.DisplayUpdater.MusicProperties.Title = mp.MetaDataArtist;
		systemMediaControls.DisplayUpdater.Update();
	}
}