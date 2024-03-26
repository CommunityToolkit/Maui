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
	/// <param name="initialPath">Initial path</param>
	/// <param name="fileName">File name with extension</param>
	/// <param name="stream"><see cref="Stream"/></param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	[SupportedOSPlatform("Android26.0")]
	[SupportedOSPlatform("iOS14.0")]
	[SupportedOSPlatform("MacCatalyst14.0")]
	[SupportedOSPlatform("Tizen")]
	[SupportedOSPlatform("Windows")]
	Task<FileSaverResult> SaveAsync(string initialPath, string fileName, Stream stream, CancellationToken cancellationToken = default);

	/// <summary>
	/// Saves a file to the default folder on the file system
	/// </summary>
	/// <param name="fileName">File name with extension</param>
	/// <param name="stream"><see cref="Stream"/></param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	[SupportedOSPlatform("Android")]
	[SupportedOSPlatform("iOS14.0")]
	[SupportedOSPlatform("MacCatalyst14.0")]
	[SupportedOSPlatform("Tizen")]
	[SupportedOSPlatform("Windows")]
	Task<FileSaverResult> SaveAsync(string fileName, Stream stream, CancellationToken cancellationToken = default);

	/// <summary>
	/// Saves a file to a target folder on the file system
	/// </summary>
	/// <param name="initialPath">Initial path</param>
	/// <param name="fileName">File name with extension</param>
	/// <param name="stream"><see cref="Stream"/></param>
	/// <param name="progress">Saving file progress in percentage</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	[SupportedOSPlatform("Android26.0")]
	[SupportedOSPlatform("iOS14.0")]
	[SupportedOSPlatform("MacCatalyst14.0")]
	[SupportedOSPlatform("Tizen")]
	[SupportedOSPlatform("Windows")]
	Task<FileSaverResult> SaveAsync(string initialPath, string fileName, Stream stream, IProgress<double> progress, CancellationToken cancellationToken = default);

	/// <summary>
	/// Saves a file to the default folder on the file system
	/// </summary>
	/// <param name="fileName">File name with extension</param>
	/// <param name="stream"><see cref="Stream"/></param>
	/// <param name="progress">Saving file progress in percentage</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	[SupportedOSPlatform("Android")]
	[SupportedOSPlatform("iOS14.0")]
	[SupportedOSPlatform("MacCatalyst14.0")]
	[SupportedOSPlatform("Tizen")]
	[SupportedOSPlatform("Windows")]
	Task<FileSaverResult> SaveAsync(string fileName, Stream stream, IProgress<double> progress, CancellationToken cancellationToken = default);
}