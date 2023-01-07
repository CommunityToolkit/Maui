#if !(ANDROID || IOS || WINDOWS || MACCATALYST || TIZEN)
global using PlatformMediaView = System.Object;
#elif ANDROID
global using PlatformMediaView = Com.Google.Android.Exoplayer2.IExoPlayer;
#elif IOS || MACCATALYST
global using PlatformMediaView = AVFoundation.AVPlayer;
#elif WINDOWS
global using PlatformMediaView = Microsoft.UI.Xaml.Controls.MediaPlayerElement;
#elif TIZEN
global using PlatformMediaView = CommunityToolkit.Maui.MediaView.TizenPlayer;
#endif

using Microsoft.Extensions.Logging;

namespace CommunityToolkit.Maui.MediaView;

/// <summary>
/// An class that acts as a manager for an <see cref="IMediaView"/> instance.
/// </summary>
public partial class MediaManager
{
	readonly IMauiContext mauiContext;
	readonly IMediaView mediaView;
#if ANDROID || IOS || MACCATALYST || WINDOWS || TIZEN
	/// <summary>
	/// The platform-specific media player.
	/// </summary>
	protected PlatformMediaView? player;
#endif

	/// <summary>
	/// Initializes a new instance of the <see cref="MediaManager"/> class.
	/// </summary>
	/// <param name="context">This application's <see cref="IMauiContext"/>.</param>
	/// <param name="mediaView">The <see cref="IMediaView"/> instance that is managed through this class.</param>
	public MediaManager(IMauiContext context, IMediaView mediaView)
	{
		this.mediaView = mediaView;
		mauiContext = context;

		Logger = mauiContext.Services.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(MediaManager));
	}

	/// <summary>
	/// Gets the <see cref="ILogger"/> instance for logging purposes.
	/// </summary>
	public ILogger Logger { get; }

	/// <summary>
	/// Invokes the play operation on the platform element.
	/// </summary>
	public void Play()
	{
		PlatformPlay();
	}

	/// <summary>
	/// Invokes the pause operation on the platform element.
	/// </summary>
	public void Pause()
	{
		PlatformPause();
	}

	/// <summary>
	/// Invokes the seek operation on the platform element.
	/// </summary>
	/// <param name="position">The position to seek to.</param>
	public void Seek(TimeSpan position)
	{
		PlatformSeek(position);
	}

	/// <summary>
	/// Invokes the stop operation on the platform element.
	/// </summary>
	public void Stop()
	{
		PlatformStop();
	}

	/// <summary>
	/// Update the media source.
	/// </summary>
	public void UpdateSource()
	{
		PlatformUpdateSource();
	}

	/// <summary>
	/// Update the media playback speed.
	/// </summary>
	public void UpdateSpeed()
	{
		PlatformUpdateSpeed();
	}

	/// <summary>
	/// Update whether of not the screen should stay on while media is being played.
	/// </summary>
	public void UpdateShouldKeepScreenOn()
	{
		PlatformUpdateShouldKeepScreenOn();
	}

	/// <summary>
	/// Update whether or not the media should start playing from the beginning
	/// when it reached the end.
	/// </summary>
	public void UpdateShouldLoopPlayback()
	{
		PlatformUpdateShouldLoopPlayback();
	}

	/// <summary>
	/// Update whether or not to show the platform playback controls.
	/// </summary>
	public void UpdateShouldShowPlaybackControls()
	{
		PlatformUpdateShouldShowPlaybackControls();
	}

	/// <summary>
	/// Update the media player status.
	/// </summary>
	public void UpdateStatus()
	{
		PlatformUpdatePosition();
	}

	/// <summary>
	/// Update the media playback volume.
	/// </summary>
	public void UpdateVolume()
	{
		PlatformUpdateVolume();
	}

	/// <summary>
	/// Invokes the platform play functionality and starts media playback.
	/// </summary>
	protected virtual partial void PlatformPlay();

	/// <summary>
	/// Invokes the platform pause functionality and pauses media playback.
	/// </summary>
	protected virtual partial void PlatformPause();

	/// <summary>
	/// Invokes the platform seek functionality and seeks to a specific position.
	/// </summary>
	/// <param name="position">The position to seek to.</param>
	protected virtual partial void PlatformSeek(TimeSpan position);

	/// <summary>
	/// Invokes the platform stop functionality and stops media playback.
	/// </summary>
	protected virtual partial void PlatformStop();

	/// <summary>
	/// Invokes the platform functionality to update the media source.
	/// </summary>
	protected virtual partial void PlatformUpdateSource();

	/// <summary>
	/// Invokes the platform functionality to update the media playback speed.
	/// </summary>
	protected virtual partial void PlatformUpdateSpeed();

	/// <summary>
	/// Invokes the platform functionality to toggle the media playback loop behavior.
	/// </summary>
	protected virtual partial void PlatformUpdateShouldLoopPlayback();

	/// <summary>
	/// Invokes the platform functionality to toggle keeping the screen on
	/// during media playback.
	/// </summary>
	protected virtual partial void PlatformUpdateShouldKeepScreenOn();

	/// <summary>
	/// Invokes the platform functionality to show or hide the platform playback controls.
	/// </summary>
	protected virtual partial void PlatformUpdateShouldShowPlaybackControls();

	/// <summary>
	/// Invokes the platform functionality to update the media playback position.
	/// </summary>
	protected virtual partial void PlatformUpdatePosition();

	/// <summary>
	/// Invokes the platform functionality to update the media playback volume.
	/// </summary>
	protected virtual partial void PlatformUpdateVolume();
}

#if !(WINDOWS || ANDROID || IOS || MACCATALYST || TIZEN)
partial class MediaManager
{
	protected virtual partial void PlatformPlay() { }
	protected virtual partial void PlatformPause() { }
	protected virtual partial void PlatformSeek(TimeSpan position) { }
	protected virtual partial void PlatformStop() { }
	protected virtual partial void PlatformUpdateSource() { }
	protected virtual partial void PlatformUpdateSpeed() { }
	protected virtual partial void PlatformUpdateShouldShowPlaybackControls() { }
	protected virtual partial void PlatformUpdatePosition() { }
	protected virtual partial void PlatformUpdateVolume() { }
	protected virtual partial void PlatformUpdateShouldKeepScreenOn() { }
	protected virtual partial void PlatformUpdateShouldLoopPlayback() { }
}
#endif