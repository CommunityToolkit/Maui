namespace CommunityToolkit.Maui.Storage;

/// <summary>
/// Allows the user to save files to the filesystem
/// </summary>
public interface IFileSaver
{
	/// <summary>
	/// Saves a file to a target folder on the file system
	/// </summary>
	/// <param name="initialPath">Initial path</param>
	/// <param name="fileName">File name with extension</param>
	/// <param name="stream"><see cref="Stream"/></param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	Task<FileSaverResult> SaveAsync(string initialPath, string fileName, Stream stream, CancellationToken cancellationToken);

	/// <summary>
	/// Saves a file to the default folder on the file system
	/// </summary>
	/// <param name="fileName">File name with extension</param>
	/// <param name="stream"><see cref="Stream"/></param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	Task<FileSaverResult> SaveAsync(string fileName, Stream stream, CancellationToken cancellationToken);
}