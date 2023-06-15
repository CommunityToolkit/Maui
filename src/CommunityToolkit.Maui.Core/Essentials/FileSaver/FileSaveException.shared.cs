using System.Runtime.Serialization;

namespace CommunityToolkit.Maui.Storage;

/// <summary>
/// Exception occurred if file is not saved
/// </summary>
[Serializable]
public class FileSaveException : Exception
{
	/// <summary>
	/// Constructor for <see cref="FileSaveException"/>
	/// </summary>
	public FileSaveException()
	{
	}

	/// <summary>
	/// Constructor for <see cref="FileSaveException"/>
	/// </summary>
	/// <param name="message"><see cref="Exception.Message"/></param>
	public FileSaveException(string message) : base(message)
	{

	}

	/// <summary>
	/// Constructor for <see cref="FileSaveException"/>
	/// </summary>
	public FileSaveException(string message, Exception innerException) : base(message, innerException)
	{
	}

	/// <summary>
	/// Constructor for <see cref="FileSaveException"/>
	/// </summary>
	protected FileSaveException(SerializationInfo info, StreamingContext context) : base(info, context)
	{
	}
}