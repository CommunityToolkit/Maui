using System.Diagnostics.CodeAnalysis;
using System.Runtime.ExceptionServices;

namespace CommunityToolkit.Maui.Storage;

/// <summary>
/// Result of the <see cref="IFileSaver"/>
/// </summary>
/// <param name="FilePath">
/// The saved file path. On Android, a saved document whose file has no path that its documents provider tells, such as a document of a cloud
/// provider, is reported by its content URI (<c>content://...</c>).
/// </param>
/// <param name="Exception">Exception if operation failed</param>
public record FileSaverResult(string? FilePath, Exception? Exception)
{
	/// <summary>
	/// Check if the operation was successful.
	/// </summary>
	[MemberNotNullWhen(true, nameof(FilePath))]
	[MemberNotNullWhen(false, nameof(Exception))]
	public bool IsSuccessful => Exception is null;

	/// <summary>
	/// Check if the operation was cancelled.
	/// </summary>
	public bool IsCancelled => Exception is OperationCanceledException;

	/// <summary>
	/// Check if the operation was successful.
	/// </summary>
	[MemberNotNull(nameof(FilePath))]
	public void EnsureSuccess()
	{
		if (!IsSuccessful)
		{
			ExceptionDispatchInfo.Throw(Exception);
		}
	}
}