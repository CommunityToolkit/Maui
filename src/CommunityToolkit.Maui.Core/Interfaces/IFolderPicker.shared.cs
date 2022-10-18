using CommunityToolkit.Maui.Core.Primitives;

namespace CommunityToolkit.Maui.Core;

/// <summary>
/// 
/// </summary>
public interface IFolderPicker
{
	/// <summary>
	/// 
	/// </summary>
	/// <param name="initialPath"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	Task<Folder?> PickAsync(string initialPath, CancellationToken cancellationToken);

	/// <summary>
	/// 
	/// </summary>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	Task<Folder?> PickAsync(CancellationToken cancellationToken);
}