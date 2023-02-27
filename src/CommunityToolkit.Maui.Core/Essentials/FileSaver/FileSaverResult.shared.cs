using System.Diagnostics.CodeAnalysis;

namespace CommunityToolkit.Maui.Storage;

/// <summary>
/// Result of the <see cref="IFileSaver"/>
/// </summary>
/// <param name="FilePath">Saved file path</param>
/// <param name="Exception">Exception if operation failed</param>
public record FileSaverResult(string? FilePath, Exception? Exception)
{
	/// <summary>
	/// Check if operation was successful.
	/// </summary>
	[MemberNotNullWhen(true, nameof(FilePath))]
	[MemberNotNullWhen(false, nameof(Exception))]
	public bool IsSuccessful => Exception is null;

	/// <summary>
	/// Check if operation was successful.
	/// </summary>
	[MemberNotNull(nameof(FilePath))]
	public void EnsureSuccess()
	{
		if (!IsSuccessful)
		{
			throw Exception;
		}
	}
}