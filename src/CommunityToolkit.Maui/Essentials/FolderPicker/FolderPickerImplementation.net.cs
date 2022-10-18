using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Primitives;

namespace CommunityToolkit.Maui.Essentials;

/// <inheritdoc />
public class FolderPickerImplementation : IFolderPicker
{

	/// <inheritdoc />
	public Task<Folder?> PickAsync(string initialPath, CancellationToken cancellationToken)
	{
		throw new NotImplementedException();
	}

	/// <inheritdoc />
	public Task<Folder?> PickAsync(CancellationToken cancellationToken)
	{
		throw new NotImplementedException();
	}
}