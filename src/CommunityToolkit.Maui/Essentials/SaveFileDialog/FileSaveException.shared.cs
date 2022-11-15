namespace CommunityToolkit.Maui.Storage;

/// <summary>
/// Exception occured if file is not saved
/// </summary>
public class FileSaveException : Exception
{
	/// <summary>
	/// Inititalizes a new instance of <see cref="FileSaveException"/>
	/// </summary>
	public FileSaveException(string message) : base(message)
	{

	}
}