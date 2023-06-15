namespace CommunityToolkit.Maui.Storage;

/// <summary>
/// Exception occurred if error has occurred during picking the folder
/// </summary>
public sealed class FolderPickerException : Exception
{
	/// <summary>
	/// Constructor for <see cref="FolderPickerException"/>
	/// </summary>
	/// <param name="message"><see cref="Exception.Message"/></param>
	public FolderPickerException(string message) : base(message)
	{

	}
}