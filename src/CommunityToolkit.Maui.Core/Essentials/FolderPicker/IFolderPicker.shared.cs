using CommunityToolkit.Maui.Core.Primitives;

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
	/// <returns><see cref="Folder"/></returns>
	Task<Folder> PickAsync(string initialPath, CancellationToken cancellationToken);

	/// <summary>
	/// Allows the user to pick a folder from the file system
	/// </summary>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns><see cref="Folder"/></returns>
	Task<Folder> PickAsync(CancellationToken cancellationToken);

	/// <summary>
	/// Allows the user to pick a folder from the file system.
	/// This method doesn't throw Exception.
	/// </summary>
	/// <param name="initialPath">Initial path</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns><see cref="FolderPickerResult"/></returns>
	async Task<FolderPickerResult> PickSafeAsync(string initialPath, CancellationToken cancellationToken)
	{
		try
		{
			var folder = await PickAsync(initialPath, cancellationToken);
			return new FolderPickerResult(folder, null);
		}
		catch (Exception e)
		{
			return new FolderPickerResult(null, e);
		}
	}

	/// <summary>
	/// Allows the user to pick a folder from the file system.
	/// This method doesn't throw Exception.
	/// </summary>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns><see cref="FolderPickerResult"/></returns>
	async Task<FolderPickerResult> PickSafeAsync(CancellationToken cancellationToken)
	{
		try
		{
			var folder = await PickAsync(cancellationToken);
			return new FolderPickerResult(folder, null);
		}
		catch (Exception e)
		{
			return new FolderPickerResult(null, e);
		}
	}
}