using CommunityToolkit.Maui.Core;

namespace CommunityToolkit.Maui.Essentials;

/// <summary>
/// 
/// </summary>
public static class SaveFileDialog
{
	/// <summary>
	/// 
	/// </summary>
	/// <param name="initialPath"></param>
	/// <param name="stream"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	public static Task<bool> SaveAsync(string initialPath, Stream stream, CancellationToken cancellationToken) =>
		Default.SaveAsync(initialPath, stream, cancellationToken);

	/// <summary>
	/// 
	/// </summary>
	/// <param name="stream"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	public static Task<bool> SaveAsync(Stream stream, CancellationToken cancellationToken) =>
		Default.SaveAsync(stream, cancellationToken);

	static ISaveFileDialog? defaultImplementation;

	/// <summary>
	/// 
	/// </summary>
	public static ISaveFileDialog Default =>
		defaultImplementation ??= new SaveFileDialogImplementation();

	internal static void SetDefault(ISaveFileDialog? implementation) =>
		defaultImplementation = implementation;
}