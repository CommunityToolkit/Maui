namespace CommunityToolkit.Maui.Storage;

/// <summary>
/// Exception occurred if file is not saved
/// </summary>
public sealed class FileSaveException : Exception
{
	/// <summary>
	/// Initializes a new instance of <see cref="FileSaveException"/>
	/// </summary>
	public FileSaveException(string message) : base(message)
	{

	}
}