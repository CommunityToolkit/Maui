namespace CommunityToolkit.Maui.Media;

/// <summary>
/// <see cref="EventArgs"/> for <see cref="ISpeechToText.RecognitionResultUpdated"/>
/// </summary>
public class SpeechToTextRecognitionResultUpdatedEventArgs : EventArgs
{
	/// <summary>
	/// Initialize a new instance of <see cref="SpeechToTextRecognitionResultUpdatedEventArgs"/>
	/// </summary>
	public SpeechToTextRecognitionResultUpdatedEventArgs(string recognitionResult)
	{
		RecognitionResult = recognitionResult;
	}

	/// <summary>
	/// Speech recognition result
	/// </summary>
	public string RecognitionResult { get; }
}