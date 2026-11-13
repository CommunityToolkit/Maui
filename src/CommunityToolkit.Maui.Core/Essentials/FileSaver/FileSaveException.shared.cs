namespace CommunityToolkit.Maui.Storage;

/// <summary>
/// Exception occurred if the file is not saved
/// </summary>
/// <remarks>
/// Initializes a new instance of <see cref="FileSaveException"/>
/// </remarks>
public sealed class FileSaveException(string message) : Exception(message)
{
}