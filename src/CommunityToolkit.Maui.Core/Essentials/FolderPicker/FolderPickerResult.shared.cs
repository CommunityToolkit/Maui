using System.Diagnostics.CodeAnalysis;
using CommunityToolkit.Maui.Core.Primitives;

namespace CommunityToolkit.Maui.Storage;

/// <summary>
/// Result of the <see cref="IFolderPicker"/>
/// </summary>
/// <param name="Folder"><see cref="Folder"/></param>
/// <param name="Exception">Exception if operation failed</param>
public record FolderPickerResult(Folder? Folder, Exception? Exception)
{
	/// <summary>
	/// Check if operation was successful.
	/// </summary>
	[MemberNotNullWhen(true, nameof(Folder))]
	[MemberNotNullWhen(false, nameof(Exception))]
	public bool IsSuccessful => Exception is null;

	/// <summary>
	/// Check if operation was successful.
	/// </summary>
	[MemberNotNull(nameof(Folder))]
	public void EnsureSuccess()
	{
		if (!IsSuccessful)
		{
			throw Exception;
		}
	}
}