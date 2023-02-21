namespace CommunityToolkit.Maui.Storage;

/// <summary>
/// Allows picking folders from the file system
/// </summary>
public interface IFolderPicker
{
	/// <summary>
	/// Allows the user to pick a folder from the file system
	/// </summary>
	/// <param name="initialPath">Initial path</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns><see cref="FolderPickerResult"/></returns>
	Task<FolderPickerResult> PickAsync(string initialPath, CancellationToken cancellationToken);

	/// <summary>
	/// Allows the user to pick a folder from the file system
	/// </summary>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns><see cref="FolderPickerResult"/></returns>
	Task<FolderPickerResult> PickAsync(CancellationToken cancellationToken);
}