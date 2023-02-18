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
	Task<string> SaveAsync(string initialPath, string fileName, Stream stream, CancellationToken cancellationToken);

	/// <summary>
	/// Saves a file to the default folder on the file system
	/// </summary>
	/// <param name="fileName">File name with extension</param>
	/// <param name="stream"><see cref="Stream"/></param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	Task<string> SaveAsync(string fileName, Stream stream, CancellationToken cancellationToken);

	/// <summary>
	/// Saves a file to a target folder on the file system.
	/// This method doesn't throw Exception.
	/// </summary>
	/// <param name="initialPath">Initial path</param>
	/// <param name="fileName">File name with extension</param>
	/// <param name="stream"><see cref="Stream"/></param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	async Task<FileSaverResult> SaveSafeAsync(string initialPath, string fileName, Stream stream, CancellationToken cancellationToken)
	{
		try
		{
			cancellationToken.ThrowIfCancellationRequested();
			var path = await SaveAsync(initialPath, fileName, stream, cancellationToken);
			return new FileSaverResult(path, null);
		}
		catch (Exception e)
		{
			return new FileSaverResult(null, e);
		}
	}

	/// <summary>
	/// Saves a file to the default folder on the file system.
	/// This method doesn't throw Exception.
	/// </summary>
	/// <param name="fileName">File name with extension</param>
	/// <param name="stream"><see cref="Stream"/></param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	async Task<FileSaverResult> SaveSafeAsync(string fileName, Stream stream, CancellationToken cancellationToken)
	{
		try
		{
			cancellationToken.ThrowIfCancellationRequested();
			var path = await SaveAsync(fileName, stream, cancellationToken);
			return new FileSaverResult(path, null);
		}
		catch (Exception e)
		{
			return new FileSaverResult(null, e);
		}
	}
}