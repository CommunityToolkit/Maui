namespace CommunityToolkit.Maui.Storage;

/// <summary>
/// Exception occured if error has occured during picking the folder
/// </summary>
public class FolderPickerException : Exception
{
	/// <summary>
	/// Inititalizes a new instance of <see cref="FolderPickerException"/>
	/// </summary>
	public FolderPickerException(string message) : base(message)
	{

	}
}