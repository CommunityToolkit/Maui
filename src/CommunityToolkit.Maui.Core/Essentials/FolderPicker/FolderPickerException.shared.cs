using System.Runtime.Serialization;

namespace CommunityToolkit.Maui.Storage;

/// <summary>
/// Exception occurred if error has occurred during picking the folder
/// </summary>
[Serializable]
public class FolderPickerException : Exception
{
	/// <summary>
	/// Constructor for <see cref="FolderPickerException"/>
	/// </summary>
	public FolderPickerException()
	{
	}

	/// <summary>
	/// Constructor for <see cref="FolderPickerException"/>
	/// </summary>
	/// <param name="message"><see cref="Exception.Message"/></param>
	public FolderPickerException(string message) : base(message)
	{

	}

	/// <summary>
	/// Constructor for <see cref="FolderPickerException"/>
	/// </summary>
	public FolderPickerException(string message, Exception innerException) : base(message, innerException)
	{
	}

	/// <summary>
	/// Constructor for <see cref="FolderPickerException"/>
	/// </summary>
	protected FolderPickerException(SerializationInfo info, StreamingContext context) : base(info, context)
	{
	}
}