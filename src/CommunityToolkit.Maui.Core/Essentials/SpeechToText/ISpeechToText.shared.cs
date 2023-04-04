using System.Globalization;

namespace CommunityToolkit.Maui.SpeechToText;

/// <summary>
/// Allows the user to convert speech to text in real time.
/// </summary>
public interface ISpeechToText : IAsyncDisposable
{
	/// <summary>
	/// Converts speech to text in real time.
	/// </summary>
	/// <param name="culture">Speak language</param>
	/// <param name="recognitionResult">Intermediate convertion result.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns>Final convertion result</returns>
	Task<string> Listen(CultureInfo culture, IProgress<string>? recognitionResult, CancellationToken cancellationToken);
}