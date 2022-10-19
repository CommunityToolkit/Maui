using CommunityToolkit.Maui.Core;

namespace CommunityToolkit.Maui.Essentials;

/// <inheritdoc cref=""/>
public static class SaveFileDialog
{
	/// <summary>
	/// Allows selecting target folder and saving files to the file system.
	/// </summary>
	/// <param name="initialPath">Initial path</param>
	/// <param name="fileName">File name with extension</param>
	/// <param name="stream"><see cref="Stream"/></param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns>True if file is saved, otherwise False</returns>
	public static Task<bool> SaveAsync(string initialPath, string fileName, Stream stream, CancellationToken cancellationToken) =>
		Default.SaveAsync(initialPath, fileName, stream, cancellationToken);

	/// <summary>
	/// Allows selecting target folder and saving files to the file system.
	/// </summary>
	/// <param name="fileName">File name with extension</param>
	/// <param name="stream"><see cref="Stream"/></param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns>True if file is saved, otherwise False</returns>
	public static Task<bool> SaveAsync(string fileName, Stream stream, CancellationToken cancellationToken) =>
		Default.SaveAsync(fileName, stream, cancellationToken);

	static ISaveFileDialog? defaultImplementation;

	/// <summary>
	/// Default implementation to use.
	/// </summary>
	public static ISaveFileDialog Default =>
		defaultImplementation ??= new SaveFileDialogImplementation();

	internal static void SetDefault(ISaveFileDialog? implementation) =>
		defaultImplementation = implementation;
}