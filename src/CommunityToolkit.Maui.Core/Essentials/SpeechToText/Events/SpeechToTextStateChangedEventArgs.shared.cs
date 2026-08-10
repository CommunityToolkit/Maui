namespace CommunityToolkit.Maui.Media;

/// <summary>
/// <see cref="EventArgs"/> for <see cref="ISpeechToText.StateChanged"/>
/// </summary>
public class SpeechToTextStateChangedEventArgs : EventArgs
{
	/// <summary>
	/// Initialize a new instance of <see cref="SpeechToTextStateChangedEventArgs"/>
	/// </summary>
	public SpeechToTextStateChangedEventArgs(SpeechToTextState state)
	{
		State = state;
	}

	/// <summary>
	/// Speech To Text State
	/// </summary>
	public SpeechToTextState State { get; }
}