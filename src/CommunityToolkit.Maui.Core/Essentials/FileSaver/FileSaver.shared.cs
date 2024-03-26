using System.Runtime.Versioning;

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
	[SupportedOSPlatform("Android26.0")]
	[SupportedOSPlatform("iOS14.0")]
	[SupportedOSPlatform("MacCatalyst14.0")]
	[SupportedOSPlatform("Tizen")]
	[SupportedOSPlatform("Windows")]
	public static Task<FileSaverResult> SaveAsync(string initialPath, string fileName, Stream stream, CancellationToken cancellationToken = default) =>
		Default.SaveAsync(initialPath, fileName, stream, cancellationToken);

	/// <inheritdoc cref="IFileSaver.SaveAsync(string, Stream, CancellationToken)"/>
	[SupportedOSPlatform("Android")]
	[SupportedOSPlatform("iOS14.0")]
	[SupportedOSPlatform("MacCatalyst14.0")]
	[SupportedOSPlatform("Tizen")]
	[SupportedOSPlatform("Windows")]
	public static Task<FileSaverResult> SaveAsync(string fileName, Stream stream, CancellationToken cancellationToken = default) =>
		Default.SaveAsync(fileName, stream, cancellationToken);

	/// <inheritdoc cref="IFileSaver.SaveAsync(string, string, Stream, IProgress{double}, CancellationToken)"/>
	[SupportedOSPlatform("Android26.0")]
	[SupportedOSPlatform("iOS14.0")]
	[SupportedOSPlatform("MacCatalyst14.0")]
	[SupportedOSPlatform("Tizen")]
	[SupportedOSPlatform("Windows")]
	public static Task<FileSaverResult> SaveAsync(string initialPath, string fileName, Stream stream, IProgress<double> progress, CancellationToken cancellationToken = default) =>
		Default.SaveAsync(initialPath, fileName, stream, progress, cancellationToken);

	/// <inheritdoc cref="IFileSaver.SaveAsync(string, Stream, IProgress{double}, CancellationToken)"/>
	[SupportedOSPlatform("Android")]
	[SupportedOSPlatform("iOS14.0")]
	[SupportedOSPlatform("MacCatalyst14.0")]
	[SupportedOSPlatform("Tizen")]
	[SupportedOSPlatform("Windows")]
	public static Task<FileSaverResult> SaveAsync(string fileName, Stream stream, IProgress<double> progress, CancellationToken cancellationToken = default) =>
		Default.SaveAsync(fileName, stream, progress, cancellationToken);

	internal static void SetDefault(IFileSaver implementation) =>
		defaultImplementation = new(implementation);
}