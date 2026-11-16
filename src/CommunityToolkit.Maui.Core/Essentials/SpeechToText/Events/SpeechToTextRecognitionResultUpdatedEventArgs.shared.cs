namespace CommunityToolkit.Maui.Media;

/// <summary>
/// <see cref="EventArgs"/> for <see cref="ISpeechToText.RecognitionResultUpdated"/>
/// </summary>
public class SpeechToTextRecognitionResultUpdatedEventArgs : EventArgs
{
	/// <summary>
	/// Initialize a new instance of <see cref="SpeechToTextRecognitionResultUpdatedEventArgs"/>
	/// </summary>
	/// <param name="recognitionResult">Speech recognition result</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="recognitionResult"/> is null.</exception>
	public SpeechToTextRecognitionResultUpdatedEventArgs(string recognitionResult)
	{
		ArgumentNullException.ThrowIfNull(recognitionResult);
		RecognitionResult = recognitionResult;
	}

	/// <summary>
	/// Speech recognition result
	/// </summary>
	public string RecognitionResult { get; }
}