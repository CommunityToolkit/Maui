using CommunityToolkit.Maui.Core.Primitives;

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
	public static Task<Folder> PickAsync(string initialPath, CancellationToken cancellationToken) =>
		Default.PickAsync(initialPath, cancellationToken);

	/// <inheritdoc cref="IFolderPicker.PickAsync(CancellationToken)"/> 
	public static Task<Folder> PickAsync(CancellationToken cancellationToken) =>
		Default.PickAsync(cancellationToken);

	/// <inheritdoc cref="IFolderPicker.PickSafeAsync(string, CancellationToken)"/> 
	public static Task<FolderPickerResult> PickSafeAsync(string initialPath, CancellationToken cancellationToken) =>
		Default.PickSafeAsync(initialPath, cancellationToken);

	/// <inheritdoc cref="IFolderPicker.PickSafeAsync(CancellationToken)"/> 
	public static Task<FolderPickerResult> PickSafeAsync(CancellationToken cancellationToken) =>
		Default.PickSafeAsync(cancellationToken);

	internal static void SetDefault(IFolderPicker implementation) =>
		defaultImplementation = new(implementation);
}