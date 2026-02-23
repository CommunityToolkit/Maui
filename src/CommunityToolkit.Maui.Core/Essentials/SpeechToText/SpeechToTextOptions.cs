using System.Globalization;

namespace CommunityToolkit.Maui.Media;

/// <summary>
/// Options for configuring speech recognition.
/// </summary>
public class SpeechToTextOptions
{
	/// <summary>
	/// The culture to use for speech recognition.
	/// </summary>
	public required CultureInfo Culture { get; init; }

	/// <summary>
	/// Include partial recognition results.
	/// </summary>
	public bool ShouldReportPartialResults { get; init; } = true;
	
	/// <summary>
	/// The duration of continuous silence after which speech recognition will automatically stop.
	/// Use <see cref="TimeSpan.MaxValue"/> (the default) to indicate that auto-stop based on silence is disabled.
	/// </summary>
	public TimeSpan AutoStopSilenceTimeout { get; init; } = TimeSpan.MaxValue;
}