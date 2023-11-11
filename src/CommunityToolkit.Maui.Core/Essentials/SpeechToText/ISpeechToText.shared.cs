using System.Globalization;

namespace CommunityToolkit.Maui.Media;

/// <summary>
/// Allows the user to convert speech to text in real time.
/// </summary>
public interface ISpeechToText : IAsyncDisposable
{
	/// <summary>
	/// Triggers when SpeechToText has real time updates
	/// </summary>
	event EventHandler<SpeechToTextRecognitionResultUpdatedEventArgs> RecognitionResultUpdated;

	/// <summary>
	/// Triggers when SpeechToText has completed
	/// </summary>
	event EventHandler<SpeechToTextRecognitionResultCompletedEventArgs> RecognitionResultCompleted;

	/// <summary>
	/// Triggers when <see cref="CurrentState"/> has changed
	/// </summary>
	event EventHandler<SpeechToTextStateChangedEventArgs> StateChanged;

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
	Task<SpeechToTextResult> ListenAsync(CultureInfo culture, IProgress<string>? recognitionResult, CancellationToken cancellationToken = default);

	/// <summary>
	/// Starts the SpeechToText service
	/// </summary>
	/// <remarks>
	/// Real time speech recognition results will be surfaced via <see cref="RecognitionResultUpdated"/> and <see cref="RecognitionResultCompleted"/>
	/// </remarks>
	/// <param name="culture">Speak language</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	Task StartListenAsync(CultureInfo culture, CancellationToken cancellationToken = default);

	/// <summary>
	/// Stops the SpeechToText service
	/// </summary>
	/// <remarks>
	/// Speech recognition results will be surfaced via <see cref="RecognitionResultCompleted"/>
	/// </remarks>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	Task StopListenAsync(CancellationToken cancellationToken = default);

	/// <summary>
	/// Request permissions for speech to text.
	/// </summary>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns>True if permissions granted</returns>
	Task<bool> RequestPermissions(CancellationToken cancellationToken = default);
}