using CommunityToolkit.Maui.Core.Primitives;

namespace CommunityToolkit.Maui.Storage;

/// <inheritdoc />
public class FolderPickerImplementation : IFolderPicker
{
	/// <inheritdoc />
	public ValueTask<Folder> PickAsync(string initialPath, CancellationToken cancellationToken) => throw new NotImplementedException();

	/// <inheritdoc />
	public ValueTask<Folder> PickAsync(CancellationToken cancellationToken) => throw new NotImplementedException();
}