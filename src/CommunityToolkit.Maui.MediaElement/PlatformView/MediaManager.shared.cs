#if !(ANDROID || IOS || WINDOWS || MACCATALYST)
global using PlatformMediaView = System.Object;
#elif ANDROID
global using PlatformMediaView = Com.Google.Android.Exoplayer2.IExoPlayer;
#elif IOS || MACCATALYST
global using PlatformMediaView = AVFoundation.AVPlayer;
#elif WINDOWS
global using PlatformMediaView = Microsoft.UI.Xaml.Controls.MediaPlayerElement;
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace CommunityToolkit.Maui.MediaElement;

public partial class MediaManager
{
	readonly IMauiContext mauiContext;
	readonly IMediaElement mediaElement;
	PlatformMediaView? player;

	public MediaManager(IMauiContext context, IMediaElement mediaElement)
	{
		this.mauiContext = context;
		this.mediaElement = mediaElement;
	}

	public void Play(TimeSpan timeSpan)
	{
		PlatformPlay(timeSpan);
	}

	public void Pause(TimeSpan timeSpan)
	{
		PlatformPause(timeSpan);
	}

	public void Stop(TimeSpan timeSpan)
	{
		PlatformStop(timeSpan);
	}

	public void UpdateSource()
	{
		PlatformUpdateSource();
	}
	public void UpdateSpeed()
	{
		PlatformUpdateSpeed();
	}
	public void UpdateShowsPlaybackControls()
	{
		PlatformUpdateShowsPlaybackControls();
	}

	public void UpdatePosition()
	{
		PlatformUpdatePosition();
	}

	public void UpdateStatus()
	{
		PlatformUpdateStatus();
	}

	public void UpdateVolume()
	{
		PlatformUpdateVolume();
	}

	public void UpdateIsLooping()
	{
		PlatformUpdateIsLooping();
	}

	protected virtual partial void PlatformPlay(TimeSpan timeSpan);
	protected virtual partial void PlatformPause(TimeSpan timeSpan);
	protected virtual partial void PlatformStop(TimeSpan timeSpan);
	protected virtual partial void PlatformUpdateSource();
	protected virtual partial void PlatformUpdateSpeed();
	protected virtual partial void PlatformUpdateShowsPlaybackControls();
	protected virtual partial void PlatformUpdatePosition();
	protected virtual partial void PlatformUpdateStatus();
	protected virtual partial void PlatformUpdateVolume();
	//TODO: Check if just android has support for this
	protected virtual partial void PlatformUpdateIsLooping();
}


#if !(WINDOWS || ANDROID || IOS || MACCATALYST)
partial class MediaManager
{
	protected virtual partial void PlatformPlay(TimeSpan timeSpan)
	{

	}

	protected virtual partial void PlatformPause(TimeSpan timeSpan)
	{

	}

	protected virtual partial void PlatformStop(TimeSpan timeSpan)
	{

	}

	protected virtual partial void PlatformUpdateSource()
	{

	}

	protected virtual partial void PlatformUpdateSpeed()
	{

	}

	protected virtual partial void PlatformUpdateShowsPlaybackControls()
	{

	}

	protected virtual partial void PlatformUpdatePosition()
	{

	}

	protected virtual partial void PlatformUpdateStatus()
	{

	}

	protected virtual partial void PlatformUpdateVolume()
	{

	}


	protected virtual partial void PlatformUpdateIsLooping()
	{

	}
}
#endif