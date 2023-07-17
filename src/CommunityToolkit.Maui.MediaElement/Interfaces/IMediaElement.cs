using CommunityToolkit.Maui.Core.Primitives;
using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui.Core;

/// <summary>
/// With MediaElement you can play multimedia inside of your app.
/// </summary>
public interface IMediaElement : IView
{
	/// <summary>
	/// Gets the media aspect ratio.
	/// </summary>
	/// <remarks>Not functional for non-visual media.</remarks>
	Aspect Aspect { get; }

	/// <summary>
	/// The current state of the <see cref="MediaElement"/>.
	/// </summary>
	MediaElementState CurrentState { get; }

	/// <summary>
	/// Gets the height (in pixels) of the loaded media in pixels.
	/// </summary>
	/// <remarks>Not reported for non-visual media.</remarks>
	int MediaHeight { get; }

	/// <summary>
	/// Gets the width (in pixels) of the loaded media in pixels.
	/// </summary>
	/// <remarks>Not reported for non-visual media.</remarks>
	int MediaWidth { get; }

	/// <summary>
	/// The current position of the playing media.
	/// </summary>
	TimeSpan Position { get; internal set; }

	/// <summary>
	/// Gets total duration of the loaded media.
	/// </summary>
	/// <remarks>Might not be available for some types, like live streams.</remarks>
	TimeSpan Duration { get; set; }

	/// <summary>
	/// Gets or sets whether the media should start playing as soon as it's loaded.
	/// </summary>
	bool ShouldAutoPlay { get; set; }

	/// <summary>
	/// Gets or sets if the media playback will restart from the beginning when it reaches the end.
	/// </summary>
	bool ShouldLoopPlayback { get; set; }

	/// <summary>
	/// Gets or sets if media playback will prevent the device display from going to sleep.
	/// </summary>
	/// <remarks>If media is paused, stopped or has completed playing, the display will turn off.</remarks>
	bool ShouldKeepScreenOn { get; set; }

	/// <summary>
	/// Gets or sets if audio should be muted.
	/// </summary>
	bool ShouldMute { get; set; }

	/// <summary>
	/// Gets or sets whether the player should show the platform playback controls.
	/// </summary>
	bool ShouldShowPlaybackControls { get; set; }

	/// <summary>
	/// Gets or sets the source of the media to play.
	/// </summary>
	MediaSource? Source { get; set; }

	/// <summary>
	/// Gets or sets the speed with which the media should be played.
	/// </summary>
	/// <remarks>A value of 1 means normal speed.
	/// Anything more than 1 is faster speed, anything less than 1 is slower speed.</remarks>
	double Speed { get; set; }

	/// <summary>
	/// Gets or sets the volume of the audio for the media.
	/// </summary>
	/// <remarks>A value of 1 means full volume, 0 is silence.</remarks>
	double Volume { get; set; }

	/// <summary>
	/// Occurs when <see cref="CurrentState"/> changed.
	/// </summary>
	event EventHandler<MediaStateChangedEventArgs> StateChanged;

	/// <summary>
	/// Occurs when the <see cref="Position"/> changes;
	/// </summary>
	event EventHandler<MediaPositionChangedEventArgs> PositionChanged;

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
	/// Triggers the <see cref="MediaElement.SeekCompleted"/> event.
	/// </summary>
	internal void SeekCompleted();

	/// <summary>
	/// Triggers a <see cref="CurrentState"/> change.
	/// </summary>
	/// <param name="newState">The new state the <see cref="MediaElement"/> transitioned to.</param>
	internal void CurrentStateChanged(MediaElementState newState);
}