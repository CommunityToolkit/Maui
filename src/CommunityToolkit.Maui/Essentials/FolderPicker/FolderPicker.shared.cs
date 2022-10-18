using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Primitives;

namespace CommunityToolkit.Maui.Essentials;

/// <summary>
/// 
/// </summary>
public static class FolderPicker
{
	/// <summary>
	/// 
	/// </summary>
	/// <param name="initialPath"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	public static Task<Folder?> PickAsync(string initialPath, CancellationToken cancellationToken) =>
		Default.PickAsync(initialPath, cancellationToken);

	/// <summary>
	/// 
	/// </summary>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	public static Task<Folder?> PickAsync(CancellationToken cancellationToken) =>
		Default.PickAsync(cancellationToken);

	static IFolderPicker? defaultImplementation;

	/// <summary>
	/// 
	/// </summary>
	public static IFolderPicker Default =>
		defaultImplementation ??= new FolderPickerImplementation();

	internal static void SetDefault(IFolderPicker? implementation) =>
		defaultImplementation = implementation;
}