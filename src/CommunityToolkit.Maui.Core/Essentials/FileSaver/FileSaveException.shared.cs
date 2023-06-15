namespace CommunityToolkit.Maui.Storage;

/// <summary>
/// Exception occurred if file is not saved
/// </summary>
public class FileSaveException : Exception
{
	/// <summary>
	/// Constructor for <see cref="FileSaveException"/>
	/// </summary>
	/// <param name="message"><see cref="Exception.Message"/></param>
	public FileSaveException(string message) : base(message)
	{

	}
}