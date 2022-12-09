namespace CommunityToolkit.Maui.MediaElement;

public class MediaFailedEventArgs : EventArgs
{
	/// <summary>
	/// Gets a description of why the media failed to load and/or play.
	/// </summary>
	public string ErrorMessage { get; } = string.Empty;

	internal MediaFailedEventArgs(string errorMessage)
	{
		ErrorMessage=errorMessage;
	}
}
