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

	/// <summary>
	/// Initializes a new instance of the <see cref="MetaDataExtensions"/> class.
	/// </summary>
	public MetaDataExtensions(SystemMediaTransportControls systemMediaTransportControls, IMediaElement MediaElement)
	{
		mediaElement = MediaElement;
		systemMediaControls = systemMediaTransportControls;
		this.systemMediaControls.ButtonPressed += SystemMediaControls_ButtonPressed;
	}


	void SystemMediaControls_ButtonPressed(SystemMediaTransportControls sender, SystemMediaTransportControlsButtonPressedEventArgs args)
	{
		if (mediaElement is null)
		{
			return;
		}

		if (args.Button == SystemMediaTransportControlsButton.Play)
		{
			MainThread.InvokeOnMainThreadAsync(() => mediaElement.Play());
		}
		else if (args.Button == SystemMediaTransportControlsButton.Pause)
		{
			MainThread.InvokeOnMainThreadAsync(() => mediaElement.Pause());
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
		if (mp.SourceType == Primitives.MediaElementSourceType.Video)
		{
			systemMediaControls.DisplayUpdater.Type = MediaPlaybackType.Video;
			systemMediaControls.DisplayUpdater.VideoProperties.Title = mp.MetaDataTitle;
			systemMediaControls.DisplayUpdater.VideoProperties.Subtitle = mp.MetaDataArtist;
		}
		else if (mp.SourceType == Primitives.MediaElementSourceType.Audio)
		{
			systemMediaControls.DisplayUpdater.Type = MediaPlaybackType.Music;
			systemMediaControls.DisplayUpdater.MusicProperties.Artist = mp.MetaDataTitle;
			systemMediaControls.DisplayUpdater.MusicProperties.Title = mp.MetaDataArtist;
		}
		systemMediaControls.DisplayUpdater.Update();
	}
}