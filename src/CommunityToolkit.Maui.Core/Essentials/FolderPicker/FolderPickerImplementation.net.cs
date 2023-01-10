using CommunityToolkit.Maui.Core.Primitives;

namespace CommunityToolkit.Maui.Storage;

/// <inheritdoc />
class FolderPickerImplementation : IFolderPicker
{
	/// <inheritdoc />
	public Task<Folder> PickAsync(string initialPath, CancellationToken cancellationToken) => throw new NotImplementedException();

	/// <inheritdoc />
	public Task<Folder> PickAsync(CancellationToken cancellationToken) => throw new NotImplementedException();
}