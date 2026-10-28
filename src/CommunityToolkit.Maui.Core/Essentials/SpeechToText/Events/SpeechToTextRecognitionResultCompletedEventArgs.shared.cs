namespace CommunityToolkit.Maui.Media;

/// <summary>
/// <see cref="EventArgs"/> for <see cref="ISpeechToText.RecognitionResultCompleted"/>
/// </summary>
public class SpeechToTextRecognitionResultCompletedEventArgs : EventArgs
{
	/// <summary>
	/// Initialize a new instance of <see cref="SpeechToTextRecognitionResultCompletedEventArgs"/>
	/// </summary>
	public SpeechToTextRecognitionResultCompletedEventArgs(SpeechToTextResult recognitionResult)
	{
		RecognitionResult = recognitionResult;
	}

	/// <summary>
	/// Speech recognition result
	/// </summary>
	public SpeechToTextResult RecognitionResult { get; }
}