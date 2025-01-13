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
	/// Starts the SpeechToText service
	/// </summary>
	/// <remarks>
	/// Real time speech recognition results will be surfaced via <see cref="RecognitionResultUpdated"/> and <see cref="RecognitionResultCompleted"/>
	/// </remarks>
	/// <param name="options">The <see cref="SpeechToTextOptions"/> used to customize how the speech will be detected and converted to text.</param>
	/// <param name="cancellationToken">A <see cref="CancellationToken"/> that provides the ability to cancel the starting process. Note that listening has started then you can only stop listening by calling <see cref="StopListenAsync"/>.</param>
	Task StartListenAsync(SpeechToTextOptions options, CancellationToken cancellationToken = default);

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