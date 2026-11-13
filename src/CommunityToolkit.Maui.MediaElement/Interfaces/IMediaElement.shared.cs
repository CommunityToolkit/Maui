using CommunityToolkit.Maui.Views;
using Microsoft.Extensions.Logging;
using Microsoft.Maui;

namespace CommunityToolkit.Maui.Core;

/// <summary>
/// With MediaElement you can play multimedia inside your app.
/// </summary>
public interface IMediaElement : IView, IAsynchronousMediaElementHandler
{
	/// <summary>
	/// Occurs when <see cref="CurrentState"/> changed.
	/// </summary>
	event EventHandler<MediaStateChangedEventArgs> StateChanged;

	/// <summary>
	/// Occurs when the <see cref="Position"/> changes;
	/// </summary>
	event EventHandler<MediaPositionChangedEventArgs> PositionChanged;

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
	int MediaHeight { get; internal set; }

	/// <summary>
	/// Gets the width (in pixels) of the loaded media in pixels.
	/// </summary>
	/// <remarks>Not reported for non-visual media.</remarks>
	int MediaWidth { get; internal set; }

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
	/// <remarks>
	/// <para>
	/// A source without data (a <see cref="UriMediaSource"/> without <see cref="UriMediaSource.Uri"/>, a <see cref="FileMediaSource"/> or
	/// <see cref="ResourceMediaSource"/> without a path, a <see cref="StreamMediaSource"/> without a stream) empties the player and reports
	/// <see cref="MediaElementState.None"/>, like a <see langword="null"/> source. On iOS and Mac Catalyst, a <see cref="ResourceMediaSource"/> whose
	/// file is not in the app bundle, or whose path has no extension, is handled the same way, and a warning is logged. A kind of
	/// <see cref="MediaSource"/> that the platform does not support is reported through <see cref="MediaFailed(MediaFailedEventArgs)"/>.
	/// </para>
	/// <para>
	/// A source that cannot be applied or played is reported through <see cref="MediaFailed(MediaFailedEventArgs)"/>. The change of the source raises
	/// events while it applies the source: <see cref="StateChanged"/> (for example for <see cref="MediaElementState.Opening"/>), PropertyChanging and
	/// PropertyChanged (for example of <see cref="Position"/> or <see cref="Duration"/>), MediaFailed for a source that cannot be applied and, on
	/// Android, MediaOpened. An exception thrown by one of their handlers is not a failure of the source, and does not reach the code that set the
	/// source, which is often MAUI's (XAML, a binding): it is caught and logged, the change goes on and applies the source, and once the change has
	/// ended the first of these exceptions is reported through MediaFailed, without setting <see cref="CurrentState"/> to
	/// <see cref="MediaElementState.Failed"/> (see <see cref="MediaFailed(MediaFailedEventArgs)"/>). The events that the player raises afterwards,
	/// for example MediaOpened on iOS, Mac Catalyst and Windows, are not part of the change.
	/// </para>
	/// </remarks>
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
	/// <remarks>A value of 1 indicates full volume, 0 is silence.</remarks>
	double Volume { get; set; }

	/// <summary>
	/// Gets or sets the title of the media.
	/// </summary>
	string MetadataTitle { get; set; }

	/// <summary>
	/// Gets or sets the artist of the media.
	/// </summary>
	string MetadataArtist { get; set; }

	/// <summary>
	/// Gets or sets the artwork Image Url.
	/// </summary>
	string MetadataArtworkUrl { get; set; }

	/// <summary>
	/// Occurs when the media has ended playing successfully.
	/// </summary>
	/// <remarks>This does not trigger when the media has failed during playback.</remarks>
	void MediaEnded();

	/// <summary>
	/// Occurs when the media has failed loading.
	/// </summary>
	/// <param name="args">Event arguments containing extra information about this event.</param>
	/// <remarks>
	/// <para>
	/// A source that cannot be applied, for example a stream that cannot be read, is reported like a source that cannot be played.
	/// </para>
	/// <para>
	/// The event is also raised, once per change of the source, when event handlers threw exceptions while the source changed (see
	/// <see cref="Source"/>). Its <see cref="MediaFailedEventArgs.ErrorMessage"/> then names the first exception: its type and its message. The source
	/// stays applied and <see cref="CurrentState"/> is not set to <see cref="MediaElementState.Failed"/>. An exception that a MediaFailed handler throws
	/// while such an exception is reported is only logged.
	/// </para>
	/// </remarks>
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
	/// <remarks>Media that has played to its end starts again from its start.</remarks>
	void Play();

	/// <summary>
	/// Seek to a specific position in the currently playing media.
	/// </summary>
	/// <param name="position">The requested position to seek to.</param>
	/// <param name="token">
	/// <see cref="CancellationToken"/> that ends the wait for the seek, with an <see cref="OperationCanceledException"/>. The seek itself goes on, and
	/// whether it then ends or fails is not reported to this call.
	/// </param>
	/// <remarks>
	/// <para>
	/// A <paramref name="position"/> outside of the current media item is clamped to it: a position before the start seeks to the start, and a
	/// position beyond <see cref="Duration"/> seeks to the end, like <see cref="Duration"/> itself. The seek then completes and raises
	/// <see cref="MediaElement.SeekCompleted"/> like any other.
	/// </para>
	/// <para>
	/// On Android, iOS and Mac Catalyst the returned task completes, and <see cref="MediaElement.SeekCompleted"/> is raised, once the seek has
	/// ended, also when something else interrupted it: <see cref="Stop"/>, another seek, the playback controls or the restart of a looping media.
	/// </para>
	/// <para>
	/// On Windows the returned task completes once the player has been given the position, and <see cref="MediaElement.SeekCompleted"/> is
	/// raised when the player reports that the seek has ended. Nothing is sought while no media is loaded or the loaded media cannot seek: the
	/// returned task completes, and <see cref="MediaElement.SeekCompleted"/> is not raised.
	/// </para>
	/// </remarks>
	/// <exception cref="InvalidOperationException">The platform player cannot seek yet: it is not initialized, or (on iOS and Mac Catalyst) it has no media item or its media is not ready to play.</exception>
	Task SeekTo(TimeSpan position, CancellationToken token = default);

	/// <summary>
	/// Stops playing the currently playing media and resets the <see cref="Position"/>.
	/// </summary>
	/// <remarks>On Windows the position is reset only when the loaded media can seek.</remarks>
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

	/// <summary>
	/// Runs the platform's update for a change of <see cref="Source"/>, catching the exceptions of the event handlers that it raises (see
	/// <see cref="Source"/>).
	/// </summary>
	internal ValueTask RunSourceUpdate(Func<ValueTask> updateSource, ILogger logger);
}