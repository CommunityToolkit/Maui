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
	event EventHandler<OnSpeechToTextRecognitionResultUpdated> RecognitionResultUpdated;

	/// <summary>
	/// Final recognition result.
	/// </summary>
	event EventHandler<OnSpeechToTextRecognitionResultCompleted> RecognitionResultCompleted;

	/// <summary>
	/// Current listening state
	/// </summary>
	SpeechToTextState CurrentState { get; }

	/// <summary>
	/// Converts speech to text in real time.
	/// </summary>
	/// <param name="culture">Speak language</param>
	/// <param name="recognitionResult">Intermediate recognition result.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns>Final recognition result</returns>
	Task<SpeechToTextResult> ListenAsync(CultureInfo culture, IProgress<string>? recognitionResult, CancellationToken cancellationToken);

	/// <summary>
	/// Converts speech to text in real time.
	/// </summary>
	/// <param name="culture">Speak language</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	Task StartListeningAsync(CultureInfo culture, CancellationToken cancellationToken);

	/// <summary>
	/// Stop listening.
	/// </summary>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	Task StopListeningAsync(CancellationToken cancellationToken);
	
	/// <summary>
	/// Request permissions for speech to text.
	/// </summary>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns>True if permissions granted</returns>
	Task<bool> RequestPermissions(CancellationToken cancellationToken);
}

/// <summary>
/// Speech To Text listening state
/// </summary>
public enum SpeechToTextState
{
	/// <summary>
	/// Listening is active
	/// </summary>
	Listening,

	/// <summary>
	/// Listening is stopped
	/// </summary>
	Stopped
}