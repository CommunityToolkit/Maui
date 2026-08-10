namespace CommunityToolkit.Maui.Core;

static class SpeechToTextOptionsDefaults
{
	public const bool ShouldReportPartialResults = true;

	public static TimeSpan AutoStopSilenceTimeout => TimeSpan.MaxValue;
}