using Windows.Media;
using CommunityToolkit.Maui.Core;

namespace CommunityToolkit.Maui.Extensions;

/// <summary>
/// A class that provides methods to update the system UI for media transport controls to display media metadata.
/// </summary>
public partial class MetaDataExtensions
{
	/// <summary>
	/// The media player to which the metadata will be applied.
	/// </summary>
	protected IMediaElement? mediaElement { get; set; }

	/// <summary>
	/// The system media transport controls for the current app.
	/// </summary>
	public SystemMediaTransportControls? SystemMediaControls { get; set; }

	/// <summary>
	/// Initializes a new instance of the <see cref="MetaDataExtensions"/> class.
	/// </summary>
	public MetaDataExtensions(SystemMediaTransportControls systemMediaTransportControls, IMediaElement MediaElement)
	{
		mediaElement = MediaElement;
		SystemMediaControls = systemMediaTransportControls;
		this.SystemMediaControls.ButtonPressed += SystemMediaControls_ButtonPressed;
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
		if (SystemMediaControls is null || mediaElement is null)
		{
			return;
		}

		if (!string.IsNullOrEmpty(mp.MetadataArtwork))
		{
			SystemMediaControls.DisplayUpdater.Thumbnail = Windows.Storage.Streams.RandomAccessStreamReference.CreateFromUri(new Uri(mp.MetadataArtwork ?? string.Empty));
		}
		if (mp.SourceType == Primitives.MediaElementSourceType.Video)
		{
			SystemMediaControls.DisplayUpdater.Type = MediaPlaybackType.Video;
			SystemMediaControls.DisplayUpdater.VideoProperties.Title = mp.MetadataTitle;
		}
		else if (mp.SourceType == Primitives.MediaElementSourceType.Audio)
		{
			SystemMediaControls.DisplayUpdater.Type = MediaPlaybackType.Music;
			SystemMediaControls.DisplayUpdater.MusicProperties.AlbumTitle = mp.MetadataAlbum;
			SystemMediaControls.DisplayUpdater.MusicProperties.Title = mp.MetadataTitle;
			SystemMediaControls.DisplayUpdater.MusicProperties.AlbumArtist = mp.MetadataArtist;
			SystemMediaControls.DisplayUpdater.MusicProperties.Artist = mp.MetadataArtist;
			SystemMediaControls.DisplayUpdater.MusicProperties.Genres.Add(mp.MetadataGenre);
		}
		SystemMediaControls.DisplayUpdater.Update();
	}
}