namespace CommunityToolkit.Maui.Storage;

/// <inheritdoc cref="IFileSaver"/>
public static class FileSaver
{
	static Lazy<IFileSaver> defaultImplementation = new(new FileSaverImplementation());

	/// <summary>
	/// Default implementation of <see cref="IFileSaver"/>
	/// </summary>
	public static IFileSaver Default => defaultImplementation.Value;

	/// <inheritdoc cref="IFileSaver.SaveAsync(string, string, Stream, CancellationToken)"/>
	public static Task<FileSaverResult> SaveAsync(string initialPath, string fileName, Stream stream, CancellationToken cancellationToken) =>
		Default.SaveAsync(initialPath, fileName, stream, cancellationToken);

	/// <inheritdoc cref="IFileSaver.SaveAsync(string, Stream, CancellationToken)"/>
	public static Task<FileSaverResult> SaveAsync(string fileName, Stream stream, CancellationToken cancellationToken) =>
		Default.SaveAsync(fileName, stream, cancellationToken);

	internal static void SetDefault(IFileSaver implementation) =>
		defaultImplementation = new(implementation);
}