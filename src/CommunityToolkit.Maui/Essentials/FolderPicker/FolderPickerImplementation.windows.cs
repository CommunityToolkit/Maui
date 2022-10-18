using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Primitives;

namespace CommunityToolkit.Maui.Essentials;

/// <summary>
/// 
/// </summary>
public class FolderPickerImplementation : IFolderPicker
{
	public Task<Folder?> PickAsync(string initialPath, CancellationToken cancellationToken)
	{
		throw new NotImplementedException();
	}

	public Task<Folder?> PickAsync(CancellationToken cancellationToken)
	{
		throw new NotImplementedException();
	}
}