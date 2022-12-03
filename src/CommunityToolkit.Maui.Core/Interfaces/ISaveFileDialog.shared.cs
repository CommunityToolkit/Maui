namespace CommunityToolkit.Maui.Core;

/// <summary>
/// Allows selecting target folder and saving files to the file system.
/// </summary>
public interface ISaveFileDialog
{
	/// <summary>
	/// Allows selecting target folder and saving files to the file system.
	/// </summary>
	/// <param name="initialPath">Initial path</param>
	/// <param name="fileName">File name with extension</param>
	/// <param name="stream"><see cref="Stream"/></param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	Task<string> SaveAsync(string initialPath, string fileName, Stream stream, CancellationToken cancellationToken);

	/// <summary>
	/// Allows selecting target folder and saving files to the file system.
	/// </summary>
	/// <param name="fileName">File name with extension</param>
	/// <param name="stream"><see cref="Stream"/></param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	Task<string> SaveAsync(string fileName, Stream stream, CancellationToken cancellationToken);
}