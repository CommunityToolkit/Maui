#if !(ANDROID || IOS || WINDOWS || MACCATALYST || TIZEN)
global using PlatformMediaElement = System.Object;
#elif ANDROID
global using PlatformMediaElement = Com.Google.Android.Exoplayer2.IExoPlayer;
#elif IOS || MACCATALYST
global using PlatformMediaElement = AVFoundation.AVPlayer;
#elif WINDOWS
global using PlatformMediaElement = Microsoft.UI.Xaml.Controls.MediaPlayerElement;
#elif TIZEN
global using PlatformMediaElement = CommunityToolkit.Maui.Core.Views.TizenPlayer;
#endif

using Microsoft.Extensions.Logging;

namespace CommunityToolkit.Maui.Core.Views;

/// <summary>
/// A class that acts as a manager for an <see cref="IMediaElement"/> instance.
/// </summary>
public partial class MediaManager
{
	/// <summary>
	/// Initializes a new instance of the <see cref="MediaManager"/> class.
	/// </summary>
	/// <param name="context">This application's <see cref="IMauiContext"/>.</param>
	/// <param name="mediaElement">The <see cref="IMediaElement"/> instance that is managed through this class.</param>
	public MediaManager(IMauiContext context, IMediaElement mediaElement)
	{
		this.MediaElement = mediaElement;
		MauiContext = context;

		Logger = MauiContext.Services.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(MediaManager));
	}

	/// <summary>
	/// The <see cref="IMediaElement"/> instance managed by this manager.
	/// </summary>
	protected IMediaElement MediaElement { get; }

	/// <summary>
	/// The <see cref="IMauiContext"/> used by this class.
	/// </summary>
	protected IMauiContext MauiContext { get; }

	/// <summary>
	/// Gets the <see cref="ILogger"/> instance for logging purposes.
	/// </summary>
	protected ILogger Logger { get; }


#if ANDROID || IOS || MACCATALYST || WINDOWS || TIZEN
	/// <summary>
	/// The platform-specific media player.
	/// </summary>
	protected PlatformMediaElement? Player { get; set; }
#endif

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
	/// Update the media aspect.
	/// </summary>
	public void UpdateAspect()
	{
		PlatformUpdateAspect();
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
	/// Update whether or not the audio should be muted.
	/// </summary>
	public void UpdateShouldMute()
	{
		PlatformUpdateShouldMute();
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
	/// Invokes the platform functionality to update the media aspect.
	/// </summary>
	protected virtual partial void PlatformUpdateAspect();

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
	/// Invokes the platform functionality to toggle muting the audio.
	/// </summary>
	protected virtual partial void PlatformUpdateShouldMute();

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
	protected virtual partial void PlatformUpdateAspect() { }
	protected virtual partial void PlatformUpdateSource() { }
	protected virtual partial void PlatformUpdateSpeed() { }
	protected virtual partial void PlatformUpdateShouldShowPlaybackControls() { }
	protected virtual partial void PlatformUpdatePosition() { }
	protected virtual partial void PlatformUpdateVolume() { }
	protected virtual partial void PlatformUpdateShouldKeepScreenOn() { }
	protected virtual partial void PlatformUpdateShouldMute() { }
	protected virtual partial void PlatformUpdateShouldLoopPlayback() { }
}
#endif