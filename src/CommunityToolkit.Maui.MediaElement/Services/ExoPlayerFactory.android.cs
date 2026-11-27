using Android.Content;
using AndroidX.Media3.Common;
using AndroidX.Media3.ExoPlayer;
using AndroidX.Media3.ExoPlayer.TrackSelection;

namespace CommunityToolkit.Maui.Media.Services;

static class ExoPlayerFactory
{
	// Several MediaElements can each own a player, so the buffer holds less than Media3's defaults (50 s minimum and maximum, 2.5 s and 5 s to start)
	const int minimumBufferMilliseconds = 15000;
	const int maximumBufferMilliseconds = 50000;
	const int bufferForPlaybackMilliseconds = 2500;
	const int bufferForPlaybackAfterRebufferMilliseconds = 5000;

	public static (IExoPlayer Player, DefaultTrackSelector TrackSelector) Create(Context context)
	{
		var audioAttributes = new AndroidX.Media3.Common.AudioAttributes.Builder()?
			.SetContentType(C.AudioContentTypeMusic)?
			.SetUsage(C.UsageMedia)?
			.Build();

		var trackSelector = new DefaultTrackSelector(context);
		var trackSelectionParameters = trackSelector.BuildUponParameters()?
			.SetPreferredAudioLanguage(C.LanguageUndetermined)?
			.SetPreferredTextLanguage(C.LanguageUndetermined)?
			.SetIgnoredTextSelectionFlags(C.SelectionFlagAutoselect);
		trackSelector.SetParameters((DefaultTrackSelector.Parameters.Builder?)trackSelectionParameters);

		var loadControlBuilder = new DefaultLoadControl.Builder();
		loadControlBuilder.SetBufferDurationsMs(
			minBufferMs: minimumBufferMilliseconds,
			maxBufferMs: maximumBufferMilliseconds,
			bufferForPlaybackMs: bufferForPlaybackMilliseconds,
			bufferForPlaybackAfterRebufferMs: bufferForPlaybackAfterRebufferMilliseconds);

		var builder = new ExoPlayerBuilder(context) ?? throw new InvalidOperationException("ExoPlayerBuilder returned null");
		builder.SetTrackSelector(trackSelector);
		builder.SetAudioAttributes(audioAttributes, true);
		builder.SetHandleAudioBecomingNoisy(true);
		builder.SetLoadControl(loadControlBuilder.Build());
		var exoPlayer = builder.Build() ?? throw new InvalidOperationException("ExoPlayerBuilder.Build() returned null");

		return (exoPlayer, trackSelector);
	}
}
