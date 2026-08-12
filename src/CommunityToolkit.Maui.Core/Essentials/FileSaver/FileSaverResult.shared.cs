using System.Diagnostics.CodeAnalysis;
using System.Runtime.ExceptionServices;
using CommunityToolkit.Maui.Core;

namespace CommunityToolkit.Maui.Storage;

/// <summary>
/// Result of the <see cref="IFileSaver"/>
/// </summary>
/// <param name="FilePath">The saved file path</param>
/// <param name="Exception">Exception if operation failed</param>
public record FileSaverResult(string? FilePath, Exception? Exception) : IResult
{
	/// <inheritdoc/>
	[MemberNotNullWhen(true, nameof(FilePath))]
	[MemberNotNullWhen(false, nameof(Exception))]
	public bool IsSuccessful => Exception is null;

	/// <inheritdoc/>
	[MemberNotNull(nameof(FilePath))]
	public void EnsureSuccess()
	{
		if (!IsSuccessful)
		{
			ExceptionDispatchInfo.Throw(Exception);
		}
	}
}