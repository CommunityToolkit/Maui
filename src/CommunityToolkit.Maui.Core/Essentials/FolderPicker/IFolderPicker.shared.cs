using System.Runtime.Versioning;

namespace CommunityToolkit.Maui.Storage;

/// <summary>
/// Allows picking folders from the file system
/// </summary>
public interface IFolderPicker
{
	/// <summary>
	/// Allows the user to pick a folder from the file system
	/// </summary>
	/// <param name="initialPath">Initial path</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns><see cref="FolderPickerResult"/></returns>
	[SupportedOSPlatform("Android26.0")]
	[SupportedOSPlatform("iOS14.0")]
	[SupportedOSPlatform("MacCatalyst14.0")]
	[SupportedOSPlatform("Tizen")]
	[SupportedOSPlatform("Windows")]
	Task<FolderPickerResult> PickAsync(string initialPath, CancellationToken cancellationToken = default);

	/// <summary>
	/// Allows the user to pick a folder from the file system
	/// </summary>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns><see cref="FolderPickerResult"/></returns>
	[SupportedOSPlatform("Android")]
	[SupportedOSPlatform("iOS14.0")]
	[SupportedOSPlatform("MacCatalyst14.0")]
	[SupportedOSPlatform("Tizen")]
	[SupportedOSPlatform("Windows")]
	Task<FolderPickerResult> PickAsync(CancellationToken cancellationToken = default);
}