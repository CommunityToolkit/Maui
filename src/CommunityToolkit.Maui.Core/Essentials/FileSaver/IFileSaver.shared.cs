using System.Runtime.Versioning;

namespace CommunityToolkit.Maui.Storage;

/// <summary>
/// Allows the user to save files to the filesystem
/// </summary>
public interface IFileSaver
{
	/// <summary>
	/// Saves a file to a target folder on the file system
	/// </summary>
	/// <param name="initialPath">Initial path. Must not be null.</param>
	/// <param name="fileName">File name with extension. Must not be null.</param>
	/// <param name="stream"><see cref="Stream"/>. Must not be null.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns><see cref="FileSaverResult"/>. A null argument that must not be null is reported in <see cref="FileSaverResult.Exception"/> as an <see cref="ArgumentNullException"/> (it is not thrown), and no file is saved</returns>
	[SupportedOSPlatform("Android26.0")]
	[SupportedOSPlatform("iOS")]
	[SupportedOSPlatform("MacCatalyst")]
	[SupportedOSPlatform("Windows")]
	Task<FileSaverResult> SaveAsync(string initialPath, string fileName, Stream stream, CancellationToken cancellationToken = default);

	/// <summary>
	/// Saves a file to the default folder on the file system
	/// </summary>
	/// <param name="fileName">File name with extension. Must not be null.</param>
	/// <param name="stream"><see cref="Stream"/>. Must not be null.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns><see cref="FileSaverResult"/>. A null argument that must not be null is reported in <see cref="FileSaverResult.Exception"/> as an <see cref="ArgumentNullException"/> (it is not thrown), and no file is saved</returns>
	[SupportedOSPlatform("Android")]
	[SupportedOSPlatform("iOS")]
	[SupportedOSPlatform("MacCatalyst")]
	[SupportedOSPlatform("Windows")]
	Task<FileSaverResult> SaveAsync(string fileName, Stream stream, CancellationToken cancellationToken = default);

	/// <summary>
	/// Saves a file to a target folder on the file system
	/// </summary>
	/// <param name="initialPath">Initial path. Must not be null.</param>
	/// <param name="fileName">File name with extension. Must not be null.</param>
	/// <param name="stream"><see cref="Stream"/>. Must not be null.</param>
	/// <param name="progress">Saving file progress in percentage</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns><see cref="FileSaverResult"/>. A null argument that must not be null is reported in <see cref="FileSaverResult.Exception"/> as an <see cref="ArgumentNullException"/> (it is not thrown), and no file is saved</returns>
	[SupportedOSPlatform("Android26.0")]
	[SupportedOSPlatform("iOS")]
	[SupportedOSPlatform("MacCatalyst")]
	[SupportedOSPlatform("Windows")]
	Task<FileSaverResult> SaveAsync(string initialPath, string fileName, Stream stream, IProgress<double> progress, CancellationToken cancellationToken = default);

	/// <summary>
	/// Saves a file to the default folder on the file system
	/// </summary>
	/// <param name="fileName">File name with extension. Must not be null.</param>
	/// <param name="stream"><see cref="Stream"/>. Must not be null.</param>
	/// <param name="progress">Saving file progress in percentage</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns><see cref="FileSaverResult"/>. A null argument that must not be null is reported in <see cref="FileSaverResult.Exception"/> as an <see cref="ArgumentNullException"/> (it is not thrown), and no file is saved</returns>
	[SupportedOSPlatform("Android")]
	[SupportedOSPlatform("iOS")]
	[SupportedOSPlatform("MacCatalyst")]
	[SupportedOSPlatform("Windows")]
	Task<FileSaverResult> SaveAsync(string fileName, Stream stream, IProgress<double> progress, CancellationToken cancellationToken = default);
}