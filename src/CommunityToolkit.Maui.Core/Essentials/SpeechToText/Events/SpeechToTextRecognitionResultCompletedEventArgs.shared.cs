namespace CommunityToolkit.Maui.Media;

/// <summary>
/// <see cref="EventArgs"/> for <see cref="ISpeechToText.RecognitionResultCompleted"/>
/// </summary>
public class SpeechToTextRecognitionResultCompletedEventArgs : EventArgs
{
	/// <summary>
	/// Initialize a new instance of <see cref="SpeechToTextRecognitionResultCompletedEventArgs"/>
	/// </summary>
	/// <param name="recognitionResult">Speech recognition result</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="recognitionResult"/> is null.</exception>
	public SpeechToTextRecognitionResultCompletedEventArgs(SpeechToTextResult recognitionResult)
	{
		ArgumentNullException.ThrowIfNull(recognitionResult);
		RecognitionResult = recognitionResult;
	}

	/// <summary>
	/// Speech recognition result
	/// </summary>
	public SpeechToTextResult RecognitionResult { get; }
}