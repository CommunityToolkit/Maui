namespace CommunityToolkit.Maui.MediaElement;

public sealed class MediaFailedEventArgs : EventArgs
{
	/// <summary>
	/// Gets a description of why the media failed to load and/or play.
	/// </summary>
	public string ErrorMessage { get; } = string.Empty;

	public MediaFailedEventArgs(string errorMessage)
	{
		ErrorMessage = errorMessage;
	}
}
