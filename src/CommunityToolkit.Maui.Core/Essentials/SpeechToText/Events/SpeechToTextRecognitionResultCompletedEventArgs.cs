namespace CommunityToolkit.Maui.Media;

/// <summary>
/// <see cref="EventArgs"/> for <see cref="ISpeechToText.RecognitionResultCompleted"/>
/// </summary>
public class SpeechToTextRecognitionResultCompletedEventArgs : EventArgs
{
	/// <summary>
	/// Initialize a new instance of <see cref="SpeechToTextRecognitionResultCompletedEventArgs"/>
	/// </summary>
	public SpeechToTextRecognitionResultCompletedEventArgs(string recognitionResult)
	{
		RecognitionResult = recognitionResult;
	}

	/// <summary>
	/// Speech recognition result
	/// </summary>
	public string RecognitionResult { get; }
}