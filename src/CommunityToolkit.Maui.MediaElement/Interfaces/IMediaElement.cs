namespace CommunityToolkit.Maui.MediaElement;

/// <summary>
/// With MediaElement you can play multimedia inside of your app.
/// </summary>
public interface IMediaElement
{
	/// <summary>
	/// Gets or sets whether the media should start playing as soon as it's loaded. Default is <see langword="false"/>.
	/// </summary>
	bool AutoPlay { get; set; }

	/// <summary>
	/// The current position of the playing media.
	/// </summary>
	TimeSpan Position { get; set; }

	/// <summary>
	/// The total duration of the loaded media.
	/// </summary>
	/// <remarks>Might not be available for some types, like live streams.</remarks>
	TimeSpan Duration { get; }

	/// <summary>
	/// Starts playing the loaded media.
	/// </summary>
	void Play();

	/// <summary>
	/// Pauses the currently playing media.
	/// </summary>
	void Pause();

	/// <summary>
	/// Stops playing the currently playing media and resets the <see cref="Position"/>.
	/// </summary>
	void Stop();

	/// <summary>
	/// The source of the media to play.
	/// </summary>
	MediaSource? Source { get; set; }

	// iOS: https://github.com/brminnick/GitTrends/blob/main/GitTrends.iOS/CustomRenderers/VideoPlayerViewCustomRenderer.cs
	// Android: https://github.com/brminnick/GitTrends/blob/main/GitTrends.Android/CustomRenderers/VideoPlayerViewCustomRenderer.cs
}