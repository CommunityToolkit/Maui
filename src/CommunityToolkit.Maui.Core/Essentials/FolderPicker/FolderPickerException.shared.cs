namespace CommunityToolkit.Maui.Storage;

/// <summary>
/// Exception occurred if error has occurred during picking the folder
/// </summary>
public sealed class FolderPickerException : Exception
{
	/// <summary>
	/// Initializes a new instance of <see cref="FolderPickerException"/>
	/// </summary>
	public FolderPickerException(string message) : base(message)
	{

	}
}