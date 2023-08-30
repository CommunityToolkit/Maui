using System.Runtime.Versioning;

namespace CommunityToolkit.Maui.Storage;

/// <inheritdoc cref="IFolderPicker"/> 
public static class FolderPicker
{
	static Lazy<IFolderPicker> defaultImplementation = new(new FolderPickerImplementation());

	/// <summary>
	/// Default implementation of <see cref="IFolderPicker"/>
	/// </summary>
	public static IFolderPicker Default => defaultImplementation.Value;

	/// <inheritdoc cref="IFolderPicker.PickAsync(string, CancellationToken)"/> 
	[SupportedOSPlatform("Android26.0")]
	[SupportedOSPlatform("iOS")]
	[SupportedOSPlatform("macOS")]
	[SupportedOSPlatform("Tizen")]
	[SupportedOSPlatform("Windows")]
	public static Task<FolderPickerResult> PickAsync(string initialPath, CancellationToken cancellationToken) =>
		Default.PickAsync(initialPath, cancellationToken);

	/// <inheritdoc cref="IFolderPicker.PickAsync(CancellationToken)"/> 
	public static Task<FolderPickerResult> PickAsync(CancellationToken cancellationToken) =>
		Default.PickAsync(cancellationToken);

	internal static void SetDefault(IFolderPicker implementation) =>
		defaultImplementation = new(implementation);
}