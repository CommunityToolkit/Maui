using CommunityToolkit.Maui.Core.Primitives;

namespace CommunityToolkit.Maui.Storage;

/// <inheritdoc cref="IFolderPicker"/> 
public static class FolderPicker
{
	static Lazy<IFolderPicker> defaultImplementation = new(new FolderPickerImplementation());

	/// <summary>
	/// Default implementation to use
	/// </summary>
	public static IFolderPicker Default => defaultImplementation.Value;

	/// <summary>
	/// Allows picking a folder from the file system.
	/// </summary>
	/// <param name="initialPath">Initial path</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns><see cref="Folder"/></returns>
	public static ValueTask<Folder> PickAsync(string initialPath, CancellationToken cancellationToken) =>
		Default.PickAsync(initialPath, cancellationToken);

	/// <summary>
	/// Allows picking a folder from the file system.
	/// </summary>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns><see cref="Folder"/></returns>
	public static ValueTask<Folder> PickAsync(CancellationToken cancellationToken) =>
		Default.PickAsync(cancellationToken);

	internal static void SetDefault(IFolderPicker implementation) =>
		defaultImplementation = new Lazy<IFolderPicker>(implementation);
}
