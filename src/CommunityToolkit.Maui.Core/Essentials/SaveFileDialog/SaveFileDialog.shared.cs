namespace CommunityToolkit.Maui.Storage;

/// <inheritdoc cref="ISaveFileDialog"/>
public static class SaveFileDialog
{
	static Lazy<ISaveFileDialog> defaultImplementation = new(new SaveFileDialogImplementation());

	/// <summary>
	/// Default implementation to use.
	/// </summary>
	public static ISaveFileDialog Default => defaultImplementation.Value;

	/// <summary>
	/// Allows selecting target folder and saving files to the file system.
	/// </summary>
	/// <param name="initialPath">Initial path</param>
	/// <param name="fileName">File name with extension</param>
	/// <param name="stream"><see cref="Stream"/></param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	public static ValueTask<string> SaveAsync(string initialPath, string fileName, Stream stream, CancellationToken cancellationToken) =>
		Default.SaveAsync(initialPath, fileName, stream, cancellationToken);

	/// <summary>
	/// Allows selecting target folder and saving files to the file system.
	/// </summary>
	/// <param name="fileName">File name with extension</param>
	/// <param name="stream"><see cref="Stream"/></param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	public static ValueTask<string> SaveAsync(string fileName, Stream stream, CancellationToken cancellationToken) =>
		Default.SaveAsync(fileName, stream, cancellationToken);

	internal static void SetDefault(ISaveFileDialog implementation) =>
		defaultImplementation = new(implementation);
}
