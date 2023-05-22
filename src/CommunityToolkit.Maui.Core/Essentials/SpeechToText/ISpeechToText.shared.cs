using System.Globalization;

namespace CommunityToolkit.Maui.Media;

/// <summary>
/// Allows the user to convert speech to text in real time.
/// </summary>
public interface ISpeechToText : IAsyncDisposable
{
	/// <summary>
	/// Converts speech to text in real time.
	/// </summary>
	/// <param name="culture">Speak language</param>
	/// <param name="recognitionResult">Intermediate recognition result.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns>Final recognition result</returns>
	Task<SpeechToTextResult> ListenAsync(CultureInfo culture, IProgress<string>? recognitionResult, CancellationToken cancellationToken);

	/// <summary>
	/// Request permissions for speech to text.
	/// </summary>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns>True if permissions granted</returns>
	Task<bool> RequestPermissions(CancellationToken cancellationToken);
}