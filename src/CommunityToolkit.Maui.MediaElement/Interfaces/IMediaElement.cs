namespace CommunityToolkit.Maui.MediaElement;

/// <summary>
/// With MediaElement you can play multimedia inside of your app.
/// </summary>
public interface IMediaElement : IView
{
	/// <summary>
	/// Gets the height (in pixels) of the loaded media in pixels.
	/// </summary>
	/// <remarks>Not reported for non-visual media.</remarks>
	int VideoHeight { get; }

	/// <summary>
	/// Gets the width (in pixels) of the loaded media in pixels.
	/// </summary>
	/// <remarks>Not reported for non-visual media.</remarks>
	int VideoWidth { get; }

	/// <summary>
	/// Gets or sets whether the media should start playing as soon as it's loaded.
	/// </summary>
	bool AutoPlay { get; set; }

	/// <summary>
	/// The current state of the <see cref="MediaElement"/>.
	/// </summary>
	MediaElementState CurrentState { get; }

	/// <summary>
	/// Gets total duration of the loaded media.
	/// </summary>
	/// <remarks>Might not be available for some types, like live streams.</remarks>
	TimeSpan Duration { get; set; }

	/// <summary>
	/// Gets or sets if the video will play when reaches the end.
	/// </summary>
	bool IsLooping { get; set; }

	/// <summary>
	/// Gets or sets if media playback will prevent the device display from going to sleep.
	/// </summary>
	/// <remarks>If media is paused, stopped or has completed playing, the display will turn off.</remarks>
	bool KeepScreenOn { get; set; }

	/// <summary>
	/// Gets or sets whether the player should show the platform playback controls.
	/// </summary>
	bool ShowsPlaybackControls { get; set; }

	/// <summary>
	/// Gets or sets the source of the media to play.
	/// </summary>
	MediaSource? Source { get; set; }

	/// <summary>
	/// Gets or sets the volume of the audio for the media.
	/// </summary>
	/// <remarks>A value of 1 means full volume, 0 is silence.</remarks>
	double Volume { get; set; }

	/// <summary>
	/// Gets or sets the speed with which the media should be played.
	/// </summary>
	/// <remarks>A value of 1 means normal speed.
	/// Anything more than 1 is faster speed, anything less than 1 is slower speed.</remarks>
	double Speed { get; set; }

	/// <summary>
	/// The current position of the playing media.
	/// </summary>
	TimeSpan Position { get; internal set; }

	/// <summary>
	/// Occurs when the <see cref="Position"/> changes;
	/// </summary>
	event EventHandler<MediaPositionChangedEventArgs> PositionChanged;

	/// <summary>
	/// Occurs when <see cref="CurrentState"/> changed.
	/// </summary>
	event EventHandler<MediaStateChangedEventArgs> StateChanged;

	/// <summary>
	/// Occurs when the media has ended playing successfully.
	/// </summary>
	/// <remarks>This does not trigger when the media has failed during playback.</remarks>
	void MediaEnded();

	/// <summary>
	/// Occurs when the media has failed loading.
	/// </summary>
	/// <param name="args">Event arguments containing extra information about this event.</param>
	void MediaFailed(MediaFailedEventArgs args);

	/// <summary>
	/// Occurs when the media has been loaded and is ready to play.
	/// </summary>
	void MediaOpened();

	/// <summary>
	/// Pauses the currently playing media.
	/// </summary>
	void Pause();

	/// <summary>
	/// Starts playing the loaded media.
	/// </summary>
	void Play();

	/// <summary>
	/// Triggered when a seek action has been completed.
	/// </summary>
	void SeekCompleted();

	/// <summary>
	/// Seek to a specific position in the currently playing media.
	/// </summary>
	/// <param name="position">The requested position to seek to.</param>
	/// <remarks>If <paramref name="position"/> is outside of the range of the current media item, nothing will happen.</remarks>
	void SeekTo(TimeSpan position);

	/// <summary>
	/// Stops playing the currently playing media and resets the <see cref="Position"/>.
	/// </summary>
	void Stop();

	/// <summary>
	/// Triggers a <see cref="CurrentState"/> change.
	/// </summary>
	/// <param name="newState">The new state the <see cref="MediaElement"/> transitioned to.</param>
	internal void CurrentStateChanged(MediaElementState newState);
}