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
}